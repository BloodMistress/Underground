using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class MetroHUD : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private Sprite oxygenTankSprite;
    [SerializeField] private Vector2 oxygenTankSize = new Vector2(320f, 61f);
    [SerializeField] private Vector2 oxygenTankPosition = new Vector2(18f, -18f);
    [SerializeField] private Vector2 oxygenSliderSize = new Vector2(168f, 11f);
    [SerializeField] private Vector2 oxygenSliderOffset = new Vector2(111f, -3f);
    [SerializeField] private Color oxygenFillColor = new Color(0.58f, 0.88f, 0.93f, 1f);

    [Header("Inventory")]
    [SerializeField] private Sprite inventoryTitleSprite;
    [SerializeField] private Sprite equipmentSlotSprite;
    [SerializeField] private Vector2 inventoryPosition = new Vector2(0f, 34f);
    [SerializeField] private Vector2 titleSize = new Vector2(135f, 52f);
    [SerializeField] private Vector2 slotSize = new Vector2(62f, 58f);
    [SerializeField] private float slotSpacing = 74f;
    [SerializeField] private int visibleSlots = 3;

    private void Awake()
    {
        ConfigureCanvas();
        BuildOxygenHUD();
        BuildInventoryHUD();
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void BuildOxygenHUD()
    {
        RectTransform panel = CreateRect("OxygenHUD", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), oxygenTankPosition, oxygenTankSize);

        Image tank = GetOrAdd<Image>(panel.gameObject);
        tank.sprite = oxygenTankSprite;
        tank.color = Color.white;
        tank.raycastTarget = false;
        tank.preserveAspect = true;

        Slider slider = FindFirstObjectByType<Slider>();
        if (slider == null)
        {
            slider = CreateRect("OxygenSlider", panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), oxygenSliderOffset, oxygenSliderSize).gameObject.AddComponent<Slider>();
        }

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.SetParent(panel, false);
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = oxygenSliderOffset;
        sliderRect.sizeDelta = oxygenSliderSize;
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        Transform background = sliderRect.Find("Background");
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }

        Transform handle = sliderRect.Find("Handle Slide Area");
        if (handle != null)
        {
            handle.gameObject.SetActive(false);
            slider.handleRect = null;
        }

        RectTransform fillArea = sliderRect.Find("Fill Area") as RectTransform;
        if (fillArea == null)
        {
            fillArea = CreateRect("Fill Area", sliderRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        RectTransform fill = fillArea.Find("Fill") as RectTransform;
        if (fill == null)
        {
            fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.gameObject.AddComponent<Image>();
        }

        Image fillImage = GetOrAdd<Image>(fill.gameObject);
        fillImage.color = oxygenFillColor;
        fillImage.raycastTarget = false;
        slider.fillRect = fill;
        slider.targetGraphic = fillImage;
        sliderRect.SetAsLastSibling();
    }

    private void BuildInventoryHUD()
    {
        RectTransform panel = CreateRect("InventoryHUD", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), inventoryPosition, new Vector2(260f, 120f));

        RectTransform title = CreateRect("InventoryTitle", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), titleSize);
        Image titleImage = GetOrAdd<Image>(title.gameObject);
        titleImage.sprite = inventoryTitleSprite;
        titleImage.color = Color.white;
        titleImage.raycastTarget = false;
        titleImage.preserveAspect = true;

        float firstX = -slotSpacing * (visibleSlots - 1) * 0.5f;
        for (int i = 0; i < visibleSlots; i++)
        {
            RectTransform slot = CreateRect("InventorySlot_" + (i + 1), panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(firstX + slotSpacing * i, 0f), slotSize);
            Image slotImage = GetOrAdd<Image>(slot.gameObject);
            slotImage.sprite = equipmentSlotSprite;
            slotImage.color = Color.white;
            slotImage.raycastTarget = false;
            slotImage.preserveAspect = true;
        }
    }

    private RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform existing = parent.Find(objectName);
        GameObject child = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
