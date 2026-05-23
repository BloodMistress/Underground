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
    [SerializeField] private float doorSearchRadius = 2.2f;
    [SerializeField] private LayerMask interactionMask = ~0;

    [Header("Door Motion")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;
    [SerializeField] private Vector3 hingeAxis = Vector3.up;

    private Camera _mainCamera;

    private void Start()
    {
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

        Vector3 searchPoint = interactionOrigin.position + interactionOrigin.forward * interactionDistance;
        if (Physics.Raycast(interactionOrigin.position, interactionOrigin.forward, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            searchPoint = hit.point;
        }

        Transform doorAssembly = FindNearestDoorAssembly(searchPoint);
        if (doorAssembly == null)
        {
            return;
        }

        Transform movingDoor = FindMovingDoorPart(doorAssembly);
        if (movingDoor == null)
        {
            return;
        }

        RuntimeSwingDoor door = movingDoor.GetComponent<RuntimeSwingDoor>();
        if (door == null)
        {
            door = movingDoor.gameObject.AddComponent<RuntimeSwingDoor>();
            door.Configure(openAngle, openSpeed, GetSwingDirection(movingDoor), hingeAxis);
            EnsureDoorCollider(movingDoor);
        }

        door.Toggle();
    }

    private void EnsureAllDoorColliders()
    {
        List<Transform> doorAssemblies = new List<Transform>();
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            Transform doorAssembly = FindDoorAssembly(renderer.transform);
            if (doorAssembly == null || doorAssemblies.Contains(doorAssembly))
            {
                continue;
            }

            doorAssemblies.Add(doorAssembly);
            Transform movingDoor = FindMovingDoorPart(doorAssembly);
            if (movingDoor != null)
            {
                EnsureDoorCollider(movingDoor);
            }
        }
    }
    private Transform FindNearestDoorAssembly(Vector3 searchPoint)
    {
        Transform nearestDoor = null;
        float bestSqrDistance = doorSearchRadius * doorSearchRadius;
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            Transform doorAssembly = FindDoorAssembly(renderer.transform);
            if (doorAssembly == null)
            {
                continue;
            }

            float sqrDistance = (renderer.bounds.ClosestPoint(searchPoint) - searchPoint).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearestDoor = doorAssembly;
            }
        }

        return nearestDoor;
    }

    private static Transform FindDoorAssembly(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (IsDoorAssemblyName(current.name) && HasNewDoorRoot(current))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform FindMovingDoorPart(Transform doorAssembly)
    {
        Transform best = null;
        Renderer[] renderers = doorAssembly.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Transform candidate = renderer.transform;
            if (candidate == doorAssembly)
            {
                continue;
            }

            string normalized = NormalizeName(candidate.name);
            if (normalized.Contains("doorway") || normalized.Contains("frame") || normalized.Contains("jamb"))
            {
                continue;
            }

            if (normalized.Contains("door") || normalized.Contains("metal"))
            {
                return candidate;
            }

            if (best == null)
            {
                best = candidate;
            }
        }

        return best != null ? best : doorAssembly;
    }

    private static void EnsureDoorCollider(Transform movingDoor)
    {
        if (movingDoor.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers = movingDoor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = movingDoor.gameObject.AddComponent<BoxCollider>();
        collider.center = movingDoor.InverseTransformPoint(bounds.center);
        Vector3 localSize = movingDoor.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private static bool HasNewDoorRoot(Transform candidate)
    {
        Transform current = candidate.parent;
        while (current != null)
        {
            if (IsNewDoorRootName(current.name))
            {
                return true;
            }

            if (IsOldDoorRootName(current.name))
            {
                return false;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsDoorAssemblyName(string objectName)
    {
        string normalized = NormalizeName(objectName);
        return normalized == "door_old_metal" || normalized.StartsWith("door_old_metal.");
    }

    private static bool IsNewDoorRootName(string objectName)
    {
        string normalized = NormalizeName(objectName);
        return normalized == "doornew" || normalized.StartsWith("doornew_(") || (normalized.StartsWith("door_(") && normalized.EndsWith(")"));
    }

    private static bool IsOldDoorRootName(string objectName)
    {
        return NormalizeName(objectName) == "door";
    }

    private static string NormalizeName(string objectName)
    {
        return objectName.ToLowerInvariant().Replace(' ', '_');
    }

    private float GetSwingDirection(Transform door)
    {
        Vector3 toPlayer = transform.position - door.position;
        float side = Vector3.Dot(Vector3.Cross(Vector3.up, door.forward), toPlayer);
        return side >= 0f ? -1f : 1f;
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

public class RuntimeSwingDoor : MonoBehaviour
{
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private float _openSpeed = 180f;
    private bool _isOpen;

    public void Configure(float openAngle, float openSpeed, float swingDirection, Vector3 worldHingeAxis)
    {
        _closedRotation = transform.rotation;
        Vector3 axis = worldHingeAxis.sqrMagnitude <= 0.0001f ? Vector3.up : worldHingeAxis.normalized;
        _openRotation = Quaternion.AngleAxis(openAngle * swingDirection, axis) * _closedRotation;
        _openSpeed = openSpeed;
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
    }

    private void Update()
    {
        Quaternion targetRotation = _isOpen ? _openRotation : _closedRotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _openSpeed * Time.deltaTime);
    }
}