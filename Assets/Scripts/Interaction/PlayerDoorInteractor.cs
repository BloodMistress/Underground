using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerDoorInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode openDoorKey = KeyCode.Q;

    [Header("Detection")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float doorSearchRadius = 2.4f;
    [SerializeField] private LayerMask interactionMask = ~0;

    [Header("Door Motion")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;
    [SerializeField] private Vector3 hingeAxis = Vector3.up;

    private readonly List<Transform> _runtimeDoors = new List<Transform>();
    private Camera _mainCamera;

    private void Start()
    {
        RefreshRuntimeDoors();
        EnsureAllDoorColliders();
    }

    private void Update()
    {
        if (WasOpenDoorPressed())
        {
            TryToggleDoor();
        }
    }

    private bool WasOpenDoorPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(openDoorKey);
#endif
    }

    private void TryToggleDoor()
    {
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return;
        }

        if (_runtimeDoors.Count == 0)
        {
            RefreshRuntimeDoors();
        }

        Vector3 searchPoint = interactionOrigin.position + interactionOrigin.forward * interactionDistance;
        if (Physics.Raycast(interactionOrigin.position, interactionOrigin.forward, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            searchPoint = hit.point;
        }

        Transform doorRoot = FindNearestDoor(searchPoint);
        if (doorRoot == null)
        {
            return;
        }

        Transform pivot = FindDoorPivot(doorRoot);
        RuntimePivotDoor door = doorRoot.GetComponent<RuntimePivotDoor>();
        if (door == null)
        {
            door = doorRoot.gameObject.AddComponent<RuntimePivotDoor>();
            door.Configure(pivot, openAngle, openSpeed, GetSwingDirection(doorRoot), hingeAxis);
            EnsureDoorCollider(doorRoot);
        }

        door.Toggle();
    }

    private void RefreshRuntimeDoors()
    {
        _runtimeDoors.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (IsRuntimeDoor(candidate))
            {
                _runtimeDoors.Add(candidate);
            }
        }
    }

    private Transform FindNearestDoor(Vector3 searchPoint)
    {
        Transform nearest = null;
        float bestSqrDistance = doorSearchRadius * doorSearchRadius;

        foreach (Transform door in _runtimeDoors)
        {
            if (door == null || !door.gameObject.activeInHierarchy)
            {
                continue;
            }

            float sqrDistance = DistanceToDoorSqr(door, searchPoint);
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = door;
            }
        }

        return nearest;
    }

    private static float DistanceToDoorSqr(Transform door, Vector3 searchPoint)
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return (door.position - searchPoint).sqrMagnitude;
        }

        float best = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
        {
            float sqr = (renderer.bounds.ClosestPoint(searchPoint) - searchPoint).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
            }
        }

        return best;
    }

    private static bool IsRuntimeDoor(Transform candidate)
    {
        string normalized = NormalizeName(candidate.name);
        if (normalized != "door" && normalized != "door2" && normalized != "door3")
        {
            return false;
        }

        Transform parent = candidate.parent;
        return parent != null && NormalizeName(parent.name) == "doors";
    }

    private static Transform FindDoorPivot(Transform doorRoot)
    {
        Transform[] children = doorRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == doorRoot)
            {
                continue;
            }

            string normalized = NormalizeName(child.name);
            if (normalized.Contains("pivot") || normalized.Contains("hinge"))
            {
                return child;
            }
        }

        return doorRoot;
    }

    private static void EnsureAllDoorColliders()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (IsRuntimeDoor(candidate))
            {
                EnsureDoorCollider(candidate);
            }
        }
    }

    private static void EnsureDoorCollider(Transform doorRoot)
    {
        if (doorRoot.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers = doorRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = doorRoot.gameObject.AddComponent<BoxCollider>();
        collider.center = doorRoot.InverseTransformPoint(bounds.center);
        Vector3 localSize = doorRoot.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private float GetSwingDirection(Transform door)
    {
        Vector3 toPlayer = transform.position - door.position;
        float side = Vector3.Dot(Vector3.Cross(Vector3.up, door.forward), toPlayer);
        float direction = side >= 0f ? -1f : 1f;
        string normalizedName = NormalizeName(door.name);
        if (normalizedName == "door" || normalizedName == "door3")
        {
            direction *= -1f;
        }

        return direction;
    }

    private static string NormalizeName(string objectName)
    {
        return objectName.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
    }

    private void ResolveInteractionOrigin()
    {
        if (interactionOrigin != null)
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            interactionOrigin = _mainCamera.transform;
            return;
        }

        Transform cameraRoot = transform.Find("PlayerCameraRoot");
        if (cameraRoot != null)
        {
            interactionOrigin = cameraRoot;
        }
    }
}

public class RuntimePivotDoor : MonoBehaviour
{
    private Vector3 _pivotPoint;
    private Vector3 _hingeAxis = Vector3.up;
    private float _closedAngle;
    private float _openAngle;
    private float _currentAngle;
    private float _targetAngle;
    private float _openSpeed = 180f;
    private bool _isOpen;

    public void Configure(Transform pivot, float openAngle, float openSpeed, float swingDirection, Vector3 worldHingeAxis)
    {
        _pivotPoint = pivot != null ? pivot.position : transform.position;
        _hingeAxis = worldHingeAxis.sqrMagnitude <= 0.0001f ? Vector3.up : worldHingeAxis.normalized;
        _closedAngle = 0f;
        _openAngle = openAngle * swingDirection;
        _currentAngle = 0f;
        _targetAngle = _closedAngle;
        _openSpeed = openSpeed;
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? _openAngle : _closedAngle;
    }

    private void Update()
    {
        float nextAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, _openSpeed * Time.deltaTime);
        float delta = nextAngle - _currentAngle;
        if (Mathf.Abs(delta) > 0.001f)
        {
            transform.RotateAround(_pivotPoint, _hingeAxis, delta);
            _currentAngle = nextAngle;
        }
    }
}
