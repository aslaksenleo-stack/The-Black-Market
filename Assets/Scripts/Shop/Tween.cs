using System;
using System.Collections;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>Small coroutine helpers for smooth, eased movement (items sliding, doors swinging).</summary>
    public static class Tween
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        public static float EaseInOut(float t) => t * t * (3f - 2f * Mathf.Clamp01(t));

        /// <summary>Back-out ease: overshoots a touch, then settles. Feels "placed".</summary>
        public static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.4f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>Moves a transform along a curved arc (lifted in the middle) to a world pose.</summary>
        public static IEnumerator MoveArc(Transform target, Vector3 toPosition, Quaternion toRotation, float seconds,
            float arcHeight = 0.12f, Func<float, float> ease = null, Vector3? toScale = null)
        {
            if (target == null) yield break;
            ease = ease ?? EaseOutCubic;
            Vector3 fromPosition = target.position;
            Quaternion fromRotation = target.rotation;
            Vector3 fromScale = target.localScale;
            Vector3 endScale = toScale ?? fromScale;

            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                if (target == null) yield break;
                float e = ease(t);
                Vector3 p = Vector3.LerpUnclamped(fromPosition, toPosition, e);
                p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * arcHeight;
                target.SetPositionAndRotation(p, Quaternion.SlerpUnclamped(fromRotation, toRotation, e));
                target.localScale = Vector3.LerpUnclamped(fromScale, endScale, e);
                yield return null;
            }

            if (target == null) yield break;
            target.SetPositionAndRotation(toPosition, toRotation);
            target.localScale = endScale;
        }

        public static IEnumerator RotateLocal(Transform target, Quaternion to, float seconds)
        {
            Quaternion from = target.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                target.localRotation = Quaternion.Slerp(from, to, EaseInOut(t));
                yield return null;
            }

            target.localRotation = to;
        }
    }
}
