# Four Fusion production upgrade

## Delivery status

The core 7×6 game is preserved. The repository now has a reusable responsive presentation system, six-language catalog, native script shaping, persistent audio/language/theme preferences, guarded gameplay and scene transitions, consent-aware ads, AI allocation fixes and release checks. **A Play Store production release is not yet certified.** Android device tests, real consent/ad lifecycle tests, native-language review and a release-key-signed bundle remain release gates. Do not treat desktop timing as Android performance.

The starting workspace already contained changes to MainMenu, GameScene, two TMP fonts, PlayerSettings and Resources metadata. These were preserved. No source scene or gameplay prefab was resaved by this implementation.

## 1. Audit summary

The pre-change inventory records 345 non-meta assets, 37 scenes and 68 textures. Three scenes ship: Splash → MainMenu → GameScene. The others are recovery copies, a render template and TMP examples. First-party scripts, scene object/component/action lists, all three gameplay prefabs, both splash animations, assets/importers, package configuration, Android templates and rendering settings were reviewed. Third-party SDK integration was reviewed; compiled vendor binaries were not independently security-audited.

Working systems retained: gravity, board dimensions, horizontal/vertical/both-diagonal wins, full-board draw detection, two-player mode, Easy/Medium/Hard AI, paired undo, 3/1 free undo quotas, two sprite themes and rewarded undo. Existing settings used PlayerPrefs. There was no match-resume save system or localization architecture.

Confirmed issues included duplicate placement/loss audio, a theme service destroyed before gameplay used it, unsafe invalid columns and overlapping undo/drop paths, incorrect turn restoration after a human result, uncancelled result routines, Easy AI full-board looping, Hard AI main-thread blocking/allocation, unsafe global-state reward callbacks, missing consent handling and image-baked English UI. See `Audit/IMPLEMENTATION.md` and `Audit/baseline-inventory.json`.

## 2–3. UI and UX

- Navy surfaces, coral primary actions, gold selection accents, consistent typography, generous spacing, labeled controls, pressed/focus/disabled states and short entrance transitions.
- Main Menu: four-piece visual identity, board illustration, Play, Settings and How to Play. Mode cards explain shared-device and AI play. Difficulty selection explicitly marks the choice before Start.
- Gameplay: board remains central, numbered/color-coded players, current-turn outline/text, legal column hit areas, free undo allowance, explicit rewarded-ad wording, pause/resume and restart/home confirmation.
- Results: distinct human/player-two/AI win and draw messages; rematch, rewarded undo and home. A draw no longer uses the loss sound.
- Settings: separate music/effects switches, volume, six native-name language buttons, original themes with swatches, privacy policy, conditional UMP privacy choices, About and version.
- Help explains the rules and undo economy. Ad/scene/privacy errors use catalog keys.
- Width-based scaling, safe-area padding, conservative banner reservation, scrollable menu/settings/modal content and portrait/landscape arrangements. The board is never mirrored for RTL, so its input indices remain stable.
- Input and game state remain in gameplay managers; the UI does not implement replacement board rules.

## 4–7. Assets, prefabs, scenes and scripts

New reusable resources: `Assets/Resources/Fusion/PanelSettings.asset`, `Fusion.uss`, `FusionTheme.tss`, `Localization.json`, five Noto font files with SIL OFL license, and service prefabs `AudioManager.prefab`, `AdsManager.prefab`, `PieceThemes.prefab`.

Existing BoardCell, RedPiece and YellowPiece prefab files are unchanged. BoardCell applies a softer tint to its authored board texture at runtime. Player/theme/result indicators display the actual original theme sprites (green/gold and red/blue), avoiding a mismatch between HUD and pieces. Existing source scenes are unchanged by this implementation; a scene-load bootstrap installs the presentation. During player builds, `FusionSceneBuildCleanup` strips superseded menu/game canvases and the old decorative backdrop from build copies. The editable originals remain in the project. Splash artwork remains intact.

Modified runtime scripts: BoardCell, BoardManager, Piece, TurnManager, AIBoard, AdManager (`AdsManager.cs`), AIManager (`AiManager.cs`), AudioManager, PieceThemeManager (`ColorManager.cs`), GameManager, InputManager, MainMenuManager, SplashManager, UIButtonSound and UIManager.

