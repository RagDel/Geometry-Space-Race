# Verification and limits

This page separates historical full-project evidence from checks runnable in this public repository. It does not report a new full Unity test run.

## Recorded private-project results

| Scope | Recorded result | What it establishes |
| --- | --- | --- |
| 0.5 EditMode regression | 317 passed, 0 failed, 0 skipped | Domain, progression, settings, geometry, and other source-level checks |
| 0.5 PlayMode regression | 282 passed, 0 failed, 0 skipped | Automated Unity scene/runtime integration checks |
| 0.5 menu-layout review | 120 layout profiles and 16 captures | Scoped menu fit and reviewed in-engine screenshots |
| 0.5.1 follow-up | Nine settings EditMode checks; selected PlayMode checks and focused rechecks passed | Verification of the changed settings, camera, and roll behavior; no repeated full suite |
| 0.5.1 Windows | Build succeeded; 10-second startup smoke reported zero errors and 760 stable scene objects | Build/startup and the declared functional workload |
| 0.5.1 Android | Export/assembly and package checks passed for version code 10, ARM64/IL2CPP, API 26–36 | Static development-package identity, signature/alignment, and notices |

The 0.5.1 PlayMode record contains an initial failing assertion that assumed the old Chase distance. It was updated for the requested camera change, and the two Distant cases passed on recheck. This is reported as focused follow-up verification, not a clean initial full-suite pass.

[Evidence summary](evidence-summary.json) records selected result fields and hashes of the underlying private evidence files. Raw logs, local machine paths, and full diagnostic payloads are excluded. Hashes preserve provenance; they do not make the private evidence independently reproducible here.

## Checks in this repository

The [portable C# harness](../samples/README.md) compiles the unchanged SkySelection, PickupState, and PowerupRules production files. It exercises deterministic appearance selection and pickup timing through their public APIs. The Unity course and Dash samples are outside that build.

These checks validate the published sample scope. They do not run the complete game, physics, UI, audio, Android player, or the historical 599-test Unity suite.

## Outstanding acceptance

Physical Android and controller use, subjective listening, and controlled visible rendered performance/endurance remain open. The available startup smoke has no valid GPU timing and does not certify a frame-rate target. The earlier 0.5 generation-preparation p95 was 1.0595 ms, above its strict 1 ms target.

The screenshots are staged in-engine verification samples, including a simulated phone layout. They are not screenshots from an executed Android build. The private Windows/Android packages are development previews and are not distributed by this repository.
