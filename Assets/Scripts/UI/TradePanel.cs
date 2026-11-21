using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    /// <summary>What the item card shows. Built by the visit controller from game data.</summary>
    public sealed class ItemCardView
    {
        public string Name;
        public string RarityName;
        public Color RarityColor;
        public string Subtitle;
        public Texture Thumbnail;
        public string EstimateText;
        public string EstimateNote;
        public string PriceLabel;
        public string PriceText;
        public string SecondLabel;
        public string SecondText;
        public string ThirdLabel;
        public string ThirdText;
        public string Warning;
        public string Description;
        public bool ShowInspect;
        public string InspectLabel = "Inspect closely";
    }

    /// <summary>One choice under the speech bubble.</summary>
    public sealed class ActionView
    {
        public string Label;
        public ButtonStyle Style = ButtonStyle.Secondary;
        public Icon? Icon;
        public bool Enabled = true;
        public Action OnClick;
    }

    /// <summary>Settings for the haggling controls.</summary>
    public sealed class OfferView
    {
        public bool PlayerBuys;
        public long Min;
        public long Max;
        public long Initial;
        public string InfoLine;
        public Action<long> Submit;
        public Action Back;
    }

    /// <summary>
    /// The trading interface around a visitor: their speech bubble (right), the item card (left),
    /// the choices under the bubble, and the offer controls for haggling. Display only: every button
    /// calls back into the visit controller, which talks to the game.
    /// </summary>
    public sealed class TradePanel : MonoBehaviour
    {
        private const float ColumnWidth = 660f;
        private const float BubbleTop = 240f;
        private const float BubbleHeight = 236f;
        private const float ActionsTop = BubbleTop + BubbleHeight + 28f;

        private RectTransform root;
        private RectTransform bubble;
        private Text nameText;
        private Text traitText;
        private RectTransform traitChip;
        private Text speechText;
        private readonly List<Image> patiencePips = new List<Image>();

        private RectTransform card;
        private RawImage thumbnail;
        private Image thumbBackdrop;
        private Text cardName;
        private Image rarityChip;
        private Text rarityText;
        private Text subtitleText;
        private Text estimateText;
        private Text estimateNote;
        private Text priceLabel;
        private Text priceText;
        private Text secondLabel;
        private Text secondText;
        private Text thirdLabel;
        private Text thirdText;
        private RectTransform warningRow;
        private Text warningText;
        private Text descriptionText;
        private Button inspectButton;

        private RectTransform actions;
        private readonly List<Button> actionButtons = new List<Button>();

        private RectTransform offerBox;
        private Text offerAmount;
        private Text offerInfo;
        private Slider offerSlider;
        private Text stepSmallDown, stepSmallUp, stepBigDown, stepBigUp;
        private OfferView offer;
        private long offerValue;
        private long smallStep;
        private bool settingSlider;

        private string fullSpeech = string.Empty;
        private float typed;

        public event Action InspectRequested;

        public bool IsVisible => root != null && (bubble.gameObject.activeSelf || card.gameObject.activeSelf);

        public void Build(RectTransform parent)
        {
            root = UIFactory.CreateRect("TradePanel", parent);
            UIFactory.Stretch(root);
            BuildBubble();
            BuildCard();
            BuildActions();
            BuildOfferBox();
            HideAll();
        }

        private void Update()
        {
            // Typewriter effect for speech: quick, so it never slows the player down.
            if (speechText != null && typed < fullSpeech.Length)
            {
                typed = Mathf.Min(fullSpeech.Length, typed + Time.unscaledDeltaTime * 90f);
                speechText.text = fullSpeech.Substring(0, Mathf.FloorToInt(typed));
            }
        }

        // ------------------------------------------------------------------ Public API

        public void HideAll()
        {
            HideSpeech();
            HideCard();
            SetActions();
            HideOffer();
        }

        public void ShowSpeech(string speaker, string trait, string line, int patienceLeft, int patienceMax)
        {
            bubble.gameObject.SetActive(true);
            nameText.text = speaker;
            traitText.text = trait;
            traitChip.gameObject.SetActive(!string.IsNullOrEmpty(trait));
            SetPatience(patienceLeft, patienceMax);
            Say(line);
        }

        public void Say(string line)
        {
            fullSpeech = line ?? string.Empty;
            typed = 0f;
            speechText.text = string.Empty;
        }

        public void SetPatience(int left, int max)
        {
            for (int i = 0; i < patiencePips.Count; i++)
            {
                patiencePips[i].gameObject.SetActive(i < max);
                patiencePips[i].color = i < left ? UITheme.Ink : new Color(UITheme.Ink.r, UITheme.Ink.g, UITheme.Ink.b, 0.18f);
            }
        }

        public void HideSpeech() => bubble.gameObject.SetActive(false);

        public void ShowCard(ItemCardView view)
        {
            card.gameObject.SetActive(true);
            cardName.text = view.Name;
            rarityText.text = view.RarityName.ToUpperInvariant();
            rarityChip.color = view.RarityColor;
            thumbBackdrop.color = new Color(view.RarityColor.r, view.RarityColor.g, view.RarityColor.b, 0.22f);
            subtitleText.text = view.Subtitle;
            thumbnail.texture = view.Thumbnail;
            thumbnail.enabled = view.Thumbnail != null;
            estimateText.text = view.EstimateText;
            estimateNote.text = view.EstimateNote;
            priceLabel.text = view.PriceLabel;
            priceText.text = view.PriceText;
            secondLabel.text = view.SecondLabel ?? string.Empty;
            secondText.text = view.SecondText ?? string.Empty;
            thirdLabel.text = view.ThirdLabel ?? string.Empty;
            thirdText.text = view.ThirdText ?? string.Empty;
            warningRow.gameObject.SetActive(!string.IsNullOrEmpty(view.Warning));
            warningText.text = view.Warning ?? string.Empty;
            descriptionText.text = view.Description;
            inspectButton.gameObject.SetActive(view.ShowInspect);
            UIFactory.SetLabel(inspectButton, view.InspectLabel);
        }

        public void HideCard() => card.gameObject.SetActive(false);

        public void SetActions(params ActionView[] views)
        {
            foreach (Button b in actionButtons) Destroy(b.gameObject);
            actionButtons.Clear();
            if (views == null) return;

            const float height = 98f, gap = 14f;
            for (int i = 0; i < views.Length; i++)
            {
                ActionView v = views[i];
                Button b = UIFactory.CreateButton("Action" + i, actions, v.Label, v.OnClick, v.Style, v.Icon);
                UIFactory.Place((RectTransform)b.transform, UIFactory.TopRight, new Vector2(0f, -i * (height + gap)), new Vector2(ColumnWidth - 40f, height));
                UIFactory.SetInteractable(b, v.Enabled);
                actionButtons.Add(b);
            }
        }

        public void ShowOffer(OfferView view)
        {
            offer = view;
            SetActions();
            offerBox.gameObject.SetActive(true);
            offerInfo.text = view.InfoLine;
            smallStep = NiceStep(Math.Max(view.Max, 1) / 40);
            long bigStep = smallStep * 5;
            stepSmallDown.text = "−" + Items.ItemText.Coins(smallStep);
            stepSmallUp.text = "+" + Items.ItemText.Coins(smallStep);
            stepBigDown.text = "−" + Items.ItemText.Coins(bigStep);
            stepBigUp.text = "+" + Items.ItemText.Coins(bigStep);
            SetOffer(view.Initial);
        }

        public void HideOffer()
        {
            offerBox.gameObject.SetActive(false);
            offer = null;
        }

        public bool IsOfferOpen => offerBox.gameObject.activeSelf;

        // ------------------------------------------------------------------ Offer logic (display only)

        private void SetOffer(long value)
        {
            if (offer == null) return;
            offerValue = Math.Max(offer.Min, Math.Min(offer.Max, value));
            offerAmount.text = Items.ItemText.Coins(offerValue);
            settingSlider = true;
            offerSlider.value = offer.Max > offer.Min ? (offerValue - offer.Min) / (float)(offer.Max - offer.Min) : 1f;
            settingSlider = false;
        }

        private void OnSlider(float t)
        {
            if (settingSlider || offer == null) return;
            long raw = offer.Min + (long)Math.Round((offer.Max - offer.Min) * t);
            SetOffer(raw / smallStep * smallStep + (raw % smallStep >= smallStep / 2 ? smallStep : 0));
        }

        private void Step(int direction, bool big)
        {
            SetOffer(offerValue + direction * smallStep * (big ? 5 : 1));
        }

        /// <summary>1, 2, 5, 10, 20, 50... the step closest above the raw value.</summary>
        private static long NiceStep(long raw)
        {
            long step = 1;
            while (true)
            {
                foreach (long m in new long[] { 1, 2, 5 })
                {
                    if (step * m >= raw) return Math.Max(5, step * m);
                }

                step *= 10;
            }
        }

        // ------------------------------------------------------------------ Layout

        private void BuildBubble()
        {
            bubble = UIFactory.CreatePanel("SpeechBubble", root, UITheme.Bubble, true, false);
            UIFactory.Place(bubble, UIFactory.TopRight, new Vector2(-UITheme.ScreenMargin, -BubbleTop), new Vector2(ColumnWidth, BubbleHeight));

            // Tail pointing toward the visitor in the middle of the screen
            Image tail = UIFactory.CreateRounded("Tail", bubble, UITheme.Bubble, 0.35f);
            UIFactory.Place(tail.rectTransform, UIFactory.MiddleLeft, new Vector2(-8f, -10f), new Vector2(46f, 46f));
            tail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.transform.SetSiblingIndex(1);

            RectTransform tag = UIFactory.CreatePanel("NameTag", bubble, UITheme.NameTag, false, false);
            UIFactory.Place(tag, UIFactory.TopLeft, new Vector2(26f, 30f), new Vector2(250f, 60f));
            nameText = UIFactory.CreateText("Name", tag, string.Empty, UITheme.FontSizeSmall + 2, UITheme.Gold, TextAnchor.MiddleCenter);
            UIFactory.Stretch(nameText.rectTransform, 6f);

            traitChip = UIFactory.CreatePanel("Trait", bubble, new Color32(232, 218, 190, 255), false, false);
            UIFactory.Place(traitChip, UIFactory.TopLeft, new Vector2(290f, 22f), new Vector2(210f, 46f));
            traitText = UIFactory.CreateText("Trait", traitChip, string.Empty, UITheme.FontSizeTiny, UITheme.InkMuted, TextAnchor.MiddleCenter);
            UIFactory.Stretch(traitText.rectTransform, 4f);

            for (int i = 0; i < 6; i++)
            {
                Image pip = UIFactory.CreateIcon("Patience" + i, bubble, Icon.Star, UITheme.Ink);
                UIFactory.Place(pip.rectTransform, UIFactory.TopRight, new Vector2(-24f - (5 - i) * 32f, -16f), new Vector2(28f, 28f));
                patiencePips.Add(pip);
            }

            speechText = UIFactory.CreateText("Speech", bubble, string.Empty, UITheme.FontSizeBody + 2, UITheme.Ink, TextAnchor.UpperLeft, UITheme.BodyFont);
            UIFactory.Stretch(speechText.rectTransform);
            speechText.rectTransform.offsetMin = new Vector2(38f, 24f);
            speechText.rectTransform.offsetMax = new Vector2(-34f, -56f);
        }

        private void BuildCard()
        {
            card = UIFactory.CreatePanel("ItemCard", root);
            UIFactory.Place(card, UIFactory.TopLeft, new Vector2(UITheme.ScreenMargin, -144f), new Vector2(600f, 720f));

            thumbBackdrop = UIFactory.CreateImage("ThumbBackdrop", card, Color.white, UISprites.Circle);
            UIFactory.Place(thumbBackdrop.rectTransform, UIFactory.TopLeft, new Vector2(24f, -22f), new Vector2(150f, 150f));
            var thumbRect = UIFactory.CreateRect("Thumb", card);
            UIFactory.Place(thumbRect, UIFactory.TopLeft, new Vector2(14f, -12f), new Vector2(170f, 170f));
            thumbnail = thumbRect.gameObject.AddComponent<RawImage>();
            thumbnail.raycastTarget = false;

            cardName = UIFactory.CreateText("Name", card, string.Empty, UITheme.FontSizeLarge, UITheme.Text, TextAnchor.UpperLeft);
            UIFactory.Place(cardName.rectTransform, UIFactory.TopLeft, new Vector2(196f, -28f), new Vector2(380f, 100f));

            rarityChip = UIFactory.CreateImage("Rarity", card, Color.white, UISprites.Pill);
            UIFactory.Place(rarityChip.rectTransform, UIFactory.TopLeft, new Vector2(196f, -124f), new Vector2(150f, 42f));
            rarityText = UIFactory.CreateText("RarityText", rarityChip.transform, string.Empty, UITheme.FontSizeTiny, UITheme.Ink, TextAnchor.MiddleCenter);
            UIFactory.Stretch(rarityText.rectTransform, 2f);

            subtitleText = UIFactory.CreateText("Subtitle", card, string.Empty, UITheme.FontSizeTiny, UITheme.TextMuted, TextAnchor.MiddleLeft);
            UIFactory.Place(subtitleText.rectTransform, UIFactory.TopLeft, new Vector2(358f, -124f), new Vector2(230f, 42f));

            Image divider = UIFactory.CreateImage("Divider", card, UITheme.PanelEdge);
            UIFactory.Row(divider.rectTransform, 196f, 2f, 24f);

            float y = 214f;
            Row("Estimate", "Market value (est.)", ref y, 52f, out Text estLabel, out estimateText, UITheme.FontSizeBody, UITheme.Text);
            estLabel.text = "Market value";
            estimateNote = UIFactory.CreateText("EstimateNote", card, string.Empty, UITheme.FontSizeTiny, UITheme.TextFaint, TextAnchor.UpperLeft);
            UIFactory.Row(estimateNote.rectTransform, y - 6f, 34f, 28f);
            y += 34f;

            Row("Price", string.Empty, ref y, 66f, out priceLabel, out priceText, UITheme.FontSizeHuge - 8, UITheme.Gold);
            Row("Second", string.Empty, ref y, 46f, out secondLabel, out secondText, UITheme.FontSizeBody, UITheme.Text);
            Row("Third", string.Empty, ref y, 46f, out thirdLabel, out thirdText, UITheme.FontSizeBody, UITheme.Text);

            warningRow = UIFactory.CreateRect("Warning", card);
            UIFactory.Row(warningRow, y + 4f, 44f, 24f);
            Image warnIcon = UIFactory.CreateIcon("Icon", warningRow, Icon.Warning, UITheme.NegativeText);
            UIFactory.Place(warnIcon.rectTransform, UIFactory.MiddleLeft, new Vector2(4f, 0f), new Vector2(36f, 36f));
            warnIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            warningText = UIFactory.CreateText("Text", warningRow, string.Empty, UITheme.FontSizeSmall, UITheme.NegativeText, TextAnchor.MiddleLeft);
            UIFactory.Stretch(warningText.rectTransform);
            warningText.rectTransform.offsetMin = new Vector2(50f, 0f);
            y += 50f;

            descriptionText = UIFactory.CreateText("Description", card, string.Empty, UITheme.FontSizeTiny + 2, UITheme.TextMuted, TextAnchor.UpperLeft, UITheme.BodyFont);
            UIFactory.Row(descriptionText.rectTransform, y + 6f, 94f, 28f);
            descriptionText.fontStyle = FontStyle.Italic;

            inspectButton = UIFactory.CreateButton("Inspect", card, "Inspect closely", () => InspectRequested?.Invoke(), ButtonStyle.Ghost, Icon.Magnifier, UITheme.FontSizeSmall + 2);
            UIFactory.Place((RectTransform)inspectButton.transform, UIFactory.BottomCentre, new Vector2(0f, 22f), new Vector2(552f, 86f));
        }

        private void Row(string name, string label, ref float y, float height, out Text labelText, out Text valueText, int valueSize, Color valueColor)
        {
            RectTransform row = UIFactory.CreateRect(name, card);
            UIFactory.Row(row, y, height, 28f);
            labelText = UIFactory.CreateText("Label", row, label, UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.MiddleLeft);
            UIFactory.Stretch(labelText.rectTransform);
            valueText = UIFactory.CreateText("Value", row, string.Empty, valueSize, valueColor, TextAnchor.MiddleRight);
            UIFactory.Stretch(valueText.rectTransform);
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            y += height;
        }

        private void BuildActions()
        {
            actions = UIFactory.CreateRect("Actions", root);
            UIFactory.Place(actions, UIFactory.TopRight, new Vector2(-UITheme.ScreenMargin, -ActionsTop), new Vector2(ColumnWidth, 460f));
        }

        private void BuildOfferBox()
        {
            offerBox = UIFactory.CreatePanel("OfferBox", root);
            UIFactory.Place(offerBox, UIFactory.TopRight, new Vector2(-UITheme.ScreenMargin, -ActionsTop), new Vector2(ColumnWidth, 486f));

            Text title = UIFactory.CreateText("Title", offerBox, "Your offer", UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.UpperCenter);
            UIFactory.Row(title.rectTransform, 22f, 40f);

            Image coin = UIFactory.CreateIcon("Coin", offerBox, Icon.Coin, UITheme.Gold);
            UIFactory.Place(coin.rectTransform, UIFactory.TopCentre, new Vector2(-150f, -78f), new Vector2(52f, 52f));
            offerAmount = UIFactory.CreateText("Amount", offerBox, "0", UITheme.FontSizeHuge, UITheme.Gold, TextAnchor.MiddleCenter);
            UIFactory.Place(offerAmount.rectTransform, UIFactory.TopCentre, new Vector2(20f, -66f), new Vector2(340f, 76f));

            offerInfo = UIFactory.CreateText("Info", offerBox, string.Empty, UITheme.FontSizeTiny, UITheme.TextMuted, TextAnchor.UpperCenter);
            UIFactory.Row(offerInfo.rectTransform, 146f, 36f, 20f);

            offerSlider = UIFactory.CreateSlider("Slider", offerBox, OnSlider);
            RectTransform sliderRect = (RectTransform)offerSlider.transform;
            UIFactory.Row(sliderRect, 192f, 64f, 40f);

            float stepW = 136f, stepH = 78f, gap = 12f;
            float startX = -(stepW * 4f + gap * 3f) * 0.5f + stepW * 0.5f;
            stepBigDown = StepButton("BigDown", startX, -1, true, stepW, stepH);
            stepSmallDown = StepButton("SmallDown", startX + (stepW + gap), -1, false, stepW, stepH);
            stepSmallUp = StepButton("SmallUp", startX + 2f * (stepW + gap), 1, false, stepW, stepH);
            stepBigUp = StepButton("BigUp", startX + 3f * (stepW + gap), 1, true, stepW, stepH);

            Button back = UIFactory.CreateButton("Back", offerBox, "Back", () => offer?.Back?.Invoke(), ButtonStyle.Ghost);
            UIFactory.Place((RectTransform)back.transform, UIFactory.BottomLeft, new Vector2(24f, 24f), new Vector2(190f, 96f));
            Button submit = UIFactory.CreateButton("Submit", offerBox, "Make offer", () => offer?.Submit?.Invoke(offerValue), ButtonStyle.Primary, Icon.Chat);
            UIFactory.Place((RectTransform)submit.transform, UIFactory.BottomRight, new Vector2(-24f, 24f), new Vector2(400f, 96f));
        }

        private Text StepButton(string name, float x, int direction, bool big, float w, float h)
        {
            Button b = UIFactory.CreateButton(name, offerBox, string.Empty, () => Step(direction, big), ButtonStyle.Secondary, null, UITheme.FontSizeSmall);
            UIFactory.Place((RectTransform)b.transform, UIFactory.TopCentre, new Vector2(x, -272f), new Vector2(w, h));
            ((RectTransform)b.transform).pivot = new Vector2(0.5f, 1f);
            return b.GetComponentInChildren<Text>();
        }
    }
}
