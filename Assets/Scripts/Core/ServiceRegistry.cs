using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackMarket.Core
{
    /// <summary>
    /// A small, explicit registry for long-lived game systems (scene loading, game state,
    /// and later inventory, economy, saving...).
    ///
    /// Why this instead of making every manager a singleton?
    /// - There is ONE place where systems are created (GameManager), so start-up order is obvious.
    /// - Systems are plain C# classes, so they can be unit-tested without Unity scenes.
    /// - Tests can register fake versions of a system.
    /// </summary>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>Raised after a service is registered. Useful for debugging start-up order.</summary>
        public static event Action<Type> ServiceRegistered;

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type type = typeof(T);
            if (Services.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    $"A service of type {type.Name} is already registered. Each system must be created only once.");
            }

            Services[type] = service;
            ServiceRegistered?.Invoke(type);
        }

        public static bool Unregister<T>() where T : class
        {
            return Services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"Service {typeof(T).Name} has not been registered. Systems are created by the GameManager when the game starts.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class
        {
            return Services.ContainsKey(typeof(T));
        }

        public static void Clear()
        {
            Services.Clear();
        }

        // If "Enter Play Mode Options" disables domain reload, static data survives between
        // Play sessions. This resets the registry every time Play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Services.Clear();
            ServiceRegistered = null;
        }
    }
}
