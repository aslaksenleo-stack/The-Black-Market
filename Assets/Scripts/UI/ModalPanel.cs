using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    /// <summary>
    /// Base for full-screen panels (inventory, upgrades, ledger, day summary): a dimmed backdrop that
    /// blocks the 3D world, a centred window with a title and a close button. While one is open the
    /// shop view is paused (see ShopHUD.ModalChanged).
    /// </summary>
    public abstract class ModalPanel : MonoBehaviour
    {
        private GameObject backdrop;
        private Text titleText;
        private Text subtitleText;
        private Button closeButton;

        protected RectTransform Window { get; private set; }

        public bool IsOpen => backdrop != null && backdrop.activeSelf;

        public event Action<ModalPanel, bool> VisibilityChanged;

        public void Build(RectTransform parent, Vector2 size, bool closable = true)
        {
            Image dim = UIFactory.CreateDim(GetType().Name, parent);
            backdrop = dim.gameObject;

            Window = UIFactory.CreatePanel("Window", dim.transform, UITheme.PanelSolid);
            UIFactory.Place(Window, UIFactory.Centre, Vector2.zero, size);
            Window.gameObject.AddComponent<CanvasGroup>();
            Window.gameObject.AddComponent<PopIn>();

            titleText = UIFactory.CreateText("Title", Window, string.Empty, UITheme.FontSizeTitle, UITheme.Gold, TextAnchor.UpperLeft, UITheme.TitleFont);
            UIFactory.Place(titleText.rectTransform, UIFactory.TopLeft, new Vector2(44f, -30f), new Vector2(size.x - 240f, 64f));
            subtitleText = UIFactory.CreateText("Subtitle", Window, string.Empty, UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.UpperLeft, UITheme.BodyFont);
            UIFactory.Place(subtitleText.rectTransform, UIFactory.TopLeft, new Vector2(46f, -96f), new Vector2(size.x - 240f, 44f));

            if (closable)
            {
                closeButton = UIFactory.CreateButton("Close", Window, string.Empty, Close, ButtonStyle.Ghost, Icon.Cross);
                UIFactory.Place((RectTransform)closeButton.transform, UIFactory.TopRight, new Vector2(-26f, -26f), new Vector2(96f, 96f));
                Transform icon = closeButton.transform.Find("Icon");
                if (icon != null)
                {
                    var r = (RectTransform)icon;
                    UIFactory.Place(r, UIFactory.Centre, Vector2.zero, new Vector2(44f, 44f));
                }
            }

            BuildContent();
            backdrop.SetActive(false);
        }

        protected abstract void BuildContent();

        protected void SetTitle(string title, string subtitle = null)
        {
            titleText.text = title;
            subtitleText.text = subtitle ?? string.Empty;
        }

        protected void Show()
        {
            if (IsOpen) return;
            backdrop.SetActive(true);
            backdrop.transform.SetAsLastSibling();
            VisibilityChanged?.Invoke(this, true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            backdrop.SetActive(false);
            OnClosed();
            VisibilityChanged?.Invoke(this, false);
        }

        protected virtual void OnClosed()
        {
        }
    }
}