New runtime scripts: FusionUI, Localization, PlayerSettingsStore, SceneTransition, RewardTransaction and AISearch. FusionPlayerValidation is compiled only in development standalone players and starts only with `--fusion-qa`. New editor tools: FusionProjectTools/FusionReleaseGuard/FusionSceneBuildCleanup and FusionValidation. Supporting Python tools inventory assets, validate catalogs, reproduce benchmarks, compile scripts and synchronize missing Unity metadata.

## 8–10. Localization and RTL

The JSON catalog contains 66 stable keys in each of English, Arabic, Hindi, Malayalam, Tamil and Urdu. Each table declares its language code, native display name, RTL flag, font resource and key/value entries. Adding a language is data-driven: add a table and bundled font; no gameplay/save identifiers change. Missing translated values fall back to English; unknown keys log an actionable error.

`Localization.Initialize` restores the language before scene presentation; changes notify the current UI and persist through scene loads/application restarts. No localization GameObject or duplicate singleton is needed. Font assets are cached rather than recreated on every screen.

Unity 6.3 Advanced Text Generator provides HarfBuzz/ICU text shaping. Text receives explicit language direction; Arabic/Urdu action rows, alignment, cards and back controls mirror. Language buttons each use their own font and direction. Indic scripts use Noto Sans Devanagari/Malayalam/Tamil; Arabic/Urdu use Noto Sans Arabic. English branding retains the Latin font. Strings are translation drafts requiring native-speaker approval before commercial release.

## 11. Audio

Only an accepted board action emits placement audio. GameManager owns result sounds; UIManager no longer plays them. Persistent audio prevents duplicate music instances and does not restart music on every scene. Settings expose independently persisted music/effects enablement and clamped master volume. Slider changes update PlayerPrefs in memory; saves happen on settings exit, backgrounding or quit rather than synchronously on every slider tick. Short effects keep one-shot playback; draw uses the existing neutral click cue.

## 12. Ads

UMP updates at launch and ad initialization requires `CanRequestAds()`. Settings exposes privacy choices when UMP requires it. The provided HTTPS privacy policy URL is configured and its page was fetched successfully.

Unity access from SDK callbacks is marshalled through the captured Unity SynchronizationContext. Loading/showing guards prevent overlapping ads. Requested banner visibility survives asynchronous load. Failed loads retry at a bounded 30-second cadence; unavailable rewarded ads show a localized message without granting or locking a move. Interstitials occur at results at most every third counted match, and an undone/replayed result is counted once.

Reward transactions capture a board instance and revision. A reward is consumed once only after earning and closing, supports either callback order, rejects failures and cannot mutate a different scene/board. Fullscreen ads suspend game time/audio and restore them when closed/failed. Unsupported desktop SDK calls are skipped. Editor/development Android builds use Google's test units; release Android retains the original publisher unit IDs.

These code paths still require real Android tests for UMP regions, dismissal without reward, offline/no-fill, failure, background/resume and mediation-specific callback timing. Pure transaction tests cannot certify SDK/network behavior.

## 13–20. Optimization and build evidence

