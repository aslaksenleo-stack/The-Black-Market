# Changelog

## [0.3.1] Saving (2026-11-21)
### Added
- The shop is saved automatically (coins, stock, upgrades, day and time, ledger) after every visit, upgrade and
  day change, and whenever the app goes to the background. Loads on start.
- `SaveData`, `SaveValidator` (rejects impossible or corrupt saves), `SaveStore` (write to temp → verify → swap,
  keeps one backup, falls back to it if the save is damaged). `ShopGame.CreateSave` / `FromSave`, `Checkpoint` event.
- 8 save tests (round trip, validation, corrupt file, backup, invalid data never overwrites a good save).

## [0.3.0] Visual direction & trading update (2026-11-21)
### Added
- **Look:** `BlackMarket/Toon` shader (banded warm light, tinted shadows, vertex-colour tint and glow, rim light)
  and `BlackMarket/Glass`; generated textures (planks, wood, brick, plaster, cobbles, cloth); one shared `Palette`;
  Alegreya Sans / Alegreya SC fonts (SIL OFL).
- **Shop:** stylised interior generated in code (`ShopEnvironment`): wooden counter with leather trading mat, bell,
  ledger, candles; shelves of real item models; display case; storage corner; portal device; banner; hanging
  lanterns and sconces; swinging entrance door; windows and door onto a lantern-lit market street.
- **Characters:** 8 stylised visitor looks (`CharacterBuilder`) with procedural walk, idle, talk and reactions
  (`CustomerAvatar`).
- **Items:** 18 items with unique models (`ItemModelBuilder`), rarity, condition, hidden flaws (visible crack).
- **Trading (plain C#):** `ShopGame` facade with atomic buy/sell, `Negotiation`, 11 personalities with dialogue,
  `VisitorGenerator`, `ValueEstimate`, `Wallet`, `ShopInventory`, `TradeLedger`, `ShopUpgrades`, `DayClock`.
- **Flow:** `VisitDirector`: sellers bring items, buyers ask for items, inspect, haggle, buy, sell, profit, leave.
- **Inspection:** lift the item to the camera and turn it (`ItemInspector`); narrows the estimate, exposes flaws.
- **UI:** new design system (`UITheme`, `UISprites`, `UIFactory`, `UIMotion`), HUD, trade panel (speech bubble,
  patience stars, item card, offer controls), inventory with 3D thumbnails, upgrades, ledger, day summary.
- **Sound:** synthesised bells, coins, chimes, footsteps and room ambience (`SoundKit`).
- **Tools/** (outside Unity): compile check + tests, art preview renderer, balance simulator.
- 60 new edit-mode tests (80 in total runnable outside Unity, plus the 14 Unity-maths tests).
### Changed
- `ShopHUD`, `UITheme`, `UIFactory`, `ShopSceneBuilder`, `ShopSceneController` rewritten. `CameraController` can
  ease toward a point (cancelled by any drag). `InteractionController` only focuses usable objects.
  `CounterInteractable` is now used for the bell and ledger. `GameManager` creates `ShopGame`.
- Player settings: Linear colour space. Cloud build fails if a game shader doesn't compile.
### Removed
- Travel button (did nothing yet), placeholder counter panel, greybox placeholder materials.
### Not yet verified
- Not run in Unity yet. Shaders and Unity-side behaviour are first exercised by the next cloud build.

## [0.2.0] Milestone 1: Shop prototype (2026-11-21)
### Added
- Touch camera look (`CameraController`, `LookMath`), tap detection (`TapDetector`), UI-aware touch handling.
- Interaction system (`IInteractable`, `InteractionController`): look-at + Interact button, or tap the object.
- `CounterInteractable` and counter panel.
- UI design system (`UITheme`, `UIFactory`, `SafeAreaFitter`) and `ShopHUD`.
- `ShopSceneController` wiring scene systems together.
- `ShopSceneBuilder`: generated greybox shop interior with placeholder materials and lighting.
- 14 new edit-mode tests (LookMath, TapDetector).
### Changed
- Generated scenes are now rebuilt on every cloud build (marker object `__GeneratedByProjectSetup`).
- Runtime assembly references `UnityEngine.UI`.

## [0.1.0] Milestone 0 complete (2026-11-20)
- First successful cloud build (#3). APK installed and launched on an Android phone: Bootstrap → Shop loads in landscape.
- Known: placeholder geometry is unlit-looking white; counter is just below the default view (camera look comes in Milestone 1).

## [0.0.3] Cloud build connected (2026-11-20)
### Changed
- Unity Build Automation target `Android` configured (Unity 6000.3.8f1, Windows Micro, debug keystore, pre-export `BlackMarket.EditorTools.CloudBuild.PreExport`, auto-build on).

## [0.0.2] No-PC cloud build pipeline (2026-11-20)
### Added
- Repository is now a complete Unity project: `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt` (6000.3.8f1).
- `CloudBuild.PreExport` hook for Unity Build Automation. Generates missing scenes and applies Android settings before each build.
- `ProjectSetup`: setup steps without dialogs, shared by the menu and the cloud build.
### Changed
- Renderer: built-in for now (URP deferred). Input: legacy for now; Input System code guarded by a package define.
- Setup now disables any extra scenes in Build Settings instead of leaving them enabled.
- Fallback Android package name `com.theblackmarket.prototype`.
- README rewritten for the phone + cloud workflow.

## [0.0.1] Milestone 0: Project foundation (2026-11-20)
### Added
- Inspection report: no existing Unity project; recommended Unity 6.3 LTS + URP (see README).
- Full `Assets/` folder structure from GDD 10.3.
- Assembly definitions: `BlackMarket.Runtime`, `BlackMarket.Editor`, `BlackMarket.Tests.EditMode`.
- Core: `GameManager` (self-creating composition root), `ServiceRegistry`, `GameStateManager`, `SceneLoader`,
  `SceneNames`, `Bootstrapper`.
- Player input: `IPlayerInput`, `MobileInputController` (touch look zones; mouse/WASD/E in editor).
- Editor menu *The Black Market > Setup*: create Bootstrap/Shop scenes, configure Android, validate setup.
- 15 edit-mode test methods (20 test cases).
- Docs: README, DEVELOPMENT_PLAN, ARCHITECTURE, ASSET_CHECKLIST, TESTING, CHANGELOG. Unity `.gitignore`.

### Not yet verified
- Compilation, Play mode and Android build. Awaiting first run in Unity.
