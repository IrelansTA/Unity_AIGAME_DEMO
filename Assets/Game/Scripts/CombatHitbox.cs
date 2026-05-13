using System.Collections.Generic;
using UnityEngine;

public sealed class CombatHitbox : MonoBehaviour
{
    public CombatActor owner;
    public bool drawDebug = true;
    public float laneTolerance = 0.35f;

    private readonly HashSet<Hurtbox> hitTargets = new HashSet<Hurtbox>();
    private Vector2 offset;
    private Vector2 size;
    private Vector2 knockback;
    private int damage;
    private float activeTimer;
    private float hitStun;
    private float invulnerability;
    private float knockbackDuration;

    private bool IsActive => activeTimer > 0f;

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponent<CombatActor>();
        }
    }

    private void Update()
    {
        if (!IsActive || owner == null || owner.IsDead)
        {
            return;
        }

        activeTimer -= Time.deltaTime;
        Scan();
    }

    public void Activate(
        int damage,
        Vector2 offset,
        Vector2 size,
        float duration,
        float hitStun,
        Vector2 knockback,
        float knockbackDuration,
        float invulnerability = 0.12f)
    {
        this.damage = damage;
        this.offset = offset;
        this.size = size;
        this.hitStun = hitStun;
        this.knockback = knockback;
        this.knockbackDuration = knockbackDuration;
        this.invulnerability = invulnerability;
        activeTimer = duration;
        hitTargets.Clear();
        Scan();
    }

    private Rect WorldRect()
    {
        float signedOffsetX = offset.x * owner.Facing;
        var center = (Vector2)transform.position + new Vector2(signedOffsetX, offset.y);
        return new Rect(center - size * 0.5f, size);
    }

    private void Scan()
    {
        Rect hitRect = WorldRect();
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

            if (!hitRect.Overlaps(hurtbox.WorldRect))
            {
                continue;
            }

            var signedKnockback = new Vector2(knockback.x * owner.Facing, knockback.y);
            var hit = new HitData(owner, owner.team, damage, hitStun, invulnerability, signedKnockback, knockbackDuration);
            if (hurtbox.actor.TakeHit(hit))
            {
                hitTargets.Add(hurtbox);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug || owner == null || !IsActive)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.85f);
        Rect rect = WorldRect();
        Gizmos.DrawWireCube(rect.center, rect.size);
    }
}
