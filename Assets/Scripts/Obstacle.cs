using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    public bool canBeJumpedOver = true;
    public int damage = 10;

    bool consumed = false; // флаг, что уже нанес урон

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true; // важно!
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed) return;

        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        Debug.Log($"[Obstacle] Trigger by {pc.gameObject.name} at pos {transform.position}. canBeJumpedOver={canBeJumpedOver} IsAirborne={pc.IsAirborne}");

     
        if (canBeJumpedOver && pc.IsAirborne)
        {
            Debug.Log("[Obstacle] player airborne Ч no damage.");
            return;
        }

        pc.TakeDamage(damage);
        consumed = true;

      
        var cols = GetComponents<Collider>();
        foreach (var col in cols) col.enabled = false;

    }


    void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        if (c is BoxCollider bc)
        {
            Gizmos.DrawWireCube(bc.center, bc.size);
        }
        else
        {
            // минимально Ч рисуем bounds
            Gizmos.DrawWireCube(c.bounds.center - transform.position, c.bounds.size);
        }
    }
}
