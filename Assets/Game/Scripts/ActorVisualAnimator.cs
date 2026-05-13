using UnityEngine;

public sealed class ActorVisualAnimator : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public CombatActor actor;
    public bool sourceFacesRight = true;
    public float framesPerSecond = 8f;

    public Sprite[] idle;
    public Sprite[] run;
    public Sprite[] attack1;
    public Sprite[] attack2;
    public Sprite[] attack3;
    public Sprite[] dash;
    public Sprite[] skill;
    public Sprite[] hurt;
    public Sprite[] death;

    private Sprite[] currentFrames;
    private int frameIndex;
    private float frameTimer;
    private bool loop;
    private string currentKey;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (actor == null)
        {
            actor = GetComponent<CombatActor>();
        }

        PlayIdle();
    }

    private void Update()
    {
        UpdateFacing();
        UpdateFrames();
    }

    public void PlayIdle()
    {
        Play("idle", idle, true);
    }

    public void PlayRun()
    {
        Play("run", run.Length == 0 ? idle : run, true);
    }

    public void PlayAttack(int comboStep)
    {
        if (comboStep == 1)
        {
            Play("attack1", attack1, false);
        }
        else if (comboStep == 2)
        {
            Play("attack2", attack2.Length == 0 ? attack1 : attack2, false);
        }
        else
        {
            Play("attack3", attack3.Length == 0 ? attack1 : attack3, false);
        }
    }

    public void PlayDash()
    {
        Play("dash", dash.Length == 0 ? run : dash, true);
    }

    public void PlaySkill()
    {
        Play("skill", skill.Length == 0 ? attack3 : skill, false);
    }

    public void PlayHurt()
    {
        Play("hurt", hurt.Length == 0 ? idle : hurt, false);
    }

    public void PlayDeath()
    {
        Play("death", death.Length == 0 ? hurt : death, false);
    }

    private void Play(string key, Sprite[] frames, bool shouldLoop)
    {
        if (currentKey == key && currentFrames == frames)
        {
            return;
        }

        currentKey = key;
        currentFrames = frames;
        loop = shouldLoop;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyFrame();
    }

    private void UpdateFrames()
    {
        if (currentFrames == null || currentFrames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >= currentFrames.Length)
            {
                frameIndex = loop ? 0 : currentFrames.Length - 1;
            }

            ApplyFrame();
        }
    }

    private void ApplyFrame()
    {
        if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
        {
            spriteRenderer.sprite = currentFrames[Mathf.Clamp(frameIndex, 0, currentFrames.Length - 1)];
        }
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || actor == null)
        {
            return;
        }

        spriteRenderer.flipX = sourceFacesRight ? actor.Facing < 0 : actor.Facing > 0;
    }
}
