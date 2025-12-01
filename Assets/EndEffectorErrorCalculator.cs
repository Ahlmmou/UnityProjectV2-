using UnityEngine;

public class EndEffectorErrorCalculator : MonoBehaviour
{
    [Header("References")]
    public Transform endEffector;   // Drag your EE here
    public Transform targetA;       // Drag Target A object
    public Transform targetB;       // Drag Target B object

    [Header("Settings")]
    public KeyCode measureKey = KeyCode.E;

    private Transform activeTarget;

    void Start()
    {
        // Default: use TargetA unless changed
        activeTarget = targetA;
    }

    void Update()
    {
        // Switch target with C / V (industry style)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.C))
        {
            activeTarget = targetA;
            Debug.Log("Active Target set to Target A.");
        }
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.V))
        {
            activeTarget = targetB;
            Debug.Log("Active Target set to Target B.");
        }

        // Compute error when pressing E
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(measureKey))
        {
            ComputePositionError();
        }
    }

    void ComputePositionError()
    {
        if (activeTarget == null || endEffector == null)
        {
            Debug.LogError("Missing Transform references!");
            return;
        }

        Vector3 eePos = endEffector.position;
        Vector3 targetPos = activeTarget.position;

        // POSITION ERROR VECTOR (EE ? Target)
        Vector3 error = targetPos - eePos;

        // ABSOLUTE XYZ ERROR (magnitudes)
        Vector3 absError = new Vector3(
            Mathf.Abs(error.x),
            Mathf.Abs(error.y),
            Mathf.Abs(error.z)
        );

        float totalError = error.magnitude;

        Debug.Log(
            $"---- Error Measurement ----\n" +
            $"Active Target: {activeTarget.name}\n" +
            $"EE Pos: {eePos}\n" +
            $"Target Pos: {targetPos}\n" +
            $"Error Vector: {error}\n" +
            $"|Error| = {totalError:F4} meters\n" +
            $"Axis Errors (X,Y,Z) = {absError}\n" +
            $"----------------------------"
        );
    }
}
