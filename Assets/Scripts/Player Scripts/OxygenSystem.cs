using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float startOxygen = 100f;
    [SerializeField] private float toxicGasDrainRate = 5f;
    [SerializeField] private float waterDrainRate = 9f;
    [SerializeField] private float recoveryRate = 8f;

    [Header("UI")]
    [SerializeField] private Slider oxygenSlider;

    [Header("Events")]
    public UnityEvent onOxygenEmpty;

    private float _oxygen;
    private int _waterZoneCount;
    private int _toxicGasZoneCount;
    private bool _isDead;

    public float CurrentOxygen => _oxygen;
    public float MaxOxygen => maxOxygen;
    public float Oxygen01 => maxOxygen <= 0f ? 0f : _oxygen / maxOxygen;
    public bool IsInWater => _waterZoneCount > 0;
    public bool IsInToxicGas => _toxicGasZoneCount > 0;
    public bool IsInBreathHazard => IsInWater || IsInToxicGas;

    private void Awake()
    {
        _oxygen = Mathf.Clamp(startOxygen, 0f, maxOxygen);

        if (oxygenSlider == null)
        {
            oxygenSlider = FindFirstObjectByType<Slider>();
        }

        RefreshUI();
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        UpdateOxygen(Time.deltaTime);
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

    private void Die()
    {
        _isDead = true;
        Debug.Log("Player ran out of oxygen.");
        onOxygenEmpty?.Invoke();
        Time.timeScale = 0f;
    }
}