| Area | Before | Change | After / evidence |
|---|---|---|---|
| AI allocation | New four-value array per evaluated window; temporary valid-move lists inside evaluation | Scalar counts and indexed legal-move loops | Desktop Mono microbenchmark: 1,000 empty-board evaluations, **3,768,000 → 0 bytes** allocated. 100 seeded positions retain identical heuristic scores. |
| AI evaluation time | Same evaluator's old allocation-heavy implementation | Above allocation removal | Same desktop run: **25.8788 → 18.6807 ms** for 1,000 evaluations. Microbenchmark only, not a device/frame-rate promise. |
| AI responsiveness | Depth-eight search ran synchronously on Unity's thread | Cancellable Task on copied plain board data; Unity updates only on main thread | Runtime tests verify one Hard AI reply. Desktop opening search measured 1,585.8429 ms on a worker. No Android CPU/battery delta measured. |
| AI correctness | Heuristic magnitude used as terminal test; Easy loops indefinitely on a full board | Explicit winner/full-board tests; bounded reservoir selection | Immediate win/block/full-board/cancellation tests pass. |
| Idle scripts | Every settled piece retained Update; empty TurnManager.Update | Move coroutine exists only during motion; remove empty callbacks | Code inspection: no per-piece Update or empty turn polling. No isolated CPU delta measured. |
| Win scan | A new temporary connected-cell list for each direction | Reuse bounded list; copy winning cells only on win | Code-based allocation reduction; no per-move profiler comparison claimed. |
| Music | Long music decompressed at load, full compression quality | Streaming/background loading, Vorbis quality 0.7 | Import settings verified. Device memory/startup delta and listening QA pending. |
| SFX | Stereo/3D-oriented import configuration | Mono 2D short effects, background loading, quality 0.7 | Configuration verified; no claimed measured memory delta. |
| Textures | Android coins/board hole inherited up to 2048 import limit | 512 maximum, ETC2 RGBA8 override, mipmaps/read-write remain disabled | Import settings verified. Texture memory/bandwidth impact needs Android measurement. Existing source PNGs and sprite IDs preserved. |
| UI/GPU/assets | Large images contained buttons, wording and entire screens | Native text/shapes; build processor excludes obsolete canvases/backdrop | Player build removes those scene dependencies. Draw calls, overdraw and GPU timing are not yet profiled on Android. |
| UI updates | Image-based fixed-pixel layout, string changes scattered in UI manager | State-driven HUD updates; rebuild only on navigation/language changes; safe-area check only reapplies on change | Code/runtime checks, no measured canvas/GPU delta. |
| Settings I/O | PlayerPrefs.Save per volume-slider value change | Defer disk flush to settings exit/lifecycle | Fewer disk writes by construction; no timing claim. |
| Startup | Fixed three-second splash before menu loading | Load menu asynchronously during splash; cap splash wait at 1.2 seconds | Configured wait reduced; cold-start/device timing not measured. |
| Scene changes | Synchronous loads and duplicate/stale service risks | Single guarded async transition; persistent services; singleton cleanup and AI cancellation | Repeated menu/game service tests pass. No measured scene-load speed claim. |

No pooling was added: a 42-cell turn-based board does not justify a new pool without evidence. No shader quality/lighting changes or broad package upgrades were made without profiling. TMP example Resources remain a potential build-cleanup opportunity; they were not deleted because the starting workspace contains user-edited fonts there.

## 21. Bugs fixed

Duplicate placement/loss sounds; missing theme service after menu unload; invalid-column indexing; human playing during AI turn; duplicate AI requests; drop/undo overlap; incorrect paired-result-undo turn; stale delayed result/interstitial after undo; full-board Easy AI loop; allocation-heavy Hard AI; heuristic-based false terminal detection; cross-scene/double rewards; lost banner visibility; ad load retry absence; Windows-only UMP calls; transient undefined camera layout; default-language selection not saving; HUD still disabled at completion of an async scene transition; repeated notification-hide timers.

## 22. Remaining issues

Windows Application Control blocks Android packaging, so there is no AAB. No Android device was attached. Production readiness still depends on native-language review, hardware layout/lifecycle/performance tests, real consent/ad/offline tests, approved native-library/16KB packaging, publisher signing and Play internal testing. Existing TMP example Resources remain in the repository and warrant a separate dependency-based cleanup after reviewing the user's font edits. Detailed test limits and release gates are listed below.

## 23. Inspector and release setup

No hand-wiring of the new screen buttons is required. The service prefabs preserve the existing serialized clips/sprites and the runtime bootstrap installs the new UI. `ProjectSettings/UIToolkitProjectSettings.asset` enables advanced text. `Four Fusion > Generate Service Prefabs` regenerates service/panel resources from a preview of MainMenu without modifying an open scene.

Existing Android configuration is retained: Unity 6000.3.11f1, API 36 target, minimum API 25, ARM64, IL2CPP and release minification. `Four Fusion > Validate Android Release` checks required scenes/fonts/panel, target, backend, architecture and HTTPS policy link. `Four Fusion > Build Android App Bundle` selects non-development AAB settings. Publisher signing credentials must be configured locally; no passwords are embedded or changed.

