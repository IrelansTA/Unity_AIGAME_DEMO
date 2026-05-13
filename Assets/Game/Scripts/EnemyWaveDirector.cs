using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyWaveDirector : MonoBehaviour
{
    public CombatActor player;
    public CombatActor initialEnemy;
    public float nextWaveDelay = 1.15f;
    public float corpseCleanupDelay = 1.35f;
    public float bossIntroDelay = 1.8f;
    public Vector2 bossSpawnOffset = new Vector2(1.05f, 0f);

    private static readonly Vector2[][] AdditionalWaves =
    {
        new[]
        {
            new Vector2(-0.55f, -0.55f),
            new Vector2(1.25f, 0.45f)
        },
        new[]
        {
            new Vector2(-1.15f, 0.58f),
            new Vector2(0.55f, -0.36f),
            new Vector2(1.95f, 0.18f)
        }
    };

    private readonly List<CombatActor> activeEnemies = new List<CombatActor>();
    private CombatActor currentEnemy;
    private CombatActor bossActor;
    private GameObject enemyTemplate;
    private Transform enemyParent;
    private CombatBounds bounds;
    private Vector3 firstSpawnPosition;
    private bool started;
    private bool waitingForNextWave;
    private bool bossSpawned;
    private bool completed;

    public event Action<CombatActor> CurrentEnemyChanged;
    public event Action<int, int> WaveStarted;
    public event Action BossWarningStarted;
    public event Action<CombatActor> BossStarted;
    public event Action EnemyRosterChanged;
    public event Action AllWavesCleared;

    public CombatActor CurrentEnemy => currentEnemy;
    public int CurrentWave { get; private set; }
    public int TotalWaves => 1 + AdditionalWaves.Length;
    public int AliveEnemyCount => CountAliveEnemies();
    public bool BossSpawned => bossSpawned;
    public CombatActor BossActor => bossActor;

    public void Begin(CombatActor playerActor, CombatActor firstEnemy)
    {
        if (started)
        {
            return;
        }

        player = playerActor;
        initialEnemy = firstEnemy;

        if (player == null || initialEnemy == null)
        {
            return;
        }

        started = true;
        enemyParent = initialEnemy.transform.parent;
        firstSpawnPosition = initialEnemy.transform.position;
        bounds = player.bounds != null ? player.bounds : initialEnemy.bounds;

        CreateTemplate();
        ConfigureEnemy(initialEnemy, firstSpawnPosition, initialEnemy.name);
        activeEnemies.Add(initialEnemy);
        CurrentWave = 1;
        SetCurrentEnemy(initialEnemy);
        WaveStarted?.Invoke(CurrentWave, TotalWaves);
        EnemyRosterChanged?.Invoke();
    }

    private void OnEnable()
    {
        CombatEvents.HitConfirmed += HandleHitConfirmed;
    }

    private void OnDisable()
    {
        CombatEvents.HitConfirmed -= HandleHitConfirmed;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].Died -= HandleEnemyDied;
            }
        }

        if (enemyTemplate != null)
        {
            Destroy(enemyTemplate);
        }
    }

    private void HandleHitConfirmed(CombatHitEvent hitEvent)
    {
        if (!started || hitEvent.source != player || hitEvent.target == null || hitEvent.target.team != CombatTeam.Enemy)
        {
            return;
        }

        if (!hitEvent.target.IsDead)
        {
            SetCurrentEnemy(hitEvent.target);
        }
    }

    private void CreateTemplate()
    {
        enemyTemplate = Instantiate(initialEnemy.gameObject, enemyParent);
        enemyTemplate.name = initialEnemy.gameObject.name + "_RuntimeTemplate";
        enemyTemplate.SetActive(false);
    }

    private void ConfigureEnemy(CombatActor actor, Vector3 position, string objectName)
    {
        actor.name = objectName;
        actor.transform.position = ClampToBounds(position);
        actor.Died -= HandleEnemyDied;
        actor.Died += HandleEnemyDied;

        if (actor.TryGetComponent(out EnemyCombatController controller))
        {
            controller.target = player;
        }

        actor.FaceTarget(player.transform);
    }

    private void HandleEnemyDied(CombatActor actor)
    {
        EnemyRosterChanged?.Invoke();
        StartCoroutine(CleanupEnemyAfterDelay(actor));

        if (currentEnemy == actor)
        {
            SetCurrentEnemy(FindClosestAliveEnemy());
        }

        if (!waitingForNextWave && AllActiveEnemiesDead())
        {
            if (CurrentWave >= TotalWaves)
            {
                if (bossSpawned)
                {
                    StartCoroutine(CompleteAfterDelay());
                }
                else
                {
                    StartCoroutine(SpawnBossAfterDelay());
                }
            }
            else
            {
                StartCoroutine(SpawnNextWaveAfterDelay());
            }
        }
    }

    private IEnumerator CleanupEnemyAfterDelay(CombatActor actor)
    {
        yield return new WaitForSeconds(corpseCleanupDelay);

        activeEnemies.Remove(actor);
        if (actor != null)
        {
            actor.Died -= HandleEnemyDied;
            Destroy(actor.gameObject);
        }

        EnemyRosterChanged?.Invoke();
    }

    private IEnumerator SpawnNextWaveAfterDelay()
    {
        waitingForNextWave = true;
        SetCurrentEnemy(null);
        yield return new WaitForSeconds(nextWaveDelay);

        if (completed || player == null || player.IsDead)
        {
            waitingForNextWave = false;
            yield break;
        }

        CurrentWave++;
        SpawnWave(CurrentWave);
        waitingForNextWave = false;
        WaveStarted?.Invoke(CurrentWave, TotalWaves);
        EnemyRosterChanged?.Invoke();
    }

    private IEnumerator SpawnBossAfterDelay()
    {
        waitingForNextWave = true;
        SetCurrentEnemy(null);
        BossWarningStarted?.Invoke();

        yield return new WaitForSeconds(Mathf.Max(0f, bossIntroDelay));

        if (completed || player == null || player.IsDead)
        {
            waitingForNextWave = false;
            yield break;
        }

        SpawnBoss();
        waitingForNextWave = false;
        EnemyRosterChanged?.Invoke();
    }

    private IEnumerator CompleteAfterDelay()
    {
        waitingForNextWave = true;
        yield return new WaitForSeconds(0.8f);

        if (!completed)
        {
            completed = true;
            SetCurrentEnemy(null);
            AllWavesCleared?.Invoke();
        }
    }

    private void SpawnWave(int waveNumber)
    {
        int additionalWaveIndex = waveNumber - 2;
        if (additionalWaveIndex < 0 || additionalWaveIndex >= AdditionalWaves.Length)
        {
            return;
        }

        Vector2[] offsets = AdditionalWaves[additionalWaveIndex];
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject instance = Instantiate(enemyTemplate, enemyParent);
            instance.name = $"Enemy_Wave{waveNumber}_{i + 1}";
            instance.SetActive(true);

            CombatActor actor = instance.GetComponent<CombatActor>();
            if (actor == null)
            {
                Destroy(instance);
                continue;
            }

            Vector3 spawnPosition = firstSpawnPosition + (Vector3)offsets[i];
            ConfigureEnemy(actor, spawnPosition, instance.name);
            activeEnemies.Add(actor);
        }

        SetCurrentEnemy(FindClosestAliveEnemy());
    }

    private void SpawnBoss()
    {
        GameObject instance = Instantiate(enemyTemplate, enemyParent);
        instance.name = "Boss_HammerGeneral";
        instance.SetActive(true);

        CombatActor actor = instance.GetComponent<CombatActor>();
        if (actor == null)
        {
            Destroy(instance);
            return;
        }

        Vector3 spawnPosition = firstSpawnPosition + (Vector3)bossSpawnOffset;
        ConfigureEnemy(actor, spawnPosition, instance.name);
        ConfigureBoss(actor);
        bossActor = actor;
        bossSpawned = true;
        activeEnemies.Add(actor);
        SetCurrentEnemy(actor);
        BossStarted?.Invoke(actor);
    }

    public void DebugDefeatCurrentEnemy()
    {
        CombatActor enemy = currentEnemy != null && !currentEnemy.IsDead
            ? currentEnemy
            : FindClosestAliveEnemy();

        enemy?.ForceDefeat();
    }

    public void DebugSkipToBoss()
    {
        if (!started || completed || player == null || player.IsDead)
        {
            return;
        }

        if (bossSpawned)
        {
            SetCurrentEnemy(bossActor);
            return;
        }

        StopAllCoroutines();
        waitingForNextWave = false;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            CombatActor enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            enemy.Died -= HandleEnemyDied;
            Destroy(enemy.gameObject);
            activeEnemies.RemoveAt(i);
        }

        CurrentWave = TotalWaves;
        SetCurrentEnemy(null);
        BossWarningStarted?.Invoke();
        SpawnBoss();
        EnemyRosterChanged?.Invoke();
    }

    private void ConfigureBoss(CombatActor actor)
    {
        actor.ConfigureStats(260, 2.4f);

        if (actor.hurtbox != null)
        {
            actor.hurtbox.offset = new Vector2(0f, 0.82f);
            actor.hurtbox.size = new Vector2(1.18f, 1.72f);
        }

        if (actor.TryGetComponent(out ActorGroundShadow shadow))
        {
            shadow.size = new Vector2(1.55f, 0.36f);
            shadow.offset = new Vector2(0f, 0.03f);
            shadow.color = new Color(0f, 0f, 0f, 0.6f);
        }

        if (actor.TryGetComponent(out EnemyCombatController enemyController))
        {
            enemyController.enabled = false;
        }

        if (actor.TryGetComponent(out CombatHitbox combatHitbox))
        {
            combatHitbox.laneTolerance = 0.35f;
        }

        ActorVisualAnimator visual = actor.visual != null ? actor.visual : actor.GetComponent<ActorVisualAnimator>();
        HammerGeneralAssets.ApplyTo(visual);
        visual?.PlayIdle();

        BossCombatController bossController = actor.GetComponent<BossCombatController>();
        if (bossController == null)
        {
            bossController = actor.gameObject.AddComponent<BossCombatController>();
        }

        bossController.actor = actor;
        bossController.hitbox = combatHitbox;
        bossController.visual = visual;
        bossController.target = player;
        bossController.laneTolerance = 0.35f;
        bossController.warningHeight = 0.08f;
        bossController.warningLocalY = 0.12f;
        bossController.projectileLocalY = 0.34f;
        bossController.slamWindup = 1.15f;
        bossController.slamWarningLocalY = 0.06f;
        bossController.slamWarningSize = new Vector2(2.05f, 0.48f);
    }

    private CombatActor FindClosestAliveEnemy()
    {
        CombatActor closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            CombatActor enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distance = player == null ? 0f : ((Vector2)(enemy.transform.position - player.transform.position)).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    private void SetCurrentEnemy(CombatActor enemy)
    {
        if (currentEnemy == enemy)
        {
            return;
        }

        currentEnemy = enemy;
        CurrentEnemyChanged?.Invoke(currentEnemy);
    }

    private bool AllActiveEnemiesDead()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            CombatActor enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    private int CountAliveEnemies()
    {
        int count = 0;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            CombatActor enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        return bounds == null ? position : bounds.Clamp(position);
    }
}
