using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 3f;  // walking speed
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
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

        // Convert input to world direction
        Vector3 move = transform.right * x + transform.forward * z;

        // Apply movement
        controller.Move(move * speed * Time.deltaTime);

        // Apply gravity so you stay grounded
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
