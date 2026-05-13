using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorGroundShadow : MonoBehaviour
{
    public CombatActor actor;
    public Vector2 offset = new Vector2(0f, 0.03f);
    public Vector2 size = new Vector2(1.05f, 0.28f);
    public Color color = new Color(0f, 0f, 0f, 0.58f);

    private const int TextureSize = 64;
    private static Sprite shadowSprite;
    private SpriteRenderer shadowRenderer;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }

        EnsureShadow();
    }

    private void LateUpdate()
    {
        if (shadowRenderer == null)
        {
            EnsureShadow();
        }

        if (shadowRenderer == null)
        {
            return;
        }

        shadowRenderer.color = color;
        shadowRenderer.transform.position = new Vector3(
            transform.position.x + offset.x,
            ActorGroundUtility.EstimateGroundY(actor) + offset.y,
            transform.position.z + 0.03f);
        shadowRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        shadowRenderer.enabled = actor == null || !actor.IsDead;

        SpriteRenderer bodyRenderer = actor == null || actor.visual == null ? null : actor.visual.spriteRenderer;
        shadowRenderer.sortingOrder = bodyRenderer == null ? 5 : bodyRenderer.sortingOrder + 1;
    }

    private void EnsureShadow()
    {
        Transform existing = transform.Find("RuntimeGroundShadow");
        if (existing != null)
        {
            shadowRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (shadowRenderer == null)
        {
            var shadowObject = new GameObject("RuntimeGroundShadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);
            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        }

        shadowRenderer.sprite = GetShadowSprite();
        shadowRenderer.color = color;
    }

    private static Sprite GetShadowSprite()
    {
        if (shadowSprite != null)
        {
            return shadowSprite;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        var center = new Vector2((TextureSize - 1) * 0.5f, (TextureSize - 1) * 0.5f);
        float radius = (TextureSize - 2) * 0.5f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float distance01 = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance01);
                alpha = alpha * alpha * 0.85f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        shadowSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return shadowSprite;
    }
}
