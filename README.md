# THE BLACK MARKET

*Buy across dimensions. Sell for profit. Build your empire.*

A first-person interdimensional trading simulator for mobile (Android first, iOS later), built in Unity with C#.
The full Game Design Document (GDD) is the source of truth.

**Current status:** Visual direction & trading update (v0.3.0): a stylised, lantern-lit shop seen from behind the
counter, with sellers and buyers who walk in, item inspection, haggling, buying, selling, profit, upgrades and
day cycles. Code compiles against Unity's API and 80 gameplay tests pass, but this version **has not been run in
Unity yet**: the next cloud build is its first real test. See `DEVELOPMENT_PLAN.md` and `TESTING.md`.

Preview renders (made outside Unity, so lighting is approximate): `Docs/preview/`.

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
2. Tap **+** > **New repository**. Name: `The-Black-Market`. Private is fine. **Don't** add a README or .gitignore.
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
2. **Source control:** choose **GitHub**, authorise Unity, select `The-Black-Market`, branch `main`.
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

Expected for v0.3.0: a black screen for a moment, then the shop: wooden counter in front of you, lanterns,
shelves of curiosities, and the door and windows onto a lantern-lit street. A first visitor walks in within a few
seconds. See `TESTING.md` for the full checklist.

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
| Look around | Drag a finger on the 3D view | Hold right mouse button + drag |
| Use what you look at (bell, ledger, item) | Tap it, or tap the prompt under the crosshair | Left-click it, or E |
| Turn an item while inspecting | Drag | Hold right mouse button + drag |
| Trade | Buttons on the right; item details on the left | Same |

## How to play
1. **Sellers** walk in carrying something. Their item goes on the counter with its name, rarity, your estimate of
   its value and their asking price. Tap the item (or **Inspect closely**) and turn it around: inspecting narrows the
   estimate and reveals hidden damage that shifty sellers don't mention. Then **Buy**, **Make an offer**, or decline.
2. **Buyers** ask for an item or a kind of item. **Show an item** from your storage; they make an offer. Sell, ask
   for more, show something else, or decline.
3. Haggling: every visitor has a hidden limit and limited patience (the stars in their speech bubble). Lowball
   offers annoy them. Run out of patience and they walk out.
4. Spend profit on **Upgrades**: a better appraiser's loupe (tighter estimates), more storage, and reputation
   (rarer sellers, richer buyers). The **Ledger** (book on the counter) records every deal.
5. At 18:00 the shop closes and you see the day's results. Ring the **bell** on the counter to call the next visitor sooner.

## Checking work without Unity
`Tools/README.md`: compile-check against Unity's API and run the tests (`Tools/CompileCheck/check.sh`), render the
generated art (`Tools/ArtPreview/preview.sh`) and simulate the economy (`Tools/BalanceSim`).

## Documentation
`DEVELOPMENT_PLAN.md` · `ARCHITECTURE.md` · `ASSET_CHECKLIST.md` · `TESTING.md` · `CHANGELOG.md` · `Tools/README.md`
