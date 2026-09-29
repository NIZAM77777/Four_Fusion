# Reference UI redesign

Implemented in Unity 6000.3.11f1 on 2026-09-28.

## Presentation

The supplied seven references guide the navy coin background, illustrated Four Fusion logo, gold primary actions, blue secondary actions, coral result/exit actions, neon borders, opponent illustrations, difficulty cards, and branded result panels.

Updated interfaces: home, opponent selection, difficulty selection, settings, help/about, gameplay HUD, pause, restart/home confirmations, loading, win, loss, and draw. The existing studio splash animation remains intact.

- `Assets/Scripts/UI/FusionUI.cs` continues using the existing UI Toolkit presentation and manager callbacks. Labels remain live and localized; artwork does not contain interactive text.
- `Assets/Resources/Fusion/Fusion.uss` defines shared spacing, control states, compact layouts, landscape layouts, RTL layouts, and scrollable settings/dialogs.
- `Assets/Scripts/UI/FusionVisuals.cs` contains reusable presentation-only surfaces, vector icons, board frame, result rays, and board socket sprite generation.
- `Assets/Resources/Fusion/Art` contains five generated raster assets derived from the user-provided visual direction: logo, backdrop, home board, local players, and AI robot. Transparent illustrations are separate from live controls. No extra package dependency was added.
- `BoardCell.cs` changes only its renderer's sprite/tint. Board dimensions, cell positions, coin renderers, movement/bounce/win animations, and drop callbacks remain owned by the existing systems.
- The two existing piece themes remain selectable and appear in the HUD, live board, and results. Gold borders/checkmarks indicate selected settings and difficulty; gold player borders indicate the active turn.
- Safe-area padding, banner reservation, width-based panel scaling, scrolling, and portrait/landscape arrangements remain supported.

No board rules, turn logic, AI search, rewards, audio managers, saved settings logic, scene-transition logic, or serialized scenes/prefabs were rewritten for this redesign.

## Verification

Validation ran in the ignored `ValidationWorkspace` copy. Its separate product name isolates test preferences from the normal game. The shipping project settings were not changed for validation.

| Check | Evidence |
| --- | --- |
| Unity Windows development build | Successful build; no script compile errors or stylesheet warnings in the final build |
| Player interaction assertions | 68 passes; empty `player-errors.txt` |
| Rules/search/reward assertions | 18 passes, including four win directions, full-board draw, AI win/block/termination/cancellation, and single reward consumption |
| Scene script references | 81 object checks across Splash, MainMenu, and GameScene; no missing scripts |
| Local play | Real column-button dispatch, animated drop lock, winning move sequence, result undo, pause/resume, home, restart cancellation, confirmed restart, rematch |
| AI play | Hard AI starts via difficulty controls, exactly one reply, human input blocked during AI turn, paired undo |
| Settings | Music/sound/volume callbacks, both skin selections, six persisted language selections |
| Result layouts | Actual local win; AI loss and draw displayed through existing manager callbacks as explicit presentation fixtures |
| Responsive rendering | 360×640, 480×900, 768×1024, 900×480; Start game visibility asserted on both phone sizes |
| Localization | Six complete catalogs with 68 unique keys each; native font files and referenced keys present; screenshots include Arabic/Urdu RTL |

The 40 actual Unity camera/UI Toolkit renders are in `Validation/Player`. They include all six language menus/settings, scrolled settings, both gameplay skins, mode/difficulty, win/loss/draw, confirmations, and screen-size variants. These are offscreen captures of the running game, not mockups.

Fresh rule and scene-reference evidence is in `Validation/ReferenceUI`. `FusionPlayerValidation` runs only in an explicitly launched Windows development player with `--fusion-qa`; it is excluded from Android and release players.

## Limits

This checkout contains local two-player and AI modes. No networking package, room/lobby implementation, host/client synchronization, disconnect UI, or separate direction-selection control was found in its active scripts. There was no corresponding online interface to reskin or exercise.

Physical Android touch/notches, live AdMob/UMP callbacks, and device performance were not verified in this pass. Reward transaction logic was tested; desktop screenshots do not certify live ads. The existing Android packaging evidence remains historical. Native-language visual inspection and catalog checks do not substitute for translation review.

## Preview

![Home](Validation/Player/menu-en.png)

![Opponent selection](Validation/Player/mode-en.png)

![Gameplay with existing red and blue pieces](Validation/Player/gameplay-red-blue.png)
