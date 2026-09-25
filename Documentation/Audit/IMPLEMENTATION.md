# Four Fusion baseline and implementation plan

Baseline inspected before implementation: 345 non-meta assets, 37 scenes (3 shipping; remaining recovery, template and TMP examples), 68 textures. Machine-readable paths, scene objects, component GUIDs, actions, dimensions and audio import settings are in `baseline-inventory.json`. All 22 first-party C# files, three gameplay prefabs, two splash animators, package manifest/lock, Android templates, build scene list and player/render settings were inspected. No existing localization, match save, custom ScriptableObject data or test suite exists. Settings currently save only theme/master volume.

## Preserve
7 columns × 6 rows, gravity placement, four-direction win/draw detection, local two-player, three AI levels, paired undo, 3/1 free undo quotas, both existing sprite themes, rewarded undo, banner/result interstitial model. Keep vendor SDK and serialized manager/prefab identifiers. Preserve pre-existing scene/font/player-setting changes.

## Confirmed problems
- Menu/result/button wording embedded in large textures; constant-pixel canvases; no safe area, language support, distinct audio settings, help or privacy link.
- Piece placement sound emitted by both input and board; loss emitted by both UI and game. Piece movement finishes before landing bounce. Disabled buttons can emit sounds.
- Theme manager exists only in MainMenu and is destroyed on scene load, yet GameScene dereferences it. Scene-only singletons do not clear themselves.
- Drop validation lacks bounds/AI ownership protection. Undo can overlap an animation and always restores Player1 even after a Player1 result; an AI winning reply can be recreated incorrectly. Pending result/interstitial routines are not cancelled by resume.
- Easy AI can loop forever on a full board. Hard search blocks Unity and allocates a 4-element array per evaluated window plus lists throughout its tree. Heuristic threshold is used as terminal detection and is not a sound winner test.
- Ads request immediately without UMP gating. Callbacks may touch Unity off-thread, capture global board state across scenes, or grant a reward twice. Failed loads have no retry path; requested banner visibility is lost before loading.
- Long music imports decompress on load at full quality. Coins/board assets are much larger than their display footprint. TMP example Resources are included despite examples not being shipping scenes.
- Android already targets API 36, min 25, ARM64, IL2CPP and release minification. Preserve these; validate rather than arbitrarily change them. Privacy URL, consent account configuration, device/16KB testing and a signed AAB are unverified.
- No measured baseline FPS/GPU/memory/device results. Static findings are hypotheses until measured.

## Ordered implementation
1. Add reusable UI Toolkit design tokens/screens over existing gameplay; retain legacy objects for serialized compatibility and disable their rendering through the presentation bootstrap. Native Advanced Text Generator is required for shaping; fonts must be bundled with licenses.
2. Menu, mode/difficulty, HUD, result and settings layouts; safe area, scrollable settings, distinct player indicators and animations. Stable six-language data, per-language font selection, RTL direction and mirrored action rows.
3. Audio ownership, persistent services/settings, asynchronous guarded scene transitions.
4. Consent-aware ads, captured reward transactions, failure recovery; board/AI/action locks and undo turn restoration.
5. Profile preserved AI evaluator, remove inner-loop allocations, move search to a cancellable worker using copied plain data and explicit terminal tests. Retain bounded 42-piece instantiation.
6. Measured asset/configuration changes and release validation, then compile, rule/regression tests and runtime UI checks. Record unresolved external requirements honestly.

This audit covers all project asset categories and first-party code. Vendor code was inventoried and integration/configuration reviewed; it is not a line-by-line security audit of vendor binaries.
