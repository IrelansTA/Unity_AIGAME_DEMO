using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorHitFlash : MonoBehaviour
{
    public CombatActor actor;
    public ActorVisualAnimator visual;
    public float flashDuration = 0.09f;
    public Color flashColor = Color.white;
    public int sortingOrderOffset = 35;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static Material sharedFlashMaterial;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer flashRenderer;
    private MaterialPropertyBlock propertyBlock;
    private float timer;
    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();
        EnsureOverlay();
        SetOverlayVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        SetOverlayVisible(false);
    }

    private void LateUpdate()
    {
        if (timer <= 0f)
        {
            SetOverlayVisible(false);
            return;
        }

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        UpdateOverlay(Mathf.Clamp01(timer / Mathf.Max(0.01f, flashDuration)));
    }

    private void HandleDamaged(CombatActor damagedActor)
    {
        if (damagedActor != actor || actor == null || actor.team != CombatTeam.Enemy)
        {
            return;
        }

        timer = flashDuration;
        UpdateOverlay(1f);
    }

    private void ResolveReferences()
    {
        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }

        if (visual == null)
        {
            visual = GetComponent<ActorVisualAnimator>();
        }

        sourceRenderer = visual == null ? null : visual.spriteRenderer;
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Subscribe()
    {
        if (actor == null || subscribed)
        {
            return;
        }

        actor.Damaged += HandleDamaged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (actor == null || !subscribed)
        {
            return;
        }

        actor.Damaged -= HandleDamaged;
        subscribed = false;
    }

    private void EnsureOverlay()
    {
        if (sourceRenderer == null)
        {
            ResolveReferences();
        }

        if (sourceRenderer == null)
        {
            return;
        }

        Transform existing = sourceRenderer.transform.Find("RuntimeHitFlashOverlay");
        if (existing != null)
        {
            flashRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (flashRenderer == null)
        {
            var overlay = new GameObject("RuntimeHitFlashOverlay", typeof(SpriteRenderer));
            overlay.transform.SetParent(sourceRenderer.transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;
            flashRenderer = overlay.GetComponent<SpriteRenderer>();
        }

        Material material = GetFlashMaterial();
        if (material != null)
        {
            flashRenderer.sharedMaterial = material;
        }
    }

    private void UpdateOverlay(float alpha01)
    {
        EnsureOverlay();
        if (sourceRenderer == null || flashRenderer == null || sourceRenderer.sprite == null)
        {
            SetOverlayVisible(false);
            return;
        }

        flashRenderer.sprite = sourceRenderer.sprite;
        flashRenderer.flipX = sourceRenderer.flipX;
        flashRenderer.flipY = sourceRenderer.flipY;
        flashRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        flashRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
        flashRenderer.maskInteraction = sourceRenderer.maskInteraction;
        flashRenderer.enabled = true;

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Color color = flashColor;
        color.a *= Mathf.SmoothStep(0f, 1f, alpha01);
        propertyBlock.SetTexture(MainTexId, sourceRenderer.sprite.texture);
        propertyBlock.SetColor(FlashColorId, color);
        flashRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (flashRenderer != null)
        {
            flashRenderer.enabled = visible;
        }
    }

    private static Material GetFlashMaterial()
    {
        if (sharedFlashMaterial != null)
        {
            return sharedFlashMaterial;
        }

        sharedFlashMaterial = Resources.Load<Material>("Materials/SpriteWhiteFlash");
        if (sharedFlashMaterial != null)
        {
            return sharedFlashMaterial;
        }

        Shader shader = Shader.Find("AIGame/SpriteWhiteFlash");
        if (shader == null)
        {
            return null;
        }

        sharedFlashMaterial = new Material(shader)
        {
            name = "RuntimeSpriteWhiteFlash"
        };
        return sharedFlashMaterial;
    }
}
