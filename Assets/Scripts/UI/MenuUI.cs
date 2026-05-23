using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private string metroSceneName = "Metro";

    private const string SettingsTitle = "\u041d\u0410\u0421\u0422\u0420\u041e\u0419\u041a\u0418";
    private const string VolumeLabel = "\u0413\u0420\u041e\u041c\u041a\u041e\u0421\u0422\u042c";
    private const string SensitivityLabel = "\u0427\u0423\u0412\u0421\u0422\u0412\u0418\u0422\u0415\u041b\u042c\u041d\u041e\u0421\u0422\u042c \u041c\u042b\u0428\u0418";
    private const string BackLabel = "\u041d\u0410\u0417\u0410\u0414";

    private Canvas _canvas;
    private GameObject _settingsPanel;
    private Slider _volumeSlider;
    private Slider _sensitivitySlider;

    private void Awake()
    {
        GameSettings.ApplySavedSettings();
    }

    public void Start1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(metroSceneName);
    }

    public void Start2()
    {
        ShowSettings();
    }

    public void Start3()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowSettings()
    {
        EnsureSettingsPanel();
        if (_settingsPanel == null)
        {
            return;
        }

        _settingsPanel.SetActive(true);
        _volumeSlider.SetValueWithoutNotify(GameSettings.GetMasterVolume());
        _sensitivitySlider.SetValueWithoutNotify(GameSettings.GetMouseSensitivity());
    }

    public void HideSettings()
    {
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }
    }

    private void EnsureSettingsPanel()
    {
        if (_settingsPanel != null)
        {
            return;
        }

        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("MainMenu needs a Canvas for the settings panel.");
            return;
        }

        _settingsPanel = CreatePanel(_canvas.transform);
        CreateLabel(_settingsPanel.transform, SettingsTitle, new Vector2(0f, 185f), 42f, FontStyles.Bold);
        CreateLabel(_settingsPanel.transform, VolumeLabel, new Vector2(-220f, 75f), 24f, FontStyles.Normal);
        _volumeSlider = CreateSlider(_settingsPanel.transform, new Vector2(115f, 75f), 0f, 1f, GameSettings.GetMasterVolume());
        _volumeSlider.onValueChanged.AddListener(GameSettings.SetMasterVolume);

        CreateLabel(_settingsPanel.transform, SensitivityLabel, new Vector2(-220f, -20f), 24f, FontStyles.Normal);
        _sensitivitySlider = CreateSlider(_settingsPanel.transform, new Vector2(115f, -20f), 0.25f, 4f, GameSettings.GetMouseSensitivity());
        _sensitivitySlider.onValueChanged.AddListener(GameSettings.SetMouseSensitivity);

        CreateButton(_settingsPanel.transform, BackLabel, new Vector2(0f, -155f), HideSettings);
        _settingsPanel.SetActive(false);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = parent.gameObject.layer;
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.94f);

        return panel;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 position, float fontSize, FontStyles style)
    {
        GameObject label = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.layer = parent.gameObject.layer;
        label.transform.SetParent(parent, false);

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(520f, 60f);

        TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = new Color(0.86f, 0.86f, 0.82f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    private static Slider CreateSlider(Transform parent, Vector2 position, float minValue, float maxValue, float value)
    {
        GameObject root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = position;
        rootRect.sizeDelta = new Vector2(360f, 28f);

        GameObject background = CreateSliderImage(root.transform, "Background", new Color(0.16f, 0.16f, 0.16f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = parent.gameObject.layer;
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(7f, 7f);
        fillAreaRect.offsetMax = new Vector2(-7f, -7f);

        GameObject fill = CreateSliderImage(fillArea.transform, "Fill", new Color(0.72f, 0.08f, 0.08f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.layer = parent.gameObject.layer;
        handleArea.transform.SetParent(root.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreateSliderImage(handleArea.transform, "Handle", new Color(0.92f, 0.92f, 0.86f, 1f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 34f);

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private static GameObject CreateSliderImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static Button CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, Vector2.zero, 28f, FontStyles.Bold);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        label.color = new Color(0.86f, 0.86f, 0.82f, 1f);

        return button;
    }
}