Configure/verify the AdMob Privacy & messaging forms and publisher accounts, Play Data safety declarations, store listing/content rating, audience and release signing outside the repository. The local policy link is not a substitute for those checks.

## 24–25. Testing and measurements

Evidence is under `Documentation/Validation`: baseline/current AI benchmark, catalog validation tooling, rule checks, scene reference checks and runtime checks. The first isolated Editor session passed game assertions but also reported an internal Unity SearchDatabase exception; its overall FAIL file is retained as diagnostic evidence rather than being presented as a clean run. Windows also blocked a Burst JIT library in the Editor. That is distinct from a first-party C# compilation error.

The standalone QA driver exercises menu/mode/difficulty/settings, each language, small/tablet/landscape sizes, win/result undo, normal/AI input locking, service duplication and a Hard AI reply. Navigation uses programmatic screen/action calls; it does not replace physical touch testing. It is development-only.

Final validation evidence: **18 pure rule/reward checks passed**, **21 standalone assertions passed with zero logged runtime errors**, all **6 × 66 catalog entries passed validation**, and the Windows development player built successfully. The opt-in capture run produced **28 screenshots** at phone, small-phone, tablet and landscape sizes. Visual review confirmed native-script rendering and caught/fixed difficulty-card overflow, theme text/swatch overlap, mismatched theme indicators and crowded landscape player cards. Small native-script footer labels receive extra vertical padding. See [standalone checks](Validation/Player/player-checks.txt), [rule checks](Validation/rule-checks.txt), [screenshots](Validation/Player/) and [reproduction notes](Validation/README.md).

ADB reported **no attached Android devices**. There are no device FPS, CPU/GPU frame time, memory, GC, battery, startup, UI rebuild or ad-network measurements. A desktop idle sample is diagnostic only and has no pre-change comparison. Because its window is hidden, it is not a representative on-screen rendering benchmark. Screenshots use a real offscreen Unity camera/UI panel render, not mockups. No claim of perfect optimization is made.

An Android validation bundle was attempted with a separate validation application ID and debug signing, leaving the original publisher signing configuration untouched. Unity processed Android scripts/scenes/assets, then **Windows Application Control blocked Unity.Android.Gradle.dll (0x800711C7)** during native packaging. No AAB was produced. This run was already elevated; no application-control policy was bypassed. An administrator-approved Android toolchain is needed before packaging/16KB/native-library validation can finish. See `Validation/android-build-status.txt`.

Other unverified cases include application-relaunch persistence on Android, interruption/restart during every animation state, physical safe areas/notches, audible sound quality/counting, all translated result combinations and sustained scene-load memory growth. Settings persistence is implemented and stored-value checks pass, but these broader lifecycle scenarios are not claimed as completed tests.

## 26. Play Store readiness checklist

- [x] Original board/game modes/difficulties/themes/undo concept retained.
- [x] Reusable responsive UI and localized messages implemented.
- [x] Six complete catalogs and bundled licensed fonts.
- [x] Native shaping, RTL direction and mirrored UI implemented.
- [x] Persistent settings and guarded gameplay/scene lifecycle implemented.
- [x] Consent gating, privacy entry point and captured reward transactions implemented.
- [x] API 36 / ARM64 / IL2CPP settings retained; release build checks added.
- [ ] Native-language review and Android text/layout approval.
- [ ] Small/large notched Android hardware, pause/resume and sustained performance profiling.
- [ ] Real consent/ad/offline/mediation QA using test units.
- [ ] Final merged Android manifest, native-library 16KB compatibility and release minification tests.
- [ ] Publisher-key-signed AAB and Play internal-track/pre-launch report.
- [ ] Store listing, content rating, Data safety and publisher configuration approval.

## References

[Unity Advanced Text Generator](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-advanced-text-generator.html), [language direction](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/language-direction.html), [runtime PanelSettings requirements](https://issuetracker.unity.com/issues/24220/errors-are-logged-continuously-in-the-player-when-panelsettings-is-created-at-runtime), [AdMob UMP integration](https://developers.google.com/admob/unity/privacy), [Google Play target API requirements](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en).
