using UnityEngine;
using UnityEngine.UI;

public sealed class CombatHudController : MonoBehaviour
{
    public CombatActor player;
    public CombatActor enemy;
    public PlayerCombatController playerController;
    public Canvas hudCanvas;

    private RectTransform root;
    private Sprite whiteSprite;
    private HealthBar playerHealthBar;
    private HealthBar enemyHealthBar;
    private CooldownWidget dashCooldown;
    private CooldownWidget skillCooldown;
    private Text comboText;
    private int playerComboHits;
    private float comboHideTime;
    private static Font builtinFont;

    public void Bind(CombatActor player, CombatActor enemy, PlayerCombatController playerController, Canvas hudCanvas)
    {
        this.player = player;
        this.enemy = enemy;
        this.playerController = playerController;
        this.hudCanvas = hudCanvas;
        EnsureHud();
        RefreshImmediate();
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
        if (whiteSprite != null)
        {
            Destroy(whiteSprite.texture);
            Destroy(whiteSprite);
        }
    }

    private void Update()
    {
        if (!EnsureHud())
        {
            return;
        }

        playerHealthBar.Update(player);
        enemyHealthBar.Update(enemy);
        UpdateCooldowns();
        UpdateComboVisibility();
    }

    private void HandleHitConfirmed(CombatHitEvent hitEvent)
    {
        if (player == null || hitEvent.source != player)
        {
            return;
        }

        playerComboHits++;
        comboHideTime = Time.unscaledTime + (hitEvent.killed ? 1.35f : 1f);
        UpdateComboText();
    }

    private bool EnsureHud()
    {
        if (root != null
            && playerHealthBar != null
            && enemyHealthBar != null
            && dashCooldown != null
            && skillCooldown != null
            && comboText != null)
        {
            return true;
        }

        if (root != null)
        {
            Destroy(root.gameObject);
            root = null;
            playerHealthBar = null;
            enemyHealthBar = null;
            dashCooldown = null;
            skillCooldown = null;
            comboText = null;
        }

        if (hudCanvas == null)
        {
            hudCanvas = FindObjectOfType<Canvas>();
        }

        if (hudCanvas == null)
        {
            return false;
        }

        whiteSprite = CreateWhiteSprite();
        root = CreateRect("RuntimeCombatHud", hudCanvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        playerHealthBar = CreateHealthBar(
            "RuntimePlayerHealthBar",
            new Vector2(0f, 1f),
            new Vector2(40f, -64f),
            new Vector2(360f, 24f),
            false,
            new Color(0.12f, 0.9f, 0.42f, 1f),
            new Color(0.95f, 0.65f, 0.18f, 1f));

        enemyHealthBar = CreateHealthBar(
            "RuntimeEnemyHealthBar",
            new Vector2(1f, 1f),
            new Vector2(-40f, -64f),
            new Vector2(360f, 24f),
            true,
            new Color(1f, 0.23f, 0.2f, 1f),
            new Color(0.95f, 0.65f, 0.18f, 1f));

        dashCooldown = CreateCooldownWidget("RuntimeDashCooldown", "K", "DASH", new Vector2(42f, 74f));
        skillCooldown = CreateCooldownWidget("RuntimeSkillCooldown", "L", "SKILL", new Vector2(112f, 74f));
        comboText = CreateComboText();
        comboText.gameObject.SetActive(false);
        return true;
    }

    private void RefreshImmediate()
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.SetImmediate(player);
        }

