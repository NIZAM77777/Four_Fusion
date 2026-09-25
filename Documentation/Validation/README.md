# Validation evidence and reproduction

Use `../PRODUCTION_UPGRADE.md` for the complete audit, implementation and release gates.

## Recorded evidence

- `rule-checks.txt`: 18 passing rules, search and reward-transaction assertions.
- `ai-benchmark.txt`: old/current evaluator measured with Unity Mono; allocations are per 1,000 evaluations, not per frame. `evaluator.json` is a separate Unity evaluator run.
- `scene-audit.txt`: shipping-scene script reference audit.
- `runtime-checks.txt`, `runtime-errors.txt`, `result.txt`: original isolated Editor run. Assertions passed but an internal Unity SearchDatabase exception caused overall FAIL; this evidence is retained.
- `Player/player-checks.txt`, `player-errors.txt`, `player-result.txt`: latest development Windows player run. A clean run has 21 PASS entries, an empty error file and PASS result.
- `Player/*.png`: 28 actual Unity camera/UI Toolkit offscreen renders. They are layout evidence, not simulated advertising/device screenshots. Settings screenshots include top and scrolled-bottom views for every language.
- `Player/desktop-profile.txt`: 120 idle frames in a hidden development window. This is not a representative visible-rendering benchmark and has no baseline or Android comparison.
- `android-build-status.txt`: Android packaging failure due to Windows Application Control; no bundle is available.

## Reproduction

Catalog validation: `python Tools/validate_catalog.py`.

AI baseline comparison: `python Tools/benchmark_ai.py`. This expects the retained original evaluator at Git HEAD and Unity's installed compiler/Mono runtime. Read the script before using it after committing a new baseline.

Unity validation tools are explicit entry points under `Assets/Editor`. Use a disposable project copy containing Assets, Packages and ProjectSettings. This delivery used the ignored `ValidationWorkspace` folder, leaving the user's open Editor and pre-existing scene changes untouched.

`FusionValidation.Run` performs rules/reference checks and the Editor play sequence. `FusionValidation.BuildWindows` builds a development player into `../.utmp/Player` using the separate product name **Four Fusion Validation**. Launch the player with `--fusion-qa --qa-output <absolute-output-directory>`, a 480×900 window and D3D11 for these captures. The driver quits after completing its checks and has a 120-second watchdog. It is excluded from Android and release players.

`Tools/compile_scripts.py` and `Tools/compile_validation_capture.py` are local compiler checks that depend on Unity-generated Library/Bee response files. The latter refreshes the development player's managed assembly; it does not rebuild resources or replace a production build. Final style/resources were built by Unity; the final managed assembly includes the last small native-script footer adjustment.

Production packaging uses **Four Fusion > Validate Android Release**, then **Four Fusion > Build Android App Bundle** in an approved Unity Android toolchain. Configure publisher signing locally. Do not ship the validation application ID/debug-signing configuration or attempt to bypass the machine's application-control policy.

Before release, test physical Android input, background/resume, relaunch persistence, real test-unit ads/consent/offline paths, native-language strings, memory/CPU/GPU/GC/startup, merged permissions, native 16KB compatibility and a publisher-signed bundle through Play internal testing.
