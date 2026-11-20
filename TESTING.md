# Testing

## Honesty note
Milestone 0 code was written in an environment **without Unity or a C# compiler**. It has been reviewed carefully
but **has not been compiled or run**. The first real test is you opening the project (README steps 3–7).
Any compile error you see is a bug on my side: send me the full Console message.

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

## Planned critical tests (later milestones, per GDD Part 14 I)
- Buying: exact money, one coin short, repeated Buy presses, merchant out of stock
- Selling: customer budget exactly equal / below price, selling item not in inventory, double sale
- Inventory: full inventory, quantity never negative
- Negotiation: each outcome reachable, deterministic for same inputs
- Upgrade costs and effects
- Saving: missing file, corrupt file, old version migration, never overwriting a valid save
- Dimension unlock requirements
