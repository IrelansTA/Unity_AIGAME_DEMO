using UnityEngine;

public readonly struct HitData
{
    public readonly CombatActor source;
    public readonly CombatTeam sourceTeam;
    public readonly int damage;
    public readonly float hitStun;
    public readonly float invulnerability;
    public readonly Vector2 knockback;
    public readonly float knockbackDuration;

    public HitData(
        CombatActor source,
        CombatTeam sourceTeam,
        int damage,
        float hitStun,
        float invulnerability,
        Vector2 knockback,
        float knockbackDuration)
    {
        this.source = source;
        this.sourceTeam = sourceTeam;
        this.damage = damage;
        this.hitStun = hitStun;
        this.invulnerability = invulnerability;
        this.knockback = knockback;
        this.knockbackDuration = knockbackDuration;
    }
}
