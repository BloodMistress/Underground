using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] points;
    public float speed = 3f;

    int currentPoint = 0;

    void Update()
    {
        if (points.Length == 0) return;

        Transform target = points[currentPoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            currentPoint = (currentPoint + 1) % points.Length;
        }
    }
}