# TODO

## Completed Recently

- Combat HUD and impact feedback first pass:
  - Runtime player/enemy health bars, delayed damage bars, combo text, Dash/Skill cooldown widgets.
  - Combat hit event channel, hit sparks, floating damage, hitstop, screen flash, and camera shake.
- Simple runtime wave director:
  - Reuses the existing scene enemy as wave 1.
  - Clones the current enemy template for waves 2 and 3.
  - Delays corpse cleanup and only shows victory after all waves are cleared.

## Immediate Verification

- Run Unity compile validation after the editor refreshes assets.
- Enter Play Mode in `CombatPrototype`.
- Verify wave 1 starts with the scene enemy and waves 2/3 spawn after all active enemies die.
- Verify HUD enemy health switches to the enemy being hit.
- Verify victory only appears after the final wave is cleared.
- Verify attacks only hit when actors are within the same lane tolerance.

## Next Priority: Hammer General Boss

Status: deferred until real image generation is available. Do not ship this Boss using the ordinary enemy appearance or code-drawn placeholder art.

- Generate Hammer General Boss sprite art:
  - Use `$imagegen` / `generate2dsprite` once the built-in `image_gen` tool or a valid CLI fallback API key is available.
  - Target a pixel-art side-view hammer general sprite sheet under `Assets/Resources/Boss/HammerGeneral`.
  - Desired delivery is 24 frames, 4 columns x 6 rows: idle, walk, slam, charge, throw, hurt/death.
  - Normalize to `256x256`, PPU `64`, point filter, stable feet line, and about `1.35x` normal enemy size.
- Add Boss phase to the existing wave flow:
  - After the 3 small enemy waves are cleared, wait about `1.8s`, show `WARNING` / `BOSS`, then spawn `Boss_HammerGeneral`.
  - Clone the runtime enemy template for the Boss and replace sprites, AI, stats, hurtbox, and combat controller at runtime.
  - Move victory from "small waves cleared" to "Boss dead".
- Implement Boss gameplay:
  - Add `BossCombatController` with about `260` HP, `2.4` move speed, short hurt stun, and readable attack selection.
  - Skill 1: charged hammer slam, `0.85s` windup, lane-aware heavy hit around `18` damage, strong shake.
  - Skill 2: charge rush, `0.65s` red lane warning, straight-line dash, about `14` damage.
  - Skill 3: boomerang hammer via `BossHammerProjectile`, `0.45s` windup, travels about `3.2` units and returns, about `12` damage.
- Reuse existing feedback systems:
  - Broadcast hits through `CombatEvents`.
  - Reuse `ImpactFeedbackController`, floating damage, hitstop, camera shake, and screen flash.
  - Bind the current enemy HUD to Boss health and show `BOSS HP current/max`.
- Boss acceptance checks:
  - Unity compile has no new `Error`, `Exception`, or `Assert`.
  - Clearing waves 1-3 spawns the Boss and does not trigger early victory.
  - All three Boss skills can happen and respect lane checks.
  - Boss death triggers `VICTORY` without breaking existing death/feedback flow.

## Next Gameplay Work

- Replace frozen idle frames with stable generated idle loops if animation polish is needed.
- Generate stronger, more readable dedicated hurt/death sheets for both hero and enemy.
- Split attack body animation and hit/impact FX into separate sprite layers.
- Add enemy hurt stun timing polish beyond the current first-pass timings.
- Add wave entry tells/spawn flash and clearer arena pacing.
- Add controller rebinding later; keep legacy Input Manager for now.

## Technical Debt

- Add an editor/menu validation script for sprite bbox/anchor QC.
- Add automated scene smoke validation through Unity MCP when available.
- Move combat tuning values into ScriptableObjects once the prototype stabilizes.
- Consider animation clips or a small state machine when actions grow beyond the current simple frame player.
