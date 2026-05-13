using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameSession : MonoBehaviour
{
    public CombatActor player;
    public CombatActor enemy;
    public Text playerHealthText;
    public Text enemyHealthText;
    public Text messageText;
    public Text hintText;

    private CombatHudController combatHud;
    private ImpactFeedbackController impactFeedback;
    private GmToolController gmTool;
    private EnemyWaveDirector waveDirector;
    private CombatActor subscribedEnemy;
    private float messageClearTime;
    private bool ended;

    private void Start()
    {
        EnsureRuntimePresentation();

        if (player != null)
        {
            player.HealthChanged += _ => RefreshHud();
            player.Died += _ => End(false);
        }

        if (hintText != null)
        {
            hintText.text = "WASD/Arrow: Move  J: Combo  K: Dash  L: Skill  R: Restart";
        }

        if (messageText != null)
        {
            messageText.text = "";
        }

        EnsureWaveDirector();
        RefreshHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (!ended && messageText != null && messageClearTime > 0f && Time.unscaledTime >= messageClearTime)
        {
            messageText.text = "";
            messageClearTime = 0f;
        }
    }

    private void OnDestroy()
    {
        SubscribeEnemy(null);

        if (waveDirector != null)
        {
            waveDirector.CurrentEnemyChanged -= HandleCurrentEnemyChanged;
            waveDirector.WaveStarted -= HandleWaveStarted;
            waveDirector.BossWarningStarted -= HandleBossWarningStarted;
            waveDirector.BossStarted -= HandleBossStarted;
            waveDirector.EnemyRosterChanged -= RefreshHud;
            waveDirector.AllWavesCleared -= HandleAllWavesCleared;
        }
    }

    private void RefreshHud()
    {
        if (playerHealthText != null && player != null)
        {
            playerHealthText.text = $"PLAYER HP  {player.CurrentHealth}/{player.maxHealth}";
        }

        if (enemyHealthText != null && enemy != null)
        {
            string label = enemy.GetComponent<BossCombatController>() == null ? "ENEMY HP" : "BOSS HP";
            enemyHealthText.text = $"{label}  {enemy.CurrentHealth}/{enemy.maxHealth}";
        }
        else if (enemyHealthText != null)
        {
            enemyHealthText.text = "ENEMY HP  --/--";
        }
    }

    private void End(bool victory)
    {
        if (ended)
        {
            return;
        }

        ended = true;
        if (messageText != null)
        {
            messageText.text = victory ? "VICTORY" : "DEFEAT";
        }
    }

    private void EnsureWaveDirector()
    {
        if (player == null || enemy == null)
        {
            return;
        }

        if (waveDirector == null)
        {
            waveDirector = GetComponent<EnemyWaveDirector>();
        }

        if (waveDirector == null)
        {
            waveDirector = gameObject.AddComponent<EnemyWaveDirector>();
        }

        waveDirector.CurrentEnemyChanged += HandleCurrentEnemyChanged;
        waveDirector.WaveStarted += HandleWaveStarted;
        waveDirector.BossWarningStarted += HandleBossWarningStarted;
        waveDirector.BossStarted += HandleBossStarted;
        waveDirector.EnemyRosterChanged += RefreshHud;
        waveDirector.AllWavesCleared += HandleAllWavesCleared;
        waveDirector.Begin(player, enemy);
    }

    private void HandleCurrentEnemyChanged(CombatActor currentEnemy)
    {
        SubscribeEnemy(currentEnemy);
        enemy = currentEnemy;
        EnsureRuntimePresentation();
        RefreshHud();
    }

    private void SubscribeEnemy(CombatActor currentEnemy)
    {
        if (subscribedEnemy == currentEnemy)
        {
            return;
        }

        if (subscribedEnemy != null)
        {
            subscribedEnemy.HealthChanged -= HandleEnemyHealthChanged;
        }

        subscribedEnemy = currentEnemy;
        if (subscribedEnemy != null)
        {
            subscribedEnemy.HealthChanged += HandleEnemyHealthChanged;
        }
    }

    private void HandleEnemyHealthChanged(CombatActor actor)
    {
        RefreshHud();
    }

    private void HandleWaveStarted(int wave, int totalWaves)
    {
        ShowTransientMessage($"WAVE {wave}/{totalWaves}", 1.2f);
    }

    private void HandleBossWarningStarted()
    {
        ShowTransientMessage("WARNING", 1.55f);
    }

    private void HandleBossStarted(CombatActor boss)
    {
        ShowTransientMessage("BOSS", 1.2f);
    }

    private void HandleAllWavesCleared()
    {
        End(true);
    }

    private void ShowTransientMessage(string message, float duration)
    {
        if (ended || messageText == null)
        {
            return;
        }

        messageText.text = message;
        messageClearTime = Time.unscaledTime + duration;
    }

    private void EnsureRuntimePresentation()
    {
        Canvas canvas = ResolveHudCanvas();
        PlayerCombatController playerController = player == null ? null : player.GetComponent<PlayerCombatController>();
        CameraFollow2D cameraFollow = ResolveCameraFollow();

        if (combatHud == null)
        {
            combatHud = GetComponent<CombatHudController>();
        }

        if (combatHud == null)
        {
            combatHud = gameObject.AddComponent<CombatHudController>();
        }

        combatHud.Bind(player, enemy, playerController, canvas);

        if (impactFeedback == null)
        {
            impactFeedback = GetComponent<ImpactFeedbackController>();
        }

        if (impactFeedback == null)
        {
            impactFeedback = gameObject.AddComponent<ImpactFeedbackController>();
        }

        impactFeedback.Bind(canvas, cameraFollow);

        if (gmTool == null)
        {
            gmTool = GetComponent<GmToolController>();
        }

        if (gmTool == null)
        {
            gmTool = gameObject.AddComponent<GmToolController>();
        }

        gmTool.Bind(player, canvas, waveDirector);
    }

    private Canvas ResolveHudCanvas()
    {
        if (playerHealthText != null)
        {
            return playerHealthText.canvas;
        }

        if (enemyHealthText != null)
        {
            return enemyHealthText.canvas;
        }

        if (hintText != null)
        {
            return hintText.canvas;
        }

        return FindObjectOfType<Canvas>();
    }

    private CameraFollow2D ResolveCameraFollow()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.TryGetComponent(out CameraFollow2D cameraFollow))
        {
            return cameraFollow;
        }

        return FindObjectOfType<CameraFollow2D>();
    }
}
