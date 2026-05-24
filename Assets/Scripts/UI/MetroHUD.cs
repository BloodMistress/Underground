using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class MetroHUD : MonoBehaviour
{
    private static MetroHUD _instance;

    [Header("Oxygen")]
    [SerializeField] private Sprite oxygenTankSprite;
    [SerializeField] private Vector2 oxygenTankSize = new Vector2(340f, 64f);
    [SerializeField] private Vector2 oxygenTankPosition = new Vector2(18f, -18f);
    [SerializeField] private Vector2 oxygenSliderSize = new Vector2(166f, 13f);
    [SerializeField] private Vector2 oxygenSliderOffset = new Vector2(88f, -1.5f);
    [SerializeField] private Color oxygenFillColor = new Color(0.58f, 0.88f, 0.93f, 1f);

    [Header("Inventory")]
    [SerializeField] private Sprite inventoryTitleSprite;
    [SerializeField] private Sprite equipmentSlotSprite;
    [SerializeField] private Vector2 inventoryPosition = new Vector2(-126f, 34f);
    [SerializeField] private Vector2 titleSize = new Vector2(135f, 52f);
    [SerializeField] private Vector2 slotSize = new Vector2(62f, 58f);
    [SerializeField] private Vector2 itemIconSize = new Vector2(52f, 52f);
    [SerializeField] private float slotSpacing = 74f;
    [SerializeField] private int visibleSlots = 3;
    [SerializeField] private Color selectedSlotColor = new Color(0.45f, 0.95f, 1f, 1f);
    [SerializeField] private Color emptySelectedSlotColor = new Color(0.45f, 0.95f, 1f, 0.55f);
    [SerializeField] private Vector3 selectedSlotScale = new Vector3(1.08f, 1.08f, 1f);

    private readonly List<Image> _slotImages = new List<Image>();
    private readonly List<Image> _itemImages = new List<Image>();
    private int _selectedSlotIndex = -1;

    public static int SelectedSlotIndex => _instance != null ? _instance._selectedSlotIndex : -1;
    public static Sprite SelectedItemSprite => _instance != null ? _instance.GetSelectedItemSprite() : null;
    public static string SelectedItemName
    {
        get
        {
            Sprite selectedSprite = SelectedItemSprite;
            return selectedSprite != null ? selectedSprite.name : string.Empty;
        }
    }

    private void Awake()
    {
        _instance = this;
        ConfigureCanvas();
        BuildOxygenHUD();
        BuildInventoryHUD();
    }

    private void Update()
    {
        HandleInventorySelectionInput();
    }

    public static bool TryAddInventoryItem(Sprite itemSprite)
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<MetroHUD>();
        }

        return _instance != null && _instance.AddInventoryItem(itemSprite);
    }

    private bool AddInventoryItem(Sprite itemSprite)
    {
        if (itemSprite == null)
        {
            return false;
        }

        foreach (Image itemImage in _itemImages)
        {
            if (itemImage.sprite != null)
            {
                continue;
            }

            itemImage.sprite = itemSprite;
            itemImage.enabled = true;
            if (_selectedSlotIndex < 0)
            {
                SelectInventorySlot(_itemImages.IndexOf(itemImage));
            }
            else
            {
                RefreshInventorySelectionVisuals();
            }

            return true;
        }

        return false;
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
        _slotImages.Clear();
        _itemImages.Clear();
        RectTransform panel = CreateRect("InventoryHUD", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), inventoryPosition, new Vector2(260f, 120f));

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
            _slotImages.Add(slotImage);

            RectTransform item = CreateRect("ItemIcon", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, itemIconSize);
            Image itemImage = GetOrAdd<Image>(item.gameObject);
            itemImage.sprite = null;
            itemImage.enabled = false;
            itemImage.color = Color.white;
            itemImage.raycastTarget = false;
            itemImage.preserveAspect = true;
            item.SetAsLastSibling();
            _itemImages.Add(itemImage);
        }

        if (_selectedSlotIndex >= visibleSlots)
        {
            _selectedSlotIndex = -1;
        }

        RefreshInventorySelectionVisuals();
    }

    private void HandleInventorySelectionInput()
    {
        for (int i = 0; i < visibleSlots; i++)
        {
            if (WasInventorySlotPressed(i))
            {
                SelectInventorySlot(i);
                return;
            }
        }
    }

    private bool WasInventorySlotPressed(int index)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (index)
        {
            case 0:
                return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
            case 1:
                return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
            case 2:
                return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
            default:
                return false;
        }
#else
        return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + index));
#endif
    }

    private void SelectInventorySlot(int index)
    {
        if (index < 0 || index >= _itemImages.Count)
        {
            return;
        }

        _selectedSlotIndex = index;
        RefreshInventorySelectionVisuals();
    }

    private void RefreshInventorySelectionVisuals()
    {
        for (int i = 0; i < _slotImages.Count; i++)
        {
            Image slotImage = _slotImages[i];
            if (slotImage == null)
            {
                continue;
            }

            bool isSelected = i == _selectedSlotIndex;
            bool hasItem = i < _itemImages.Count && _itemImages[i] != null && _itemImages[i].sprite != null;
            slotImage.color = isSelected ? (hasItem ? selectedSlotColor : emptySelectedSlotColor) : Color.white;
            slotImage.transform.localScale = isSelected ? selectedSlotScale : Vector3.one;
        }
    }

    private Sprite GetSelectedItemSprite()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _itemImages.Count)
        {
            return null;
        }

        return _itemImages[_selectedSlotIndex] != null ? _itemImages[_selectedSlotIndex].sprite : null;
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
