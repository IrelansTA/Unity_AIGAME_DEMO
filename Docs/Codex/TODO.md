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
