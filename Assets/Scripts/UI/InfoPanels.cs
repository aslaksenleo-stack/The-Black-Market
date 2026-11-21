using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    public sealed class UpgradeView
    {
        public string Name;
        public Icon Icon;
        public int Level;
        public int MaxLevel;
        public string Current;
        public string Next;
        public string ButtonLabel;
        public bool CanBuy;
        public Action Buy;
    }

    /// <summary>Spend coins on the shop. Each card shows what the upgrade does now and what the next level gives.</summary>
    public sealed class UpgradesPanel : ModalPanel
    {
        private RectTransform cards;

        protected override void BuildContent()
        {
            cards = UIFactory.CreateRect("Cards", Window);
            UIFactory.Stretch(cards);
            cards.offsetMin = new Vector2(40f, 40f);
            cards.offsetMax = new Vector2(-40f, -150f);
        }

        public void Open(string coinsText, IReadOnlyList<UpgradeView> upgrades)
        {
            SetTitle("Upgrades", "Improve the shop to find better deals. You have " + coinsText + ".");
            Rebuild(upgrades);
            Show();
        }

        public void Refresh(string coinsText, IReadOnlyList<UpgradeView> upgrades)
        {
            if (!IsOpen) return;
            SetTitle("Upgrades", "Improve the shop to find better deals. You have " + coinsText + ".");
            Rebuild(upgrades);
        }

        private void Rebuild(IReadOnlyList<UpgradeView> upgrades)
        {
            foreach (Transform child in cards) Destroy(child.gameObject);

            float width = (1500f - 80f - 2f * 24f) / 3f;
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeView u = upgrades[i];
                RectTransform card = UIFactory.CreatePanel(u.Name, cards, UITheme.Surface, false);
                UIFactory.Place(card, UIFactory.TopLeft, new Vector2(i * (width + 24f), 0f), new Vector2(width, 540f));

                Image icon = UIFactory.CreateIcon("Icon", card, u.Icon, UITheme.Gold);
                UIFactory.Place(icon.rectTransform, UIFactory.TopCentre, new Vector2(0f, -30f), new Vector2(92f, 92f));

                Text name = UIFactory.CreateText("Name", card, u.Name, UITheme.FontSizeLarge - 4, UITheme.Text, TextAnchor.UpperCenter);
                UIFactory.Row(name.rectTransform, 136f, 52f, 16f);

                for (int l = 0; l < u.MaxLevel; l++)
                {
                    Image pip = UIFactory.CreateImage("Level" + l, card, l < u.Level ? UITheme.Gold : UITheme.Inset, UISprites.Pill);
                    float pipW = 70f, gap = 10f;
                    float x = -((pipW + gap) * u.MaxLevel - gap) * 0.5f + l * (pipW + gap) + pipW * 0.5f;
                    UIFactory.Place(pip.rectTransform, UIFactory.TopCentre, new Vector2(x, -198f), new Vector2(pipW, 16f));
                    pip.rectTransform.pivot = new Vector2(0.5f, 1f);
                }

                Text now = UIFactory.CreateText("Now", card, "Now: " + u.Current, UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.UpperCenter, UITheme.BodyFont);
                UIFactory.Row(now.rectTransform, 240f, 80f, 24f);
                Text next = UIFactory.CreateText("Next", card, string.IsNullOrEmpty(u.Next) ? "Fully upgraded" : "Next: " + UIFactory.Colored(u.Next, UITheme.PositiveText),
                    UITheme.FontSizeSmall, UITheme.Text, TextAnchor.UpperCenter, UITheme.BodyFont);
                UIFactory.Row(next.rectTransform, 320f, 80f, 24f);

                Button buy = UIFactory.CreateButton("Buy", card, u.ButtonLabel, u.Buy, u.CanBuy ? ButtonStyle.Primary : ButtonStyle.Secondary, Icon.Coin, UITheme.FontSizeSmall + 2);
                UIFactory.Place((RectTransform)buy.transform, UIFactory.BottomCentre, new Vector2(0f, 24f), new Vector2(width - 48f, 96f));
                UIFactory.SetInteractable(buy, u.CanBuy);
            }
        }
    }

    public sealed class LedgerLine
    {
        public string Time;
        public string Text;
        public string Amount;
        public Color AmountColor;
    }

    /// <summary>The trade ledger: today's numbers, all-time totals and the latest deals.</summary>
    public sealed class LedgerPanel : ModalPanel
    {
        private Text summary;
        private RectTransform list;

        protected override void BuildContent()
        {
            RectTransform left = UIFactory.CreatePanel("Summary", Window, UITheme.Surface, false);
            UIFactory.Place(left, UIFactory.TopLeft, new Vector2(40f, -150f), new Vector2(460f, 620f));
            summary = UIFactory.CreateText("Text", left, string.Empty, UITheme.FontSizeSmall + 2, UITheme.Text, TextAnchor.UpperLeft, UITheme.BodyFont);
            UIFactory.Stretch(summary.rectTransform, 30f);
            summary.lineSpacing = 1.25f;

            list = UIFactory.CreateRect("Records", Window);
            UIFactory.Place(list, UIFactory.TopLeft, new Vector2(530f, -150f), new Vector2(730f, 620f));
        }

        public void Open(string subtitle, string summaryText, IReadOnlyList<LedgerLine> lines)
        {
            SetTitle("Ledger", subtitle);
            summary.text = summaryText;
            foreach (Transform child in list) Destroy(child.gameObject);

            if (lines.Count == 0)
            {
                Text none = UIFactory.CreateText("None", list, "No deals yet. Every trade you make is written here.", UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.UpperLeft, UITheme.BodyFont);
                UIFactory.Stretch(none.rectTransform, 10f);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                LedgerLine l = lines[i];
                RectTransform row = UIFactory.CreateRect("Row" + i, list);
                UIFactory.Row(row, i * 62f, 58f);
                if (i % 2 == 0)
                {
                    Image band = UIFactory.CreateRounded("Band", row, new Color(1f, 1f, 1f, 0.04f), 0.5f);
                    UIFactory.Stretch(band.rectTransform);
                }

                Text time = UIFactory.CreateText("Time", row, l.Time, UITheme.FontSizeTiny, UITheme.TextFaint, TextAnchor.MiddleLeft);
                UIFactory.Place(time.rectTransform, UIFactory.MiddleLeft, new Vector2(14f, 0f), new Vector2(130f, 56f));
                time.rectTransform.pivot = new Vector2(0f, 0.5f);
                Text text = UIFactory.CreateText("Text", row, l.Text, UITheme.FontSizeTiny + 2, UITheme.Text, TextAnchor.MiddleLeft, UITheme.BodyFont);
                UIFactory.Place(text.rectTransform, UIFactory.MiddleLeft, new Vector2(140f, 0f), new Vector2(400f, 56f));
                text.rectTransform.pivot = new Vector2(0f, 0.5f);
                Text amount = UIFactory.CreateText("Amount", row, l.Amount, UITheme.FontSizeSmall, l.AmountColor, TextAnchor.MiddleRight);
                UIFactory.Place(amount.rectTransform, UIFactory.MiddleRight, new Vector2(-14f, 0f), new Vector2(200f, 56f));
                amount.rectTransform.pivot = new Vector2(1f, 0.5f);
            }

            Show();
        }
    }

    /// <summary>Shown at closing time: the day's results, then the next day starts.</summary>
    public sealed class DaySummaryPanel : ModalPanel
    {
        private Text body;
        private Action onContinue;
        private Button continueButton;

        protected override void BuildContent()
        {
            body = UIFactory.CreateText("Body", Window, string.Empty, UITheme.FontSizeBody, UITheme.Text, TextAnchor.UpperLeft, UITheme.BodyFont);
            UIFactory.Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(48f, 150f);
            body.rectTransform.offsetMax = new Vector2(-48f, -150f);
            body.lineSpacing = 1.3f;

            continueButton = UIFactory.CreateButton("Continue", Window, "Open shop", () => Continue(), ButtonStyle.Primary, Icon.Bell);
            UIFactory.Place((RectTransform)continueButton.transform, UIFactory.BottomCentre, new Vector2(0f, 36f), new Vector2(440f, 104f));
        }

        public void Open(string title, string subtitle, string bodyText, string buttonLabel, Action onContinue)
        {
            SetTitle(title, subtitle);
            body.text = bodyText;
            UIFactory.SetLabel(continueButton, buttonLabel);
            this.onContinue = onContinue;
            Show();
        }

        private void Continue()
        {
            Action callback = onContinue;
            onContinue = null;
            Close();
            callback?.Invoke();
        }
    }

    /// <summary>Overlay during close inspection: what to do, progress, the finding, and a Done button.</summary>
    public sealed class InspectOverlay : MonoBehaviour
    {
        private GameObject root;
        private Text hint;
        private Image bar;
        private RectTransform barRect;
        private Text result;
        private RectTransform resultPanel;
        private Action onDone;

        public bool IsVisible => root != null && root.activeSelf;

        public void Build(RectTransform parent)
        {
            RectTransform r = UIFactory.CreateRect("InspectOverlay", parent);
            UIFactory.Stretch(r);
            root = r.gameObject;

            RectTransform top = UIFactory.CreatePanel("Hint", r);
            UIFactory.Place(top, UIFactory.TopCentre, new Vector2(0f, -UITheme.ScreenMargin), new Vector2(720f, 118f));
            Image icon = UIFactory.CreateIcon("Icon", top, Icon.Magnifier, UITheme.Gold);
            UIFactory.Place(icon.rectTransform, UIFactory.MiddleLeft, new Vector2(30f, 10f), new Vector2(50f, 50f));
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            hint = UIFactory.CreateText("Text", top, string.Empty, UITheme.FontSizeSmall + 2, UITheme.Text, TextAnchor.MiddleLeft);
            UIFactory.Place(hint.rectTransform, UIFactory.TopLeft, new Vector2(100f, -14f), new Vector2(590f, 54f));

            Image track = UIFactory.CreateImage("Track", top, UITheme.Inset, UISprites.Pill);
            UIFactory.Place(track.rectTransform, UIFactory.BottomLeft, new Vector2(100f, 26f), new Vector2(580f, 18f));
            bar = UIFactory.CreateImage("Bar", track.transform, UITheme.Gold, UISprites.Pill);
            barRect = bar.rectTransform;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;

            resultPanel = UIFactory.CreatePanel("Result", r);
            UIFactory.Place(resultPanel, UIFactory.BottomCentre, new Vector2(0f, 170f), new Vector2(820f, 110f));
            result = UIFactory.CreateText("Text", resultPanel, string.Empty, UITheme.FontSizeSmall + 2, UITheme.Text, TextAnchor.MiddleCenter, UITheme.BodyFont);
            UIFactory.Stretch(result.rectTransform, 18f);

            Button done = UIFactory.CreateButton("Done", r, "Put it down", () => onDone?.Invoke(), ButtonStyle.Secondary, Icon.Check);
            UIFactory.Place((RectTransform)done.transform, UIFactory.BottomCentre, new Vector2(0f, UITheme.ScreenMargin), new Vector2(400f, 108f));

            root.SetActive(false);
        }

        public void Show(string itemName, Action done)
        {
            onDone = done;
            root.SetActive(true);
            hint.text = "Drag to turn the " + itemName + " and look closely.";
            SetProgress(0f);
            resultPanel.gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            barRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            barRect.offsetMax = Vector2.zero;
            bar.enabled = progress > 0.01f;
        }

        public void ShowResult(string text, bool bad)
        {
            resultPanel.gameObject.SetActive(true);
            result.text = text;
            result.color = bad ? UITheme.NegativeText : UITheme.Text;
        }

        public void Hide()
        {
            root.SetActive(false);
            onDone = null;
        }
    }
}
