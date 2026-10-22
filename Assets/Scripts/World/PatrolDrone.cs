using UnityEngine;

/// <summary>
/// Dron de seguridad de la DTA que patrulla entre puntos, con hélices animadas (2 fotogramas)
/// y sombra en el suelo. Es decorativo: no tiene colisión.
/// </summary>
public class PatrolDrone : MonoBehaviour
{
    public Vector2[] waypoints = new Vector2[0];
    public float speed = 1.6f;
    public float waitSeconds = 1.2f;
    public SpriteRenderer body;
    public Sprite frameA;
    public Sprite frameB;
    public float frameRate = 14f;

    int target;
    float waitUntil;

    void Update()
    {
        if (body != null && frameA != null && frameB != null)
        {
            body.sprite = Mathf.FloorToInt(Time.time * frameRate) % 2 == 0 ? frameA : frameB;
        }
        if (waypoints.Length < 2 || Time.time < waitUntil) return;

        Vector2 position = transform.position;
        Vector2 goal = waypoints[target];
        Vector2 next = Vector2.MoveTowards(position, goal, speed * Time.deltaTime);
        if (body != null && Mathf.Abs(goal.x - position.x) > 0.01f) body.flipX = goal.x < position.x;
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        if ((next - goal).sqrMagnitude < 0.0004f)
        {
            target = (target + 1) % waypoints.Length;
            waitUntil = Time.time + waitSeconds;
        }
    }
}
