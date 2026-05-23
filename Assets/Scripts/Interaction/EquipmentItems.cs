using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class EquipmentItems : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("Detection")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float pickupSearchRadius = 1.8f;
    [SerializeField] private float maxLookAngle = 70f;
    [SerializeField] private bool logDebugMessages = true;

    [Header("Inventory Sprites")]
    [SerializeField] private Sprite bucketSprite;
    [SerializeField] private Sprite key1Sprite;
    [SerializeField] private Sprite key2Sprite;

    private Camera _mainCamera;

    private void Update()
    {
        if (WasPickupPressed())
        {
            TryPickupItem();
        }
    }

    private bool WasPickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(pickupKey);
#endif
    }

    private void TryPickupItem()
    {
        ResolveInteractionOrigin();

        PickupTarget pickup = FindBestPickupTarget();
        if (!pickup.IsValid)
        {
            LogDebug("No pickup item found. Expected scene object names: bucket, key1, key2.");
            return;
        }

        if (!MetroHUD.TryAddInventoryItem(pickup.InventorySprite))
        {
            LogDebug("Found " + pickup.Root.name + ", but inventory did not accept the sprite. Check empty slots and sprite references.");
            return;
        }

        LogDebug("Picked up " + pickup.Root.name + ".");
        pickup.Root.gameObject.SetActive(false);
    }

    private PickupTarget FindBestPickupTarget()
    {
        PickupTarget bestPickup = default;
        float bestScore = float.PositiveInfinity;
        Vector3 originPosition = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Vector3 originForward = interactionOrigin != null ? interactionOrigin.forward : transform.forward;
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            PickupTarget pickup = FindPickupRoot(renderer.transform);
            if (!pickup.IsValid)
            {
                continue;
            }

            Vector3 targetPosition = renderer.bounds.center;
            Vector3 toTarget = targetPosition - originPosition;
            float forwardDistance = Vector3.Dot(originForward, toTarget);
            if (forwardDistance < 0f || forwardDistance > interactionDistance)
            {
                continue;
            }

            if (Vector3.Angle(originForward, toTarget.normalized) > maxLookAngle)
            {
                continue;
            }

            Vector3 closestPointOnLookLine = originPosition + originForward * forwardDistance;
            float sideDistance = Vector3.Distance(renderer.bounds.ClosestPoint(closestPointOnLookLine), closestPointOnLookLine);
            if (sideDistance > pickupSearchRadius)
            {
                continue;
            }

            float score = forwardDistance + sideDistance * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                bestPickup = pickup;
            }
        }

        return bestPickup;
    }

    private PickupTarget FindPickupRoot(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            Sprite inventorySprite = GetSpriteForObjectName(current.name);
            if (inventorySprite != null)
            {
                return new PickupTarget(current, inventorySprite);
            }

            current = current.parent;
        }

        return default;
    }

    private Sprite GetSpriteForObjectName(string objectName)
    {
        switch (NormalizeName(objectName))
        {
            case "bucket":
                return bucketSprite;
            case "key1":
                return key1Sprite;
            case "key2":
                return key2Sprite;
            default:
                return null;
        }
    }

    private static string NormalizeName(string objectName)
    {
        int suffixIndex = objectName.IndexOf('(');
        if (suffixIndex >= 0)
        {
            objectName = objectName.Substring(0, suffixIndex);
        }

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

        interactionOrigin = transform.Find("PlayerCameraRoot");
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AssignDefaultSprites();
    }

    private void OnValidate()
    {
        AssignDefaultSprites();
    }

    private void AssignDefaultSprites()
    {
        AssignSpriteIfEmpty(ref bucketSprite, "Assets/UI/bucket.png");
        AssignSpriteIfEmpty(ref key1Sprite, "Assets/UI/key1.png");
        AssignSpriteIfEmpty(ref key2Sprite, "Assets/UI/key2.png");
    }

    private static void AssignSpriteIfEmpty(ref Sprite sprite, string assetPath)
    {
        if (sprite != null)
        {
            return;
        }

        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
#endif

    private void LogDebug(string message)
    {
        if (logDebugMessages)
        {
            Debug.Log("[EquipmentItems] " + message, this);
        }
    }

    private readonly struct PickupTarget
    {
        public readonly Transform Root;
        public readonly Sprite InventorySprite;
        public bool IsValid => Root != null && InventorySprite != null;

        public PickupTarget(Transform root, Sprite inventorySprite)
        {
            Root = root;
            InventorySprite = inventorySprite;
        }
    }
}
