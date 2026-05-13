using System;
using UnityEngine;

public readonly struct CombatHitEvent
{
    public readonly CombatActor source;
    public readonly CombatActor target;
    public readonly int damage;
    public readonly Vector2 worldPoint;
    public readonly Vector2 knockback;
    public readonly bool killed;

    public CombatHitEvent(
        CombatActor source,
        CombatActor target,
        int damage,
        Vector2 worldPoint,
        Vector2 knockback,
        bool killed)
    {
        this.source = source;
        this.target = target;
        this.damage = damage;
        this.worldPoint = worldPoint;
        this.knockback = knockback;
        this.killed = killed;
    }
}

public static class CombatEvents
{
    public static event Action<CombatHitEvent> HitConfirmed;

    public static void RaiseHitConfirmed(CombatHitEvent hitEvent)
    {
        HitConfirmed?.Invoke(hitEvent);
    }
}
