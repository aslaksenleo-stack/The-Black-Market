using System;
using BlackMarket.Interaction;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// The shop counter. Using it opens the counter panel, where customer trades will happen.
    /// It only announces that it was used; ShopSceneController decides what happens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CounterInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Use counter";

        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled;

        public event Action Used;

        public void Interact()
        {
            Used?.Invoke();
        }
    }
}
