using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class ImpactFeedbackController : MonoBehaviour
{
    public Canvas hudCanvas;
    public CameraFollow2D cameraFollow;
    public float hitStopTimeScale = 0.08f;
    public float flashFadeSpeed = 5.5f;

    private const int SparkSortingOrder = 900;

    private Camera mainCamera;
    private Image flashImage;
    private RectTransform canvasRect;
    private Material particleMaterial;
    private static Font builtinFont;
    private float flashAlpha;
    private Color flashColor;
    private bool hitStopActive;
    private float hitStopTimer;
    private float savedTimeScale = 1f;
    private float savedFixedDeltaTime;

    public void Bind(Canvas canvas, CameraFollow2D follow)
    {
        hudCanvas = canvas;
        cameraFollow = follow;
        EnsureCanvasEffects();
    }

    private void OnEnable()
    {
        CombatEvents.HitConfirmed += HandleHitConfirmed;
    }

    private void OnDisable()
    {
        CombatEvents.HitConfirmed -= HandleHitConfirmed;
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
        {
            Destroy(particleMaterial);
        }
    }

    private void Update()
    {
        TickHitStop();
        TickFlash();
    }

    private void HandleHitConfirmed(CombatHitEvent hitEvent)
    {
        float power = Mathf.Clamp01((hitEvent.damage - 8f) / 22f);
        if (hitEvent.killed)
        {
            power = Mathf.Min(1f, power + 0.25f);
        }

        bool playerHit = hitEvent.source != null && hitEvent.source.team == CombatTeam.Player;
        Color primary = playerHit ? new Color(1f, 0.84f, 0.18f, 1f) : new Color(1f, 0.24f, 0.16f, 1f);

        SpawnHitSpark(hitEvent.worldPoint, primary, power);
        SpawnDamageNumber(hitEvent, primary, power);
        PulseScreenFlash(primary, Mathf.Lerp(0.08f, 0.18f, power));

        if (cameraFollow == null)
        {
            cameraFollow = ResolveCameraFollow();
        }

        cameraFollow?.AddShake(Mathf.Lerp(0.045f, 0.16f, power), Mathf.Lerp(0.1f, 0.18f, power));
        BeginHitStop(Mathf.Lerp(0.025f, 0.065f, power));
    }

    private void SpawnHitSpark(Vector2 worldPoint, Color color, float power)
    {
        var spark = new GameObject("RuntimeHitSpark");
        spark.transform.position = new Vector3(worldPoint.x, worldPoint.y, -0.15f);

        var particles = spark.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, Mathf.Lerp(0.15f, 0.24f, power));
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, Mathf.Lerp(4.8f, 7.2f, power));
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, Mathf.Lerp(0.1f, 0.18f, power));
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
        main.gravityModifier = 0f;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(12f, 26f, power)))
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.85f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.28f),
                new GradientColorKey(new Color(1f, 0.1f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = SparkSortingOrder;
        renderer.material = GetParticleMaterial();

        particles.Play();
        Destroy(spark, 0.7f);
    }

    private void SpawnDamageNumber(CombatHitEvent hitEvent, Color color, float power)
    {
        if (!EnsureCanvasEffects() || GetMainCamera() == null)
        {
            return;
        }

        Vector2 origin = WorldToCanvasPoint(hitEvent.worldPoint + new Vector2(0f, 0.45f));
        var damageObject = new GameObject("RuntimeDamageNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        damageObject.transform.SetParent(hudCanvas.transform, false);

        var rect = damageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(140f, 54f);
        rect.anchoredPosition = origin;

        var text = damageObject.GetComponent<Text>();
        text.raycastTarget = false;
        text.font = GetBuiltinFont();
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.fontSize = Mathf.RoundToInt(Mathf.Lerp(27f, 42f, power));
        text.text = hitEvent.damage.ToString();
        text.color = color;

        var outline = damageObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        float xDirection = Mathf.Abs(hitEvent.knockback.x) > 0.01f ? Mathf.Sign(hitEvent.knockback.x) : Random.Range(-1f, 1f);
        StartCoroutine(AnimateDamageNumber(damageObject, rect, text, origin, color, xDirection, power));
    }

    private IEnumerator AnimateDamageNumber(
        GameObject damageObject,
        RectTransform rect,
        Text text,
        Vector2 origin,
        Color baseColor,
        float xDirection,
        float power)
    {
        float duration = 0.72f;
        float elapsed = 0f;
        Vector2 drift = new Vector2(28f * xDirection, Mathf.Lerp(64f, 88f, power));

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = 1f - (1f - t) * (1f - t);
            rect.anchoredPosition = origin + drift * eased + Vector2.down * (18f * t * t);
            rect.localScale = Vector3.one * (1f + 0.28f * Mathf.Sin(Mathf.Clamp01(t * 2.4f) * Mathf.PI) * (1f - t));

            Color color = baseColor;
            color.a = t < 0.55f ? 1f : Mathf.InverseLerp(1f, 0.55f, t);
            text.color = color;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(damageObject);
    }

    private void PulseScreenFlash(Color color, float alpha)
    {
        if (!EnsureCanvasEffects())
        {
            return;
        }

        flashColor = color;
        flashAlpha = Mathf.Max(flashAlpha, alpha);
        ApplyFlashColor();
    }

    private void TickFlash()
    {
        if (flashImage == null || flashAlpha <= 0f)
        {
            return;
        }

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, flashFadeSpeed * Time.unscaledDeltaTime);
        ApplyFlashColor();
    }

    private void ApplyFlashColor()
    {
        if (flashImage == null)
        {
            return;
        }

        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
    }

    private void BeginHitStop(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (!hitStopActive)
        {
            savedTimeScale = Time.timeScale;
            savedFixedDeltaTime = Time.fixedDeltaTime;
            hitStopActive = true;
        }

        hitStopTimer = Mathf.Max(hitStopTimer, duration);
        Time.timeScale = Mathf.Min(savedTimeScale, hitStopTimeScale);
        Time.fixedDeltaTime = savedFixedDeltaTime * Time.timeScale;
    }

    private void TickHitStop()
    {
        if (!hitStopActive)
        {
            return;
        }

        hitStopTimer -= Time.unscaledDeltaTime;
        if (hitStopTimer <= 0f)
        {
            RestoreTimeScale();
        }
    }

    private void RestoreTimeScale()
    {
        if (!hitStopActive)
        {
            return;
        }

        Time.timeScale = savedTimeScale;
        Time.fixedDeltaTime = savedFixedDeltaTime;
        hitStopActive = false;
        hitStopTimer = 0f;
    }

    private bool EnsureCanvasEffects()
    {
        if (hudCanvas == null)
        {
            hudCanvas = FindObjectOfType<Canvas>();
        }

        if (hudCanvas == null)
        {
            return false;
        }

        canvasRect = hudCanvas.transform as RectTransform;
        if (flashImage == null)
        {
            var flashObject = new GameObject("RuntimeScreenFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flashObject.transform.SetParent(hudCanvas.transform, false);
            flashObject.transform.SetAsFirstSibling();

            var rect = flashObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            flashImage = flashObject.GetComponent<Image>();
            flashImage.raycastTarget = false;
            flashImage.color = Color.clear;
        }

        return canvasRect != null;
    }

    private Vector2 WorldToCanvasPoint(Vector2 worldPoint)
    {
        Vector2 screenPoint = GetMainCamera().WorldToScreenPoint(worldPoint);
        Camera uiCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : GetMainCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint);
        return localPoint;
    }

    private Camera GetMainCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        return mainCamera;
    }

    private CameraFollow2D ResolveCameraFollow()
    {
        Camera camera = GetMainCamera();
        if (camera != null && camera.TryGetComponent(out CameraFollow2D follow))
        {
            return follow;
        }

        return FindObjectOfType<CameraFollow2D>();
    }

    private static Font GetBuiltinFont()
    {
        if (builtinFont == null)
        {
            builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return builtinFont;
    }

    private Material GetParticleMaterial()
    {
        if (particleMaterial != null)
        {
            return particleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        particleMaterial = new Material(shader);
        return particleMaterial;
    }
}
