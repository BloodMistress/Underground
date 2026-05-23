using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInventoryInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Detection")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float pickupSearchRadius = 1.6f;
    [SerializeField] private float maxLookAngle = 65f;

    [Header("Items")]
    [SerializeField] private Sprite bucketIcon;

    private Camera _mainCamera;

    private void Update()
    {
        if (WasInteractPressed())
        {
            TryPickup();
        }
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(interactKey);
#endif
    }

    private void TryPickup()
    {
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return;
        }

        Transform bucket = FindNearestBucket();
        if (bucket == null)
        {
            return;
        }

        if (!MetroHUD.TryAddInventoryItem(bucketIcon))
        {
            return;
        }

        bucket.gameObject.SetActive(false);
    }

    private Transform FindNearestBucket()
    {
        Transform nearest = null;
        float bestScore = float.PositiveInfinity;
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            Transform bucketRoot = FindBucketRoot(renderer.transform);
            if (bucketRoot == null)
            {
                continue;
            }

            Vector3 target = renderer.bounds.center;
            Vector3 toTarget = target - interactionOrigin.position;
            float forwardDistance = Vector3.Dot(interactionOrigin.forward, toTarget);
            if (forwardDistance < 0f || forwardDistance > interactionDistance)
            {
                continue;
            }

            float angle = Vector3.Angle(interactionOrigin.forward, toTarget.normalized);
            if (angle > maxLookAngle)
            {
                continue;
            }

            Vector3 closestPointOnLook = interactionOrigin.position + interactionOrigin.forward * forwardDistance;
            float sideDistance = Vector3.Distance(renderer.bounds.ClosestPoint(closestPointOnLook), closestPointOnLook);
            if (sideDistance > pickupSearchRadius)
            {
                continue;
            }

            float score = forwardDistance + sideDistance * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                nearest = bucketRoot;
            }
        }

        return nearest;
    }

    private static Transform FindBucketRoot(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (NormalizeName(current.name) == "bucket")
            {
                return current;
            }

            current = current.parent;
        }

        return null;
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