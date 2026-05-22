using StarterAssets;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerStealthProfile : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private float standingVisibility = 1f;
    [SerializeField] private float crouchingVisibility = 0.35f;
    [SerializeField] private float underwaterVisibility = 0.08f;
    [SerializeField] private float movingVisibilityBonus = 0.25f;

    [Header("Noise")]
    [SerializeField] private float walkNoiseRadius = 5f;
    [SerializeField] private float sprintNoiseRadius = 10f;
    [SerializeField] private float crouchNoiseRadius = 2f;
    [SerializeField] private float underwaterNoiseMultiplier = 0.35f;
    [SerializeField] private float minAudibleSpeed = 0.15f;

    [Header("Water Check")]
    [SerializeField] private Transform visibilityCheckPoint;
    [SerializeField] private bool useMainCameraAsVisibilityPoint = true;

    private CharacterController _controller;
    private FirstPersonController _firstPersonController;

    public bool IsCrouching => _firstPersonController != null && _firstPersonController.IsCrouching;
    public bool IsUnderwater => IsVisibilityPointInWater();
    public float HorizontalSpeed => _controller == null ? 0f : new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

    public float VisibilityMultiplier
    {
        get
        {
            if (IsUnderwater)
            {
                return Mathf.Clamp(underwaterVisibility, 0.01f, 1f);
            }

            float visibility = IsCrouching ? crouchingVisibility : standingVisibility;
            if (HorizontalSpeed > minAudibleSpeed && !IsCrouching)
            {
                visibility += movingVisibilityBonus;
            }
            return Mathf.Clamp(visibility, 0.05f, 2f);
        }
    }

    public float NoiseRadius
    {
        get
        {
            if (HorizontalSpeed < minAudibleSpeed)
            {
                return 0f;
            }

            float noiseRadius = IsCrouching ? crouchNoiseRadius : (HorizontalSpeed > 4.5f ? sprintNoiseRadius : walkNoiseRadius);
            return IsUnderwater ? noiseRadius * underwaterNoiseMultiplier : noiseRadius;
        }
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _firstPersonController = GetComponent<FirstPersonController>();
        ResolveVisibilityPoint();
    }

    private void Update()
    {
        ResolveVisibilityPoint();
    }

    private bool IsVisibilityPointInWater()
    {
        if (visibilityCheckPoint == null)
        {
            return false;
        }

        return WaterZone.ContainsPoint(visibilityCheckPoint.position);
    }

    private void ResolveVisibilityPoint()
    {
        if (visibilityCheckPoint != null || !useMainCameraAsVisibilityPoint)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            visibilityCheckPoint = mainCamera.transform;
            return;
        }

        Transform cameraTarget = transform.Find("PlayerCameraRoot");
        if (cameraTarget == null)
        {
            cameraTarget = transform.Find("CinemachineCameraTarget");
        }

        visibilityCheckPoint = cameraTarget;
    }
}
