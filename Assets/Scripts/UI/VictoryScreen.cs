using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private Sprite victoryTitleSprite;
    [SerializeField] private Sprite menuButtonSprite;
    [SerializeField] private Sprite firecrackerLeftSprite;
    [SerializeField] private Sprite firecrackerRightSprite;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private Color backgroundColor = new Color(0.18f, 0.45f, 0.42f, 1f);

    private static VictoryScreen _instance;
    private GameObject _root;

    private void Awake()
    {
        _instance = this;
        AssignDefaultSpritesInEditor();
        ConfigureCanvas();
        BuildScreen();
        Hide();
    }

    public static void ShowVictory()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<VictoryScreen>();
        }

        if (_instance == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("VictoryScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
            }

            if (canvas != null)
            {
                _instance = canvas.gameObject.AddComponent<VictoryScreen>();
            }
        }

        if (_instance != null)
        {
            _instance.Show();
            return;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Show()
    {
        AssignDefaultSpritesInEditor();
        if (_root == null)
        {
            BuildScreen();
        }

        _root.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void BuildScreen()
    {
        if (_root != null)
        {
            return;
        }

        RectTransform root = CreateRect("VictoryScreen", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        _root = root.gameObject;

        Image background = _root.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = true;

        CreateImage("FirecrackerLeft", root, firecrackerLeftSprite, new Vector2(0f, 0f), new Vector2(380f, 160f), new Vector2(570f, 420f), false);
        CreateImage("FirecrackerRight", root, firecrackerRightSprite, new Vector2(1f, 0f), new Vector2(-380f, 160f), new Vector2(570f, 420f), false);
        CreateImage("VictoryTitle", root, victoryTitleSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(650f, 145f), false);
        CreateButton("MenuButton", root, menuButtonSprite, new Vector2(0f, -40f), new Vector2(420f, 78f), ReturnToMenu);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, bool raycastTarget)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(name, parent, sprite, new Vector2(0.5f, 0.5f), position, size, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(onClick);
        return button;
    }

    private void AssignDefaultSpritesInEditor()
    {
#if UNITY_EDITOR
        if (victoryTitleSprite == null)
        {
            victoryTitleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/\u0412\u042b \u0412\u042b\u0418\u0413\u0420\u0410\u041b\u0418.png");
        }

        if (menuButtonSprite == null)
        {
            menuButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MenuButton.png");
        }

        if (firecrackerLeftSprite == null)
        {
            firecrackerLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/firecrackerLeft.png");
        }

        if (firecrackerRightSprite == null)
        {
            firecrackerRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/firecrackerRight.png");
        }
#endif
    }

    private void OnValidate()
    {
        AssignDefaultSpritesInEditor();
    }
}
