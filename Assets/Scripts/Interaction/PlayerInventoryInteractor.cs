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
    [SerializeField] private float pickupSearchRadius = 1.4f;
    [SerializeField] private LayerMask interactionMask = ~0;

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

        Vector3 searchPoint = interactionOrigin.position + interactionOrigin.forward * interactionDistance;
        if (Physics.Raycast(interactionOrigin.position, interactionOrigin.forward, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            searchPoint = hit.point;
        }

        Transform bucket = FindNearestBucket(searchPoint);
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

    private Transform FindNearestBucket(Vector3 searchPoint)
    {
        Transform nearest = null;
        float bestSqrDistance = pickupSearchRadius * pickupSearchRadius;
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            Transform bucketRoot = FindBucketRoot(renderer.transform);
            if (bucketRoot == null)
            {
                continue;
            }

            float sqrDistance = (renderer.bounds.ClosestPoint(searchPoint) - searchPoint).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
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
        return objectName.ToLowerInvariant().Replace(' ', '_');
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