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

    [Header("Monster Pressure")]
    [SerializeField] private Transform monsterThreat;
    [SerializeField] private float monsterThreatRadius = 6f;
    [SerializeField] private float monsterThreatExtraDrainRate = 7f;

    [Header("Bucket Oxygen")]
    [SerializeField] private float bucketOxygenAmount = 18f;

    [Header("Breathing Check")]
    [SerializeField] private Transform breathingPoint;
    [SerializeField] private bool useMainCameraAsBreathingPoint = true;

    [Header("UI")]
    [SerializeField] private Slider oxygenSlider;

    [Header("Underwater Audio")]
    [SerializeField] private AudioClip underwaterAmbientClip;
    [SerializeField] private AudioSource underwaterAmbientSource;
    [SerializeField] private float underwaterAmbientVolume = 0.85f;
    [SerializeField] private float underwaterAmbientVolumeMultiplier = 1.35f;
    [SerializeField] private float underwaterFadeSpeed = 4f;

    [Header("Underwater View")]
    [SerializeField] private bool createUnderwaterViewOverlay = true;
    [SerializeField] private Color underwaterOverlayColor = new Color(0.14f, 0.55f, 0.78f, 0.28f);
    [SerializeField] private float underwaterOverlayFadeSpeed = 3.5f;
    [SerializeField] private Image underwaterOverlayImage;

    [Header("Critical Oxygen Warning")]
    [SerializeField] private bool createCriticalOxygenOverlay = true;
    [SerializeField] private Color criticalOxygenOverlayColor = new Color(1f, 0.04f, 0.02f, 0.42f);
    [SerializeField] private float criticalOxygenThreshold = 0.25f;
    [SerializeField] private float criticalOxygenFadeSpeed = 5f;
    [SerializeField] private float criticalOxygenPulseSpeed = 2.5f;
    [SerializeField] private Image criticalOxygenOverlayImage;

    [Header("Events")]
    public UnityEvent onOxygenEmpty;

    private float _oxygen;
    private int _waterZoneCount;
    private int _toxicGasZoneCount;
    private bool _isDead;
    private bool _wasInWater;
    private bool _underwaterAudioConfigured;
    private bool _bucketOxygenAvailableThisDive;
    private bool _wasTouchingWaterForBucket;
    private Canvas _underwaterOverlayCanvas;
    private Canvas _criticalOxygenOverlayCanvas;

    public float CurrentOxygen => _oxygen;
    public float MaxOxygen => maxOxygen;
    public float Oxygen01 => maxOxygen <= 0f ? 0f : _oxygen / maxOxygen;
    public bool IsInWater => IsBreathingPointInWater();
    public bool IsTouchingWater => _waterZoneCount > 0 || IsInWater;
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
        EnsureCriticalOxygenOverlay();
        RefreshUI();
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        ResolveBreathingPoint();
        ResolveMonsterThreat();
        UpdateBucketOxygenAvailability();
        UpdateOxygen(Time.deltaTime);
        UpdateUnderwaterAudio(Time.deltaTime);
        UpdateUnderwaterViewOverlay(Time.deltaTime);
        UpdateCriticalOxygenOverlay(Time.deltaTime);
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

    public bool TryUseBucketOxygen()
    {
        UpdateBucketOxygenAvailability();

        if (!IsInWater || !_bucketOxygenAvailableThisDive || bucketOxygenAmount <= 0f || _oxygen >= maxOxygen - 0.01f)
        {
            return false;
        }

        _bucketOxygenAvailableThisDive = false;
        AddOxygen(bucketOxygenAmount);
        return true;
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

        drainRate += GetMonsterThreatDrainRate();
        _oxygen = Mathf.Clamp(_oxygen - drainRate * deltaTime, 0f, maxOxygen);
    }

    private float GetMonsterThreatDrainRate()
    {
        if (monsterThreat == null || monsterThreatRadius <= 0f || monsterThreatExtraDrainRate <= 0f)
        {
            return 0f;
        }

        float distance = Vector3.Distance(transform.position, monsterThreat.position);
        if (distance > monsterThreatRadius)
        {
            return 0f;
        }

        float proximity01 = 1f - Mathf.Clamp01(distance / monsterThreatRadius);
        return monsterThreatExtraDrainRate * proximity01;
    }

    private void UpdateBucketOxygenAvailability()
    {
        bool isTouchingWater = IsTouchingWater;
        if (isTouchingWater && !_wasTouchingWaterForBucket)
        {
            _bucketOxygenAvailableThisDive = true;
        }
        else if (!isTouchingWater)
        {
            _bucketOxygenAvailableThisDive = false;
        }

        _wasTouchingWaterForBucket = isTouchingWater;
    }

    private bool IsBreathingPointInWater()
    {
        if (breathingPoint == null)
        {
            return false;
        }

        return WaterZone.ContainsPoint(breathingPoint.position);
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

    private void ResolveMonsterThreat()
    {
        if (monsterThreat != null)
        {
            return;
        }

        MonsterAI monster = FindFirstObjectByType<MonsterAI>();
        if (monster != null)
        {
            monsterThreat = monster.transform;
        }
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

        if (_underwaterAudioConfigured && underwaterAmbientSource.clip == underwaterAmbientClip)
        {
            return;
        }

        underwaterAmbientSource.clip = underwaterAmbientClip;
        underwaterAmbientSource.loop = true;
        underwaterAmbientSource.playOnAwake = false;
        underwaterAmbientSource.spatialBlend = 0f;
        underwaterAmbientSource.volume = 0f;
        underwaterAmbientSource.mute = false;
        underwaterAmbientSource.ignoreListenerVolume = true;
        underwaterAmbientSource.ignoreListenerPause = true;
        underwaterAmbientSource.priority = 32;
        _underwaterAudioConfigured = true;
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

        float targetVolume = isInWater ? Mathf.Clamp01(underwaterAmbientVolume * underwaterAmbientVolumeMultiplier) : 0f;
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

    private void EnsureCriticalOxygenOverlay()
    {
        if (!createCriticalOxygenOverlay || criticalOxygenOverlayImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("CriticalOxygenOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _criticalOxygenOverlayCanvas = canvasObject.GetComponent<Canvas>();
        _criticalOxygenOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _criticalOxygenOverlayCanvas.sortingOrder = 60;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlayRect = new GameObject("CriticalOxygenOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        overlayRect.SetParent(canvasObject.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        criticalOxygenOverlayImage = overlayRect.GetComponent<Image>();
        criticalOxygenOverlayImage.color = new Color(criticalOxygenOverlayColor.r, criticalOxygenOverlayColor.g, criticalOxygenOverlayColor.b, 0f);
        criticalOxygenOverlayImage.raycastTarget = false;
    }

    private void UpdateCriticalOxygenOverlay(float deltaTime)
    {
        EnsureCriticalOxygenOverlay();
        if (criticalOxygenOverlayImage == null)
        {
            return;
        }

        float threshold = Mathf.Clamp01(criticalOxygenThreshold);
        float targetAlpha = 0f;
        if (threshold > 0f && Oxygen01 <= threshold)
        {
            float severity = Mathf.InverseLerp(threshold, 0f, Oxygen01);
            float pulse = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(Time.time * criticalOxygenPulseSpeed, 1f));
            targetAlpha = criticalOxygenOverlayColor.a * severity * pulse;
        }

        Color currentColor = criticalOxygenOverlayImage.color;
        currentColor.r = criticalOxygenOverlayColor.r;
        currentColor.g = criticalOxygenOverlayColor.g;
        currentColor.b = criticalOxygenOverlayColor.b;
        currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, criticalOxygenFadeSpeed * deltaTime);
        criticalOxygenOverlayImage.color = currentColor;

        if (_criticalOxygenOverlayCanvas != null)
        {
            _criticalOxygenOverlayCanvas.enabled = currentColor.a > 0.001f || targetAlpha > 0f;
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
