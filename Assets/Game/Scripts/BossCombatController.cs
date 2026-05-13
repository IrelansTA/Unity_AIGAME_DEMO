using System.Collections;
using UnityEngine;

public sealed class BossCombatController : MonoBehaviour
{
    public CombatActor actor;
    public CombatHitbox hitbox;
    public ActorVisualAnimator visual;
    public CombatActor target;

    public float laneTolerance = 0.35f;
    public float slamRangeX = 1.45f;
    public float throwRangeX = 4.15f;
    public float decisionCooldown = 0.68f;
    public float warningHeight = 0.08f;
    public float warningLocalY = 0.12f;
    public float projectileLocalY = 0.34f;
    public float slamWindup = 1.15f;
    public float slamWarningLocalY = 0.06f;
    public Vector2 slamWarningSize = new Vector2(2.05f, 0.48f);

    private static Sprite warningSprite;
    private static Sprite warningOvalSprite;
    private static Sprite crackSprite;
    private bool acting;
    private float cooldownTimer;
    private int nextSkillIndex;

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

    private void OnEnable()
    {
        acting = false;
        cooldownTimer = 0.55f;
    }

    private void Update()
    {
        if (actor == null || actor.IsDead || target == null || target.IsDead)
        {
            return;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        if (acting || actor.IsStunned)
        {
            return;
        }

        actor.FaceTarget(target.transform);

        Vector2 toTarget = target.transform.position - transform.position;
        bool inLane = Mathf.Abs(toTarget.y) <= laneTolerance;
        float absX = Mathf.Abs(toTarget.x);

        if (inLane && absX <= throwRangeX && cooldownTimer <= 0f)
        {
            StartNextSkill(absX);
            return;
        }

        Vector2 move = toTarget;
        if (absX <= slamRangeX)
        {
            move.x = 0f;
        }

        if (inLane)
        {
            move.y = 0f;
        }

        if (move.sqrMagnitude > 0.01f)
        {
            actor.Move(move.normalized, 0.86f);
            visual?.PlayRun();
        }
        else
        {
            visual?.PlayIdle();
        }
    }

    private void StartNextSkill(float targetDistanceX)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            int skill = nextSkillIndex;
            nextSkillIndex = (nextSkillIndex + 1) % 3;

            if (skill == 0 && targetDistanceX <= slamRangeX + 0.25f)
            {
                StartCoroutine(DoSlam());
                return;
            }

            if (skill == 1 && targetDistanceX >= 0.85f)
            {
                StartCoroutine(DoCharge());
                return;
            }

            if (skill == 2)
            {
                StartCoroutine(DoThrow());
                return;
            }
        }

