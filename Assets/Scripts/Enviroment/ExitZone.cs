using UnityEngine;

public class ExitZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<OxygenSystem>() != null)
        {
            Debug.Log("Player reached the exit.");
            VictoryScreen.ShowVictory();
        }
    }
}
