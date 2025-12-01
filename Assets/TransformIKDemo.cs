using UnityEngine;
using TMPro;

/// <summary>
/// CCD-style IK demo that rotates joint Transforms directly.
/// - Assign joints[0..N-1] from base to last joint (no fixed base).
/// - Each joint is rotated around a chosen local axis (jointAxesLocal[i]).
/// - Press C or V (or use UI buttons calling ChooseTarget0/1) to move toward Target 0 or 1.
/// - Shows desired vs actual EE position and error if infoText is assigned.
/// </summary>
public class TransformIKDemo : MonoBehaviour
{
    [Header("Robot Setup")]
    [Tooltip("Joint transforms from base to last joint (no fixed base).")]
    public Transform[] joints;       // e.g. Link1..Link5

    [Tooltip("End-effector transform (tip of the robot).")]
    public Transform endEffector;

    [Header("Joint Axes (local)")]
    [Tooltip("Local rotation axis for each joint (e.g., (1,0,0) for local X, (0,0,1) for local Z).")]
    public Vector3[] jointAxesLocal; // same length as joints

    [Header("Targets (size = 2)")]
    [Tooltip("Two target positions that the EE should try to reach.")]
    public Transform[] targets;      // size 2

    [Header("IK Settings")]
    [Tooltip("Max rotation per joint per iteration (degrees).")]
    public float maxStepAngleDeg = 2f;
    [Tooltip("How many CCD iterations per frame.")]
    public int iterationsPerFrame = 4;
    [Tooltip("Stop when EE is closer than this (meters).")]
    public float positionThreshold = 0.005f;

    [Header("Input Keys")]
    public KeyCode target0Key = KeyCode.C;
    public KeyCode target1Key = KeyCode.V;

    [Header("Optional UI")]
    public TMP_Text infoText;  // assign a TextMeshProUGUI here if you want

    private int currentTargetIndex = -1;
    private bool isSolving = false;

    private void Start()
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("[TransformIKDemo] Please assign 'joints' array.");
            enabled = false;
            return;
        }

        if (jointAxesLocal == null || jointAxesLocal.Length != joints.Length)
        {
            // Default: assume all joints rotate around local Z axis
            jointAxesLocal = new Vector3[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                jointAxesLocal[i] = Vector3.forward; // (0,0,1) ? local Z
            }
        }
    }

    private void Update()
    {
        // Select target 0 (C)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(target0Key) && targets.Length > 0 && targets[0] != null)
        {
            StartSolving(0);
        }

        // Select target 1 (V)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(target1Key) && targets.Length > 1 && targets[1] != null)
        {
            StartSolving(1);
        }

        if (isSolving && currentTargetIndex >= 0)
        {
            Transform target = targets[currentTargetIndex];
            Vector3 targetPos = target.position;

            // Perform several CCD iterations per frame
            for (int iter = 0; iter < iterationsPerFrame; iter++)
            {
                RunCCDStep(targetPos);
            }

            // Compute error
            Vector3 eePos = endEffector.position;
            float posError = Vector3.Distance(eePos, targetPos);

            if (posError < positionThreshold)
            {
                isSolving = false;
            }

            // UI info
            if (infoText != null)
            {
                infoText.text =
                    $"Target: {currentTargetIndex}\n" +
                    $"Desired: {targetPos:F3}\n" +
                    $"Actual:  {eePos:F3}\n" +
                    $"Error:   {posError:F4} m";
            }
        }
    }

    private void StartSolving(int targetIndex)
    {
        currentTargetIndex = targetIndex;
        isSolving = true;
        Debug.Log($"[TransformIKDemo] Moving toward Target {targetIndex}.");
    }

    /// <summary>
    /// One CCD sweep over all joints, rotating them around their local axis.
    /// </summary>
    private void RunCCDStep(Vector3 targetPos)
    {
        if (joints == null || joints.Length == 0 || endEffector == null)
            return;

        // Go from last joint back to first
        for (int i = joints.Length - 1; i >= 0; i--)
        {
            Transform jt = joints[i];
            if (!jt) continue;

            Vector3 jointPos = jt.position;
            Vector3 toEnd = endEffector.position - jointPos;
            Vector3 toTarget = targetPos - jointPos;

            if (toEnd.sqrMagnitude < 1e-8f || toTarget.sqrMagnitude < 1e-8f)
                continue;

            toEnd.Normalize();
            toTarget.Normalize();

            // Axis in world space from local axis
            Vector3 axisLocal = jointAxesLocal[i];
            if (axisLocal == Vector3.zero)
                continue;

            Vector3 axisWorld = jt.TransformDirection(axisLocal.normalized);

            // Signed angle between the two directions around that axis
            float signedAngle = Vector3.SignedAngle(toEnd, toTarget, axisWorld);
            if (Mathf.Abs(signedAngle) < 0.001f)
                continue;

            float step = Mathf.Clamp(signedAngle, -maxStepAngleDeg, maxStepAngleDeg);

            // Apply rotation around axisWorld in world space
            Quaternion deltaRot = Quaternion.AngleAxis(step, axisWorld);
            jt.rotation = deltaRot * jt.rotation;
        }
    }

    // For UI Buttons if you want:
    public void ChooseTarget0()
    {
        if (targets.Length > 0 && targets[0] != null)
            StartSolving(0);
    }

    public void ChooseTarget1()
    {
        if (targets.Length > 1 && targets[1] != null)
            StartSolving(1);
    }
}
