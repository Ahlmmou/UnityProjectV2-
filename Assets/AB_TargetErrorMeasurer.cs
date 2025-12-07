using UnityEngine;
using TMPro;

public class AB_TargetErrorMeasurer : MonoBehaviour
{
    [Header("Robot Setup")]
    [Tooltip("End-effector transform (tip of the robot).")]
    public Transform endEffector;

    [Header("Targets (size = 2)")]
    [Tooltip("Two target positions the user should try to reach.")]
    public Transform[] targets; // size = 2

    [Header("Keys")]
    public KeyCode target0Key = KeyCode.C;  // Pick Target A
    public KeyCode target1Key = KeyCode.V;  // Pick Target B

    [Tooltip("Key to press when user thinks they reached the target.")]
    public KeyCode markReachedKey = KeyCode.X;  // <- we use X instead of Space

    [Header("Optional UI")]
    public TMP_Text infoText; // drag in your UI text if you want

    private int currentTargetIndex = -1;
    private int trialCount = 0;

    private void Update()
    {
        // Choose Target 0
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(target0Key) && targets.Length > 0 && targets[0] != null)
        {
            currentTargetIndex = 0;
            UpdateInfoText("Target A selected.\nMove the robot using the AB controller, then press X.");
            Debug.Log("[AB_TargetErrorMeasurer] Target A selected.");
        }

        // Choose Target 1
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(target1Key) && targets.Length > 1 && targets[1] != null)
        {
            currentTargetIndex = 1;
            UpdateInfoText("Target B selected.\nMove the robot using the AB controller, then press X.");
            Debug.Log("[AB_TargetErrorMeasurer] Target B selected.");
        }

        // Mark reached & compute error
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(markReachedKey))
        {
            if (currentTargetIndex < 0)
            {
                UpdateInfoText("?? No target selected. Press C or V first.");
                Debug.LogWarning("[AB_TargetErrorMeasurer] Cannot mark — no target selected.");
                return;
            }

            if (endEffector == null || targets[currentTargetIndex] == null)
            {
                Debug.LogError("[AB_TargetErrorMeasurer] EndEffector or target not assigned.");
                return;
            }

            trialCount++;

            Vector3 desired = targets[currentTargetIndex].position;
            Vector3 actual = endEffector.position;
            Vector3 errorVec = actual - desired;
            float errorDist = errorVec.magnitude;

            string msg =
                $"Trial #{trialCount} | Target {(currentTargetIndex == 0 ? "A" : "B")}\n" +
                $"Desired: {desired:F3}\n" +
                $"Reached: {actual:F3}\n" +
                $"Error vector: {errorVec:F3}\n" +
                $"Error distance: {errorDist:F4} m";

            UpdateInfoText(msg);
            Debug.Log("[AB_TargetErrorMeasurer] " + msg);
        }
    }

    private void UpdateInfoText(string msg)
    {
        if (infoText != null)
            infoText.text = msg;
    }
}
