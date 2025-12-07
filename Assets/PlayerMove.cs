using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 3f;  // walking speed
    public float gravity = -9.81f;

    [Header("Movement Orientation")]
    [Tooltip("Optional: set to the camera or any transform whose yaw defines forward. If empty, will try Camera.main, else this transform.")]
    public Transform moveOrientation;

    private CharacterController controller;
    private Vector3 velocity;
    private Transform fallbackCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        fallbackCamera = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        // Read keyboard input (arrows or WASD both work)
        float x = 0f;
        float z = 0f;

        // Only read input if chat is NOT active
        if (!Assets.Scripts.ChatbotInputHelper.IsChatbotInputActive())
        {
            x = Input.GetAxis("Horizontal"); // ? ? or A D
            z = Input.GetAxis("Vertical");   // ? ? or W S
        }

        // Convert input to world direction using a yaw reference
        GetPlanarBasis(out Vector3 fwd, out Vector3 right);
        Vector3 move = right * x + fwd * z;

        // Apply movement
        controller.Move(move * speed * Time.deltaTime);

        // Apply gravity so you stay grounded
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void GetPlanarBasis(out Vector3 fwd, out Vector3 right)
    {
        Transform basis = moveOrientation ? moveOrientation : (fallbackCamera ? fallbackCamera : transform);
        fwd = basis.forward; fwd.y = 0f;
        right = basis.right; right.y = 0f;

        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;

        fwd.Normalize();
        right.Normalize();
    }
}
