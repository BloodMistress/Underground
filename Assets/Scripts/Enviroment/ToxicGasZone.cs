using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ToxicGasZone : MonoBehaviour
{
    [SerializeField] private bool configureColliderAsTrigger = true;
    [SerializeField] private bool hideSolidVolumeAtRuntime = true;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        if (configureColliderAsTrigger)
        {
            ConfigureCollider();
        }

        if (hideSolidVolumeAtRuntime)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.EnterToxicGasZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.ExitToxicGasZone();
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
