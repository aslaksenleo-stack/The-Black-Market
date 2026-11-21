# Tools (not part of the game)

Everything here lives outside `Assets/`, so Unity ignores it. These tools let the project be checked on a
Linux machine without Unity (needs the .NET 8 SDK; the art preview also needs Node and Playwright's Chromium).

## CompileCheck: compile against Unity's API and run the tests
```
Tools/CompileCheck/check.sh
```
Downloads Unity *reference* assemblies from nuget.org (`fetch-refs.sh`, cached in `Tools/.cache`), compiles
`Assets/Scripts` (runtime) and `Assets/Editor`, and runs the edit-mode tests with NUnit.

Limits (be honest about what a pass means):
- The reference assemblies are Unity 2021.3 (runtime) and 2021.1 (editor), not 6000.3. APIs added later show as
  false errors (`NamedBuildTarget` is filtered out); editor-only `UnityEngine` members are missing.
- The assemblies are API stubs: tests that execute Unity maths (`LookMathTests`, `TapDetectorTests`) can't run
  here and are skipped. All gameplay logic is plain C# precisely so it can be tested this way.
- Shaders are not compiled. The cloud build validates them (`ProjectSetup.Validate`).

## ArtPreview: see the generated art
```
Tools/ArtPreview/preview.sh   # writes Tools/ArtPreview/out/*.png
```
Runs the real `MeshBuilder`, `CharacterBuilder`, `ItemModelBuilder`, `ShopEnvironment` and `TexturePainter`
code against a small UnityEngine maths shim (`shim/UnityShim.cs`), then renders with three.js and a shader that
approximates `Assets/Shaders/Toon.shader`. Lighting is close but not identical to Unity's.

## BalanceSim: check the economy
```
cd Tools/BalanceSim && dotnet run -c Release
```
Plays 40 seeded weeks with a careful trader and a naive one (always accepts the asking price). At v0.3.0:
careful ≈ doubles net worth in 7 days; naive goes broke. Re-run after changing `PersonalityCatalog` or upgrades.
