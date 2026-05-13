using System;
using System.IO;
using UnityEngine;

public sealed class GmToolController : MonoBehaviour
{
    [Serializable]
    public sealed class GmToolSettings
    {
        public bool playerInvincible;
    }

    public KeyCode toggleKey = KeyCode.Tab;
    public CombatActor player;
    public EnemyWaveDirector waveDirector;
    public GmToolSettings settings = new GmToolSettings();

    private bool panelVisible;
    private bool loaded;
    private GUIStyle titleStyle;
    private GUIStyle lineStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;
    private Font chineseFont;

    private string ConfigPath => Path.Combine(Application.persistentDataPath, "gm-tool-settings.json");

    public void Bind(CombatActor player, Canvas _, EnemyWaveDirector waveDirector = null)
    {
        this.player = player;
        this.waveDirector = waveDirector;

        if (!loaded)
        {
            LoadSettings();
        }

        ApplySettings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            panelVisible = !panelVisible;
        }

        ApplySettings();
    }

    private void OnGUI()
    {
        if (!panelVisible)
        {
            return;
        }

        EnsureStyles();

        GUILayout.BeginArea(new Rect(24f, 112f, 280f, 228f));
        GUILayout.Label("GM工具", titleStyle);
        GUILayout.Space(10f);

        bool newInvincible = GUILayout.Toggle(settings.playerInvincible, "角色无敌", lineStyle, GUILayout.Height(30f));
        if (newInvincible != settings.playerInvincible)
        {
            SetPlayerInvincible(newInvincible);
        }

        GUILayout.Label(settings.playerInvincible ? "当前：开启" : "当前：关闭", hintStyle);
        GUILayout.Space(8f);

        if (GUILayout.Button("恢复玩家生命", buttonStyle, GUILayout.Height(30f)))
        {
            player?.RestoreToFullHealth();
        }

        GUI.enabled = waveDirector != null && waveDirector.CurrentEnemy != null;
        if (GUILayout.Button("击败当前敌人", buttonStyle, GUILayout.Height(30f)))
        {
            waveDirector.DebugDefeatCurrentEnemy();
        }

        GUI.enabled = waveDirector != null && !waveDirector.BossSpawned;
        if (GUILayout.Button("直接召唤Boss", buttonStyle, GUILayout.Height(30f)))
        {
            waveDirector.DebugSkipToBoss();
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private void LoadSettings()
    {
        loaded = true;

        if (File.Exists(ConfigPath))
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), settings);
            return;
        }

        SaveSettings();
    }

    private void SaveSettings()
    {
        string directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ConfigPath, JsonUtility.ToJson(settings, true));
    }

    private void ApplySettings()
    {
        if (player != null)
        {
            player.invincible = settings.playerInvincible;
        }
    }

    private void SetPlayerInvincible(bool enabled)
    {
        settings.playerInvincible = enabled;
        ApplySettings();
        SaveSettings();
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        lineStyle = new GUIStyle(GUI.skin.toggle)
        {
            font = chineseFont,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            onNormal = { textColor = new Color(0.4f, 1f, 0.5f, 1f) },
            hover = { textColor = new Color(1f, 0.9f, 0.35f, 1f) },
            onHover = { textColor = new Color(0.55f, 1f, 0.65f, 1f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = chineseFont,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(1f, 0.9f, 0.35f, 1f) },
            active = { textColor = new Color(0.55f, 1f, 0.65f, 1f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.84f, 0.25f, 1f) }
        };
    }
}
