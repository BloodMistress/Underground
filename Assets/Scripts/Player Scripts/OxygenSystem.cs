using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float startOxygen = 100f;
    [SerializeField] private float toxicGasDrainRate = 5f;
    [SerializeField] private float waterDrainRate = 9f;
    [SerializeField] private float recoveryRate = 8f;

    [Header("Breathing Check")]
    [SerializeField] private Transform breathingPoint;
    [SerializeField] private bool useMainCameraAsBreathingPoint = true;

    [Header("UI")]
    [SerializeField] private Slider oxygenSlider;

    [Header("Underwater Audio")]
    [SerializeField] private AudioClip underwaterAmbientClip;
    [SerializeField] private AudioSource underwaterAmbientSource;
    [SerializeField] private float underwaterAmbientVolume = 0.65f;
    [SerializeField] private float underwaterFadeSpeed = 4f;

    [Header("Underwater View")]
    [SerializeField] private bool createUnderwaterViewOverlay = true;
    [SerializeField] private Color underwaterOverlayColor = new Color(0.14f, 0.55f, 0.78f, 0.28f);
    [SerializeField] private float underwaterOverlayFadeSpeed = 3.5f;
    [SerializeField] private Image underwaterOverlayImage;

    [Header("Events")]
    public UnityEvent onOxygenEmpty;

    private float _oxygen;
    private int _waterZoneCount;
    private int _toxicGasZoneCount;
    private bool _isDead;
    private bool _wasInWater;
    private Canvas _underwaterOverlayCanvas;

    public float CurrentOxygen => _oxygen;
    public float MaxOxygen => maxOxygen;
    public float Oxygen01 => maxOxygen <= 0f ? 0f : _oxygen / maxOxygen;
    public bool IsInWater => IsBreathingPointInWater();
    public bool IsInToxicGas => _toxicGasZoneCount > 0;
    public bool IsInBreathHazard => IsInWater || IsInToxicGas;

    private void Awake()
    {
        _oxygen = Mathf.Clamp(startOxygen, 0f, maxOxygen);
        ResolveBreathingPoint();
        AssignDefaultUnderwaterAudioInEditor();

        if (oxygenSlider == null)
        {
            oxygenSlider = FindFirstObjectByType<Slider>();
        }

        EnsureUnderwaterAudioSource();
        EnsureUnderwaterViewOverlay();
        RefreshUI();
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        ResolveBreathingPoint();
        UpdateOxygen(Time.deltaTime);
        UpdateUnderwaterAudio(Time.deltaTime);
        UpdateUnderwaterViewOverlay(Time.deltaTime);
        RefreshUI();

        if (_oxygen <= 0f)
        {
            Die();
        }
    }

    public void EnterAirZone()
    {
        // Kept for compatibility with existing AirZone objects. Safe air is now the default state.
    }

    public void ExitAirZone()
    {
        // Kept for compatibility with existing AirZone objects. Safe air is now the default state.
    }

    public void EnterWaterZone()
    {
        _waterZoneCount++;
    }

    public void ExitWaterZone()
    {
        _waterZoneCount = Mathf.Max(0, _waterZoneCount - 1);
    }

    public void EnterToxicGasZone()
    {
        _toxicGasZoneCount++;
    }

    public void ExitToxicGasZone()
    {
        _toxicGasZoneCount = Mathf.Max(0, _toxicGasZoneCount - 1);
    }

    public void AddOxygen(float amount)
    {
        _oxygen = Mathf.Clamp(_oxygen + amount, 0f, maxOxygen);
        RefreshUI();
    }

    private void UpdateOxygen(float deltaTime)
    {
        if (!IsInBreathHazard)
        {
            _oxygen = Mathf.Min(maxOxygen, _oxygen + recoveryRate * deltaTime);
            return;
        }

        float drainRate = 0f;
        if (IsInWater)
        {
            drainRate = Mathf.Max(drainRate, waterDrainRate);
        }
        if (IsInToxicGas)
        {
            drainRate = Mathf.Max(drainRate, toxicGasDrainRate);
        }

        _oxygen = Mathf.Clamp(_oxygen - drainRate * deltaTime, 0f, maxOxygen);
    }

    private bool IsBreathingPointInWater()
    {
        if (breathingPoint != null && WaterZone.ContainsPoint(breathingPoint.position))
        {
            return true;
        }

        return _waterZoneCount > 0;
    }

    private void ResolveBreathingPoint()
    {
        if (breathingPoint != null || !useMainCameraAsBreathingPoint)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            breathingPoint = mainCamera.transform;
            return;
        }

        Transform cameraTarget = transform.Find("PlayerCameraRoot");
        if (cameraTarget == null)
        {
            cameraTarget = transform.Find("CinemachineCameraTarget");
        }

        breathingPoint = cameraTarget;
    }

    private void RefreshUI()
    {
        if (oxygenSlider == null)
        {
            return;
        }

        oxygenSlider.minValue = 0f;
        oxygenSlider.maxValue = maxOxygen;
        oxygenSlider.value = _oxygen;
    }

    private void EnsureUnderwaterAudioSource()
    {
        AssignDefaultUnderwaterAudioInEditor();
        if (underwaterAmbientClip == null)
        {
            return;
        }

        if (underwaterAmbientSource == null)
        {
            underwaterAmbientSource = gameObject.AddComponent<AudioSource>();
        }

        underwaterAmbientSource.clip = underwaterAmbientClip;
        underwaterAmbientSource.loop = true;
        underwaterAmbientSource.playOnAwake = false;
        underwaterAmbientSource.spatialBlend = 0f;
        underwaterAmbientSource.volume = 0f;
    }

    private void UpdateUnderwaterAudio(float deltaTime)
    {
        EnsureUnderwaterAudioSource();
        if (underwaterAmbientSource == null || underwaterAmbientClip == null)
        {
            return;
        }

        bool isInWater = IsInWater;
        if (isInWater && !underwaterAmbientSource.isPlaying)
        {
            underwaterAmbientSource.Play();
        }

        float targetVolume = isInWater ? underwaterAmbientVolume : 0f;
        underwaterAmbientSource.volume = Mathf.MoveTowards(underwaterAmbientSource.volume, targetVolume, underwaterFadeSpeed * deltaTime);

        if (!isInWater && _wasInWater && underwaterAmbientSource.volume <= 0.001f)
        {
            underwaterAmbientSource.Stop();
        }

        _wasInWater = isInWater;
    }

    private void EnsureUnderwaterViewOverlay()
    {
        if (!createUnderwaterViewOverlay || underwaterOverlayImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("UnderwaterViewOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _underwaterOverlayCanvas = canvasObject.GetComponent<Canvas>();
        _underwaterOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _underwaterOverlayCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlayRect = new GameObject("UnderwaterViewOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        overlayRect.SetParent(canvasObject.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        underwaterOverlayImage = overlayRect.GetComponent<Image>();
        underwaterOverlayImage.color = new Color(underwaterOverlayColor.r, underwaterOverlayColor.g, underwaterOverlayColor.b, 0f);
        underwaterOverlayImage.raycastTarget = false;
    }

    private void UpdateUnderwaterViewOverlay(float deltaTime)
    {
        EnsureUnderwaterViewOverlay();
        if (underwaterOverlayImage == null)
        {
            return;
        }

        bool isInWater = IsInWater;
        float targetAlpha = isInWater ? underwaterOverlayColor.a : 0f;
        Color currentColor = underwaterOverlayImage.color;
        currentColor.r = underwaterOverlayColor.r;
        currentColor.g = underwaterOverlayColor.g;
        currentColor.b = underwaterOverlayColor.b;
        currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, underwaterOverlayFadeSpeed * deltaTime);
        underwaterOverlayImage.color = currentColor;

        if (_underwaterOverlayCanvas != null)
        {
            _underwaterOverlayCanvas.enabled = currentColor.a > 0.001f || isInWater;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AssignDefaultUnderwaterAudio();
    }

    private void OnValidate()
    {
        AssignDefaultUnderwaterAudio();
    }

    private void AssignDefaultUnderwaterAudio()
    {
        AssignDefaultUnderwaterAudioInEditor();
    }
#endif

    private void AssignDefaultUnderwaterAudioInEditor()
    {
#if UNITY_EDITOR
        if (underwaterAmbientClip != null)
        {
            return;
        }

        underwaterAmbientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/music and sounds/ambientsound_underwater.wav");
        if (underwaterAmbientClip != null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("ambientsound_underwater t:AudioClip", new[] { "Assets/music and sounds" });
        if (guids.Length > 0)
        {
            underwaterAmbientClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
#endif
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("Player ran out of oxygen.");
        onOxygenEmpty?.Invoke();
        GameOverScreen.ShowGameOver();
    }
}
