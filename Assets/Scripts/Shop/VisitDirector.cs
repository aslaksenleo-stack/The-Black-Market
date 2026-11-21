using System;
using System.Collections;
using System.Collections.Generic;
using BlackMarket.Art;
using BlackMarket.Audio;
using BlackMarket.Customers;
using BlackMarket.Economy;
using BlackMarket.Items;
using BlackMarket.Trading;
using BlackMarket.UI;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Runs each visit in the shop, from the door opening to the visitor leaving:
    ///
    /// Seller: walks in carrying an item → puts it on the counter → asks a price. The player can
    ///         inspect it, buy at their price, haggle, or decline.
    /// Buyer:  walks in → asks for an item or a category. The player can offer something from stock,
    ///         then sell at their bid, haggle for more, show something else, or decline.
    ///
    /// All decisions go through ShopGame (which validates and applies them); this class only stages the
    /// scene (characters, items, door, camera) and fills the HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisitDirector : MonoBehaviour
    {
        private const float GameMinuteSeconds = 2f;

        private ShopGame game;
        private ShopHUD hud;
        private ShopAnchors anchors;
        private ShopDoor door;
        private ItemInspector inspector;
        private Player.CameraController cameraController;
        private Transform playerEye;

        private Visitor visitor;
        private CustomerAvatar avatar;
        private CounterItem counterItem;
        private Action showActions;
        private bool busy;
        private bool inspecting;
        private bool waitingForNewDay;
        private float idleTimer = 2.5f;
        private float clockTimer;

        /// <summary>Raised when the player starts (true) or stops (false) inspecting; the scene pauses looking around.</summary>
        public event Action<bool> InspectingChanged;

        public bool HasVisitor => visitor != null;

        public void Initialise(ShopGame shopGame, ShopHUD shopHud, ShopAnchors shopAnchors, ShopDoor shopDoor,
            ItemInspector itemInspector, Player.CameraController cam, Transform eye)
        {
            game = shopGame;
            hud = shopHud;
            anchors = shopAnchors;
            door = shopDoor;
            inspector = itemInspector;
            cameraController = cam;
            playerEye = eye;

            hud.Trade.InspectRequested += StartInspect;
            inspector.Completed += OnInspectionComplete;
            hud.SetStatus(game.VisitCount == 0 ? "Your shop is open. Your first visitor is on the way…" : "Waiting for visitors…");
        }

        private void OnDestroy()
        {
            if (hud != null) hud.Trade.InspectRequested -= StartInspect;
            if (inspector != null) inspector.Completed -= OnInspectionComplete;
        }

        // ------------------------------------------------------------------ Pacing

        private void Update()
        {
            if (game == null) return;

            if (inspecting)
            {
                hud.Inspect.SetProgress(inspector.Progress);
            }

            if (hud.IsModalOpen) return;

            // Time passes slowly while the shop is open.
            if (!game.Clock.IsClosingTime && !waitingForNewDay)
            {
                clockTimer += Time.deltaTime;
                if (clockTimer >= GameMinuteSeconds)
                {
                    clockTimer = 0f;
                    game.Clock.Advance(1);
                }
            }

            if (visitor != null || busy || waitingForNewDay) return;

            if (game.Clock.IsClosingTime)
            {
                ShowDaySummary();
                return;
            }

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                StartCoroutine(VisitRoutine());
            }
        }

        /// <summary>The counter bell: call the next visitor now.</summary>
        public void RingBell()
        {
            SoundKit.Play(Sound.CounterBell);
            if (visitor != null || busy)
            {
                hud.ShowToast("Someone is already at the counter.");
            }
            else if (game.Clock.IsClosingTime)
            {
                hud.ShowToast("The shop is closed for today.");
            }
            else
            {
                idleTimer = 0f;
                hud.SetStatus("Someone heard the bell…");
            }
        }

        // ------------------------------------------------------------------ Arrival and departure

        private IEnumerator VisitRoutine()
        {
            busy = true;
            visitor = game.BeginVisit();
            if (visitor == null)
            {
                busy = false;
                yield break;
            }

            hud.SetStatus(string.Empty);
            int seed = game.VisitCount * 7919 + visitor.LookId.Length;
            avatar = CustomerAvatar.Create(visitor.LookId, seed, BrowTilt(visitor.Personality), anchors.CustomerStart, 200f);

            if (visitor.Role == VisitorRole.Seller)
            {
                counterItem = CounterItem.Create(visitor.OfferedItem, avatar.HoldPoint.position, avatar.HoldPoint.rotation);
                counterItem.transform.SetParent(avatar.HoldPoint, true);
                counterItem.Interactable = false;
                counterItem.Used += _ => StartInspect();
                avatar.SetHolding(true);
            }

            yield return avatar.WalkPath(new[] { anchors.DoorOutside });
            door.Open();
            yield return new WaitForSeconds(0.25f);
            yield return avatar.WalkPath(new[] { anchors.DoorInside });
            door.Close();
            yield return avatar.WalkPath(new[] { anchors.CustomerCounter });

            avatar.FaceTowards(playerEye.position);
            avatar.LookAt(playerEye);
            cameraController.FocusOn(anchors.CustomerCounter + Vector3.up * 1.15f, 0.9f);
            yield return new WaitForSeconds(0.35f);

            if (visitor.Role == VisitorRole.Seller)
            {
                yield return PlaceSellerItem();
                Speak(DialogueMoment.Greeting, visitor.AskingPrice);
                ShowSellerCard();
                ShowSellerActions();
            }
            else
            {
                avatar.SetLeaning(true);
                Speak(DialogueMoment.Greeting, 0);
                ShowBuyerRequestActions();
            }

            busy = false;
        }

        private IEnumerator PlaceSellerItem()
        {
            counterItem.transform.SetParent(null, true);
            avatar.SetHolding(false);
            avatar.SetLeaning(true);
            Quaternion facing = Quaternion.Euler(0f, 180f + UnityEngine.Random.Range(-15f, 15f), 0f);
            yield return Tween.MoveArc(counterItem.transform, anchors.ItemSpot, facing, 0.55f, 0.08f, Tween.EaseOutBack);
            SoundKit.Play(Sound.ItemDown);
            counterItem.Interactable = true;
        }

        private IEnumerator LeaveRoutine(float delay)
        {
            busy = true;
            SetActions();
            hud.Trade.HideOffer();
            yield return new WaitForSeconds(delay);

            hud.Trade.HideAll();
            avatar.SetLeaning(false);
            avatar.LookAt(null);
            if (counterItem != null && counterItem.transform.parent == avatar.HoldPoint) avatar.SetHolding(true);

            yield return avatar.WalkPath(new[] { anchors.DoorInside });
            door.Open();
            yield return new WaitForSeconds(0.2f);
            yield return avatar.WalkPath(new[] { anchors.DoorOutside });
            door.Close();
            yield return avatar.WalkPath(new[] { anchors.CustomerStart });

            if (counterItem != null) Destroy(counterItem.gameObject);
            Destroy(avatar.gameObject);
            counterItem = null;
            avatar = null;
            visitor = null;
            game.EndVisit();

            idleTimer = UnityEngine.Random.Range(3f, 6f);
            hud.SetStatus(game.Clock.IsClosingTime ? string.Empty : "Waiting for visitors… Ring the bell to call one in.");
            busy = false;
        }

        // ------------------------------------------------------------------ Seller flow

        private void ShowSellerActions()
        {
            long ask = game.CurrentNegotiation.CurrentPrice;
            TradeError canBuy = game.CanBuyAt(ask);
            string buyLabel = canBuy == TradeError.NotEnoughCoins ? "Can't afford " + ItemText.Coins(ask)
                : canBuy == TradeError.InventoryFull ? "Storage full"
                : "Buy for " + ItemText.Coins(ask);

            showActions = ShowSellerActions;
            SetActions(
                new ActionView { Label = buyLabel, Style = ButtonStyle.Primary, Icon = Icon.Check, Enabled = canBuy == TradeError.None, OnClick = AcceptPrice },
                new ActionView { Label = "Make an offer", Icon = Icon.Chat, OnClick = OpenSellerOffer },
                new ActionView { Label = "No thanks", Style = ButtonStyle.Danger, Icon = Icon.Cross, OnClick = Decline });
        }

        private void OpenSellerOffer()
        {
            Negotiation n = game.CurrentNegotiation;
            long ask = n.CurrentPrice;
            long min = Math.Max(5, ItemInstance.RoundValue(ask * 0.15));
            long max = Math.Max(min, Math.Min(ask, game.Wallet.Coins));
            ValueEstimate est = game.Estimate(visitor.OfferedItem);
            showActions = OpenSellerOffer;
            hud.Trade.ShowOffer(new OfferView
            {
                PlayerBuys = true,
                Min = min,
                Max = max,
                Initial = Math.Min(max, Math.Max(min, ItemInstance.RoundValue(Math.Min(ask * 0.7, est.Mid * 0.8)))),
                InfoLine = $"They ask {ItemText.Coins(ask)}   ·   Value {est}",
                Submit = Propose,
                Back = () => { hud.Trade.HideOffer(); ShowSellerActions(); }
            });
        }

        // ------------------------------------------------------------------ Buyer flow

        private void ShowBuyerRequestActions()
        {
            int matching = game.Inventory.FindAll(visitor.Wants).Count;
            showActions = ShowBuyerRequestActions;
            SetActions(
                new ActionView
                {
                    Label = matching > 0 ? "Show an item" : "Nothing in stock fits",
                    Style = ButtonStyle.Primary, Icon = Icon.Box, Enabled = matching > 0, OnClick = OpenPicker
                },
                new ActionView { Label = "Sorry, I don't have that", Style = ButtonStyle.Danger, Icon = Icon.Cross, OnClick = Decline });
        }

        private void OpenPicker()
        {
            var entries = new List<InventoryEntryView>();
            IReadOnlyList<ItemInstance> stock = game.Inventory.Items;
            foreach (ItemInstance item in stock)
            {
                InventoryEntryView e = EntryFor(item);
                e.Selectable = visitor.Wants(item) && item != game.ItemOnOffer;
                e.Reason = item == game.ItemOnOffer ? "On the counter" : "Not what they want";
                entries.Add(e);
            }

            var snapshot = new List<ItemInstance>(stock);
            hud.Inventory.Open("Offer an item", $"{visitor.DisplayName} wants {visitor.Request.Describe()}.", entries,
                index => StartCoroutine(OfferToBuyer(snapshot[index])), "Your storage is empty.");
        }

        private IEnumerator OfferToBuyer(ItemInstance item)
        {
            if (visitor == null || busy) yield break;
            TradeResult r = game.OfferItemToBuyer(item);
            if (!r.Success)
            {
                hud.ShowToast(r.ErrorMessage);
                yield break;
            }

            busy = true;
            SetActions();

            // Put away anything already on the counter, then bring the chosen item up from under it.
            if (counterItem != null)
            {
                CounterItem old = counterItem;
                counterItem = null;
                yield return Tween.MoveArc(old.transform, anchors.StockDrop, old.transform.rotation, 0.3f, 0.05f, null, Vector3.one * 0.3f);
                Destroy(old.gameObject);
            }

            counterItem = CounterItem.Create(item, anchors.StockDrop, Quaternion.Euler(0f, 180f, 0f));
            counterItem.transform.localScale = Vector3.one * 0.3f;
            counterItem.Used += _ => StartInspect();
            yield return Tween.MoveArc(counterItem.transform, anchors.ItemSpot, Quaternion.Euler(0f, 180f + UnityEngine.Random.Range(-15f, 15f), 0f),
                0.5f, 0.15f, Tween.EaseOutBack, Vector3.one);
            SoundKit.Play(Sound.ItemDown);

            avatar.React(AvatarReaction.Thinking);
            yield return new WaitForSeconds(0.6f);
            Speak(DialogueMoment.Counter, game.CurrentNegotiation.CurrentPrice);
            ShowBuyerCard();
            ShowBuyerDealActions();
            busy = false;
        }

        private void ShowBuyerDealActions()
        {
            long bid = game.CurrentNegotiation.CurrentPrice;
            bool otherOptions = game.Inventory.FindAll(i => visitor.Wants(i) && i != game.ItemOnOffer).Count > 0;
            showActions = ShowBuyerDealActions;

            var list = new List<ActionView>
            {
                new ActionView { Label = "Sell for " + ItemText.Coins(bid), Style = ButtonStyle.Primary, Icon = Icon.Check, OnClick = AcceptPrice },
                new ActionView { Label = "Ask for more", Icon = Icon.Chat, OnClick = OpenBuyerOffer }
            };
            if (otherOptions) list.Add(new ActionView { Label = "Show something else", Style = ButtonStyle.Ghost, Icon = Icon.Box, OnClick = OpenPicker });
            list.Add(new ActionView { Label = "Not for that price", Style = ButtonStyle.Danger, Icon = Icon.Cross, OnClick = Decline });
            SetActions(list.ToArray());
        }

        private void OpenBuyerOffer()
        {
            Negotiation n = game.CurrentNegotiation;
            long bid = n.CurrentPrice;
            ValueEstimate est = game.Estimate(game.ItemOnOffer);
            long max = Math.Max(bid + 10, ItemInstance.RoundValue(Math.Max(bid * 2.5, est.High * 2.0)));
            showActions = OpenBuyerOffer;
            hud.Trade.ShowOffer(new OfferView
            {
                PlayerBuys = false,
                Min = bid,
                Max = max,
                Initial = ItemInstance.RoundValue(Math.Max(bid * 1.2, est.Mid * 1.1)),
                InfoLine = $"They offer {ItemText.Coins(bid)}   ·   You paid {ItemText.Coins(game.ItemOnOffer.PurchasePrice)}",
                Submit = Propose,
                Back = () => { hud.Trade.HideOffer(); ShowBuyerDealActions(); }
            });
        }

        // ------------------------------------------------------------------ Deals (both directions)

        private void AcceptPrice()
        {
            if (busy) return;
            bool flawKnown = FlawKnownNow();
            TradeResult result = game.AcceptCurrentPrice();
            if (!result.Success)
            {
                hud.ShowToast(result.ErrorMessage);
                return;
            }

            OnDeal(result, flawKnown);
        }

        private void Propose(long price)
        {
            if (busy || game.CurrentNegotiation == null) return;
            bool flawKnown = FlawKnownNow();
            ProposalResult r = game.Propose(price);

            if (r.Trade.Error != TradeError.None && r.Trade.Error != TradeError.NoDeal)
            {
                hud.ShowToast(r.Trade.ErrorMessage);
                return;
            }

            switch (r.Answer.Outcome)
            {
                case NegotiationOutcome.Accepted:
                    hud.Trade.HideOffer();
                    OnDeal(r.Trade, flawKnown);
                    break;

                case NegotiationOutcome.Countered:
                case NegotiationOutcome.FinalOffer:
                    avatar.React(r.Answer.Insulted ? AvatarReaction.Annoyed : AvatarReaction.Thinking);
                    SoundKit.Play(Sound.Refused, 0.5f);
                    DialogueMoment moment = r.Answer.Insulted ? DialogueMoment.Insulted
                        : r.Answer.Outcome == NegotiationOutcome.FinalOffer ? DialogueMoment.FinalOffer : DialogueMoment.Counter;
                    Speak(moment, r.Answer.Price);
                    hud.Trade.HideOffer(); // their new price now shows on the deal button
                    if (visitor.Role == VisitorRole.Seller)
                    {
                        ShowSellerCard();
                        ShowSellerActions();
                    }
                    else
                    {
                        ShowBuyerCard();
                        ShowBuyerDealActions();
                    }

                    break;

                case NegotiationOutcome.WalkedAway:
                    hud.Trade.HideOffer();
                    avatar.React(AvatarReaction.Annoyed);
                    SoundKit.Play(Sound.Refused);
                    Speak(DialogueMoment.WalkAway, game.CurrentNegotiation.CurrentPrice);
                    if (visitor.Role == VisitorRole.Seller) StartCoroutine(TakeItemBack());
                    StartCoroutine(LeaveRoutine(1.8f));
                    break;
            }
        }

        // Buying reveals everything about an item, so whether the player knew about a flaw must be read before the deal.
        private bool FlawKnownNow() => visitor != null && visitor.Role == VisitorRole.Seller && visitor.OfferedItem.FlawRevealed;

        private void OnDeal(TradeResult result, bool flawKnownBeforeDeal)
        {
            avatar.React(AvatarReaction.Happy);
            SoundKit.Play(Sound.Coins);
            SoundKit.Play(Sound.DealDone, 0.7f);
            Speak(DialogueMoment.Accept, result.Price);
            SetActions();

            if (visitor.Role == VisitorRole.Seller)
            {
                hud.ShowCoinChange(-result.Price);
                ItemInstance bought = visitor.OfferedItem;
                if (bought.HasHiddenFlaw && !flawKnownBeforeDeal)
                {
                    hud.ShowToast($"Uh oh: once it's yours you spot a hidden flaw. It's only worth about {ItemText.Coins(bought.TrueValue)}.", 4f);
                }
                else
                {
                    hud.ShowToast($"Bought {bought.Definition.Name} for {ItemText.Coins(result.Price)}. It's in your storage.");
                }

                hud.Trade.HideCard();
                StartCoroutine(StowItem());
            }
            else
            {
                hud.ShowCoinChange(result.Price);
                string profit = result.Profit >= 0
                    ? UIFactory.Colored("+" + ItemText.Coins(result.Profit), UITheme.PositiveText)
                    : UIFactory.Colored("−" + ItemText.Coins(-result.Profit), UITheme.NegativeText);
                hud.ShowToast($"Sold for {ItemText.Coins(result.Price)}  ·  Profit {profit}", 3.5f);
                hud.Trade.HideCard();
                StartCoroutine(HandOverItem());
            }

            StartCoroutine(LeaveRoutine(1.6f));
        }

        private IEnumerator StowItem()
        {
            CounterItem item = counterItem;
            counterItem = null;
            if (item == null) yield break;
            item.Interactable = false;
            yield return Tween.MoveArc(item.transform, anchors.StockDrop, item.transform.rotation * Quaternion.Euler(0f, 90f, 0f), 0.5f, 0.15f, null, Vector3.one * 0.25f);
            Destroy(item.gameObject);
        }

        private IEnumerator HandOverItem()
        {
            CounterItem item = counterItem;
            if (item == null) yield break;
            item.Interactable = false;
            avatar.SetHolding(true);
            yield return Tween.MoveArc(item.transform, avatar.HoldPoint.position, avatar.HoldPoint.rotation, 0.45f, 0.1f);
            if (item != null && avatar != null) item.transform.SetParent(avatar.HoldPoint, true);
        }

        private IEnumerator TakeItemBack()
        {
            if (counterItem == null || avatar == null) yield break;
            counterItem.Interactable = false;
            avatar.SetLeaning(false);
            avatar.SetHolding(true);
            yield return Tween.MoveArc(counterItem.transform, avatar.HoldPoint.position, avatar.HoldPoint.rotation, 0.45f, 0.1f);
            if (counterItem != null && avatar != null) counterItem.transform.SetParent(avatar.HoldPoint, true);
        }

        private void Decline()
        {
            if (busy) return;
            game.DeclineVisitor();
            avatar.React(visitor.Role == VisitorRole.Seller ? AvatarReaction.ShakeHead : AvatarReaction.Nod);
            Speak(DialogueMoment.Declined, game.CurrentNegotiation != null ? game.CurrentNegotiation.CurrentPrice : 0);
            hud.Trade.HideCard();
            hud.Trade.HideOffer();

            if (visitor.Role == VisitorRole.Seller)
            {
                StartCoroutine(TakeItemBack());
            }
            else if (counterItem != null)
            {
                StartCoroutine(StowItem());
            }

            StartCoroutine(LeaveRoutine(1.4f));
        }

        // ------------------------------------------------------------------ Inspection

        private void StartInspect()
        {
            if (inspecting || busy || counterItem == null || !counterItem.Interactable || hud.IsModalOpen) return;
            inspecting = true;
            hud.Trade.HideSpeech();
            hud.Trade.SetActions();
            hud.Trade.HideOffer();
            hud.SetWorldControlsVisible(false);
            InspectingChanged?.Invoke(true);

            ItemInstance item = counterItem.Item;
            inspector.Begin(counterItem, item.IsInspected);
            hud.Inspect.Show(item.Definition.Name, EndInspect);
            if (item.IsInspected) hud.Inspect.ShowResult(InspectionSummary(item, false), item.HasHiddenFlaw && item.FlawRevealed);
        }

        private void OnInspectionComplete(CounterItem inspected)
        {
            if (inspected == null || inspected.Item == null) return;
            ItemInstance item = inspected.Item;
            bool flawFound = game.Inspect(item);
            SoundKit.Play(flawFound ? Sound.FlawFound : Sound.DealDone, flawFound ? 1f : 0.5f);
            hud.Inspect.ShowResult(InspectionSummary(item, flawFound), flawFound);
            RefreshCard();
        }

        private string InspectionSummary(ItemInstance item, bool justFound)
        {
            ValueEstimate est = game.Estimate(item);
            if (item.HasHiddenFlaw)
            {
                return (justFound ? "You find a hidden crack the seller didn't mention! " : "Hidden flaw: cracked. ") +
                       "Real value: " + est + ".";
            }

            return $"Condition: {ItemText.ConditionName(item.Condition)}. No hidden damage. Value narrowed to {est}.";
        }

        private void EndInspect()
        {
            if (!inspecting) return;
            inspecting = false;
            inspector.End();
            hud.Inspect.Hide();
            hud.SetWorldControlsVisible(true);
            InspectingChanged?.Invoke(false);

            if (visitor != null)
            {
                hud.Trade.ShowSpeech(visitor.DisplayName, visitor.Personality.Trait, CurrentLine, PatienceLeft, visitor.Patience);
                RefreshCard();
                showActions?.Invoke();
            }
        }

        // ------------------------------------------------------------------ HUD content

        private string currentLine = string.Empty;
        private string CurrentLine => currentLine;
        private int PatienceLeft => game.CurrentNegotiation != null ? game.CurrentNegotiation.PatienceLeft : visitor.Patience;

        private void Speak(DialogueMoment moment, long price)
        {
            currentLine = game.Say(moment, price);
            avatar.Talk(Mathf.Clamp(currentLine.Length * 0.035f, 0.8f, 3f));
            hud.Trade.ShowSpeech(visitor.DisplayName, visitor.Personality.Trait, currentLine, PatienceLeft, visitor.Patience);
        }

        private void SetActions(params ActionView[] actions)
        {
            hud.Trade.SetActions(actions);
        }

        private void RefreshCard()
        {
            if (visitor == null) return;
            if (visitor.Role == VisitorRole.Seller && counterItem != null) ShowSellerCard();
            else if (visitor.Role == VisitorRole.Buyer && game.ItemOnOffer != null) ShowBuyerCard();
        }

        private void ShowSellerCard()
        {
            ItemInstance item = visitor.OfferedItem;
            ValueEstimate est = game.Estimate(item);
            long ask = game.CurrentNegotiation.CurrentPrice;
            ItemCardView view = BaseCard(item, est);
            view.PriceLabel = ask < visitor.AskingPrice ? "Their price (was " + ItemText.Coins(visitor.AskingPrice) + ")" : "Asking price";
            view.PriceText = ItemText.Coins(ask);
            view.SecondLabel = "Potential profit";
            view.SecondText = ProfitRange(est.Low - ask, est.High - ask);
            view.ThirdLabel = "Your coins";
            view.ThirdText = ItemText.Coins(game.Wallet.Coins);
            hud.Trade.ShowCard(view);
        }

        private void ShowBuyerCard()
        {
            ItemInstance item = game.ItemOnOffer;
            ValueEstimate est = game.Estimate(item);
            long bid = game.CurrentNegotiation.CurrentPrice;
            ItemCardView view = BaseCard(item, est);
            view.PriceLabel = "Their offer";
            view.PriceText = ItemText.Coins(bid);
            view.SecondLabel = "You paid";
            view.SecondText = ItemText.Coins(item.PurchasePrice);
            view.ThirdLabel = "Your profit";
            view.ThirdText = Signed(bid - item.PurchasePrice);
            hud.Trade.ShowCard(view);
        }

        private ItemCardView BaseCard(ItemInstance item, ValueEstimate est)
        {
            int spread = Mathf.RoundToInt(game.Upgrades.EstimateSpread * (item.IsInspected ? 50f : 100f));
            return new ItemCardView
            {
                Name = item.Definition.Name,
                RarityName = ItemText.RarityName(item.Definition.Rarity),
                RarityColor = Palette.RarityColor(item.Definition.Rarity),
                Subtitle = ItemText.CategoryName(item.Definition.Category) + "  ·  " + ItemText.ConditionName(item.Condition),
                Thumbnail = ItemThumbnails.Get(item.Definition.ModelId, item.HasHiddenFlaw && item.FlawRevealed),
                EstimateText = est.ToString(),
                EstimateNote = item.IsInspected ? $"Inspected  ·  ±{spread}%" : $"Rough guess (±{spread}%)  ·  inspect to narrow it",
                Warning = item.HasHiddenFlaw && item.FlawRevealed ? "Hidden flaw: cracked. Worth far less." : null,
                Description = item.Definition.Description,
                ShowInspect = true,
                InspectLabel = item.IsInspected ? "Look again" : "Inspect closely"
            };
        }

        private static string ProfitRange(long low, long high)
        {
            if (low >= 0) return UIFactory.Colored("+" + ItemText.Coins(low) + " to +" + ItemText.Coins(high), UITheme.PositiveText);
            if (high <= 0) return UIFactory.Colored("−" + ItemText.Coins(-high) + " to −" + ItemText.Coins(-low), UITheme.NegativeText);
            return UIFactory.Colored("−" + ItemText.Coins(-low), UITheme.NegativeText) + " to " + UIFactory.Colored("+" + ItemText.Coins(high), UITheme.PositiveText);
        }

        private static string Signed(long value)
        {
            return value >= 0
                ? UIFactory.Colored("+" + ItemText.Coins(value), UITheme.PositiveText)
                : UIFactory.Colored("−" + ItemText.Coins(-value), UITheme.NegativeText);
        }

        public InventoryEntryView EntryFor(ItemInstance item)
        {
            ValueEstimate est = game.Estimate(item);
            return new InventoryEntryView
            {
                Name = item.Definition.Name,
                RarityName = ItemText.RarityName(item.Definition.Rarity),
                RarityColor = Palette.RarityColor(item.Definition.Rarity),
                Detail = $"{ItemText.ConditionName(item.Condition)}  ·  paid {ItemText.Coins(item.PurchasePrice)}",
                Value = "Worth " + est,
                Thumbnail = ItemThumbnails.Get(item.Definition.ModelId, item.HasHiddenFlaw && item.FlawRevealed)
            };
        }

        private static float BrowTilt(Personality p)
        {
            switch (p.Id)
            {
                case "seller_greedy":
                case "buyer_noble": return 14f;
                case "seller_shady": return 10f;
                case "seller_haggler":
                case "buyer_bargain": return 6f;
                case "seller_desperate": return -14f;
                case "buyer_scholar": return -5f;
                default: return 0f;
            }
        }

        // ------------------------------------------------------------------ Closing time

        private void ShowDaySummary()
        {
            waitingForNewDay = true;
            DaySummary s = game.Ledger.SummaryForDay(game.Clock.Day);
            string profit = s.Profit >= 0
                ? UIFactory.Colored("+" + ItemText.Coins(s.Profit), UITheme.PositiveText)
                : UIFactory.Colored("−" + ItemText.Coins(-s.Profit), UITheme.NegativeText);
            string body =
                $"Bought   {s.Purchases} item{(s.Purchases == 1 ? "" : "s")}  ({ItemText.Coins(s.Spent)} coins)\n" +
                $"Sold   {s.Sales} item{(s.Sales == 1 ? "" : "s")}  ({ItemText.Coins(s.Earned)} coins)\n" +
                $"Profit on sales   {profit}\n" +
                (s.Invested > 0 ? $"Invested in upgrades   {ItemText.Coins(s.Invested)}\n" : string.Empty) +
                $"\nCoins now   {UIFactory.Gold(ItemText.Coins(game.Wallet.Coins))}\n" +
                $"Stock   {game.Inventory.Count}/{game.Inventory.Capacity}";

            hud.DaySummary.Open($"Day {s.Day} closed", "The lanterns are dimmed and the door is locked.", body,
                $"Open shop  ·  Day {s.Day + 1}", StartNextDay);
        }

        private void StartNextDay()
        {
            game.CloseDay();
            waitingForNewDay = false;
            idleTimer = 2f;
            hud.SetStatus($"Day {game.Clock.Day}. The shop is open.");
        }
    }
}
