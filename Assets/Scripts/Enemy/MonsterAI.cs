using StarterAssets;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    private enum MonsterState
    {
        Patrol,
        LookAround,
        Investigate,
        Chase
    }

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerStealthProfile playerStealth;
    [SerializeField] private Transform eyes;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private float patrolPointTolerance = 0.6f;
    [SerializeField] private float lookAroundTime = 3f;
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
    [SerializeField] private float lostSightChaseTime = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string lookStateName = "staying";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string runningParameter = "IsRunning";
    [SerializeField] private string lookingParameter = "IsLooking";

    [Header("Events")]
    public UnityEvent onPlayerCaught;

    private NavMeshAgent _agent;
    private MonsterState _state;
    private int _patrolIndex;
    private float _lookTimer;
    private float _searchTimer;
    private float _lostSightTimer;
    private float _suspicion;
    private Vector3 _lastKnownPlayerPosition;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
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

        if (eyes == null)
        {
            eyes = transform;
        }

        EnterPatrol();
    }

    private void Update()
    {
        if (player == null)
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
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            EnterLookAround();
            return;
        }

        _agent.speed = patrolSpeed;
        Transform target = patrolPoints[_patrolIndex];
        if (target != null && _agent.destination != target.position)
        {
            _agent.SetDestination(target.position);
        }

        if (!_agent.pathPending && _agent.remainingDistance <= patrolPointTolerance)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            if (Random.value <= lookAroundEveryPoint)
            {
                EnterLookAround();
            }
        }
    }

    private void UpdateLookAround(float deltaTime)
    {
        _agent.ResetPath();
        _lookTimer -= deltaTime;
        transform.Rotate(Vector3.up, 35f * deltaTime);

        if (_lookTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private void UpdateInvestigate(float deltaTime)
    {
        _agent.speed = patrolSpeed;
        _searchTimer -= deltaTime;

        if (!_agent.pathPending && _agent.remainingDistance <= patrolPointTolerance)
        {
            EnterLookAround();
        }

        if (_searchTimer <= 0f && _suspicion < suspicionToInvestigate)
        {
            EnterPatrol();
        }
    }

    private void UpdateChase(float deltaTime)
    {
        _agent.speed = chaseSpeed;
        _agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        if (_lostSightTimer <= 0f)
        {
            EnterInvestigate(_lastKnownPlayerPosition);
        }
    }

    private void EnterPatrol()
    {
        _state = MonsterState.Patrol;
        _agent.speed = patrolSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[_patrolIndex] != null)
        {
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void EnterLookAround()
    {
        _state = MonsterState.LookAround;
        _lookTimer = lookAroundTime;
        _agent.ResetPath();
    }

    private void EnterInvestigate(Vector3 position)
    {
        _state = MonsterState.Investigate;
        _searchTimer = searchTime;
        _agent.speed = patrolSpeed;
        _agent.SetDestination(position);
    }

    private void EnterChase()
    {
        _state = MonsterState.Chase;
        _agent.speed = chaseSpeed;
        _lostSightTimer = lostSightChaseTime;
    }

    private void CatchPlayer()
    {
        Debug.Log("Monster caught the player.");
        onPlayerCaught?.Invoke();
        Time.timeScale = 0f;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = _agent != null ? _agent.velocity.magnitude : 0f;
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
        if (string.IsNullOrWhiteSpace(parameterName)) return;
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
        if (string.IsNullOrWhiteSpace(parameterName)) return;
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
        if (string.IsNullOrWhiteSpace(stateName)) return;

        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash) && !animator.GetCurrentAnimatorStateInfo(0).shortNameHash.Equals(stateHash))
        {
            animator.CrossFade(stateHash, 0.15f);
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