        StartCoroutine(targetDistanceX <= slamRangeX + 0.25f ? DoSlam() : DoThrow());
    }

    private IEnumerator DoSlam()
    {
        acting = true;
        cooldownTimer = decisionCooldown + 0.8f;
        actor.FaceTarget(target.transform);
        visual?.PlayAttack(1);

        Vector2 impactCenter = SlamImpactCenter();
        ShowSlamWarning(impactCenter, slamWarningSize, slamWindup);
        SpawnChargeEffect(impactCenter, slamWindup);

        yield return new WaitForSeconds(slamWindup);

        if (CanAct())
        {
            hitbox.laneTolerance = laneTolerance;
            hitbox.Activate(18, new Vector2(1.05f, 0.38f), new Vector2(2.15f, 0.95f), 0.22f, 0.48f, new Vector2(1.55f, 0f), 0.16f, 0.18f);
            SpawnSlamImpact(impactCenter);
            FindObjectOfType<CameraFollow2D>()?.AddShake(0.16f, 0.2f);
        }

        yield return new WaitForSeconds(0.34f);
        EndAction();
    }

    private IEnumerator DoCharge()
    {
        acting = true;
        cooldownTimer = decisionCooldown + 0.35f;
        actor.FaceTarget(target.transform);
        visual?.PlayAttack(2);
        ShowLaneWarning(0.65f, 3.25f, warningHeight);

        yield return new WaitForSeconds(0.65f);

        if (CanAct())
        {
            hitbox.laneTolerance = laneTolerance;
            hitbox.Activate(14, new Vector2(0.68f, 0.52f), new Vector2(1.45f, 1.12f), 0.34f, 0.34f, new Vector2(1.28f, 0f), 0.13f, 0.14f);

            float elapsed = 0f;
            while (elapsed < 0.34f && CanAct())
            {
                actor.Move(new Vector2(actor.Facing, 0f), 4.05f);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.18f);
        EndAction();
    }

    private IEnumerator DoThrow()
    {
        acting = true;
        cooldownTimer = decisionCooldown + 0.5f;
        actor.FaceTarget(target.transform);
        visual?.PlayAttack(3);

        yield return new WaitForSeconds(0.45f);

        if (CanAct())
        {
            BossHammerProjectile projectile = BossHammerProjectile.Spawn(
                actor,
                target,
                HammerGeneralAssets.LoadProjectileFrames(),
                new Vector2(0.82f, ProjectileLocalY()),
                3.2f,
                12,
                laneTolerance);

            float elapsed = 0f;
            while (projectile != null && elapsed < 1.25f && CanAct())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.12f);
        EndAction();
    }

    private bool CanAct()
    {
        return actor != null && !actor.IsDead && target != null && !target.IsDead;
    }

    private void EndAction()
    {
        acting = false;
        if (CanAct())
        {
            visual?.PlayIdle();
        }
    }

    private void ShowLaneWarning(float duration, float length, float height)
    {
        var warningObject = new GameObject("BossChargeLaneWarning");
        warningObject.transform.position = new Vector3(
            transform.position.x + actor.Facing * length * 0.5f,
            GroundY() + warningLocalY,
            transform.position.z + 0.04f);
        warningObject.transform.localScale = new Vector3(length, height, 1f);

        var renderer = warningObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWarningSprite();
        renderer.color = new Color(1f, 0.08f, 0.04f, 0.48f);
        renderer.sortingOrder = 720;
        warningObject.AddComponent<BossLaneWarningPulse>().Initialize(renderer, duration, length, height, 0.04f);

        Destroy(warningObject, duration);
    }

    private void ShowSlamWarning(Vector2 center, Vector2 size, float duration)
    {
        var warningObject = new GameObject("BossSlamGroundWarning");
        warningObject.transform.position = new Vector3(center.x, center.y, transform.position.z + 0.04f);
        warningObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = warningObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWarningOvalSprite();
        renderer.color = new Color(1f, 0.08f, 0.04f, 0.5f);
        renderer.sortingOrder = 721;
        warningObject.AddComponent<BossLaneWarningPulse>().Initialize(renderer, duration, size.x, size.y, 0.12f);

        Destroy(warningObject, duration + 0.05f);
    }

    private void SpawnChargeEffect(Vector2 center, float duration)
    {
        var effectObject = new GameObject("BossSlamChargeFX");
        effectObject.transform.position = new Vector3(center.x, center.y + 0.12f, transform.position.z + 0.05f);
        ParticleSystem ps = effectObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.16f, 0.05f, 0.95f), new Color(1f, 0.72f, 0.16f, 0.88f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 36f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.72f;
        shape.radiusThickness = 0.18f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.04f, 0.28f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.82f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.08f, 0.02f), 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        SetParticleSorting(ps, 736);
        Destroy(effectObject, duration + 0.6f);
    }

    private void SpawnSlamImpact(Vector2 center)
    {
        var crackObject = new GameObject("BossGroundCrack");
        crackObject.transform.position = new Vector3(center.x, center.y - 0.01f, transform.position.z + 0.05f);
        crackObject.transform.localScale = new Vector3(1.95f, 0.62f, 1f);
        SpriteRenderer crackRenderer = crackObject.AddComponent<SpriteRenderer>();
        crackRenderer.sprite = GetCrackSprite();
        crackRenderer.color = new Color(1f, 1f, 1f, 0.98f);
        crackRenderer.sortingOrder = 734;
        crackObject.AddComponent<BossTemporarySpriteFx>().Initialize(1.6f, 0.98f, 0.05f, crackObject.transform.localScale);

        var shockObject = new GameObject("BossSlamShockwave");
        shockObject.transform.position = new Vector3(center.x, center.y + 0.02f, transform.position.z + 0.06f);
        shockObject.transform.localScale = new Vector3(0.35f, 0.08f, 1f);
        SpriteRenderer shockRenderer = shockObject.AddComponent<SpriteRenderer>();
        shockRenderer.sprite = GetWarningOvalSprite();
        shockRenderer.color = new Color(1f, 0.72f, 0.28f, 0.65f);
        shockRenderer.sortingOrder = 735;
        shockObject.AddComponent<BossTemporarySpriteFx>().Initialize(0.42f, 0.65f, 0f, new Vector3(5.2f, 1.1f, 1f));

        var smokeObject = new GameObject("BossSlamSmokeFX");
        smokeObject.transform.position = new Vector3(center.x, center.y + 0.05f, transform.position.z + 0.07f);
        ParticleSystem ps = smokeObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.38f, 0.34f, 0.3f, 0.72f), new Color(0.8f, 0.72f, 0.58f, 0.58f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 34) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.52f;
        shape.scale = new Vector3(1.7f, 0.26f, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.36f, 0.36f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        SetParticleSorting(ps, 737);
        Destroy(smokeObject, 1.5f);
    }

    private Vector2 SlamImpactCenter()
    {
        return new Vector2(transform.position.x + actor.Facing * 1.04f, GroundY() + slamWarningLocalY);
    }

    private float ProjectileLocalY()
    {
        return projectileLocalY;
    }

    private float GroundY()
    {
        return ActorGroundUtility.EstimateGroundY(actor);
    }

    private static void SetParticleSorting(ParticleSystem ps, int sortingOrder)
    {
        ParticleSystemRenderer renderer = ps == null ? null : ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static Sprite GetWarningSprite()
    {
        if (warningSprite != null)
        {
            return warningSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        warningSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return warningSprite;
    }

    private static Sprite GetWarningOvalSprite()
    {
        if (warningOvalSprite != null)
        {
            return warningOvalSprite;
        }

        Texture2D texture = new Texture2D(96, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = (x + 0.5f - texture.width * 0.5f) / (texture.width * 0.5f);
                float ny = (y + 0.5f - texture.height * 0.5f) / (texture.height * 0.5f);
                float d = nx * nx + ny * ny;
                float alpha = d <= 1f ? Mathf.Clamp01((1f - d) * 1.8f) : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        warningOvalSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
        return warningOvalSprite;
    }

    private static Sprite GetCrackSprite()
    {
        if (crackSprite != null)
        {
            return crackSprite;
        }

        Texture2D texture = new Texture2D(192, 72, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        Color dark = new Color(0.12f, 0.07f, 0.04f, 0.9f);
        Color rim = new Color(1f, 0.37f, 0.08f, 0.58f);
        Vector2Int[] path =
        {
            new Vector2Int(12, 36),
            new Vector2Int(42, 32),
            new Vector2Int(72, 38),
            new Vector2Int(102, 30),
            new Vector2Int(132, 36),
            new Vector2Int(180, 33)
        };

        for (int i = 0; i < path.Length - 1; i++)
        {
            DrawLine(texture, path[i], path[i + 1], rim, 5);
            DrawLine(texture, path[i], path[i + 1], dark, 3);
        }

        DrawBranch(texture, new Vector2Int(55, 34), new Vector2Int(40, 21), rim, dark);
        DrawBranch(texture, new Vector2Int(83, 36), new Vector2Int(96, 51), rim, dark);
        DrawBranch(texture, new Vector2Int(119, 33), new Vector2Int(107, 18), rim, dark);
        DrawBranch(texture, new Vector2Int(142, 35), new Vector2Int(157, 49), rim, dark);

        texture.Apply();
        crackSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        return crackSprite;
    }

    private static void DrawBranch(Texture2D texture, Vector2Int start, Vector2Int end, Color rim, Color dark)
    {
        DrawLine(texture, start, end, rim, 4);
        DrawLine(texture, start, end, dark, 2);
    }

    private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, Color color, int thickness)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int err = dx - dy;
        int x = start.x;
        int y = start.y;
        int radius = Mathf.Max(0, thickness / 2);

        while (true)
        {
            for (int oy = -radius; oy <= radius; oy++)
            {
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int px = x + ox;
                    int py = y + oy;
                    if (px >= 0 && py >= 0 && px < texture.width && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }

            if (x == end.x && y == end.y)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}

public sealed class BossLaneWarningPulse : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float duration;
    private float baseLength;
    private float baseHeight;
    private float scalePulse = 0.04f;
    private float elapsed;

    public void Initialize(SpriteRenderer renderer, float duration, float length, float height, float scalePulse = 0.04f)
    {
        spriteRenderer = renderer;
        this.duration = Mathf.Max(0.01f, duration);
        baseLength = length;
        baseHeight = height;
        this.scalePulse = scalePulse;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 24f);
        transform.localScale = new Vector3(baseLength * (1f + scalePulse * pulse), baseHeight * (1f + scalePulse * pulse), 1f);

        if (spriteRenderer == null)
        {
            return;
        }

        float fade = Mathf.Clamp01(1f - elapsed / duration);
        float alpha = Mathf.Lerp(0.22f, 0.58f, pulse) * Mathf.Lerp(0.65f, 1f, fade);
        spriteRenderer.color = new Color(1f, 0.08f, 0.04f, alpha);
    }
}

public sealed class BossTemporarySpriteFx : MonoBehaviour
{
    private float duration = 0.5f;
    private float initialAlpha = 1f;
    private float holdRatio;
    private Vector3 targetScale = Vector3.one;
    private Vector3 startScale = Vector3.one;
    private SpriteRenderer spriteRenderer;
    private float elapsed;

    public void Initialize(float duration, float initialAlpha, float holdRatio, Vector3 targetScale)
    {
        this.duration = Mathf.Max(0.05f, duration);
        this.initialAlpha = initialAlpha;
        this.holdRatio = Mathf.Clamp01(holdRatio);
        this.targetScale = targetScale;
        startScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.localScale = new Vector3(
            Mathf.Lerp(startScale.x, targetScale.x, t),
            Mathf.Lerp(startScale.y, targetScale.y, t),
            Mathf.Lerp(startScale.z, targetScale.z, t));

        if (spriteRenderer != null)
        {
            float fadeT = Mathf.InverseLerp(holdRatio, 1f, t);
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(initialAlpha, 0f, fadeT);
            spriteRenderer.color = color;
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }
}
