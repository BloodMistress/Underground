using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Crouch")]
        [Tooltip("Move speed while crouching in m/s")]
        public float CrouchSpeed = 1.7f;
        [Tooltip("CharacterController height while crouching")]
        public float CrouchHeight = 1.2f;
        [Tooltip("Local camera target Y position while crouching")]
        public float CrouchCameraHeight = 0.85f;
        [Tooltip("How quickly the body and camera blend between standing and crouching")]
        public float CrouchTransitionSpeed = 10.0f;
        [Tooltip("Enemy visibility multiplier while crouching. 1 is normal visibility")]
        [Range(0.1f, 1.0f)]
        public float CrouchVisibilityMultiplier = 0.45f;

        [Header("Swimming")]
        [Tooltip("Move speed multiplier while the player is in water")]
        public float SwimSpeedMultiplier = 0.8f;
        [Tooltip("CharacterController height while swimming")]
        public float SwimHeight = 0.9f;
        [Tooltip("Local camera target Y position while swimming")]
        public float SwimCameraHeight = 0.55f;
        [Tooltip("How much the visible capsule tilts forward while swimming")]
        public float SwimVisualPitch = 82.0f;
        [Tooltip("Idle vertical velocity while floating in water")]
        public float SwimFloatVelocity = 0.12f;
        [Tooltip("Upward speed while holding jump in water")]
        public float SwimUpSpeed = 2.4f;
        [Tooltip("Downward speed while holding crouch in water")]
        public float SwimDownSpeed = 1.4f;
        [Tooltip("How long swimming remains active after the body briefly leaves water. Prevents jitter at the water surface")]
        public float SwimExitGraceTime = 0.35f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        [Header("Camera Fallback")]
        [Tooltip("Use Main Camera directly when there is no Cinemachine PlayerFollowCamera in the scene")]
        public bool DriveMainCameraWithoutCinemachine = true;

        [Header("Footsteps")]
        [SerializeField] private AudioClip playerFootstepClip;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private float walkStepInterval = 0.55f;
        [SerializeField] private float sprintStepInterval = 0.36f;
        [SerializeField] private float crouchStepInterval = 0.78f;
        [SerializeField] private float walkFootstepVolume = 0.48f;
        [SerializeField] private float sprintFootstepVolume = 0.58f;
        [SerializeField] private float crouchFootstepVolume = 0.2f;
        [SerializeField] private float minFootstepSpeed = 0.18f;

        public bool IsCrouching { get; private set; }
        public bool IsSwimming { get; private set; }
        public float VisibilityMultiplier => IsCrouching ? CrouchVisibilityMultiplier : 1.0f;

        private float _cinemachineTargetPitch;
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _standingHeight;
        private Vector3 _standingCenter;
        private Vector3 _crouchingCenter;
        private Vector3 _standingCameraLocalPosition;
        private Transform _capsuleVisual;
        private Quaternion _capsuleVisualStandingLocalRotation;
        private System.Type _waterZoneType;
        private System.Reflection.MethodInfo _waterContainsPointMethod;
        private float _footstepTimer;
        private float _swimExitGraceTimer;
        private bool _footstepAudioConfigured;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;
            _crouchingCenter = _standingCenter - Vector3.up * ((_standingHeight - CrouchHeight) * 0.5f);

            if (CinemachineCameraTarget != null)
            {
                _standingCameraLocalPosition = CinemachineCameraTarget.transform.localPosition;
            }

            Transform capsule = transform.Find("Capsule");
            _capsuleVisual = capsule != null ? capsule : null;
            if (_capsuleVisual != null)
            {
                _capsuleVisualStandingLocalRotation = _capsuleVisual.localRotation;
            }

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            RotationSpeed = Mathf.Clamp(PlayerPrefs.GetFloat("settings.mouseSensitivity", RotationSpeed), 0.25f, 4.0f);
            EnsureFootstepAudioSource();
        }

        private void Update()
        {
            GroundedCheck();
            UpdateCrouch();
            JumpAndGravity();
            Move();
            UpdateFootsteps(Time.deltaTime);
        }

        private void LateUpdate()
        {
            CameraRotation();
            UpdateFallbackCamera();
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input != null && _input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                if (CinemachineCameraTarget != null)
                {
                    CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                }

                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void UpdateFallbackCamera()
        {
            if (!DriveMainCameraWithoutCinemachine || _mainCamera == null || CinemachineCameraTarget == null)
            {
                return;
            }

            if (GameObject.Find("PlayerFollowCamera") != null)
            {
                return;
            }

            Transform target = CinemachineCameraTarget.transform;
            _mainCamera.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        private void Move()
        {
            if (_input == null)
            {
                return;
            }

            float targetSpeed = IsSwimming ? MoveSpeed * SwimSpeedMultiplier : (IsCrouching ? CrouchSpeed : (_input.sprint ? SprintSpeed : MoveSpeed));

            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;
            }

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void UpdateCrouch()
        {
            UpdateSwimmingState();
            bool wantsCrouch = !IsSwimming && IsCrouchPressed();

            if (!wantsCrouch && IsCrouching && !CanStandUp())
            {
                wantsCrouch = true;
            }

            IsCrouching = wantsCrouch;

            float targetHeight = IsSwimming ? SwimHeight : (IsCrouching ? CrouchHeight : _standingHeight);
            Vector3 targetCenter = IsSwimming ? GetLoweredControllerCenter(targetHeight) : (IsCrouching ? _crouchingCenter : _standingCenter);
            float blend = 1f - Mathf.Exp(-CrouchTransitionSpeed * Time.deltaTime);

            _controller.height = Mathf.Lerp(_controller.height, targetHeight, blend);
            _controller.center = Vector3.Lerp(_controller.center, targetCenter, blend);

            if (CinemachineCameraTarget != null)
            {
                Vector3 targetCameraPosition = _standingCameraLocalPosition;
                targetCameraPosition.y = IsSwimming ? SwimCameraHeight : (IsCrouching ? CrouchCameraHeight : _standingCameraLocalPosition.y);
                CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, targetCameraPosition, blend);
            }

            if (_capsuleVisual != null)
            {
                Vector3 targetScale = _capsuleVisual.localScale;
                targetScale.y = IsSwimming ? SwimHeight / _standingHeight : (IsCrouching ? CrouchHeight / _standingHeight : 1.0f);
                _capsuleVisual.localScale = Vector3.Lerp(_capsuleVisual.localScale, targetScale, blend);

                Quaternion targetRotation = IsSwimming ? _capsuleVisualStandingLocalRotation * Quaternion.Euler(SwimVisualPitch, 0f, 0f) : _capsuleVisualStandingLocalRotation;
                _capsuleVisual.localRotation = Quaternion.Slerp(_capsuleVisual.localRotation, targetRotation, blend);
            }
        }

        private Vector3 GetLoweredControllerCenter(float targetHeight)
        {
            return _standingCenter - Vector3.up * ((_standingHeight - targetHeight) * 0.5f);
        }

        private bool IsPlayerTouchingWater()
        {
            Vector3 bodyPoint = transform.position + (_controller != null ? _controller.center : Vector3.up);
            if (IsPointInWater(bodyPoint))
            {
                return true;
            }

            if (_controller != null)
            {
                float halfHeight = _controller.height * 0.5f;
                Vector3 bottomPoint = transform.position + _controller.center + Vector3.down * halfHeight;
                Vector3 topPoint = transform.position + _controller.center + Vector3.up * halfHeight;
                if (IsPointInWater(bottomPoint) || IsPointInWater(topPoint))
                {
                    return true;
                }
            }

            return CinemachineCameraTarget != null && IsPointInWater(CinemachineCameraTarget.transform.position);
        }

        private void UpdateSwimmingState()
        {
            bool bodyIsInWater = IsPlayerInWaterForSwimming();
            if (bodyIsInWater)
            {
                IsSwimming = true;
                _swimExitGraceTimer = Mathf.Max(0f, SwimExitGraceTime);
                return;
            }

            if (IsSwimming && _swimExitGraceTimer > 0f)
            {
                _swimExitGraceTimer -= Time.deltaTime;
                return;
            }

            IsSwimming = false;
        }

        private bool IsPlayerInWaterForSwimming()
        {
            Vector3 centerPoint = transform.position + (_controller != null ? _standingCenter : Vector3.up);
            if (IsPointInWater(centerPoint))
            {
                return true;
            }

            if (_controller == null)
            {
                return false;
            }

            Vector3 lowerBodyPoint = centerPoint + Vector3.down * (_standingHeight * 0.25f);
            return IsPointInWater(lowerBodyPoint);
        }

        private bool IsPointInWater(Vector3 point)
        {
            if (_waterContainsPointMethod == null)
            {
                _waterZoneType = System.Type.GetType("WaterZone, Assembly-CSharp");
                _waterContainsPointMethod = _waterZoneType?.GetMethod("ContainsPoint", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            }

            if (_waterContainsPointMethod == null)
            {
                return false;
            }

            object result = _waterContainsPointMethod.Invoke(null, new object[] { point });
            return result is bool isInWater && isInWater;
        }

        private void EnsureFootstepAudioSource()
        {
            AssignDefaultFootstepClipInEditor();
            if (playerFootstepClip == null)
            {
                return;
            }

            if (footstepAudioSource == null)
            {
                AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
                foreach (AudioSource source in sources)
                {
                    if (source != null && source.clip != null && source.clip.name == playerFootstepClip.name)
                    {
                        footstepAudioSource = source;
                        break;
                    }
                }
            }

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (_footstepAudioConfigured && footstepAudioSource.clip == playerFootstepClip)
            {
                return;
            }

            footstepAudioSource.clip = playerFootstepClip;
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = 0f;
            footstepAudioSource.mute = false;
            footstepAudioSource.ignoreListenerPause = true;
            footstepAudioSource.priority = 48;
            _footstepAudioConfigured = true;
        }

        private void UpdateFootsteps(float deltaTime)
        {
            EnsureFootstepAudioSource();
            if (!ShouldPlayFootsteps())
            {
                _footstepTimer = 0f;
                if (footstepAudioSource != null && footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Stop();
                }

                return;
            }

            _footstepTimer -= deltaTime;
            if (_footstepTimer > 0f || footstepAudioSource.isPlaying)
            {
                return;
            }

            bool isSprinting = _input != null && _input.sprint && !IsCrouching;
            float interval = IsCrouching ? crouchStepInterval : (isSprinting ? sprintStepInterval : walkStepInterval);
            float volume = IsCrouching ? crouchFootstepVolume : (isSprinting ? sprintFootstepVolume : walkFootstepVolume);

            footstepAudioSource.pitch = Random.Range(0.96f, 1.04f) * (isSprinting ? 1.08f : 1f);
            footstepAudioSource.volume = volume;
            footstepAudioSource.Play();
            _footstepTimer = Mathf.Max(0.08f, interval);
        }

        private bool ShouldPlayFootsteps()
        {
            if (playerFootstepClip == null || footstepAudioSource == null || _controller == null || _input == null)
            {
                return false;
            }

            if (!Grounded || IsSwimming || IsPlayerTouchingWater() || _input.move == Vector2.zero)
            {
                return false;
            }

            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
            return horizontalVelocity.magnitude >= minFootstepSpeed;
        }

        private void AssignDefaultFootstepClipInEditor()
        {
#if UNITY_EDITOR
            if (playerFootstepClip != null)
            {
                return;
            }

            playerFootstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/music and sounds/Player_footsteps.mp3");
            if (playerFootstepClip != null)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("Player_footsteps t:AudioClip", new[] { "Assets/music and sounds" });
            if (guids.Length > 0)
            {
                playerFootstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AssignDefaultFootstepClipInEditor();
        }
#endif

        private bool IsCrouchPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
#else
            return false;
#endif
        }

        private bool CanStandUp()
        {
            float radius = Mathf.Max(0.05f, _controller.radius - _controller.skinWidth);
            Vector3 bottom = transform.position + _standingCenter + Vector3.down * ((_standingHeight * 0.5f) - radius);
            Vector3 top = transform.position + _standingCenter + Vector3.up * ((_standingHeight * 0.5f) - radius);
            int mask = GroundLayers.value == 0 ? Physics.DefaultRaycastLayers : GroundLayers.value;
            return !Physics.CheckCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
        }

        private void JumpAndGravity()
        {
            if (_input == null)
            {
                return;
            }

            if (IsSwimming)
            {
                _fallTimeoutDelta = FallTimeout;
                _jumpTimeoutDelta = JumpTimeout;

                float targetVerticalSpeed = SwimFloatVelocity;
                if (_input.jump)
                {
                    targetVerticalSpeed = SwimUpSpeed;
                }
                else if (IsCrouchPressed())
                {
                    targetVerticalSpeed = -SwimDownSpeed;
                }

                _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, targetVerticalSpeed, Time.deltaTime * SpeedChangeRate);
                return;
            }

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !IsCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
