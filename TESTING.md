# Testing

## Honesty note
v0.3.0 was written **without Unity**. What *was* verified: all C# compiles against Unity's API (reference
assemblies, see `Tools/README.md`), the 80 gameplay tests pass, the generated art was rendered and reviewed outside
Unity (`Docs/preview/`), and the economy was simulated (`Tools/BalanceSim`). What was **not**: shader compilation,
the scene build, and anything that only happens inside Unity (lighting, UI layout on a real screen, touch). The next
cloud build is the first real test. Any error you see is a bug on my side: send me the build log lines with "error".

## How to run automated tests
- **Cloud:** a successful cloud build proves all code compiles. If your Build Automation configuration offers
  a unit-test option, turn it on to run the EditMode tests on every build.
- **With a computer:** **Window > General > Test Runner** > **EditMode** > **Run All**.

## Automated tests

| File | Tests | Covers | Status |
|---|---|---|---|
| `GameStateManagerTests` | 6 | Default state, allowed/blocked transitions, travel round trip, events only on real changes | Not yet run |
| `ServiceRegistryTests` | 6 | Register/get, missing service, duplicates, null, TryGet, unregister | Not yet run |
| `SceneNamesTests` | 3 methods (8 cases) | Scene → state mapping, Bootstrap first | Not yet run |
| `LookMathTests` | 7 | Degrees per inch, same feel on all screen densities, look direction, invert, yaw/pitch clamps, unknown DPI | Not yet run |
| `TapDetectorTests` | 7 | Tap vs long press vs drag, release far away, no double taps, cancel | Not yet run |
| `NegotiationTests` | 13 | Accept at/over limit, counters move toward offer but never past limit, insults cost double patience, walk-away, determinism, ended haggles refuse offers | ✅ pass (outside Unity) |
| `ShopGameTests` | 22 | Exact money, one coin short, repeated Buy, full storage, unaffordable offers refused before the visitor hears them, full buy→sell profit loop, selling at a loss, wrong/unowned items, budgets, upgrades, inspection and flaws, day close, seeded visitors | ✅ pass |
| `SaveTests` | 8 | Snapshot round trip, validator rejects impossible saves, missing file = new game, backup kept, corrupt save falls back to backup, invalid data never overwrites a good save | ✅ pass |
| `EconomyTests` (Wallet, Inventory, Item, Visitor, DayClock) | 25 | Wallet limits, inventory add/remove/full/duplicates, catalogue ids and models unique, values, estimates contain the true value, personalities valid, desperate vs greedy pricing, buyers want stock | ✅ pass |

## Manual checklist: Milestone 0 (phone)
| # | Check | Expected | Result |
|---|---|---|---|
| 1 | Cloud build | Status **Success**. Log contains `[CloudBuild] Pre-export setup complete.` | |
| 2 | Install APK | Installs without errors | |
| 3 | Launch | Black for a moment, then grey shop: counter in front, walls around | |
| 4 | Rotate phone | Stays landscape and flips between both landscape sides | |
| 5 | Leave app and return | Game resumes, no crash | |

## Manual checklist: Milestone 1 (phone)
| # | Check | Expected | Result |
|---|---|---|---|
| 1 | Launch | Warm-lit shop interior: counter in front, door ahead, portal device to the right | |
| 2 | Drag finger | View turns smoothly; stops at about 100° left/right; can look down at the counter | |
| 3 | Drag starting on a button | Camera does NOT move | |
| 4 | Look at counter | Crosshair grows/turns brass; **Use counter** button appears bottom-right | |
| 5 | Press Use counter | Counter panel opens; camera can't move while it's open | |
| 6 | Close | Panel closes, looking works again | |
| 7 | Tap the counter directly | Counter panel opens | |
| 8 | Inventory / Upgrades / Travel | Short message appears at the top and fades | |
| 9 | Phone with a notch | No buttons hidden under the notch | |

## Manual checklist: v0.3.0 (phone)
| # | Check | Expected | Result |
|---|---|---|---|
| 1 | Cloud build | Success; log has `[CloudBuild] Pre-export setup complete.` and no shader errors | |
| 2 | Launch | Warm, lantern-lit shop: wooden counter, leather mat, bell, ledger; shelves of objects; door and windows show a violet street with lights | |
| 3 | Look around | Drag turns the view; shelves left/right, portal on the right, banner "THE BLACK MARKET" on the left | |
| 4 | First visitor | Within ~10 s someone walks up the street, the door opens with a bell, they walk to the counter carrying an item and put it on the mat | |
| 5 | Item card | Left card: name, rarity, value range, asking price, potential profit, description | |
| 6 | Inspect | Tap the item: it lifts to the camera; dragging turns it; bar fills; result text appears; "Put it down" returns it | |
| 7 | Buy | "Buy for …": coins drop, coin sound, item slides under the counter, visitor leaves happy | |
| 8 | Haggle | "Make an offer": slider and ± buttons; low offers get counters, stars (patience) drop; too many and they walk out | |
| 9 | Buyer | A later visitor asks for an item you own; "Show an item" opens storage with 3D thumbnails; offering puts it on the mat | |
| 10 | Sell | "Sell for …": coins rise with a green "+…", toast shows profit, visitor leaves carrying it | |
| 11 | Upgrades | Bottom-left Upgrades: buying one takes coins and changes the stated effect (e.g. storage 6 → 9 slots) | |
| 12 | Ledger | Tap the book on the counter (or Ledger button): today's numbers and recent deals | |
| 13 | Bell | Tap the bell with no visitor: a visitor comes sooner | |
| 14 | Closing | At 18:00 a day summary appears; "Open shop · Day 2" continues | |
| 15 | Shifty seller | Inspecting a "Shifty" seller's item sometimes reveals a crack and the value drops | |
| 16 | Notch / rotate | Nothing important under the notch; landscape both ways | |
| 17 | Saving | Make a deal, close the app fully, reopen: same coins, stock, upgrades and day | |

## Planned critical tests (later milestones, per GDD Part 14 I)
- Buying: exact money, one coin short, repeated Buy presses, merchant out of stock
- Selling: customer budget exactly equal / below price, selling item not in inventory, double sale
- Inventory: full inventory, quantity never negative
- Negotiation: each outcome reachable, deterministic for same inputs
- Upgrade costs and effects
- Saving: missing file, corrupt file, old version migration, never overwriting a valid save
- Dimension unlock requirements
