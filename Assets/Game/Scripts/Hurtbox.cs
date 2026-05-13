using System.Collections.Generic;
using UnityEngine;

public sealed class Hurtbox : MonoBehaviour
{
    public static readonly List<Hurtbox> ActiveHurtboxes = new List<Hurtbox>();

    public CombatActor actor;
    public Vector2 offset = new Vector2(0f, 0.55f);
    public Vector2 size = new Vector2(0.75f, 1.25f);

    public Rect WorldRect
    {
        get
        {
            var center = (Vector2)transform.position + offset;
            return new Rect(center - size * 0.5f, size);
        }
    }

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }
    }

    private void OnEnable()
    {
        if (!ActiveHurtboxes.Contains(this))
        {
            ActiveHurtboxes.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveHurtboxes.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.25f, 0.85f);
        var rect = WorldRect;
        Gizmos.DrawWireCube(rect.center, rect.size);
    }
}
