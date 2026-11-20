# THE BLACK MARKET

*Buy across dimensions. Sell for profit. Build your empire.*

A first-person interdimensional trading simulator for mobile (Android first, iOS later), built in Unity with C#.
The full Game Design Document (GDD) is the source of truth.

**Current status:** Milestone 0 (Project foundation). Code written, **not yet compiled**. The first cloud build is the first real test.
See `DEVELOPMENT_PLAN.md`.

---

## How this project is built (no PC needed)

This repository is a complete Unity project. You don't need Unity installed.

```
Code change pushed to GitHub
   └─► Unity Build Automation (Unity's cloud) downloads the repo
        └─► runs BlackMarket.EditorTools.CloudBuild.PreExport
             (creates missing scenes, sets Android settings)
        └─► builds an Android APK
   └─► you install the APK on your phone from the Unity Cloud website
```

Why Unity's service and not free GitHub Actions? GitHub-hosted builds need a Unity licence file that can only be
created with Unity Hub on a computer. Unity Build Automation handles the licence itself.

| Item | Value |
|---|---|
| Unity version | **6000.3.8f1** (Unity 6.3 LTS), set in `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | Built-in renderer for now (most reliable for headless builds). Switch to URP is planned for Milestone 1 or 9 |
| Input | Legacy Input Manager for now. Code is ready for the Input System package when it's added |
| Packages | Only Unity's own (`Packages/manifest.json`). No third-party dependencies |
| Android | IL2CPP, ARM64, landscape, package name `com.theblackmarket.prototype` (change before publishing) |

---

## One-time setup on your phone

Unity changes its website now and then, so button names may differ slightly. Look for the closest match.

### 1. GitHub
1. Create a free account at github.com.
2. Tap **+** > **New repository**. Name: `TheBlackMarket`. Private is fine. **Don't** add a README or .gitignore.
3. Tell Claude the repository name. Claude pushes this project to it.

### 2. Unity account and project
1. Create a free account at unity.com (Unity Personal).
2. Open **cloud.unity.com** and sign in. Create a **project** called `The Black Market`
   (it may ask you to create an organization first; any name is fine).

### 3. Unity Build Automation
1. In the project, open **DevOps** > **Build Automation** and start setup.
   Unity may ask for a payment method. Build Automation is pay-as-you-go with a monthly free allowance
   (at time of writing about 200 Windows build minutes a month), so the early builds here should stay free.
   Check the current pricing on the screen before you agree.
2. **Source control:** choose **GitHub**, authorise Unity, select `TheBlackMarket`, branch `main`.
3. **Add a build target / configuration:**
   - Platform: **Android**
   - Unity version: **6000.3.8f1** (or "use project version")
   - Machine / builder: **Windows** (the largest free allowance)
   - **Build App Bundle (AAB): off.** You want an **APK** to install directly
   - Keystore: leave empty / use the default debug keystore
   - **Auto-build on new commits: on**
   - Advanced settings > **Pre-export method name:** `BlackMarket.EditorTools.CloudBuild.PreExport`
4. Start the first build. The first one is slow (often 20–40 minutes) because nothing is cached yet.

### 4. Install on your phone
1. When the build shows **Success**, open it on cloud.unity.com on your phone and tap **Download** (or **Install**).
2. Open the downloaded `.apk`. Allow your browser to **Install unknown apps** when asked.
3. Tap **Install**, then open **The Black Market**.

Expected for Milestone 0: a black screen for a moment, then a grey placeholder shop in landscape (floor, a
counter in front of you, walls). The camera doesn't move yet. That comes in Milestone 1.

### If a build fails
Open the failed build > **Log** (or download the full log). Copy the lines containing **error** (usually red or
near the end) and send them to Claude.

---

## Working with a computer (optional, later)
If you get access to a computer, install Unity Hub + Unity 6000.3.8f1 with Android Build Support, clone this
repository and open it. The **The Black Market > Setup** menu does the same setup as the cloud build.
Tests: **Window > General > Test Runner > EditMode > Run All**.

## Controls
| Action | Phone | Editor |
|---|---|---|
| Look | Drag a finger | Hold right mouse button + drag |
| Move (marketplaces) | Virtual joystick (later) | WASD |
| Interact | On-screen button (later) | E |

## Documentation
`DEVELOPMENT_PLAN.md` · `ARCHITECTURE.md` · `ASSET_CHECKLIST.md` · `TESTING.md` · `CHANGELOG.md`
