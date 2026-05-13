using UnityEngine;

public sealed class EnemyCombatController : MonoBehaviour
{
    public CombatActor actor;
    public CombatHitbox hitbox;
    public ActorVisualAnimator visual;
    public CombatActor target;

    public float attackRangeX = 1.15f;
    public float laneTolerance = 0.32f;
    public float attackCooldown = 1.1f;

    private float cooldownTimer;
    private float actionLockTimer;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }

        if (hitbox == null)
        {
            hitbox = GetComponent<CombatHitbox>();
        }

        if (visual == null)
        {
            visual = GetComponent<ActorVisualAnimator>();
        }
    }

    private void Update()
    {
        if (actor == null || actor.IsDead || target == null || target.IsDead)
        {
            return;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        actionLockTimer = Mathf.Max(0f, actionLockTimer - Time.deltaTime);

        if (actor.IsStunned || actionLockTimer > 0f)
        {
            return;
        }

        actor.FaceTarget(target.transform);

        Vector2 toTarget = target.transform.position - transform.position;
        bool inLane = Mathf.Abs(toTarget.y) <= laneTolerance;
        bool inRange = Mathf.Abs(toTarget.x) <= attackRangeX;

        if (inLane && inRange && cooldownTimer <= 0f)
        {
            StartAttack();
            return;
        }

        Vector2 move = toTarget;
        if (inRange)
        {
            move.x = 0f;
        }

        if (inLane)
        {
            move.y = 0f;
        }

        if (move.sqrMagnitude > 0.01f)
        {
            actor.Move(move.normalized, 0.72f);
            visual?.PlayRun();
        }
        else
        {
            visual?.PlayIdle();
        }
    }

    private void StartAttack()
    {
        cooldownTimer = attackCooldown;
        actionLockTimer = 0.55f;
        hitbox.Activate(8, new Vector2(0.78f, 0.52f), new Vector2(1.25f, 0.9f), 0.18f, 0.25f, new Vector2(0.7f, 0f), 0.12f);
        visual?.PlayAttack(1);
    }
}
