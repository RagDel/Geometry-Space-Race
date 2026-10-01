# Source samples

Five complete production C# files from Geometry Space Race are included here, with their original bytes preserved. [origin-manifest.json](origin-manifest.json) records their upstream paths, sizes, SHA-256 hashes and the extraction commit, `d121768e52a7d6986dfa87ebe1196c1338c582f4`.

| Sample | Responsibility | Dependencies |
| --- | --- | --- |
| [SkySelection.cs](portable/src/SkySelection.cs) | Seeded texture shuffle bags with bounded random access and configurable checkpoint cadence | .NET `System` |
| [PickupState.cs](portable/src/PickupState.cs) | Rocket, Hourglass and Laser timers on the supplied active-play clock; refreshed collection and one-shot laser resolution | .NET `System`, `PowerupRules` |
| [PowerupRules.cs](portable/src/PowerupRules.cs) | Shared pickup types, durations, multipliers and limits | None beyond C# |
| [CylinderCourse.cs](unity/CylinderCourse.cs) | Seeded curved course frames, curvature and rebased world coordinates | `UnityEngine` vectors, quaternions and math |
| [DashController.cs](unity/DashController.cs) | Directional dash trajectory and three charges recharged sequentially on the committed gameplay clock | `UnityEngine` vectors and math |

## Run the portable checks

With .NET SDK 8 installed, run from the repository root:

```sh
dotnet run --project samples/portable/Verification.csproj --configuration Release
```

The console project compiles the three portable production files directly. It uses no NuGet packages; its `NuGet.Config` clears package sources. It returns a nonzero exit code if a check fails. No Unity engine replacements or alternative implementations of the sampled algorithms are used.

The checks exercise observable behavior: complete ten-texture bags and neighbor exclusion across 64 bags for three seeds, reproduction after reverse-order and maximum-checkpoint queries, ten-checkpoint cadence, seed diversity, constructor/query bounds, pickup clock consumption and reset, refresh without duration stacking, single laser resolution, earliest pending expiry, and backwards/non-finite clock rejection. The constructor's default cadence is 35; the game's sky caller configures it to 10. Both are checked explicitly.

Verification on **2026-10-01** used the existing Unity installation's **.NET SDK 8.0.318**, targeting **net8.0** in Release configuration. **12/12 checks passed with 25,289 assertions**, exit code 0. The retained [console result](verification-results.txt) is specific to this portable sample harness.

## Extraction and verification scope

The source files came from `Assets/Game/Scripts/Presentation` and `Assets/Game/Scripts/Domain` within the upstream Unity project. Hashes of the copies were compared with the original files. None of the five sources was rewritten or simplified for the showcase.

[Program.cs](portable/Program.cs), its project/configuration files and this documentation were added for the showcase. The behavior checks draw on the original `SkySelectionTests.cs` and the pickup contracts exercised by `Patch04PickupTests.cs`; they add direct API boundary checks. They are not a copy of the Unity NUnit suite.

The two `unity/` samples require a Unity project providing `UnityEngine`; they are supplied for source inspection and excluded from the portable build. No Unity Editor, PlayMode, physics, rendering, input, pause integration or platform build check was run for this extraction. `PickupState` receives a clock from its caller, so same-time and slow-motion timer checks do not establish the game's pause integration. The portable harness also does not verify Unity random-state isolation or the full procedural generation pipeline.

This directory is a selected source showcase. It omits the remaining gameplay integration, scenes, prefabs, settings, tests and game assets required to build the complete game. Source extraction does not grant rights to omitted third-party assets; see the repository's licensing notes.
