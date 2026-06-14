using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MonsterAI : MonoBehaviour
{
    private enum MonsterState
    {
        Patrol,
        LookAround,
        Investigate,
        Chase
    }

    private enum PatrolMode
    {
        Loop,
        PingPong,
        Random
    }

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerStealthProfile playerStealth;
    [SerializeField] private Transform eyes;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private float patrolPointTolerance = 0.6f;
    [SerializeField] private float lookAroundTime = 3f;
    [SerializeField] private int lookAroundAnimationRepeats = 3;
    [SerializeField] private float lookAroundEveryPoint = 1f;

    [Header("Detection")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private float viewAngle = 75f;
    [SerializeField] private float suspicionToInvestigate = 0.45f;
    [SerializeField] private float suspicionToChase = 1f;
    [SerializeField] private float suspicionGain = 0.8f;
    [SerializeField] private float suspicionLoss = 0.25f;
    [SerializeField] private float hearingSuspicion = 0.55f;
    [SerializeField] private float searchTime = 5f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4.2f;
    [SerializeField] private float catchDistance = 1.35f;
    [SerializeField] private float catchHeightTolerance = 2.5f;
    [SerializeField] private float lostSightChaseTime = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string lookStateName = "staying";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string runningParameter = "IsRunning";
    [SerializeField] private string lookingParameter = "IsLooking";

    [Header("Footsteps")]
    [SerializeField] private AudioClip monsterFootstepClip;
    [SerializeField] private AudioSource monsterFootstepSource;
    [SerializeField] private float patrolFootstepVolume = 0.75f;
    [SerializeField] private float chaseFootstepVolume = 1f;
    [SerializeField] private float monsterFootstepVolumeMultiplier = 1.35f;
    [SerializeField] private float minFootstepSpeed = 0.12f;
    [SerializeField] private float monsterFootstepSpatialBlend = 0.65f;
    [SerializeField] private float monsterFootstepMinDistance = 4f;
    [SerializeField] private float monsterFootstepMaxDistance = 35f;

    [Header("Events")]
    public UnityEvent onPlayerCaught;

    private NavMeshAgent _agent;
    private MonsterState _state;
    private int _patrolIndex;
    private int _patrolDirection = 1;
    private float _lookTimer;
    private float _lookAnimationTimer;
    private int _lookAnimationRepeatsRemaining;
    private float _searchTimer;
    private float _lostSightTimer;
    private float _suspicion;
    private Vector3 _lastKnownPlayerPosition;
    private bool _hasCaughtPlayer;
    private bool _warnedAboutNavigation;
    private int _lastRequestedAnimationStateHash;
    private bool _monsterFootstepAudioConfigured;

    private bool CanNavigate => _agent != null && _agent.enabled && _agent.isOnNavMesh;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        ResolveAnimator();
    }

    private void Start()
    {
        ResolvePlayer();
        ResolveAnimator();

        if (eyes == null)
        {
            eyes = transform;
        }

        EnsureMonsterFootstepAudioSource();
        EnterPatrol();
    }

    private void Update()
    {
        ResolveAnimator();

        if (player == null)
        {
            ResolvePlayer();
            UpdateAnimation();
            return;
        }

        TryCatchPlayerByDistance();
        if (_hasCaughtPlayer)
        {
            UpdateAnimation();
            return;
        }

        UpdateDetection(Time.deltaTime);

        switch (_state)
        {
            case MonsterState.Patrol:
                UpdatePatrol();
                break;
            case MonsterState.LookAround:
                UpdateLookAround(Time.deltaTime);
                break;
            case MonsterState.Investigate:
                UpdateInvestigate(Time.deltaTime);
                break;
            case MonsterState.Chase:
                UpdateChase(Time.deltaTime);
                break;
        }

        UpdateAnimation();
        UpdateMonsterFootsteps(Time.deltaTime);
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (playerStealth == null && player != null)
        {
            playerStealth = player.GetComponent<PlayerStealthProfile>();
        }
    }

    private void ResolveAnimator()
    {
        if (animator != null && IsUsableAnimator(animator))
        {
            return;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator bestAnimator = null;

        foreach (Animator candidate in animators)
        {
            if (!IsUsableAnimator(candidate))
            {
                continue;
            }

            if (candidate.transform != transform)
            {
                animator = candidate;
                return;
            }

            bestAnimator = candidate;
        }

        if (bestAnimator != null)
        {
            animator = bestAnimator;
        }
    }

    private static bool IsUsableAnimator(Animator candidate)
    {
        return candidate != null
            && candidate.enabled
            && candidate.runtimeAnimatorController != null
            && candidate.avatar != null
            && candidate.avatar.isValid;
    }

    private void UpdateDetection(float deltaTime)
    {
        bool canSeePlayer = CanSeePlayer(out float visibilityStrength);
        bool canHearPlayer = CanHearPlayer();

        if (canSeePlayer)
        {
            _lastKnownPlayerPosition = player.position;
            _suspicion = Mathf.Clamp01(_suspicion + suspicionGain * visibilityStrength * deltaTime);
            _lostSightTimer = lostSightChaseTime;
        }
        else
        {
            _suspicion = Mathf.Clamp01(_suspicion - suspicionLoss * deltaTime);
            _lostSightTimer -= deltaTime;
        }

        if (canHearPlayer && _state != MonsterState.Chase)
        {
            _lastKnownPlayerPosition = player.position;
            _suspicion = Mathf.Max(_suspicion, hearingSuspicion);
        }

        if (_suspicion >= suspicionToChase)
        {
            EnterChase();
        }
        else if (_suspicion >= suspicionToInvestigate && _state == MonsterState.Patrol)
        {
            EnterInvestigate(_lastKnownPlayerPosition);
        }
    }

    private bool CanSeePlayer(out float visibilityStrength)
    {
        visibilityStrength = 0f;

        Vector3 origin = eyes.position;
        Vector3 target = player.position + Vector3.up * 1.1f;
        Vector3 toPlayer = target - origin;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance)
        {
            return false;
        }

        float angle = Vector3.Angle(eyes.forward, toPlayer.normalized);
        if (angle > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(player) && hit.transform != player)
            {
                return false;
            }
        }

        float distanceFactor = 1f - Mathf.Clamp01(distance / viewDistance);
        float angleFactor = 1f - Mathf.Clamp01(angle / (viewAngle * 0.5f));
        float playerVisibility = playerStealth != null ? playerStealth.VisibilityMultiplier : 1f;
        visibilityStrength = Mathf.Clamp01((0.25f + distanceFactor + angleFactor) * 0.5f * playerVisibility);
        return visibilityStrength > 0.05f;
    }

    private bool CanHearPlayer()
    {
        if (playerStealth == null)
        {
            return false;
        }

        float noiseRadius = playerStealth.NoiseRadius;
        if (noiseRadius <= 0f)
        {
            return false;
        }

        return Vector3.Distance(transform.position, player.position) <= noiseRadius;
    }

    private void UpdatePatrol()
    {
        if (!EnsureNavigationReady())
        {
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            EnterLookAround();
            return;
        }

        _agent.speed = patrolSpeed;
        Transform target = patrolPoints[_patrolIndex];
        if (target != null && Vector3.Distance(_agent.destination, target.position) > 0.2f)
        {
            _agent.SetDestination(target.position);
        }

        if (!_agent.pathPending && _agent.remainingDistance <= patrolPointTolerance)
        {
            AdvancePatrolIndex();
            if (Random.value <= lookAroundEveryPoint)
            {
                EnterLookAround();
            }
        }
    }

    private void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            _patrolIndex = 0;
            return;
        }

        if (patrolPoints.Length == 1)
        {
            _patrolIndex = 0;
            return;
        }

        switch (patrolMode)
        {
            case PatrolMode.PingPong:
                _patrolIndex += _patrolDirection;
                if (_patrolIndex >= patrolPoints.Length)
                {
                    _patrolDirection = -1;
                    _patrolIndex = patrolPoints.Length - 2;
                }
                else if (_patrolIndex < 0)
                {
                    _patrolDirection = 1;
                    _patrolIndex = 1;
                }
                break;
            case PatrolMode.Random:
                int nextIndex = Random.Range(0, patrolPoints.Length);
                if (patrolPoints.Length > 1 && nextIndex == _patrolIndex)
                {
                    nextIndex = (nextIndex + 1) % patrolPoints.Length;
                }
                _patrolIndex = nextIndex;
                break;
            default:
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                break;
        }
    }

    private void UpdateLookAround(float deltaTime)
    {
        StopMoving();
        _lookTimer -= deltaTime;
        PlayLookAroundCycle(deltaTime);

        if (_lookTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private void UpdateInvestigate(float deltaTime)
    {
        if (!EnsureNavigationReady())
        {
            return;
        }

        _searchTimer -= deltaTime;
        _agent.speed = patrolSpeed;
        _agent.SetDestination(_lastKnownPlayerPosition);

        if (!_agent.pathPending && _agent.remainingDistance <= patrolPointTolerance)
        {
            EnterLookAround();
        }
        //if (!_agent.pathPending && _agent.remainingDistance <= patrolPointTolerance)
        //{
        //    _state = MonsterState.LookAround;
        //    _lookTimer = searchTime;
        //    StopMoving();
        //}

        if (_searchTimer <= 0f && _suspicion < suspicionToInvestigate)
        {
            EnterPatrol();
        }
    }

    private void UpdateChase(float deltaTime)
    {
        if (!EnsureNavigationReady())
        {
            return;
        }

        _agent.speed = chaseSpeed;
        _agent.SetDestination(player.position);

        if (IsPlayerWithinCatchDistance())
        {
            CatchPlayer();
            return;
        }

        if (_lostSightTimer <= 0f)
        {
            EnterInvestigate(_lastKnownPlayerPosition);
        }
    }

    private bool EnsureNavigationReady()
    {
        if (CanNavigate)
        {
            return true;
        }

        if (!_warnedAboutNavigation)
        {
            Debug.LogWarning("MonsterAI needs a NavMeshAgent placed on a baked NavMesh. Add/configure NavMeshAgent on the monster and bake NavMesh for Metro.", this);
            _warnedAboutNavigation = true;
        }

        return false;
    }

    private void TryCatchPlayerByDistance()
    {
        if (_hasCaughtPlayer || player == null)
        {
            return;
        }

        if (IsPlayerWithinCatchDistance())
        {
            CatchPlayer();
        }
    }

    private bool IsPlayerWithinCatchDistance()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 monsterPosition = transform.position;
        Vector3 playerPosition = player.position;
        float verticalDistance = Mathf.Abs(monsterPosition.y - playerPosition.y);
        Vector2 monsterXZ = new Vector2(monsterPosition.x, monsterPosition.z);
        Vector2 playerXZ = new Vector2(playerPosition.x, playerPosition.z);

        if (verticalDistance <= catchHeightTolerance && Vector2.Distance(monsterXZ, playerXZ) <= catchDistance)
        {
            return true;
        }

        Collider monsterCollider = GetComponent<Collider>();
        Collider playerCollider = player.GetComponentInChildren<Collider>();
        if (monsterCollider == null || playerCollider == null)
        {
            return false;
        }

        Vector3 monsterClosest = monsterCollider.ClosestPoint(playerCollider.bounds.center);
        Vector3 playerClosest = playerCollider.ClosestPoint(monsterClosest);
        return Vector3.Distance(monsterClosest, playerClosest) <= catchDistance;
    }

    private void StopMoving()
    {
        if (CanNavigate)
        {
            _agent.ResetPath();
        }
    }

    private void EnterPatrol()
    {
        _state = MonsterState.Patrol;
        if (CanNavigate && patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[_patrolIndex] != null)
        {
            _agent.speed = patrolSpeed;
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void EnterLookAround()
    {
        _state = MonsterState.LookAround;
        _lookAnimationRepeatsRemaining = Mathf.Max(1, lookAroundAnimationRepeats);
        float lookClipLength = GetAnimationClipLength(lookStateName);
        _lookTimer = lookClipLength > 0f ? lookClipLength * _lookAnimationRepeatsRemaining : lookAroundTime;
        _lookAnimationTimer = 0f;
        StopMoving();
        PlayLookAroundCycle(0f);
    }

    private void EnterInvestigate(Vector3 position)
    {
        _state = MonsterState.Investigate;
        _searchTimer = searchTime;
        if (CanNavigate)
        {
            _agent.speed = patrolSpeed;
            _agent.SetDestination(position);
        }
    }

    private void EnterChase()
    {
        _state = MonsterState.Chase;
        _lostSightTimer = lostSightChaseTime;
        if (CanNavigate)
        {
            _agent.speed = chaseSpeed;
            _agent.SetDestination(player.position);
        }
    }

    private void CatchPlayer()
    {
        if (_hasCaughtPlayer)
        {
            return;
        }

        _hasCaughtPlayer = true;
        StopMoving();
        Debug.Log("Monster caught the player.");
        onPlayerCaught?.Invoke();
        if (monsterFootstepSource != null && monsterFootstepSource.isPlaying)
        {
            monsterFootstepSource.Stop();
        }

        GameOverScreen.ShowGameOver();
    }

    private void EnsureMonsterFootstepAudioSource()
    {
        AssignDefaultMonsterFootstepClipInEditor();
        if (monsterFootstepClip == null)
        {
            return;
        }

        if (monsterFootstepSource == null)
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in sources)
            {
                if (source != null && source.clip != null && source.clip.name == monsterFootstepClip.name)
                {
                    monsterFootstepSource = source;
                    break;
                }
            }
        }

        if (monsterFootstepSource == null)
        {
            monsterFootstepSource = gameObject.AddComponent<AudioSource>();
        }

        if (_monsterFootstepAudioConfigured && monsterFootstepSource.clip == monsterFootstepClip)
        {
            ApplyMonsterFootstepSourceSettings();
            return;
        }

        monsterFootstepSource.clip = monsterFootstepClip;
        ApplyMonsterFootstepSourceSettings();
        _monsterFootstepAudioConfigured = true;
    }

    private void ApplyMonsterFootstepSourceSettings()
    {
        if (monsterFootstepSource == null)
        {
            return;
        }

        monsterFootstepSource.loop = true;
        monsterFootstepSource.playOnAwake = false;
        monsterFootstepSource.spatialBlend = Mathf.Clamp01(monsterFootstepSpatialBlend);
        monsterFootstepSource.mute = false;
        monsterFootstepSource.ignoreListenerPause = true;
        monsterFootstepSource.ignoreListenerVolume = true;
        monsterFootstepSource.priority = 32;
        monsterFootstepSource.rolloffMode = AudioRolloffMode.Linear;
        monsterFootstepSource.minDistance = Mathf.Max(0.1f, monsterFootstepMinDistance);
        monsterFootstepSource.maxDistance = Mathf.Max(monsterFootstepSource.minDistance + 0.1f, monsterFootstepMaxDistance);
    }

    private void UpdateMonsterFootsteps(float deltaTime)
    {
        EnsureMonsterFootstepAudioSource();
        if (!ShouldPlayMonsterFootsteps())
        {
            if (monsterFootstepSource != null && monsterFootstepSource.isPlaying)
            {
                monsterFootstepSource.Stop();
            }

            return;
        }

        bool isChasing = _state == MonsterState.Chase;
        monsterFootstepSource.volume = Mathf.Clamp01((isChasing ? chaseFootstepVolume : patrolFootstepVolume) * monsterFootstepVolumeMultiplier);
        monsterFootstepSource.pitch = isChasing ? 1.45f : 1f;
        if (!monsterFootstepSource.isPlaying)
        {
            monsterFootstepSource.Play();
        }
    }

    private bool ShouldPlayMonsterFootsteps()
    {
        if (monsterFootstepClip == null || monsterFootstepSource == null || !CanNavigate || _hasCaughtPlayer)
        {
            return false;
        }

        if (_state == MonsterState.LookAround)
        {
            return false;
        }

        return _agent.velocity.magnitude >= minFootstepSpeed;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = CanNavigate ? _agent.velocity.magnitude : 0f;
        TrySetFloat(speedParameter, speed);
        TrySetBool(runningParameter, _state == MonsterState.Chase);
        TrySetBool(lookingParameter, _state == MonsterState.LookAround);

        if (_state == MonsterState.Chase)
        {
            CrossFadeIfExists(runStateName);
        }
        else if (_state == MonsterState.LookAround)
        {
            CrossFadeIfExists(lookStateName);
        }
        else if (speed > 0.1f)
        {
            CrossFadeIfExists(walkStateName);
        }
    }

    private void TrySetFloat(string parameterName, float value)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private void TrySetBool(string parameterName, bool value)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void CrossFadeIfExists(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || animator == null || animator.runtimeAnimatorController == null) return;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.shortNameHash == stateHash)
        {
            _lastRequestedAnimationStateHash = stateHash;
            return;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.shortNameHash == stateHash)
            {
                _lastRequestedAnimationStateHash = stateHash;
                return;
            }
        }

        if (_lastRequestedAnimationStateHash == stateHash && currentState.normalizedTime < 0.08f)
        {
            return;
        }

        _lastRequestedAnimationStateHash = stateHash;
        animator.CrossFade(stateHash, 0.15f);
    }

    private void PlayLookAroundCycle(float deltaTime)
    {
        if (animator == null || string.IsNullOrWhiteSpace(lookStateName))
        {
            return;
        }

        _lookAnimationTimer -= deltaTime;
        if (_lookAnimationTimer > 0f || _lookAnimationRepeatsRemaining <= 0)
        {
            return;
        }

        int stateHash = Animator.StringToHash(lookStateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        animator.Play(stateHash, 0, 0f);
        _lastRequestedAnimationStateHash = stateHash;
        _lookAnimationRepeatsRemaining--;

        float clipLength = GetAnimationClipLength(lookStateName);
        _lookAnimationTimer = clipLength > 0f ? clipLength : Mathf.Max(0.1f, lookAroundTime / Mathf.Max(1, lookAroundAnimationRepeats));
    }

    private float GetAnimationClipLength(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
        {
            return 0f;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == stateName)
            {
                return clip.length;
            }
        }

        return 0f;
    }

    private void AssignDefaultMonsterFootstepClipInEditor()
    {
#if UNITY_EDITOR
        if (monsterFootstepClip != null)
        {
            return;
        }

        monsterFootstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/music and sounds/Monster_footsteps.mp3");
        if (monsterFootstepClip != null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("Monster_footsteps t:AudioClip", new[] { "Assets/music and sounds" });
        if (guids.Length > 0)
        {
            monsterFootstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignDefaultMonsterFootstepClipInEditor();
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        TryCatchPlayerFromCollider(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCatchPlayerFromCollider(collision.collider);
    }

    private void TryCatchPlayerFromCollider(Collider other)
    {
        if (_hasCaughtPlayer || player == null || other == null)
        {
            return;
        }

        if (other.transform == player || other.transform.IsChildOf(player))
        {
            CatchPlayer();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform eyeTransform = eyes != null ? eyes : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * eyeTransform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * eyeTransform.forward;
        Gizmos.DrawRay(eyeTransform.position, left * viewDistance);
        Gizmos.DrawRay(eyeTransform.position, right * viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
