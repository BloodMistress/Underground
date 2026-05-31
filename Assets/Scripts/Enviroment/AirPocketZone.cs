using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AirPocketZone : MonoBehaviour
{
    [SerializeField] private float oxygenAmount = 35f;
    [SerializeField] private float oxygenPerSecond = 20f;
    [SerializeField] private bool destroyWhenEmpty = false;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        RestoreOxygen(other, Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        RestoreOxygen(other, Time.deltaTime);
    }

    private void RestoreOxygen(Collider other, float deltaTime)
    {
        if (oxygenAmount <= 0f)
        {
            if (destroyWhenEmpty)
            {
                Destroy(gameObject);
            }
            return;
        }

        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen == null)
        {
            return;
        }

        float transfer = Mathf.Min(oxygenPerSecond * deltaTime, oxygenAmount);
        oxygen.AddOxygen(transfer);
        oxygenAmount -= transfer;
    }
}
