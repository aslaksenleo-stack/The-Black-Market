using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    public sealed class InventoryEntryView
    {
        public string Name;
        public string RarityName;
        public Color RarityColor;
        public string Detail;
        public string Value;
        public Texture Thumbnail;

        /// <summary>Pick mode: whether this item can be offered to the current buyer.</summary>
        public bool Selectable;

        /// <summary>Pick mode: shown instead of the Offer button when not selectable.</summary>
        public string Reason;
    }

    /// <summary>
    /// The shop's stock as a grid of item cards with their real 3D thumbnails. In pick mode (a buyer is
    /// waiting) items the buyer wants get an "Offer" button; the rest explain why they can't be offered.
    /// </summary>
    public sealed class InventoryPanel : ModalPanel
    {
        private const int Columns = 4;
        private static readonly Vector2 CellSize = new Vector2(320f, 330f);

        private RectTransform grid;
        private Text emptyText;
        private ScrollRect scroll;
        private Action<int> onPick;

        protected override void BuildContent()
        {
            RectTransform viewport = UIFactory.CreateRect("Viewport", Window);
            UIFactory.Stretch(viewport);
            viewport.offsetMin = new Vector2(40f, 36f);
            viewport.offsetMax = new Vector2(-40f, -150f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            grid = UIFactory.CreateRect("Grid", viewport);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = CellSize;
            layout.spacing = new Vector2(22f, 22f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;
            layout.childAlignment = TextAnchor.UpperCenter;
            var fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = grid;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            emptyText = UIFactory.CreateText("Empty", Window, string.Empty, UITheme.FontSizeBody, UITheme.TextMuted, TextAnchor.MiddleCenter, UITheme.BodyFont);
            UIFactory.Stretch(emptyText.rectTransform, 120f);
        }

        /// <param name="onPick">Pick mode when not null: called with the index of the chosen entry.</param>
        public void Open(string title, string subtitle, IReadOnlyList<InventoryEntryView> entries, Action<int> onPick, string emptyMessage)
        {
            this.onPick = onPick;
            SetTitle(title, subtitle);

            foreach (Transform child in grid)
            {
                child.gameObject.SetActive(false); // inactive children are ignored by the grid layout this frame
                Destroy(child.gameObject);
            }
            emptyText.text = entries.Count == 0 ? emptyMessage : string.Empty;

            for (int i = 0; i < entries.Count; i++)
            {
                BuildCell(entries[i], i);
            }

            Show();
            scroll.verticalNormalizedPosition = 1f;
        }

        protected override void OnClosed()
        {
            onPick = null;
        }

        private void BuildCell(InventoryEntryView e, int index)
        {
            bool pickMode = onPick != null;
            RectTransform cell = UIFactory.CreatePanel("Item" + index, grid, UITheme.Surface, false);

            Image stripe = UIFactory.CreateRounded("Rarity", cell, new Color(e.RarityColor.r, e.RarityColor.g, e.RarityColor.b, 0.2f));
            UIFactory.Row(stripe.rectTransform, 0f, 150f);
            stripe.raycastTarget = false;

            RectTransform thumbRect = UIFactory.CreateRect("Thumb", cell);
            UIFactory.Place(thumbRect, UIFactory.TopCentre, new Vector2(0f, -4f), new Vector2(150f, 150f));
            var thumb = thumbRect.gameObject.AddComponent<RawImage>();
            thumb.texture = e.Thumbnail;
            thumb.raycastTarget = false;
            thumb.enabled = e.Thumbnail != null;

            Image chip = UIFactory.CreateImage("Chip", cell, e.RarityColor, UISprites.Pill);
            UIFactory.Place(chip.rectTransform, UIFactory.TopLeft, new Vector2(14f, -14f), new Vector2(118f, 34f));
            Text chipText = UIFactory.CreateText("Text", chip.transform, e.RarityName.ToUpperInvariant(), 20, UITheme.Ink, TextAnchor.MiddleCenter);
            UIFactory.Stretch(chipText.rectTransform);

            Text name = UIFactory.CreateText("Name", cell, e.Name, UITheme.FontSizeSmall, UITheme.Text, TextAnchor.UpperCenter);
            UIFactory.Row(name.rectTransform, 158f, 66f, 14f);

            Text detail = UIFactory.CreateText("Detail", cell, e.Detail, UITheme.FontSizeTiny - 2, UITheme.TextMuted, TextAnchor.UpperCenter, UITheme.BodyFont);
            UIFactory.Row(detail.rectTransform, 222f, 30f, 10f);

            if (pickMode)
            {
                if (e.Selectable)
                {
                    int captured = index;
                    Button offer = UIFactory.CreateButton("Offer", cell, "Offer", () => Pick(captured), ButtonStyle.Primary, null, UITheme.FontSizeSmall);
                    UIFactory.Place((RectTransform)offer.transform, UIFactory.BottomCentre, new Vector2(0f, 12f), new Vector2(290f, 64f));
                }
                else
                {
                    Text reason = UIFactory.CreateText("Reason", cell, e.Reason, UITheme.FontSizeTiny - 2, UITheme.TextFaint, TextAnchor.MiddleCenter);
                    UIFactory.Place(reason.rectTransform, UIFactory.BottomCentre, new Vector2(0f, 14f), new Vector2(290f, 60f));
                    cell.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
                }
            }
            else
            {
                Text value = UIFactory.CreateText("Value", cell, e.Value, UITheme.FontSizeTiny, UITheme.Gold, TextAnchor.MiddleCenter);
                UIFactory.Place(value.rectTransform, UIFactory.BottomCentre, new Vector2(0f, 14f), new Vector2(290f, 56f));
            }
        }

        private void Pick(int index)
        {
            Action<int> callback = onPick;
            Close();
            callback?.Invoke(index);
        }
    }
}
