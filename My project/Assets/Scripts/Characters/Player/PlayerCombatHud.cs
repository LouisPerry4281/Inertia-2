using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatHud : MonoBehaviour
{
    private const string HudName = "Player Combat HUD";

    [SerializeField] private PlayerStyleManager styleManager;
    [SerializeField] private PlayerJuiceManager juiceManager;
    [SerializeField] private PlayerCombatManager combatManager;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI styleText;
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI juiceText;
    [SerializeField] private TextMeshProUGUI flowText;
    [SerializeField] private Image juiceFill;
    [SerializeField] private Image styleFill;
    [SerializeField] private Image flowFill;
    [SerializeField] private Image flowPulse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHudExists()
    {
        if (FindAnyObjectByType<PlayerCombatHud>() != null)
            return;

        GameObject hudObject = new GameObject(HudName);
        DontDestroyOnLoad(hudObject);
        hudObject.AddComponent<PlayerCombatHud>().BuildHud();
    }

    private void Update()
    {
        ResolveReferences();
        UpdateHud();
    }

    private void BuildHud()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform panel = CreatePanel(root);
        rankText = CreateText(panel, "Dry", 34, FontStyles.Bold, new Vector2(20f, -16f), new Vector2(240f, 52f), TextAlignmentOptions.Left);
        styleText = CreateText(panel, "STYLE 0", 22, FontStyles.Bold, new Vector2(20f, -72f), new Vector2(190f, 32f), TextAlignmentOptions.Left);
        streakText = CreateText(panel, "HITS 0", 18, FontStyles.Normal, new Vector2(20f, -104f), new Vector2(160f, 28f), TextAlignmentOptions.Left);

        styleFill = CreateBar(panel, new Vector2(20f, -140f), new Vector2(220f, 8f), new Color(0.95f, 0.82f, 0.2f, 1f));
        juiceText = CreateText(panel, "JUICE 0/0", 20, FontStyles.Bold, new Vector2(20f, -164f), new Vector2(190f, 30f), TextAlignmentOptions.Left);
        juiceFill = CreateBar(panel, new Vector2(20f, -202f), new Vector2(220f, 14f), new Color(0.1f, 0.8f, 1f, 1f));
        flowText = CreateText(panel, "FLOW OFF", 18, FontStyles.Bold, new Vector2(20f, -224f), new Vector2(190f, 28f), TextAlignmentOptions.Left);
        flowFill = CreateBar(panel, new Vector2(20f, -258f), new Vector2(220f, 12f), new Color(0.3f, 0.95f, 1f, 1f));

        flowPulse = gameObject.AddComponent<Image>();
        flowPulse.raycastTarget = false;
        flowPulse.color = Color.clear;
        flowPulse.rectTransform.anchorMin = Vector2.zero;
        flowPulse.rectTransform.anchorMax = Vector2.one;
        flowPulse.rectTransform.offsetMin = Vector2.zero;
        flowPulse.rectTransform.offsetMax = Vector2.zero;
    }

    private RectTransform CreatePanel(RectTransform parent)
    {
        GameObject panelObject = new GameObject("Combat HUD Panel");
        panelObject.transform.SetParent(parent, false);

        RectTransform panel = panelObject.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(28f, -28f);
        panel.sizeDelta = new Vector2(280f, 292f);

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.48f);

        return panel;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string text, int fontSize, FontStyles fontStyle, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(text);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private Image CreateBar(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        GameObject backgroundObject = new GameObject("Bar Background");
        backgroundObject.transform.SetParent(parent, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.anchoredPosition = anchoredPosition;
        backgroundRect.sizeDelta = size;

        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.16f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(backgroundRect, false);

        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        return fill;
    }

    private void ResolveReferences()
    {
        if (styleManager == null)
        {
            PlayerManager player = FindAnyObjectByType<PlayerManager>();
            styleManager = player != null ? player.playerStyleManager : FindAnyObjectByType<PlayerStyleManager>();
        }

        if (combatManager == null)
        {
            PlayerManager player = FindAnyObjectByType<PlayerManager>();
            combatManager = player != null ? player.playerCombatManager : FindAnyObjectByType<PlayerCombatManager>();
        }

        if (juiceManager == null)
        {
            juiceManager = PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance : FindAnyObjectByType<PlayerJuiceManager>();
        }
    }

    private void UpdateHud()
    {
        if (styleManager != null)
        {
            rankText.text = styleManager.CurrentRankName;
            styleText.text = $"STYLE {Mathf.RoundToInt(styleManager.CurrentStyle)}";
            streakText.text = $"HITS {styleManager.HitStreak}";
            styleFill.fillAmount = Mathf.Clamp01(styleManager.CurrentStyle / 1000f);
        }

        if (juiceManager != null)
        {
            juiceText.text = $"JUICE {Mathf.RoundToInt(juiceManager.currentJuice)}/{Mathf.RoundToInt(juiceManager.MaxJuice)}";
            juiceFill.fillAmount = juiceManager.JuiceNormalized;
        }

        UpdateFlowHud();
    }

    private void UpdateFlowHud()
    {
        bool isFlowActive = combatManager != null && combatManager.IsInFlow;
        float flowAmount = combatManager != null ? combatManager.FlowNormalized : 0f;

        if (flowText != null)
        {
            flowText.text = isFlowActive ? "FLOW STATE" : "FLOW OFF";
            flowText.color = isFlowActive ? new Color(0.35f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.42f);
        }

        if (flowFill != null)
        {
            flowFill.fillAmount = flowAmount;
            flowFill.color = isFlowActive ? new Color(0.3f, 0.95f, 1f, 1f) : new Color(0.3f, 0.95f, 1f, 0.2f);
        }

        if (flowPulse != null)
        {
            float alpha = isFlowActive ? Mathf.Lerp(0.08f, 0.18f, Mathf.PingPong(Time.unscaledTime * 2.5f, 1f)) : 0f;
            flowPulse.color = new Color(0.15f, 0.8f, 1f, alpha);
        }
    }
}
