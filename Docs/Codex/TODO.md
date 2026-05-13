# TODO

## Immediate Verification

- Run Unity compile validation after the editor refreshes assets.
- Enter Play Mode in `CombatPrototype`.
- Verify hero idle no longer shimmers.
- Verify hero run has four distinct poses and does not appear larger than idle.
- Verify hero hurt and death no longer shift off the ground line.
- Verify enemy idle/walk/attack/hurt/death do not pop when switching actions.
- Verify enemy facing remains correct after scene reload.
- Verify attacks only hit when actors are within the same lane tolerance.

## Next Gameplay Work

- Replace frozen idle frames with stable generated idle loops if animation polish is needed.
- Generate stronger, more readable dedicated hurt/death sheets for both hero and enemy.
- Split attack body animation and hit/impact FX into separate sprite layers.
- Add hit spark and screen shake feedback.
- Add enemy hurt stun timing polish and death cleanup.
- Add multiple enemies and simple spawn waves.
- Add controller rebinding later; keep legacy Input Manager for now.

## Technical Debt

- Add an editor/menu validation script for sprite bbox/anchor QC.
- Add automated scene smoke validation through Unity MCP when available.
- Move combat tuning values into ScriptableObjects once the prototype stabilizes.
- Consider animation clips or a small state machine when actions grow beyond the current simple frame player.
