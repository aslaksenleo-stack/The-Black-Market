using UnityEngine;

namespace BlackMarket.UI
{
    /// <summary>
    /// Shrinks this rect to the phone's "safe area" so buttons never sit under a camera notch,
    /// punch-hole or rounded corner. Re-applies when the phone rotates.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect appliedArea;
        private Vector2Int appliedScreenSize;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != appliedArea ||
                appliedScreenSize.x != Screen.width ||
                appliedScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            appliedArea = Screen.safeArea;
            appliedScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 min = appliedArea.position;
            Vector2 max = appliedArea.position + appliedArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
