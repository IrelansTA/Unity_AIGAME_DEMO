using UnityEngine;

public static class HammerGeneralAssets
{
    private const string BossFramePath = "Boss/HammerGeneral/Processed/hammer-general-";
    private const string ProjectileFramePath = "Boss/HammerGeneral/Projectile/hammer-projectile-";

    public static bool ApplyTo(ActorVisualAnimator visual)
    {
        if (visual == null)
        {
            return false;
        }

        visual.sourceFacesRight = true;
        visual.framesPerSecond = 7f;
        visual.idle = LoadFrames(BossFramePath, 1, 4);
        visual.run = LoadFrames(BossFramePath, 5, 8);
        visual.attack1 = LoadFrames(BossFramePath, 9, 12);
        visual.attack2 = LoadFrames(BossFramePath, 13, 16);
        visual.attack3 = LoadFrames(BossFramePath, 17, 20);
        visual.dash = visual.attack2;
        visual.skill = visual.attack1;
        visual.hurt = LoadFrames(BossFramePath, 21, 22);
        visual.death = LoadFrames(BossFramePath, 22, 24);
        return HasAny(visual.idle) && HasAny(visual.run) && HasAny(visual.attack1);
    }

    public static Sprite[] LoadProjectileFrames()
    {
        return LoadFrames(ProjectileFramePath, 1, 4);
    }

    private static Sprite[] LoadFrames(string prefix, int first, int last)
    {
        int count = Mathf.Max(0, last - first + 1);
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Resources.Load<Sprite>(prefix + (first + i));
        }

        return frames;
    }

    private static bool HasAny(Sprite[] frames)
    {
        if (frames == null)
        {
            return false;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return true;
            }
        }

        return false;
    }
}
