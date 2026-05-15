using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    public string inputPrefix = "P1";

    [Header("Movement")]
    public float laneOffset = 2f;
    public float lateralSpeed = 10f;
    public float jumpForce = 7f;
    public LayerMask groundMask;

    [Header("Ground Check")]
    public float extraGroundCheck = 0.05f;

    [Header("Finish (info only)")]
    public float finishZ;
    private bool finished = false;

    private Rigidbody rb;
    private CapsuleCollider cap;
    private int currentLane = 1;
    private bool axisInUse;
    private float trackCenterX = 0f;

    // Управление вводом (можно отключать извне)
    private bool inputEnabled = true;

    public bool IsAirborne { get; private set; }
    private PlayerHealth health;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cap = GetComponent<CapsuleCollider>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        health = GetComponent<PlayerHealth>();
    }

    public void SetTrackCenterX(float x) => trackCenterX = x;

    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    void Update()
    {
        HandleInput();
        CheckGround();
    }

    void HandleInput()
    {
        if (!inputEnabled) return;

        float h = Input.GetAxisRaw(inputPrefix + "_Horiz");

        if (!axisInUse)
        {
            if (h <= -0.5f) { ChangeLane(-1); axisInUse = true; }
            else if (h >= 0.5f) { ChangeLane(+1); axisInUse = true; }
        }
        if (Mathf.Abs(h) < 0.1f) axisInUse = false;

        if (Input.GetButtonDown(inputPrefix + "_Jump") && !IsAirborne)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void ChangeLane(int delta)
    {
        currentLane = Mathf.Clamp(currentLane + delta, 0, 2);
    }

    void FixedUpdate()
    {
        Vector3 pos = rb.position;
        float targetX = trackCenterX + (currentLane - 1) * laneOffset;
        float newX = Mathf.MoveTowards(pos.x, targetX, lateralSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector3(newX, pos.y, pos.z));
    }

    void CheckGround()
    {
        Vector3 feetCenter = transform.position + Vector3.down * (cap.height * 0.5f - cap.radius);
        float checkRadius = cap.radius * 0.9f;
        float checkDistance = extraGroundCheck + 0.01f;

        IsAirborne = !Physics.CheckSphere(feetCenter, checkRadius + checkDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    public void TakeDamage(int dmg)
    {
        if (health != null) health.Damage(dmg);
    }

   
    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        // Запускаем корутину на этом объекте — она будет работать даже если Bonus уничтожится
        StartCoroutine(ApplySpeedCoroutine(multiplier, duration));
    }

    private IEnumerator ApplySpeedCoroutine(float multiplier, float duration)
    {
        float original = lateralSpeed;
        lateralSpeed = lateralSpeed * multiplier;
        Debug.Log($"[PlayerController] {name} lateralSpeed -> {lateralSpeed} (mult {multiplier})");
        yield return new WaitForSeconds(duration);
        lateralSpeed = original;
        Debug.Log($"[PlayerController] {name} lateralSpeed restored -> {lateralSpeed}");
    }
}
