using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerMovementWithButtonSteps : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Footstep Settings")]
    public AudioClip[] footstepSounds;
    public float stepInterval = 0.5f;  // seconds between steps when walking
    public Vector2 randomPitch = new Vector2(0.95f, 1.05f);

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 velocity;
    private float stepTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;  // 3D sound
    }

    void Update()
    {
        // --- Movement input ---
        float moveX = 0f;
        float moveZ = 0f;

        // Only read input if chat is NOT active
        if (!Assets.Scripts.ChatbotInputHelper.IsChatbotInputActive())
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }
        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;

        // Apply movement
        controller.Move(move * speed * Time.deltaTime);

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // --- Footstep Trigger ---
        bool movingKeyPressed = move.magnitude > 0.1f && controller.isGrounded;

        if (movingKeyPressed)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        int n = (footstepSounds.Length == 1) ? 0 : Random.Range(0, footstepSounds.Length);
        audioSource.pitch = Random.Range(randomPitch.x, randomPitch.y);
        audioSource.PlayOneShot(footstepSounds[n]);
    }
}
