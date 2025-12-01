using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Note: This class is defined outside the main MonoBehaviour to hold data
[System.Serializable]
public class TargetSet
{
    [Tooltip("The desired target angle (in degrees) for each controlled joint, in order (Link1 to Link5).")]
    // Since all joints rotate about X, these angles represent the X-axis rotation target for each ArticulationBody.
    public float[] jointTargets;
}

/// <summary>
/// Controls a 5-DOF ArticulationBody robot arm to trace a square path 
/// using predefined joint angle targets, bypassing the need for Inverse Kinematics.
/// </summary>
public class SquareTrajectory : MonoBehaviour
{
    // --- INSPECTOR VARIABLES ---

    [Header("Robot Joints (Must Match AB_RobotController Order)")]
    [Tooltip("The ArticulationBody joints that control the arm, taken directly from AB_RobotController.")]
    public ArticulationBody[] robotJoints;

    [Header("Trajectory Definition")]
    [Tooltip("Define the 4 (or more) sets of joint angles that form the corners of the square.")]
    public List<TargetSet> squareCorners = new List<TargetSet>();

    [Tooltip("Time (in seconds) to wait after setting targets before moving to the next corner.")]
    public float cornerWaitTime = 1.0f;

    [Tooltip("The duration (in seconds) that the movement to a new corner should take.")]
    public float movementDuration = 4.0f; // INCREASED: Changed from 2.0s to 4.0s for slower movement

    // --- PRIVATE STATE ---
    private bool isMoving = false;
    private Coroutine trajectoryCoroutine;

    // --- DEFAULT CORNERS INITIALIZATION ---

    // Constructor ensures the list is populated with initial values 
    // when the script is first attached.
    public SquareTrajectory()
    {
        // This initialization provides a safe, visible path for a 5-DOF arm.
        // Joint order assumed: [Base Swivel (Link1), Shoulder Pitch (Link2), Elbow Pitch (Link3), Wrist Pitch (Link4), Wrist Roll (Link5)]

        if (squareCorners.Count == 0)
        {
            // Corner 1: Bottom-Left (Extended forward and down)
            // Angles: [0, -20, 60, 30, 0]
            squareCorners.Add(new TargetSet { jointTargets = new float[] { 0, -20, 60, 30, 0 } });

            // Corner 2: Top-Left (Retracted and Up)
            // Angles: [0, 20, 30, 60, 0]
            squareCorners.Add(new TargetSet { jointTargets = new float[] { 0, 20, 30, 60, 0 } });

            // Corner 3: Top-Right (Same as C2, but Base is rotated to 45 deg)
            // Angles: [45, 20, 30, 60, 0]
            squareCorners.Add(new TargetSet { jointTargets = new float[] { 45, 20, 30, 60, 0 } });

            // Corner 4: Bottom-Right (Same as C1, but Base is rotated to 45 deg)
            // Angles: [45, -20, 60, 30, 0]
            squareCorners.Add(new TargetSet { jointTargets = new float[] { 45, -20, 60, 30, 0 } });
        }
    }

