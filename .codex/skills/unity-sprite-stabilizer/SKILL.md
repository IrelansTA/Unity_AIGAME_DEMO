---
name: unity-sprite-stabilizer
description: "Use when Unity 2D sprite animation frames have jitter, size mismatch, pivot/anchor drift, transparent magenta edges, or hurt/death/run frames that jump when switching actions. Audits and normalizes generated PNG sprites while preserving Unity .meta GUID references."
---

# Unity Sprite Stabilizer

Use this skill after generating or replacing Unity 2D character sprites, especially for side-view beat-em-up actors where idle, run, attack, hurt, and death frames must switch without visible popping.

## Core Rules

- Preserve existing `.png.meta` files and GUIDs. Rewrite referenced PNGs in place instead of rebinding scene arrays unless the user explicitly wants new assets.
- Treat the canvas size, pivot, visual center, and feet/ground anchor as separate things:
  - Canvas stays fixed, usually `192x192` in this project.
  - Unity import pivot can remain `Center`.
  - Visual body center should land on a stable pixel column, here `x=95.5`.
  - Feet or grounded body bottom should land on a stable pixel row, here `bottom=149`.
- Do not normalize every action to the same bbox height. Running, crouching, hurt, and death naturally have different heights. Scale consistency is about character body scale, not equal bbox height.
- For tiny idle shimmer, freeze idle frames to one clean standing frame unless a stable generated idle loop already exists.
- For locomotion, keep four distinct silhouettes. If frames hash differently but look identical, regenerate or choose clearer source poses, then scale the whole pose consistently.
- Clear solid magenta and low-alpha magenta edges before final QC. Resizing can reintroduce nearly transparent purple pixels.

## AIGAME_DEMO Fix Pattern

The DNF-style vertical slice hit these failures:

- Run/walk frames were generated as different raw sizes, so fixed `192x192` canvases still showed visual scale changes.
- A prior fix forced run bbox height to match idle height, which made run look too large.
- Idle had four slightly different generated frames and shimmered in place.
- Hurt/death were left outside the later normalization pass, so death landed one pixel off the common ground line.
- Enemy facing was wrong because scene data marked `sourceFacesRight` as false even though the sprite art faced right.
- Hitboxes interacted across lanes because overlap ignored actor Y distance.

The repair:

- Use `Assets/Art/Generated/*/V2Aligned` as runtime targets and preserve their `.meta` files.
- Freeze idle frames to a single clean frame for now.
- Rebuild hero run from clearer run sources, scaling the whole pose to match idle body scale rather than equalizing bbox height.
- Re-anchor every runtime frame to `192x192`, `anchor_x=95.5`, `ground_bottom=149`.
- Include hurt/death in the same pass.
- Verify with an audit sheet and bbox/hash table.

## Workflow

1. Find scene-referenced sprite GUIDs and map them back to PNG paths:
   - Search `Assets/Scenes/*.unity` for `guid:`.
   - Search matching `*.png.meta` files under `Assets/Art/Generated`.
2. Generate an audit sheet before editing:
   - Checker background.
   - Vertical center line.
   - Ground line.
   - Printed bbox, center, bottom, hash per frame.
3. Normalize in place:
   - Scrub magenta and alpha <= 8 to transparent.
   - Crop non-transparent bbox.
   - Paste back to the fixed canvas using stable `anchor_x` and `ground_bottom`.
   - Freeze idle frame groups if they shimmer.
4. Re-audit:
   - Idle should be `unique=1` if frozen.
   - Run/walk should remain `unique>=4` for four-frame locomotion.
   - All grounded frames should share the same bottom line.
   - No magenta-ish non-transparent pixels should remain.
5. Only then commit. Include the runtime PNGs, source raw/regenerated assets that explain the fix, this skill, and handoff docs.

## Helper

Use `scripts/stabilize_sprites.py` for deterministic PNG normalization and audit sheet generation. Example:

```powershell
python .codex/skills/unity-sprite-stabilizer/scripts/stabilize_sprites.py `
  --folder Assets/Art/Generated/Hero/V2Aligned `
  --files hero-v2-1.png hero-v2-2.png hero-v2-3.png hero-v2-4.png `
  --freeze hero-v2-1.png:hero-v2-1.png,hero-v2-2.png,hero-v2-3.png,hero-v2-4.png `
  --anchor-x 95.5 `
  --ground-bottom 149 `
  --audit-out C:/tmp/hero_audit.png `
  --write
```
