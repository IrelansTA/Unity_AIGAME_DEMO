using UnityEngine;

public static class ActorGroundUtility
{
    public static float EstimateGroundY(CombatActor actor)
    {
        SpriteRenderer renderer = actor == null || actor.visual == null ? null : actor.visual.spriteRenderer;
        if (renderer == null || renderer.sprite == null)
        {
            return actor == null ? 0f : actor.transform.position.y - 0.8f;
        }

        return EstimateGroundY(renderer);
    }

    public static float EstimateGroundY(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return 0f;
        }

        Sprite sprite = renderer.sprite;
        float pixelsPerUnit = Mathf.Max(1f, sprite.pixelsPerUnit);
        float bottomInsetPixels = Mathf.Max(0f, sprite.textureRect.y - sprite.rect.y);

        if (bottomInsetPixels <= 0.5f)
        {
            bottomInsetPixels = sprite.rect.height <= 200f
                ? sprite.rect.height * 0.22f
                : sprite.rect.height * 0.05f;
        }

        return renderer.bounds.min.y + bottomInsetPixels / pixelsPerUnit * Mathf.Abs(renderer.transform.lossyScale.y);
    }
}
