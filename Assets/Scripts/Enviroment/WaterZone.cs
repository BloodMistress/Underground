using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterZone : MonoBehaviour
{
    private static readonly List<Collider> ZoneColliders = new();

    [SerializeField] private bool configureColliderAsTrigger = true;

    private Collider _zoneCollider;

    public static bool ContainsPoint(Vector3 point)
    {
        for (int i = ZoneColliders.Count - 1; i >= 0; i--)
        {
            Collider zoneCollider = ZoneColliders[i];
            if (zoneCollider == null)
            {
                ZoneColliders.RemoveAt(i);
                continue;
            }

            if (!zoneCollider.enabled || !zoneCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 closestPoint = zoneCollider.ClosestPoint(point);
            if ((closestPoint - point).sqrMagnitude <= 0.0001f)
            {
                return true;
            }
        }

        return false;
    }

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();
        if (configureColliderAsTrigger)
        {
            ConfigureCollider();
        }
    }

    private void OnEnable()
    {
        _zoneCollider = GetComponent<Collider>();
        if (_zoneCollider != null && !ZoneColliders.Contains(_zoneCollider))
        {
            ZoneColliders.Add(_zoneCollider);
        }
    }

    private void OnDisable()
    {
        if (_zoneCollider != null)
        {
            ZoneColliders.Remove(_zoneCollider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.EnterWaterZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.ExitWaterZone();
        }
    }

    private void ConfigureCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }
}
