using UnityEngine;
using System.Collections;

public class CircularMotionController : MonoBehaviour
{
    // --- Dependencies ---
    [Tooltip("The AB_RobotController component on the robot's base or an ancestor. This is required to access and control the joints.")]
    public AB_RobotController robotController;

    // --- Configurable Parameters ---
    [Header("Forward Kinematics Circle Settings")]
    [Tooltip("The speed at which the rotating joint moves (degrees/second). This determines the speed of the single-cycle circle.")]
    public float jointSpeed = 120f; // Increased speed for faster single rotation
    [Tooltip("The amplitude (in degrees) for the vertical joint oscillation. Determines the circle's radius.")]
    public float verticalJointAmplitudeDegrees = 5f;
    [Tooltip("The offset angle (in degrees) for the vertical joint to lift the arm up or down.")]
    public float verticalJointOffsetDegrees = 30f;
    [Tooltip("Time (in seconds) taken for the robot to smoothly return to its initial position after the circle is complete.")]
    public float returnDuration = 1.5f;

    [Header("Joint Configuration")]
    [Tooltip("The index of the joint that rotates (e.g., Base=0, Wrist=3). Base rotation is often best for a full circle.")]
    public int rotatingJointIndex = 0; // Defaulting to Base (0)
    [Tooltip("The index of the joint that provides the vertical/radial oscillation (e.g., Shoulder=1, Elbow=2, Wrist=4).")]
    public int verticalJointIndex = 1; // Changed default to Shoulder (1)

    [Header("Control")]
    [Tooltip("The key to press to start the single circular motion.")]
    public KeyCode toggleKey = KeyCode.S; // Set to 'S' key

    private Coroutine circleCoroutine;
    private bool isMoving = false;
    private bool isReturning = false; // New flag to track return state

    private ArticulationBody rotatingJoint;
    private ArticulationBody verticalJoint;

    private float initialRotatingJointTarget;
    private float initialVerticalJointTarget; // New variable to store the initial vertical joint target

