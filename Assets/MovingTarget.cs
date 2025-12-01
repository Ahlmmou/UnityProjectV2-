using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    public float amplitude = 0.4f;
    public float speed = 1.0f;

    private float baseY;
    private bool active = false;

    void Start()
    {
        baseY = transform.position.y;
    }

    void Update()
    {
        // Start motion when S is pressed
        if (!active && Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.S))
            active = true;

        if (!active) return;

        // Triangle wave (zig-zag)
        float t = Mathf.PingPong(Time.time * speed, 1f);
        float yOffset = (t * 2f - 1f) * amplitude;

        transform.position = new Vector3(
            transform.position.x,
            baseY + yOffset,
            transform.position.z
        );
    }
}