    // --- NEW: INPUT HANDLING ---
    void Update()
    {
        // Check if the 'S' key is pressed and the robot is not already moving
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.S) && !isMoving)
        {
            StartSquare();
        }
    }
    // ---------------------------


    // --- PUBLIC METHODS ---

    /// <summary>
    /// Starts the square trajectory movement sequence.
    /// </summary>
    [ContextMenu("Start Square Trajectory (Press S)")] // Updated ContextMenu text
    public void StartSquare()
    {
        if (robotJoints == null || robotJoints.Length == 0)
        {
            Debug.LogError("[SquareTrajectory] Robot Joints are NOT assigned. Ensure AB_RobotController has its joints set up.");
            return;
        }

        // Basic error checking
        if (robotJoints.Length != 5)
        {
            Debug.LogError($"[SquareTrajectory] This script is optimized for 5 DOF. Found {robotJoints.Length}. Please adjust joint targets.");
            return;
        }

        if (squareCorners.Count < 4)
        {
            Debug.LogError("[SquareTrajectory] Not enough corner target sets defined (need at least 4 for a square).");
            return;
        }

        if (squareCorners[0].jointTargets.Length != robotJoints.Length)
        {
            Debug.LogError($"[SquareTrajectory] TargetSet DOF mismatch: Defined corners require {squareCorners[0].jointTargets.Length} targets, but the robot has {robotJoints.Length} DOF. Please correct the predefined angles or re-capture.");
            return;
        }

        if (isMoving)
        {
            Debug.LogWarning("[SquareTrajectory] Robot is already performing a trajectory.");
            return;
        }

        // Disable manual keyboard input while trajectory runs
        var controller = GetComponent<AB_RobotController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        trajectoryCoroutine = StartCoroutine(ExecuteSquareTrajectory());
    }

    /// <summary>
    /// Stops the robot's current movement and resets the state.
    /// </summary>
    [ContextMenu("Stop Trajectory")]
    public void StopSquare()
    {
        if (trajectoryCoroutine != null)
        {
            StopCoroutine(trajectoryCoroutine);
            trajectoryCoroutine = null;
        }
        isMoving = false;
        Debug.Log("[SquareTrajectory] Trajectory stopped.");

        // Re-enable manual input
        var controller = GetComponent<AB_RobotController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    // --- PRIVATE COROUTINE LOGIC ---

    /// <summary>
    /// Coroutine that executes the square trajectory by setting joint targets sequentially.
    /// </summary>
    private IEnumerator ExecuteSquareTrajectory()
    {
        isMoving = true;
        Debug.Log("[SquareTrajectory] Starting square trajectory.");

        // Loop through all defined corner targets
        foreach (var targetSet in squareCorners)
        {
            // Apply the new joint targets directly to the ArticulationBody xDrive
            for (int i = 0; i < robotJoints.Length; i++)
            {
                var joint = robotJoints[i];
                if (joint == null) continue;

                var d = joint.xDrive;
                // Set the target position (in degrees)
                d.target = targetSet.jointTargets[i];
                joint.xDrive = d;
            }

            // 1. Wait for the movement to complete (determined by the robot's drive velocity/acceleration).
            yield return new WaitForSeconds(movementDuration);

            // 2. Wait at the corner before starting the next move.
            yield return new WaitForSeconds(cornerWaitTime);
        }

        isMoving = false;
        Debug.Log("[SquareTrajectory] Square trajectory completed successfully. Re-enabling manual control.");

        // Re-enable manual input
        var controller = GetComponent<AB_RobotController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    // --- FALLBACK INITIALIZATION ---

    void Awake()
    {
        // Safety check: automatically grab the joint array from the AB_RobotController
        if (robotJoints == null || robotJoints.Length == 0)
        {
            var controller = GetComponent<AB_RobotController>();
            if (controller != null && controller.joints != null)
            {
                robotJoints = controller.joints;
                Debug.Log("[SquareTrajectory] Auto-assigned joints from AB_RobotController. Robot has " + robotJoints.Length + " DOF.");
            }
            else
            {
                Debug.LogWarning("[SquareTrajectory] AB_RobotController not found or joints not initialized. Please ensure 'Robot Joints' is assigned.");
            }
        }
    }

    /// <summary>
    /// Helper method to create a new TargetSet in the list from the robot's current joint positions.
    /// This is included for fine-tuning the predefined square in the future.
    /// </summary>
    [ContextMenu("Capture Current Position (Fine-Tuning)")]
    public void CaptureCurrentPosition()
    {
        if (robotJoints == null || robotJoints.Length == 0)
        {
            Debug.LogError("[SquareTrajectory] Cannot capture position: robotJoints array is empty. Check if AB_RobotController has its 'joints' assigned.");
            return;
        }

        float[] currentTargets = new float[robotJoints.Length];
        for (int i = 0; i < robotJoints.Length; i++)
        {
            var ab = robotJoints[i];
            if (ab == null) continue;

            // Get the current target position in degrees (xDrive.target is the last set target)
            currentTargets[i] = ab.xDrive.target;
        }

        TargetSet newTarget = new TargetSet { jointTargets = currentTargets };
        squareCorners.Add(newTarget);
        Debug.Log($"Captured current joint positions for {robotJoints.Length} DOF and added to squareCorners list. Total corners: {squareCorners.Count}");
    }
}