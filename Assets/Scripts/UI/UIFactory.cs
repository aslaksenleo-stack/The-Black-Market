using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    /// <summary>
    /// Reusable UI building blocks (canvas, panel, text, button) built in code with UITheme styling.
    /// Building UI in code keeps every screen consistent and lets the cloud build create it
    /// without anyone laying it out by hand in the Unity Editor.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // Scale everything relative to a 1920x1080 landscape layout, matching on height,
            // so buttons are the same physical proportion on every phone.
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return (RectTransform)rectObject.transform;
        }

        /// <summary>Positions a rect relative to one anchor point (e.g. TopLeft) with a fixed size.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Makes a rect fill its parent, with an optional inset on every side.</summary>
        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Centre;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>A dark panel with a thin brass border. Add content as children of the returned rect.</summary>
        public static RectTransform CreatePanel(string name, Transform parent)
        {
            Image border = CreateImage(name, parent, UITheme.Border);
            Image fill = CreateImage("Fill", border.transform, UITheme.Background);
            fill.raycastTarget = false;
            Stretch(fill.rectTransform, UITheme.BorderWidth);
            return border.rectTransform;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A bordered button with a centred label. Darkens while pressed.</summary>
        public static Button CreateButton(string name, Transform parent, string label, Action onClick)
        {
            Image border = CreateImage(name, parent, UITheme.Border);

            // White fill tinted by the button's colour states below.
            Image fill = CreateImage("Fill", border.transform, Color.white);
            Stretch(fill.rectTransform, UITheme.BorderWidth);

            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor = UITheme.Surface;
            colors.highlightedColor = UITheme.Surface;
            colors.selectedColor = UITheme.Surface;
            colors.pressedColor = UITheme.SurfacePressed;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            Text text = CreateText("Label", border.transform, label, UITheme.FontSizeBody, UITheme.Text, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 12f);

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            return button;
        }

        /// <summary>Buttons only work if the scene has exactly one EventSystem. Creates it if missing.</summary>
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
