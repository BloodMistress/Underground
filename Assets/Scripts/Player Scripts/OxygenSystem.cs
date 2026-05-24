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

    [Header("Events")]
    public UnityEvent onOxygenEmpty;

    private float _oxygen;
    private int _waterZoneCount;
    private int _toxicGasZoneCount;
    private bool _isDead;
    private bool _wasInWater;

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

        if (oxygenSlider == null)
        {
            oxygenSlider = FindFirstObjectByType<Slider>();
        }

        EnsureUnderwaterAudioSource();
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
        if (breathingPoint != null)
        {
            return WaterZone.ContainsPoint(breathingPoint.position);
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
        if (underwaterAmbientClip != null)
        {
            return;
        }

        underwaterAmbientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/music and sounds/ambientsound_underwater.wav");
    }
#endif

    private void Die()
    {
        _isDead = true;
        Debug.Log("Player ran out of oxygen.");
        onOxygenEmpty?.Invoke();
        GameOverScreen.ShowGameOver();
    }
}
