# TODO

## Completed Recently

- Hero skill FX first pass:
  - Replaced the first-pass chi burst with a 6-frame hand-aligned flame release under `Assets/Resources/Hero/SkillFireBurst`.
  - `PlayerSkillEffectPlayer` now delays the FX slightly, anchors it to the skill hand offset, and uses custom sprite pivots so the flame nozzle stays on the hand.
  - `PlayerCombatController.StartSkill()` plays the sequence when `L` skill fires.
  - QC pass confirmed no edge-touch frames and isolated Unity screenshot showed the flame aligned to the hero hand.
- Hammer General Boss first pass:
  - Generated real pixel-art Boss frames under `Assets/Resources/Boss/HammerGeneral`.
  - Added `BossCombatController`, `BossHammerProjectile`, and `HammerGeneralAssets`.
  - After the 3 small enemy waves, the director now shows `WARNING`, spawns `Boss_HammerGeneral`, and delays victory until Boss death.
  - Boss has 260 HP, larger hurtbox, Boss HUD label, hammer slam, charge rush with red lane warning, and boomerang hammer projectile.
  - Lowered the boomerang hammer projectile spawn height from `0.50` to `0.34` so it leaves closer to the Boss hand instead of floating high.
  - Reprocessed Boss and projectile sprites from the raw magenta sheets with stronger despill/edge cleanup.
  - Throw frames 19/20 now reuse the empty-hand release pose so the Boss does not visibly hold a hammer while the projectile is out.
  - Boss ground-slam particle systems now assign an explicit URP particle material instead of relying on the renderer default shader.
- Runtime presentation/GM first pass:
  - Every `CombatActor` now gets a darker, wider semi-transparent oval ground shadow at runtime.
  - Added Tab-toggled Chinese IMGUI GM display through `GmToolController`; it does not create Canvas UI controls.
  - GM settings serialize to `Application.persistentDataPath/gm-tool-settings.json`.
  - First GM option: player invincibility.
  - Added GM playtest buttons for restore player HP, defeat current enemy, and jump directly to the Hammer General Boss.
- Enemy/Boss hit flash first pass:
  - Enemy-team `CombatActor` instances automatically add `ActorHitFlash`.
  - `ActorHitFlash` listens to `CombatActor.Damaged` and renders a short white silhouette overlay with `AIGame/SpriteWhiteFlash`.
  - The shader samples the active sprite texture alpha through a material property block, so the flash follows the current enemy/Boss frame.
- Combat HUD and impact feedback first pass:
  - Runtime player/enemy health bars, delayed damage bars, combo text, Dash/Skill cooldown widgets.
  - Combat hit event channel, hit sparks, floating damage, hitstop, screen flash, and camera shake.
- Simple runtime wave director:
  - Reuses the existing scene enemy as wave 1.
  - Clones the current enemy template for waves 2 and 3.
  - Delays corpse cleanup and only shows victory after all waves are cleared.

## Immediate Verification

- Unity compile validation after asset refresh: no new `Error`, `Exception`, or `Assert` observed.
- Boss asset/import validation: 24 Boss frames and 4 projectile frames load from `Resources`, PPU `64`, point filter.
- MCP manual validation: runtime Boss clone configures to `260/260` HP with `BossCombatController`, Boss sprites, and projectile sprites.
- Runtime probe validation:
  - Player and active enemy receive `ActorGroundShadow`.
  - `GmToolController` is attached by `GameSession`.
  - With GM invincibility enabled, a 999-damage enemy hit is rejected and player HP stays `120/120`.
- Compile smoke after GM playtest tools:
  - `AssetDatabase.Refresh()` completed.
  - Dynamic compile probe reached `CombatActor.RestoreToFullHealth()` successfully.
- Boss hammer height validation:
  - Isolated screenshot of `BossProjectileHeightPreview` confirmed the hammer now aligns near the throw hand.
  - Post-fix probe loaded Boss/projectile sprites and confirmed `projectileLocalY=0.34`.
- Hero skill FX / Boss shader validation:
  - `SkillFireBurst` imports are Sprite, PPU `64`, point-filtered, uncompressed, custom pivot `(0.21875, 0.5)`.
  - Probe loaded 6/6 FX frames, spawned the runtime skill effect, and resolved Boss slam particles to `Universal Render Pipeline/Particles/Unlit`.
  - Isolated screenshot of `CodexFxVerificationPreview` confirmed hand-aligned flame, darker/wider shadow, and alpha-shaped enemy white flash.
- Still needs hands-on Play Mode pass in `CombatPrototype`:
  - Verify wave 1 starts with the scene enemy and waves 2/3 spawn after all active enemies die.
  - Verify `WARNING` / `BOSS` timing feels good.
  - Verify HUD enemy health switches to the enemy being hit and shows `BOSS HP` for the Boss.
  - Verify victory only appears after the Boss is dead.
  - Verify attacks only hit when actors are within the same lane tolerance.

## Next Priority: Boss Playtest And Tuning

Status: first pass implemented with generated art. Next work is tuning, animation cleanup, and a real hands-on combat pass.

- Use the Tab GM panel to accelerate the pass:
  - Toggle player invincibility when observing Boss attacks.
  - Use "击败当前敌人" to advance waves quickly.
  - Use "直接召唤Boss" to jump straight into Boss tuning.
- Play the full wave sequence at normal speed and tune:
  - Boss movement speed, decision cooldown, and skill rotation readability.
  - Slam/charge/throw hitbox offsets and lane tolerance.
  - Charge warning length/opacity and spawn timing.
  - Boss HP/time-to-kill relative to the current player combo and skill damage.
- Clean up Boss animation frames if needed:
  - Check the reprocessed Boss edges in Game View against the training hall floor.
  - If the throw still reads oddly, generate dedicated no-hammer throw body frames rather than reusing the empty-hand release pose.
- Consider adding a dedicated Boss intro flash or screen shake once pacing feels right.

## Next Gameplay Work

- Replace frozen idle frames with stable generated idle loops if animation polish is needed.
- Generate stronger, more readable dedicated hurt/death sheets for both hero and enemy.
- Split attack body animation and hit/impact FX into separate sprite layers.
- Add enemy hurt stun timing polish beyond the current first-pass timings.
- Add wave entry tells/spawn flash and clearer arena pacing.
- Tune hero skill flame delay/offset after a hands-on Play Mode pass if the impact feels too early, too far forward, or too small.
- Add controller rebinding later; keep legacy Input Manager for now.

## Technical Debt

- Add an editor/menu validation script for sprite bbox/anchor QC.
- Add automated scene smoke validation through Unity MCP when available.
- Move combat tuning values into ScriptableObjects once the prototype stabilizes.
- Consider animation clips or a small state machine when actions grow beyond the current simple frame player.
