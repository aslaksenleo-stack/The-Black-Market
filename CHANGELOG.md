# Changelog

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