        if (enemyHealthBar != null)
        {
            enemyHealthBar.SetImmediate(enemy);
        }
    }

    private void UpdateCooldowns()
    {
        if (dashCooldown == null || skillCooldown == null)
        {
            return;
        }

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<PlayerCombatController>();
        }

        if (playerController == null)
        {
            dashCooldown.Update(0f, 0f, true);
            skillCooldown.Update(0f, 0f, true);
            return;
        }

        dashCooldown.Update(
            playerController.DashCooldownRemaining01,
            playerController.DashCooldownRemaining,
            playerController.IsDashReady);

        skillCooldown.Update(
            playerController.SkillCooldownRemaining01,
            playerController.SkillCooldownRemaining,
            playerController.IsSkillReady);
    }

    private void UpdateComboVisibility()
    {
        if (comboText == null || playerComboHits <= 0)
        {
            return;
        }

        if (Time.unscaledTime <= comboHideTime)
        {
            return;
        }

        playerComboHits = 0;
        comboText.gameObject.SetActive(false);
    }

    private void UpdateComboText()
    {
        if (comboText == null)
        {
            return;
        }

        comboText.gameObject.SetActive(true);
        comboText.text = playerComboHits == 1 ? "1 HIT" : $"{playerComboHits} HITS";
    }

    private HealthBar CreateHealthBar(
        string name,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        bool rightToLeft,
        Color fillColor,
        Color delayedColor)
    {
        RectTransform frame = CreateRect(name, root);
        frame.anchorMin = anchor;
        frame.anchorMax = anchor;
        frame.pivot = new Vector2(anchor.x, 1f);
        frame.anchoredPosition = position;
        frame.sizeDelta = size;

        Image frameImage = AddImage(frame.gameObject, new Color(0.02f, 0.025f, 0.03f, 0.88f));
        frameImage.sprite = whiteSprite;

        var outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform track = CreateRect("Track", frame);
        Stretch(track, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        AddImage(track.gameObject, new Color(0.1f, 0.04f, 0.045f, 0.95f)).sprite = whiteSprite;

        Image delayed = CreateFillImage("Delayed", track, delayedColor, rightToLeft);
        Image fill = CreateFillImage("Fill", track, fillColor, rightToLeft);
        return new HealthBar(fill, delayed);
    }

    private CooldownWidget CreateCooldownWidget(string name, string key, string caption, Vector2 position)
    {
        RectTransform frame = CreateRect(name, root);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.zero;
        frame.pivot = new Vector2(0f, 0f);
        frame.anchoredPosition = position;
        frame.sizeDelta = new Vector2(58f, 58f);

        Image frameImage = AddImage(frame.gameObject, new Color(0.02f, 0.025f, 0.03f, 0.88f));
        frameImage.sprite = whiteSprite;

        var outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.16f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform overlayRect = CreateRect("CooldownOverlay", frame);
        Stretch(overlayRect, Vector2.zero, Vector2.zero);
        Image overlay = AddImage(overlayRect.gameObject, new Color(0f, 0f, 0f, 0.72f));
        overlay.sprite = whiteSprite;
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = true;
        overlay.fillAmount = 0f;

        Text keyText = CreateText("Key", frame, key, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(keyText.rectTransform, Vector2.zero, Vector2.zero);

        Text timerText = CreateText("Timer", frame, "", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.32f, 1f));
        Stretch(timerText.rectTransform, Vector2.zero, Vector2.zero);

        Text captionText = CreateText("Caption", frame, caption, 11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.9f, 1f));
        captionText.rectTransform.anchorMin = new Vector2(0f, 0f);
        captionText.rectTransform.anchorMax = new Vector2(1f, 0f);
        captionText.rectTransform.pivot = new Vector2(0.5f, 1f);
        captionText.rectTransform.anchoredPosition = new Vector2(0f, -3f);
        captionText.rectTransform.sizeDelta = new Vector2(0f, 18f);

        return new CooldownWidget(overlay, keyText, timerText);
    }

    private Text CreateComboText()
    {
        Text text = CreateText("RuntimeComboText", root, "", 36, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.18f, 1f));
        text.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);
        text.rectTransform.anchoredPosition = new Vector2(0f, -92f);
        text.rectTransform.sizeDelta = new Vector2(260f, 58f);

        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private Image CreateFillImage(string name, RectTransform parent, Color color, bool rightToLeft)
    {
        RectTransform rect = CreateRect(name, parent);
        Stretch(rect, Vector2.zero, Vector2.zero);
        Image image = AddImage(rect.gameObject, color);
        image.sprite = whiteSprite;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = rightToLeft ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return image;
    }

    private Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var text = textObject.GetComponent<Text>();
        text.raycastTarget = false;
        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = color;
        text.text = value;
        return text;
    }

    private static Font GetBuiltinFont()
    {
        if (builtinFont == null)
        {
            builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return builtinFont;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Image AddImage(GameObject gameObject, Color color)
    {
        var image = gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        return image;
    }

    private static Sprite CreateWhiteSprite()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private sealed class HealthBar
    {
        private readonly Image fill;
        private readonly Image delayed;
        private float delayedAmount = 1f;

        public HealthBar(Image fill, Image delayed)
        {
            this.fill = fill;
            this.delayed = delayed;
        }

        public void SetImmediate(CombatActor actor)
        {
            delayedAmount = actor == null ? 0f : actor.Health01;
            fill.fillAmount = delayedAmount;
            delayed.fillAmount = delayedAmount;
        }

        public void Update(CombatActor actor)
        {
            float target = actor == null ? 0f : actor.Health01;
            fill.fillAmount = target;

            if (target > delayedAmount)
            {
                delayedAmount = target;
            }
            else
            {
                delayedAmount = Mathf.MoveTowards(delayedAmount, target, 0.38f * Time.unscaledDeltaTime);
            }

            delayed.fillAmount = delayedAmount;
        }
    }

    private sealed class CooldownWidget
    {
        private readonly Image overlay;
        private readonly Text keyText;
        private readonly Text timerText;

        public CooldownWidget(Image overlay, Text keyText, Text timerText)
        {
            this.overlay = overlay;
            this.keyText = keyText;
            this.timerText = timerText;
        }

        public void Update(float remaining01, float remainingSeconds, bool ready)
        {
            overlay.fillAmount = remaining01;
            keyText.color = ready ? Color.white : new Color(0.62f, 0.66f, 0.72f, 1f);
            timerText.text = ready ? "" : Mathf.CeilToInt(remainingSeconds).ToString();
        }
    }
}
