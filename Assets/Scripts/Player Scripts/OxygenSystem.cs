using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float startOxygen = 100f;
    [SerializeField] private float toxicGasDrainRate = 5f;
    [SerializeField] private float waterDrainRate = 9f;
    [SerializeField] private float recoveryRate = 18f;
    [SerializeField] private float safeAirRecoveryRate = 8f;
    [SerializeField] private float heldBreathDrainRate = 0f;
    [SerializeField] private float holdBreathDuration = 8f;
    [SerializeField] private float holdBreathRecoveryDelay = 3f;

    [Header("UI")]
    [SerializeField] private Slider oxygenSlider;

    [Header("Events")]
    public UnityEvent onOxygenEmpty;

    private float _oxygen;
    private float _heldBreathRemaining;
    private float _holdBreathCooldown;
    private int _airZoneCount;
    private int _waterZoneCount;
    private int _toxicGasZoneCount;
    private bool _isDead;

    public float CurrentOxygen => _oxygen;
    public float MaxOxygen => maxOxygen;
    public float Oxygen01 => maxOxygen <= 0f ? 0f : _oxygen / maxOxygen;
    public float HeldBreathRemaining => _heldBreathRemaining;
    public float HeldBreath01 => holdBreathDuration <= 0f ? 0f : _heldBreathRemaining / holdBreathDuration;
    public bool IsInAirZone => _airZoneCount > 0;
    public bool IsInWater => _waterZoneCount > 0;
    public bool IsInToxicGas => _toxicGasZoneCount > 0;
    public bool IsInBreathHazard => IsInWater || IsInToxicGas;
    public bool IsHoldingBreath => IsInBreathHazard && CanHoldBreathInput() && _heldBreathRemaining > 0f && _holdBreathCooldown <= 0f && !IsInAirZone;

    private void Awake()
    {
        _oxygen = Mathf.Clamp(startOxygen, 0f, maxOxygen);
        _heldBreathRemaining = holdBreathDuration;

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
        _airZoneCount++;
    }

    public void ExitAirZone()
    {
        _airZoneCount = Mathf.Max(0, _airZoneCount - 1);
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
        if (IsInAirZone)
        {
            RecoverOxygen(recoveryRate, deltaTime);
            RechargeHeldBreath(deltaTime);
            return;
        }

        if (!IsInBreathHazard)
        {
            RecoverOxygen(safeAirRecoveryRate, deltaTime);
            RechargeHeldBreath(deltaTime);
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

        if (IsHoldingBreath)
        {
            drainRate = heldBreathDrainRate;
            _heldBreathRemaining = Mathf.Max(0f, _heldBreathRemaining - deltaTime);

            if (_heldBreathRemaining <= 0f)
            {
                _holdBreathCooldown = holdBreathRecoveryDelay;
            }
        }
        else
        {
            if (_holdBreathCooldown > 0f)
            {
                _holdBreathCooldown -= deltaTime;
            }
        }

        _oxygen = Mathf.Clamp(_oxygen - drainRate * deltaTime, 0f, maxOxygen);
    }

    private void RecoverOxygen(float rate, float deltaTime)
    {
        _oxygen = Mathf.Min(maxOxygen, _oxygen + rate * deltaTime);
    }

    private void RechargeHeldBreath(float deltaTime)
    {
        if (_holdBreathCooldown <= 0f)
        {
            _heldBreathRemaining = Mathf.Min(holdBreathDuration, _heldBreathRemaining + deltaTime);
        }
    }

    private bool CanHoldBreathInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        bool keyboardHold = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
        bool mouseHold = Mouse.current != null && Mouse.current.rightButton.isPressed;
        return keyboardHold || mouseHold;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetMouseButton(1);
#else
        return false;
#endif
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
