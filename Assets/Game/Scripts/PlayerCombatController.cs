using UnityEngine;

public sealed class PlayerCombatController : MonoBehaviour
{
    public CombatActor actor;
    public CombatHitbox hitbox;
    public ActorVisualAnimator visual;

    public KeyCode attackKey = KeyCode.J;
    public KeyCode dashKey = KeyCode.K;
    public KeyCode skillKey = KeyCode.L;

    public float comboWindow = 0.7f;
    public float dashDuration = 0.16f;
    public float dashSpeedMultiplier = 3.1f;
    public float dashCooldown = 0.55f;
    public float skillCooldown = 2.2f;

    private int comboStep;
    private float comboExpireTime;
    private float actionLockTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float skillCooldownTimer;
    private Vector2 dashDirection;

    public bool IsDashReady => dashCooldownTimer <= 0f;
    public bool IsSkillReady => skillCooldownTimer <= 0f;
    public float DashCooldownRemaining => dashCooldownTimer;
    public float SkillCooldownRemaining => skillCooldownTimer;
    public float DashCooldownRemaining01 => dashCooldown <= 0f ? 0f : Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    public float SkillCooldownRemaining01 => skillCooldown <= 0f ? 0f : Mathf.Clamp01(skillCooldownTimer / skillCooldown);

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
        if (actor == null || actor.IsDead)
        {
            return;
        }

        TickTimers();

        if (actor.IsStunned)
        {
            return;
        }

        Vector2 moveInput = ReadMoveInput();

        if (dashTimer > 0f)
        {
            actor.Move(dashDirection, dashSpeedMultiplier);
            visual?.PlayDash();
            return;
        }

        if (Input.GetKeyDown(skillKey) && skillCooldownTimer <= 0f && actionLockTimer <= 0f)
        {
            StartSkill();
            return;
        }

        if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0f && actionLockTimer <= 0f)
        {
            StartDash(moveInput);
            return;
        }

        if (Input.GetKeyDown(attackKey) && actionLockTimer <= 0f)
        {
            StartAttack();
            return;
        }

        if (actionLockTimer > 0f)
        {
            return;
        }

        actor.Move(moveInput);
        if (moveInput.sqrMagnitude > 0.01f)
        {
            visual?.PlayRun();
        }
        else
        {
            visual?.PlayIdle();
        }
    }

    private void TickTimers()
    {
        float deltaTime = Time.deltaTime;
        actionLockTimer = Mathf.Max(0f, actionLockTimer - deltaTime);
        dashTimer = Mathf.Max(0f, dashTimer - deltaTime);
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - deltaTime);
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - deltaTime);

        if (Time.time > comboExpireTime)
        {
            comboStep = 0;
        }
    }

    private Vector2 ReadMoveInput()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private void StartAttack()
    {
        comboStep = comboStep % 3 + 1;
        comboExpireTime = Time.time + comboWindow;

        if (comboStep == 1)
        {
            actionLockTimer = 0.26f;
            hitbox.Activate(10, new Vector2(0.82f, 0.52f), new Vector2(1.25f, 0.9f), 0.16f, 0.18f, new Vector2(0.35f, 0f), 0.08f);
        }
        else if (comboStep == 2)
        {
            actionLockTimer = 0.31f;
            hitbox.Activate(12, new Vector2(0.88f, 0.5f), new Vector2(1.35f, 0.95f), 0.17f, 0.22f, new Vector2(0.55f, 0f), 0.1f);
        }
        else
        {
            actionLockTimer = 0.42f;
            hitbox.Activate(18, new Vector2(1.02f, 0.48f), new Vector2(1.6f, 1.05f), 0.2f, 0.34f, new Vector2(1.1f, 0f), 0.13f);
        }

        visual?.PlayAttack(comboStep);
    }

    private void StartDash(Vector2 moveInput)
    {
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : new Vector2(actor.Facing, 0f);
        if (Mathf.Abs(dashDirection.x) > 0.01f)
        {
            actor.SetFacing(dashDirection.x > 0f ? 1 : -1);
        }

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        actionLockTimer = dashDuration;
        actor.StartInvulnerability(dashDuration);
        visual?.PlayDash();
    }

    private void StartSkill()
    {
        skillCooldownTimer = skillCooldown;
        actionLockTimer = 0.58f;
        hitbox.Activate(30, new Vector2(1.2f, 0.55f), new Vector2(2.25f, 1.25f), 0.28f, 0.42f, new Vector2(1.65f, 0f), 0.16f, 0.18f);
        visual?.PlaySkill();
    }
}
