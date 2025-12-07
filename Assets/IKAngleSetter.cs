using UnityEngine;

public class IKAngleSetter : MonoBehaviour
{
    [Header("Robot Joints (ArticulationBodies in order from base to tip)")]
    public ArticulationBody[] joints;

    [Header("Angle step per key press (degrees)")]
    public float angleStep = 5f;

    private int currentJointIndex = 0;

    void Start()
    {
        Debug.Log("IKKeyboardControl started on " + gameObject.name);

        if (joints == null || joints.Length == 0)
        {
            Debug.LogWarning("No joints assigned to IKKeyboardControl.");
        }
        else
        {
            Debug.Log("Initial selected joint: 0 (" + joints[0].name + ")");
        }
    }

    void Update()
    {
        // -------- Select joint with keys 1..5 --------
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Alpha1)) SelectJoint(0);
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Alpha2)) SelectJoint(1);
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Alpha3)) SelectJoint(2);
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Alpha4)) SelectJoint(3);
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Alpha5)) SelectJoint(4);

        // -------- Rotate selected joint with arrows --------
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.UpArrow))
        {
            AdjustCurrentJointAngle(+angleStep);
        }
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.DownArrow))
        {
            AdjustCurrentJointAngle(-angleStep);
        }
    }

    void SelectJoint(int index)
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogWarning("No joints assigned to IKKeyboardControl.");
            return;
        }

        if (index < 0 || index >= joints.Length)
        {
            Debug.LogWarning("Joint index " + index + " is out of range.");
            return;
        }

        currentJointIndex = index;
        Debug.Log("Selected joint " + currentJointIndex + " (" + joints[currentJointIndex].name + ")");
    }

    void AdjustCurrentJointAngle(float delta)
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogWarning("No joints assigned to IKKeyboardControl.");
            return;
        }

        ArticulationBody joint = joints[currentJointIndex];

        var drive = joint.xDrive;
        float newTarget = drive.target + delta;
        drive.target = newTarget;
        joint.xDrive = drive;

        Debug.Log("Joint " + currentJointIndex + " (" + joint.name + ") angle set to " + newTarget + " degrees.");
    }
}
