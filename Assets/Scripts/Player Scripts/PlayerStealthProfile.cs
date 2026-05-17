using StarterAssets;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerStealthProfile : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private float standingVisibility = 1f;
    [SerializeField] private float crouchingVisibility = 0.35f;
    [SerializeField] private float movingVisibilityBonus = 0.25f;

    [Header("Noise")]
    [SerializeField] private float walkNoiseRadius = 5f;
    [SerializeField] private float sprintNoiseRadius = 10f;
    [SerializeField] private float crouchNoiseRadius = 2f;
    [SerializeField] private float minAudibleSpeed = 0.15f;

    private CharacterController _controller;
    private FirstPersonController _firstPersonController;

    public bool IsCrouching => _firstPersonController != null && _firstPersonController.IsCrouching;
    public float HorizontalSpeed => _controller == null ? 0f : new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

    public float VisibilityMultiplier
    {
        get
        {
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

            if (IsCrouching)
            {
                return crouchNoiseRadius;
            }

            return HorizontalSpeed > 4.5f ? sprintNoiseRadius : walkNoiseRadius;
        }
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _firstPersonController = GetComponent<FirstPersonController>();
    }
}
