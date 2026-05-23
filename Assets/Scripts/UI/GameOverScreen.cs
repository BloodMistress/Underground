using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private Sprite deathTitleSprite;
    [SerializeField] private Sprite startAgainSprite;
    [SerializeField] private Sprite menuButtonSprite;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private Color backgroundColor = new Color(0.27f, 0.08f, 0.08f, 1f);

    private static GameOverScreen _instance;
    private GameObject _root;

    private void Awake()
    {
        _instance = this;
        ConfigureCanvas();
        BuildScreen();
        Hide();
    }

    public static void ShowGameOver()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<GameOverScreen>();
        }

        if (_instance != null)
        {
            _instance.Show();
            return;
        }

        Time.timeScale = 0f;
    }

    private void Show()
    {
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

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        canvas.sortingOrder = 100;

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

        RectTransform root = CreateRect("GameOverScreen", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        _root = root.gameObject;

        Image background = _root.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = true;

        CreateImage("DeathTitle", root, deathTitleSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(610f, 130f), false);
        CreateButton("StartAgain", root, startAgainSprite, new Vector2(0f, -35f), new Vector2(505f, 82f), RestartGame);
        CreateButton("MenuButton", root, menuButtonSprite, new Vector2(0f, -160f), new Vector2(410f, 76f), ReturnToMenu);
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
}
