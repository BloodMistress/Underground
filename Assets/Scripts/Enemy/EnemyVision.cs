using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform player;
    public float detectionDistance = 5f;

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < detectionDistance)
        {
            Debug.Log("Игрок пойман");
            Time.timeScale = 0f;
        }
    }
}