    void Start()
    {
        // 1. Get the AB_RobotController component
        if (robotController == null)
        {
            robotController = GetComponentInParent<AB_RobotController>();
            if (robotController == null)
            {
                Debug.LogError("AB_RobotController not linked! Please drag the robot's base object into the Robot Controller slot.");
                enabled = false;
                return;
            }
        }

        // 2. Get the required joints for the circular motion
        if (robotController.joints != null && robotController.joints.Length > rotatingJointIndex && robotController.joints.Length > verticalJointIndex)
        {
            rotatingJoint = robotController.joints[rotatingJointIndex];
            verticalJoint = robotController.joints[verticalJointIndex];

            // Store the current target of the joints
            initialRotatingJointTarget = rotatingJoint.xDrive.target;
            initialVerticalJointTarget = verticalJoint.xDrive.target; // Save initial vertical target
        }
        else
        {
            Debug.LogError($"Robot Controller's 'joints' array is insufficient. Ensure joints at indices {rotatingJointIndex} (rotating) and {verticalJointIndex} (vertical) are assigned.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // Only allow key press if not currently moving OR returning
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(toggleKey) && !isMoving && !isReturning)
        {
            StartMotion();
        }
    }

    private void StartMotion()
    {
        // Safety check to ensure the coroutine isn't running
        if (circleCoroutine != null)
        {
            StopCoroutine(circleCoroutine);
        }

        circleCoroutine = StartCoroutine(PerformCircleMotion());
        isMoving = true;

        // Disable the AB_RobotController to stop conflicting keyboard control
        if (robotController != null)
        {
            robotController.enabled = false;
            Debug.Log("FK Single Circular Motion Started. Keyboard control disabled.");
        }
    }

    // Called automatically when the circle motion finishes
    private void EndCircleMotion()
    {
        if (circleCoroutine != null)
        {
            StopCoroutine(circleCoroutine);
            circleCoroutine = null;
        }

        isMoving = false;

        // Start the return motion immediately
        StartCoroutine(ReturnToInitialPosition());
    }

    private void ReenableControls()
    {
        isReturning = false;
        // Re-enable the AB_RobotController
        if (robotController != null)
        {
            robotController.enabled = true;
            Debug.Log("Robot motion finished. Keyboard control re-enabled.");
        }
    }

    IEnumerator PerformCircleMotion()
    {
        float angleCompleted = 0f; // Track total angle moved
        // We use the last set target as the starting point for the rotation
        float startAngle = rotatingJoint.xDrive.target;

        while (angleCompleted < 360f)
        {
            // Calculate time since last frame
            float deltaTime = Time.deltaTime;
            float angleStep = jointSpeed * deltaTime;

            // Ensure we don't overshoot 360 degrees on the last step
            if (angleCompleted + angleStep > 360f)
            {
                angleStep = 360f - angleCompleted;
            }

            angleCompleted += angleStep;

            // 1. Control Rotating Joint: Continuous rotation for 360 degrees
            float newRotatingAngle = startAngle + angleCompleted;

            var rotatingDrive = rotatingJoint.xDrive;
            // *** FIX: Clamp to joint limits to prevent errors ***
            rotatingDrive.target = Mathf.Clamp(newRotatingAngle, rotatingDrive.lowerLimit, rotatingDrive.upperLimit);
            rotatingJoint.xDrive = rotatingDrive;

            // 2. Control Vertical Joint: Sinusoidal Oscillation
            float oscillation = Mathf.Sin(angleCompleted * Mathf.Deg2Rad) * verticalJointAmplitudeDegrees;
            float newVerticalAngle = verticalJointOffsetDegrees + oscillation;

            var verticalDrive = verticalJoint.xDrive;
            // *** FIX: Clamp to joint limits to prevent errors ***
            verticalDrive.target = Mathf.Clamp(newVerticalAngle, verticalDrive.lowerLimit, verticalDrive.upperLimit);
            verticalJoint.xDrive = verticalDrive;

            yield return null;
        }

        // Motion is complete, stop the coroutine and trigger return
        EndCircleMotion();
    }

    IEnumerator ReturnToInitialPosition()
    {
        isReturning = true;

        // Capture the current end positions for interpolation
        float currentRotatingAngle = rotatingJoint.xDrive.target;
        float currentVerticalAngle = verticalJoint.xDrive.target;

        float time = 0f;
        while (time < returnDuration)
        {
            time += Time.deltaTime;
            float t = time / returnDuration; // Normalized time (0 to 1)

            // Interpolate the Rotating Joint back to its initial target
            float targetR = Mathf.Lerp(currentRotatingAngle, initialRotatingJointTarget, t);
            var rotatingDrive = rotatingJoint.xDrive;
            rotatingDrive.target = Mathf.Clamp(targetR, rotatingDrive.lowerLimit, rotatingDrive.upperLimit);
            rotatingJoint.xDrive = rotatingDrive;

            // Interpolate the Vertical Joint back to its initial target
            float targetV = Mathf.Lerp(currentVerticalAngle, initialVerticalJointTarget, t);
            var verticalDrive = verticalJoint.xDrive;
            verticalDrive.target = Mathf.Clamp(targetV, verticalDrive.lowerLimit, verticalDrive.upperLimit);
            verticalJoint.xDrive = verticalDrive;

            yield return null;
        }

        // Ensure the final targets are exactly the initial positions
        var rotatingDriveFinal = rotatingJoint.xDrive;
        rotatingDriveFinal.target = initialRotatingJointTarget;
        rotatingJoint.xDrive = rotatingDriveFinal;

        var verticalDriveFinal = verticalJoint.xDrive;
        verticalDriveFinal.target = initialVerticalJointTarget;
        verticalJoint.xDrive = verticalDriveFinal;

        // Re-enable controls
        ReenableControls();
    }
}