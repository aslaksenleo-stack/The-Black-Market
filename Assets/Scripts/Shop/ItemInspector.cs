using System;
using System.Collections;
using BlackMarket.Art;
using BlackMarket.Audio;
using BlackMarket.Player;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Close-up inspection: lifts the item from the counter to just in front of the camera, where the
    /// player turns it with a finger. Turning it around (or studying it for a few seconds) completes
    /// the inspection, which tightens the value estimate and reveals hidden flaws.
    /// Put this on the camera; it reads drags from the player's input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemInspector : MonoBehaviour
    {
        [Tooltip("Degrees of turning that complete an inspection.")]
        [SerializeField] private float degreesToComplete = 320f;

        [Tooltip("Seconds of simply looking that complete an inspection.")]
        [SerializeField] private float secondsToComplete = 4f;

        [SerializeField] private float rotateDegreesPerInch = 160f;

        private IPlayerInput input;
        private CounterItem current;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Quaternion modelHomeRotation;
        private Light inspectLight;
        private bool completed;
        private bool settled;

        public bool IsInspecting => current != null;

        /// <summary>0..1.</summary>
        public float Progress { get; private set; }

        /// <summary>Raised once per item when the inspection completes.</summary>
        public event Action<CounterItem> Completed;

        private void Awake()
        {
            input = GetComponentInParent<IPlayerInput>();

            var lightObject = new GameObject("InspectLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0.25f, 0.3f, 0.1f);
            inspectLight = lightObject.AddComponent<Light>();
            inspectLight.type = LightType.Point;
            inspectLight.color = new Color(1f, 0.88f, 0.72f);
            inspectLight.intensity = 0.9f;
            inspectLight.range = 1.6f;
            inspectLight.renderMode = LightRenderMode.ForcePixel;
            inspectLight.enabled = false;
        }

        /// <param name="alreadyInspected">Skips the progress (the item was inspected before).</param>
        public void Begin(CounterItem item, bool alreadyInspected)
        {
            if (item == null || current != null) return;
            current = item;
            completed = alreadyInspected;
            Progress = alreadyInspected ? 1f : 0f;
            settled = false;
            homePosition = item.transform.position;
            homeRotation = item.transform.rotation;
            modelHomeRotation = item.Model.localRotation;
            inspectLight.enabled = true;
            SoundKit.Play(Sound.Inspect);

            Bounds bounds = ArtLibrary.RenderBounds(item.Model.gameObject);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = Mathf.Clamp(size * 2.6f, 0.32f, 0.9f);
            Vector3 centerOffset = bounds.center - item.transform.position;
            Vector3 target = transform.position + transform.forward * distance - centerOffset - transform.up * 0.02f;
            Quaternion facing = Quaternion.LookRotation(-Flat(transform.forward), Vector3.up);
            StartCoroutine(LiftTo(target, facing));
        }

        public void End()
        {
            if (current == null) return;
            CounterItem item = current;
            current = null;
            inspectLight.enabled = false;
            StopAllCoroutines();
            StartCoroutine(ReturnHome(item, homePosition, homeRotation, modelHomeRotation));
        }

        private IEnumerator LiftTo(Vector3 position, Quaternion rotation)
        {
            yield return Tween.MoveArc(current.transform, position, rotation, 0.45f, 0.05f);
            settled = true;
        }

        private static IEnumerator ReturnHome(CounterItem item, Vector3 position, Quaternion rotation, Quaternion modelRotation)
        {
            Quaternion fromModel = item.Model.localRotation;
            float t = 0f;
            IEnumerator move = Tween.MoveArc(item.transform, position, rotation, 0.4f, 0.04f);
            while (move.MoveNext())
            {
                t += Time.deltaTime / 0.4f;
                if (item != null) item.Model.localRotation = Quaternion.Slerp(fromModel, modelRotation, Tween.EaseOutCubic(t));
                yield return move.Current;
            }

            if (item != null) item.Model.localRotation = modelRotation;
            SoundKit.Play(Sound.ItemDown, 0.6f);
        }

        private void Update()
        {
            if (current == null || !settled) return;

            Vector2 delta = input != null ? input.LookDelta : Vector2.zero;
            float degreesPerPixel = rotateDegreesPerInch / LookMath.EffectiveDpi(Screen.dpi);
            float turned = 0f;

            if (delta != Vector2.zero)
            {
                Quaternion yaw = Quaternion.AngleAxis(-delta.x * degreesPerPixel, transform.up);
                Quaternion pitch = Quaternion.AngleAxis(delta.y * degreesPerPixel, transform.right);
                current.Model.rotation = yaw * pitch * current.Model.rotation;
                turned = delta.magnitude * degreesPerPixel;
            }
            else
            {
                // Slow idle turn so the item never looks frozen.
                current.Model.Rotate(transform.up, 12f * Time.deltaTime, Space.World);
            }

            if (!completed)
            {
                Progress = Mathf.Clamp01(Progress + turned / degreesToComplete + Time.deltaTime / secondsToComplete);
                if (Progress >= 1f)
                {
                    completed = true;
                    Completed?.Invoke(current);
                }
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }
    }
}
