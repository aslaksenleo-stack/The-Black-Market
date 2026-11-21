using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    public enum ButtonStyle
    {
        /// <summary>Do the deal: green.</summary>
        Primary,
        /// <summary>Normal action: dark.</summary>
        Secondary,
        /// <summary>Decline / leave: dark with a red edge.</summary>
        Danger,
        /// <summary>Quiet action: transparent with an edge.</summary>
        Ghost
    }

    /// <summary>
    /// Reusable UI building blocks (canvas, panels, text, icons, buttons, sliders) built in code with
    /// UITheme styling. Building UI in code keeps every screen consistent and lets the cloud build
    /// create it without anyone laying it out by hand.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        public static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        /// <summary>Raised whenever any factory-made button is pressed (used for the click sound).</summary>
        public static event Action Clicked;

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

        /// <summary>Fills the parent's width at a fixed height, measured from the parent's top.</summary>
        public static void Row(RectTransform rect, float top, float height, float inset = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(inset, -top - height);
            rect.offsetMax = new Vector2(-inset, -top);
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            }

            return image;
        }

        /// <summary>A rounded rectangle image.</summary>
        public static Image CreateRounded(string name, Transform parent, Color color, float cornerScale = 1f)
        {
            Image image = CreateImage(name, parent, color, UISprites.Rounded);
            image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, cornerScale);
            return image;
        }

        /// <summary>
        /// A rounded, softly shadowed panel with a hairline edge. Add content as children of the returned rect.
        /// The panel blocks touches so they don't reach the 3D world behind it.
        /// </summary>
        public static RectTransform CreatePanel(string name, Transform parent, Color? fill = null, bool shadow = true, bool edge = true)
        {
            RectTransform root = CreateRect(name, parent);

            if (shadow)
            {
                Image s = CreateImage("Shadow", root, UITheme.Shadow, UISprites.Shadow);
                s.raycastTarget = false;
                Stretch(s.rectTransform, -26f);
                s.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            }

            Image body = CreateRounded("Fill", root, fill ?? UITheme.Panel);
            Stretch(body.rectTransform);

            if (edge)
            {
                Image e = CreateImage("Edge", root, UITheme.PanelEdge, UISprites.RoundedEdge);
                e.raycastTarget = false;
                Stretch(e.rectTransform);
            }

            return root;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment, Font font = null)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : UITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.lineSpacing = 1.05f;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateIcon(string name, Transform parent, Icon icon, Color color)
        {
            Image image = CreateImage(name, parent, color, UISprites.Get(icon));
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        /// <summary>A rounded button with a label and optional icon. Shrinks slightly while pressed.</summary>
        public static Button CreateButton(string name, Transform parent, string label, Action onClick,
            ButtonStyle style = ButtonStyle.Secondary, Icon? icon = null, int fontSize = UITheme.FontSizeBody)
        {
            RectTransform root = CreateRect(name, parent);
            root.gameObject.AddComponent<CanvasGroup>();

            Color fill, edge, text;
            switch (style)
            {
                case ButtonStyle.Primary:
                    fill = UITheme.Positive; edge = UITheme.PositiveEdge; text = Color.white;
                    break;
                case ButtonStyle.Danger:
                    fill = UITheme.Surface; edge = UITheme.NegativeEdge; text = UITheme.NegativeText;
                    break;
                case ButtonStyle.Ghost:
                    fill = new Color(0f, 0f, 0f, 0.25f); edge = UITheme.PanelEdge; text = UITheme.Text;
                    break;
                default:
                    fill = UITheme.Surface; edge = new Color32(255, 226, 180, 48); text = UITheme.Text;
                    break;
            }

            Image body = CreateRounded("Fill", root, Color.white, 0.8f);
            Stretch(body.rectTransform);
            Image outline = CreateImage("Edge", root, edge, UISprites.RoundedEdge);
            outline.pixelsPerUnitMultiplier = 1.25f;
            outline.raycastTarget = false;
            Stretch(outline.rectTransform);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = fill;
            colors.selectedColor = fill;
            colors.pressedColor = Color.Lerp(fill, Color.white, 0.18f);
            colors.disabledColor = fill;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            Text labelText = CreateText("Label", root, label, fontSize, text, icon.HasValue ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform, 10f);
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;

            if (icon.HasValue)
            {
                Image iconImage = CreateIcon("Icon", root, icon.Value, text);
                Place(iconImage.rectTransform, MiddleLeft, new Vector2(28f, 0f), new Vector2(48f, 48f));
                iconImage.rectTransform.pivot = new Vector2(0f, 0.5f);
                labelText.rectTransform.offsetMin = new Vector2(92f, 10f);
            }

            root.gameObject.AddComponent<PressScale>();
            button.onClick.AddListener(() => Clicked?.Invoke());
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>A tall tile button with the icon above the label (bottom navigation bar).</summary>
        public static Button CreateTileButton(string name, Transform parent, string label, Icon icon, Action onClick)
        {
            Button button = CreateButton(name, parent, label, onClick);
            Text text = button.GetComponentInChildren<Text>();
            text.fontSize = UITheme.FontSizeSmall;
            text.alignment = TextAnchor.LowerCenter;
            text.rectTransform.offsetMin = new Vector2(8f, 14f);
            Image iconImage = CreateIcon("Icon", button.transform, icon, UITheme.Gold);
            Place(iconImage.rectTransform, TopCentre, new Vector2(0f, -16f), new Vector2(54f, 54f));
            return button;
        }

        public static void SetLabel(Button button, string label)
        {
            Text text = button.transform.Find("Label")?.GetComponent<Text>();
            if (text != null) text.text = label;
        }

        /// <summary>Enables/disables a button and fades it so the state is obvious.</summary>
        public static void SetInteractable(Button button, bool interactable)
        {
            button.interactable = interactable;
            var group = button.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = interactable ? 1f : 0.4f;
        }

        /// <summary>A horizontal slider (track, gold fill, round handle).</summary>
        public static Slider CreateSlider(string name, Transform parent, Action<float> onChanged)
        {
            RectTransform root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            Image track = CreateImage("Track", root, UITheme.Inset, UISprites.Pill);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, 16f);

            RectTransform fillArea = CreateRect("FillArea", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-24f, 16f);
            Image fill = CreateImage("Fill", fillArea, UITheme.Gold, UISprites.Pill);
            fill.rectTransform.sizeDelta = new Vector2(24f, 0f);

            RectTransform handleArea = CreateRect("HandleArea", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(24f, 0f);
            handleArea.offsetMax = new Vector2(-24f, 0f);
            Image handle = CreateImage("Handle", handleArea, UITheme.Text, UISprites.Circle);
            handle.rectTransform.sizeDelta = new Vector2(52f, 52f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        /// <summary>A full-screen dark backdrop that blocks touches (behind modal panels).</summary>
        public static Image CreateDim(string name, Transform parent)
        {
            Image dim = CreateImage(name, parent, UITheme.Dim);
            Stretch(dim.rectTransform);
            return dim;
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

        /// <summary>Formats coins with the coin colour for rich text.</summary>
        public static string Gold(string text) => $"<color=#{UITheme.Hex(UITheme.Gold)}>{text}</color>";

        public static string Colored(string text, Color color) => $"<color=#{UITheme.Hex(color)}>{text}</color>";
    }
}
