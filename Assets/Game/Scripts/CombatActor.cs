using System;
using UnityEngine;

public sealed class CombatActor : MonoBehaviour
{
    public CombatTeam team;
    public int maxHealth = 100;
    public float moveSpeed = 4f;
    public CombatBounds bounds;
    public Hurtbox hurtbox;
    public ActorVisualAnimator visual;

    public event Action<CombatActor> HealthChanged;
    public event Action<CombatActor> Damaged;
    public event Action<CombatActor> Died;

    public int CurrentHealth { get; private set; }
    public int Facing { get; private set; } = 1;
    public bool IsDead { get; private set; }
    public bool IsStunned => stunTimer > 0f || knockbackTimer > 0f;
    public float Health01 => maxHealth <= 0 ? 0f : (float)CurrentHealth / maxHealth;

    private float stunTimer;
    private float invulnerabilityTimer;
    private float knockbackTimer;
    private Vector2 knockbackVelocity;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (hurtbox == null)
        {
            hurtbox = GetComponent<Hurtbox>();
        }

        if (visual == null)
        {
            visual = GetComponent<ActorVisualAnimator>();
        }

        if (hurtbox != null)
        {
            hurtbox.actor = this;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stunTimer = Mathf.Max(0f, stunTimer - deltaTime);
        invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);

        if (knockbackTimer > 0f)
        {
            transform.position += (Vector3)(knockbackVelocity * deltaTime);
            if (bounds != null)
            {
                transform.position = bounds.Clamp(transform.position);
            }

            knockbackTimer = Mathf.Max(0f, knockbackTimer - deltaTime);
        }
    }

    public void Move(Vector2 input, float speedMultiplier = 1f)
    {
        if (IsDead)
        {
            return;
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        if (Mathf.Abs(input.x) > 0.01f)
        {
            SetFacing(input.x > 0f ? 1 : -1);
        }

        transform.position += (Vector3)(input * moveSpeed * speedMultiplier * Time.deltaTime);
        if (bounds != null)
        {
            transform.position = bounds.Clamp(transform.position);
        }
    }

    public void SetFacing(int facing)
    {
        Facing = facing >= 0 ? 1 : -1;
    }

    public void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        float deltaX = target.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            SetFacing(deltaX > 0f ? 1 : -1);
        }
    }

    public void StartInvulnerability(float duration)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, duration);
    }

    public bool TakeHit(HitData hit)
    {
        if (IsDead || invulnerabilityTimer > 0f || hit.sourceTeam == team)
        {
            return false;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - hit.damage);
        stunTimer = Mathf.Max(stunTimer, hit.hitStun);
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, hit.invulnerability);

        if (hit.knockbackDuration > 0.001f)
        {
            knockbackTimer = hit.knockbackDuration;
            knockbackVelocity = hit.knockback / hit.knockbackDuration;
        }

        Damaged?.Invoke(this);
        HealthChanged?.Invoke(this);

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            stunTimer = 0f;
            knockbackTimer = 0f;
            visual?.PlayDeath();
            Died?.Invoke(this);
        }
        else
        {
            visual?.PlayHurt();
        }

        return true;
    }
}
