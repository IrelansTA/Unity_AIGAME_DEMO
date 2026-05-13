using System.Collections.Generic;
using UnityEngine;

public sealed class BossHammerProjectile : MonoBehaviour
{
    public CombatActor owner;
    public CombatActor target;
    public SpriteRenderer spriteRenderer;
    public Sprite[] spinFrames;
    public int damage = 12;
    public float speed = 8.5f;
    public float travelDistance = 3.2f;
    public float laneTolerance = 0.35f;
    public Vector2 hitboxSize = new Vector2(1.25f, 0.85f);

    private readonly HashSet<Hurtbox> hitTargets = new HashSet<Hurtbox>();
    private Vector2 direction = Vector2.right;
    private float travelled;
    private float frameTimer;
    private int frameIndex;
    private bool returning;

    public static BossHammerProjectile Spawn(
        CombatActor owner,
        CombatActor target,
        Sprite[] frames,
        Vector2 startOffset,
        float travelDistance,
        int damage,
        float laneTolerance)
    {
        var projectileObject = new GameObject("BossHammerProjectile", typeof(SpriteRenderer), typeof(BossHammerProjectile));
        var projectile = projectileObject.GetComponent<BossHammerProjectile>();
        projectile.owner = owner;
        projectile.target = target;
        projectile.spinFrames = frames;
        projectile.damage = damage;
        projectile.travelDistance = travelDistance;
        projectile.laneTolerance = laneTolerance;
        projectile.direction = owner == null ? Vector2.right : new Vector2(owner.Facing, 0f);

        Vector3 origin = owner == null
            ? Vector3.zero
            : owner.transform.position + new Vector3(startOffset.x * owner.Facing, startOffset.y, -0.05f);

        projectileObject.transform.position = origin;

        projectile.spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        projectile.spriteRenderer.sortingOrder = 860;
        projectile.spriteRenderer.sprite = FirstFrame(frames);
        projectile.spriteRenderer.flipX = owner != null && owner.Facing < 0;
        return projectile;
    }

    private void Update()
    {
        if (owner == null || owner.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Animate();
        Move();
        ScanHits();
    }

    private void Animate()
    {
        if (spriteRenderer == null || spinFrames == null || spinFrames.Length == 0)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        while (frameTimer >= 0.075f)
        {
            frameTimer -= 0.075f;
            frameIndex = (frameIndex + 1) % spinFrames.Length;
            if (spinFrames[frameIndex] != null)
            {
                spriteRenderer.sprite = spinFrames[frameIndex];
            }
        }
    }

    private void Move()
    {
        if (!returning)
        {
            float step = speed * Time.deltaTime;
            transform.position += (Vector3)(direction * step);
            travelled += step;
            if (travelled >= travelDistance)
            {
                returning = true;
            }

            return;
        }

        Vector3 returnTarget = owner.transform.position + new Vector3(0.45f * owner.Facing, transform.position.y - owner.transform.position.y, -0.05f);
        transform.position = Vector3.MoveTowards(transform.position, returnTarget, speed * 1.15f * Time.deltaTime);
        if ((transform.position - returnTarget).sqrMagnitude <= 0.03f)
        {
            Destroy(gameObject);
        }
    }

    private void ScanHits()
    {
        if (owner == null)
        {
            return;
        }

        Rect projectileRect = WorldRect();
        for (int i = 0; i < Hurtbox.ActiveHurtboxes.Count; i++)
        {
            Hurtbox hurtbox = Hurtbox.ActiveHurtboxes[i];
            if (hurtbox == null || hurtbox.actor == null || hurtbox.actor == owner)
            {
                continue;
            }

            if (hitTargets.Contains(hurtbox) || hurtbox.actor.team == owner.team)
            {
                continue;
            }

            if (Mathf.Abs(hurtbox.actor.transform.position.y - owner.transform.position.y) > laneTolerance)
            {
                continue;
            }

            Rect targetRect = ExpandRect(hurtbox.WorldRect, 0.22f, 0.14f);
            if (!projectileRect.Overlaps(targetRect))
            {
                continue;
            }

            Vector2 knockback = new Vector2(1.0f * owner.Facing, 0f);
            var hit = new HitData(owner, owner.team, damage, 0.28f, 0.14f, knockback, 0.12f);
            if (hurtbox.actor.TakeHit(hit))
            {
                hitTargets.Add(hurtbox);
                CombatEvents.RaiseHitConfirmed(new CombatHitEvent(
                    owner,
                    hurtbox.actor,
                    damage,
                    OverlapCenter(projectileRect, targetRect),
                    knockback,
                    hurtbox.actor.IsDead));
            }
        }
    }

    private Rect WorldRect()
    {
        Vector2 center = transform.position;
        return new Rect(center - hitboxSize * 0.5f, hitboxSize);
    }

    private static Vector2 OverlapCenter(Rect first, Rect second)
    {
        float xMin = Mathf.Max(first.xMin, second.xMin);
        float xMax = Mathf.Min(first.xMax, second.xMax);
        float yMin = Mathf.Max(first.yMin, second.yMin);
        float yMax = Mathf.Min(first.yMax, second.yMax);

        if (xMin <= xMax && yMin <= yMax)
        {
            return new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        }

        return first.center;
    }

    private static Rect ExpandRect(Rect rect, float x, float y)
    {
        return new Rect(rect.xMin - x, rect.yMin - y, rect.width + x * 2f, rect.height + y * 2f);
    }

    private static Sprite FirstFrame(Sprite[] frames)
    {
        if (frames == null)
        {
            return null;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return frames[i];
            }
        }

        return null;
    }
}
