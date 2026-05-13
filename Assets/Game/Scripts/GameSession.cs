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
    private bool ended;

    private void Start()
    {
        EnsureRuntimePresentation();

        if (player != null)
        {
            player.HealthChanged += _ => RefreshHud();
            player.Died += _ => End(false);
        }

        if (enemy != null)
        {
            enemy.HealthChanged += _ => RefreshHud();
            enemy.Died += _ => End(true);
        }

        if (hintText != null)
        {
            hintText.text = "WASD/Arrow: Move  J: Combo  K: Dash  L: Skill  R: Restart";
        }

        if (messageText != null)
        {
            messageText.text = "";
        }

        RefreshHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            enemyHealthText.text = $"ENEMY HP  {enemy.CurrentHealth}/{enemy.maxHealth}";
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
