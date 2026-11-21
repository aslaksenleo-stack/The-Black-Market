using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlackMarket.UI
{
    /// <summary>Panel appears with a short fade and a gentle scale-up every time it is enabled.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopIn : MonoBehaviour
    {
        [SerializeField] private float seconds = 0.18f;
        [SerializeField] private float fromScale = 0.94f;

        private CanvasGroup group;
        private float t;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            t = 0f;
            Apply(0f);
        }

        private void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / seconds);
            Apply(1f - Mathf.Pow(1f - t, 3f));
        }

        private void Apply(float e)
        {
            if (group != null) group.alpha = e;
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, e);
        }
    }

    /// <summary>Buttons shrink slightly while pressed: a small, satisfying touch response.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Pressed = 0.95f;
        private float target = 1f;

        public void OnPointerDown(PointerEventData eventData) => target = Pressed;
        public void OnPointerUp(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        private void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 2.5f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Briefly scales a UI element up and back (e.g. the coin counter when money changes).</summary>
    public sealed class Pulse : MonoBehaviour
    {
        private float t = 1f;
        private float strength;

        public void Play(float amount = 0.12f)
        {
            t = 0f;
            strength = amount;
        }

        private void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.35f);
            float s = 1f + Mathf.Sin(t * Mathf.PI) * strength;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Text that floats upward and fades out (e.g. "+600").</summary>
    public sealed class FloatAway : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 start;
        private float t;

        public static void Attach(GameObject target)
        {
            target.AddComponent<FloatAway>();
        }

        private void Awake()
        {
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            rect = (RectTransform)transform;
            start = rect.anchoredPosition;
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime / 1.4f;
            rect.anchoredPosition = start + new Vector2(0f, 80f * (1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f)));
            group.alpha = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>Calls back every frame while the object is active (for tiny bespoke animations).</summary>
    public sealed class UIUpdate : MonoBehaviour
    {
        public Action<float> Tick;

        private void Update() => Tick?.Invoke(Time.unscaledDeltaTime);
    }
}
