using System.Collections;
using UnityEngine;

public sealed class PlayerSkillEffectPlayer : MonoBehaviour
{
    public CombatActor actor;
    public ActorVisualAnimator visual;
    public string resourcePrefix = "Hero/SkillFireBurst/skill-fire-burst-";
    public float framesPerSecond = 15f;
    public float releaseDelay = 0.1f;
    public Vector2 handLocalOffset = new Vector2(0.6f, 0.02f);
    public Vector2 localScale = new Vector2(1.05f, 0.9f);
    public int sortingOrderOffset = 5;

    private Sprite[] frames;
    private Coroutine delayedPlay;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }

        if (visual == null)
        {
            visual = GetComponent<ActorVisualAnimator>();
        }
    }

    public void Play()
    {
        if (delayedPlay != null)
        {
            StopCoroutine(delayedPlay);
        }

        if (releaseDelay > 0f)
        {
            delayedPlay = StartCoroutine(PlayAfterDelay());
            return;
        }

        SpawnEffect();
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(releaseDelay);
        delayedPlay = null;
        SpawnEffect();
    }

    private void SpawnEffect()
    {
        Sprite[] loadedFrames = LoadFrames();
        if (actor == null || actor.IsDead || loadedFrames.Length == 0)
        {
            return;
        }

        var effectObject = new GameObject("PlayerSkillFireBurstFx", typeof(SpriteRenderer), typeof(RuntimeSpriteSequence));
        effectObject.transform.position = ResolveHandPosition();
        effectObject.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

        var renderer = effectObject.GetComponent<SpriteRenderer>();
        renderer.sprite = loadedFrames[0];
        renderer.flipX = actor.Facing < 0;
        renderer.sortingOrder = ResolveSortingOrder();

        var sequence = effectObject.GetComponent<RuntimeSpriteSequence>();
        sequence.Play(loadedFrames, framesPerSecond);
    }

    private Vector3 ResolveHandPosition()
    {
        SpriteRenderer actorRenderer = visual == null ? null : visual.spriteRenderer;
        if (actorRenderer == null)
        {
            actorRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        int facing = actor == null ? 1 : actor.Facing;
        Transform origin = actorRenderer == null ? transform : actorRenderer.transform;
        Vector3 scale = origin.lossyScale;
        return origin.position + new Vector3(
            handLocalOffset.x * facing * Mathf.Abs(scale.x),
            handLocalOffset.y * Mathf.Abs(scale.y),
            -0.08f);
    }

    private int ResolveSortingOrder()
    {
        SpriteRenderer actorRenderer = visual == null ? null : visual.spriteRenderer;
        if (actorRenderer == null)
        {
            actorRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        return actorRenderer == null ? 850 : actorRenderer.sortingOrder + sortingOrderOffset;
    }

    private Sprite[] LoadFrames()
    {
        if (frames != null && frames.Length > 0)
        {
            return frames;
        }

        var loaded = new Sprite[6];
        int count = 0;
        for (int i = 0; i < loaded.Length; i++)
        {
            Sprite frame = Resources.Load<Sprite>(resourcePrefix + (i + 1));
            if (frame != null)
            {
                loaded[count] = frame;
                count++;
            }
        }

        frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = loaded[i];
        }

        return frames;
    }
}

public sealed class RuntimeSpriteSequence : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameDuration = 0.08f;
    private float timer;
    private int frameIndex;

    public void Play(Sprite[] sequenceFrames, float framesPerSecond)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = sequenceFrames;
        frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        timer = 0f;
        frameIndex = 0;

        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            frameIndex++;
            if (frameIndex >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
