# Geometry Space Race

A 3D arcade flight game built with Unity and C#. Fly through a procedural space tunnel, avoid moving geometric obstacles, collect powerups, and upgrade across three spacecraft.

**Portfolio showcase · development preview 0.5.1 · Windows and Android · solo-directed, AI-assisted development**

![Geometry Space Race flight view, version 0.5.1](assets/screenshots/flight-0.5.1.png)

[Gameplay and screenshots](docs/GAMEPLAY.md) · [Architecture](docs/ARCHITECTURE.md) · [C# source samples](samples/README.md) · [Verification](docs/VERIFICATION.md)

## The game

Each run starts with three lives and the first spacecraft. Survive successive levels to install modules, choose upgrades, and reach the white and red craft. Nitro, Phase, EMP, and a charged directional Dash give the player different ways to navigate the course. Hearts restore lives; Rocket, Hourglass, and Laser pickups change the immediate flight challenge.

The presentation includes five camera views, changing skies and tunnel surfaces, spacecraft exhaust and wing trails, and separate desktop and touch layouts. Keyboard, gamepad, and Android touch input share the same underlying run rules.

## Engineering highlights

| Challenge | Implemented approach |
| --- | --- |
| Procedural obstacles must leave a traversable route | Seeded generation reserves a corridor and bounds placement attempts; moving obstacles receive separate collision checks |
| Long runs must keep a bounded working set | Eight chunk slots, a 344-hazard pool, and floating-origin rebasing reuse the active world |
| Curved flight must remain stable at large distances | Analytic course frames and double-precision origin subtraction preserve local geometry — [course source](samples/unity/CylinderCourse.cs) |
| Slow motion and pause must preserve effect timing | Pickup timers use the committed active-play clock separately from movement scaling — [pickup source](samples/portable/src/PickupState.cs) |
| Dash must feel smooth without skipping collision paths | A quintic trajectory integrates displacement over partial steps — [Dash source](samples/unity/DashController.cs) |
| Visual variation must not alter gameplay randomness | Seeded shuffle bags support reproducible, independent appearance queries — [selection source](samples/portable/src/SkySelection.cs) |

The [architecture case study](docs/ARCHITECTURE.md) explains how these systems fit together. Five production C# files are published unchanged, with their origins and hashes recorded. The portable sample includes an executable verification harness; the two Unity samples require UnityEngine.

## Stack

Unity **6000.6.2f1**, Universal Render Pipeline **17.6.0**, C#, Input System **1.20.0**, uGUI/TextMeshPro, Unity Test Framework, and Blender for asset preparation. The private project produces Windows x64/Mono and Android ARM64/IL2CPP development previews.

## My role

I define the gameplay requirements and scope, direct visual and asset choices, review the experience, and guide testing and iteration. Codex assists with implementation, debugging, tests, documentation, and asset production. This is an AI-assisted project; the portfolio does not imply that I manually wrote every line or authored every asset.

## Status and verification

The **0.5 baseline** passed **317 EditMode and 282 PlayMode tests**, plus 120 menu-layout profiles. Version **0.5.1** received focused follow-up checks, successful Windows and Android packaging, and a Windows startup smoke. Those are recorded private-project results; the full suite was not rerun for 0.5.1.

Physical Android/controller acceptance, listening checks, and controlled rendered performance remain open. [Evidence and limitations](docs/VERIFICATION.md).

This repository publishes a case study, in-engine screenshots, and selected source. Game binaries, the complete Unity checkout, music, and raw assets are outside its scope. [Credits and publication scope](docs/CREDITS.md).
