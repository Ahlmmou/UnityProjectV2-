using UnityEngine;

/// <summary>
/// Keeps links connected, locks unwanted motion, and isolates robot control from player input.
/// </summary>
public class JointController : MonoBehaviour
{
    [Header("Assign your joints in order from base to end")]
    public ArticulationBody[] joints;

    [Header("Target angles for each joint (degrees)")]
    public float[] targetAngles;

    [Header("Speed of motion (degrees/sec)")]
    public float speed = 30f;

    [Header("Enable manual robot control (toggle with F key)")]
    public bool controlEnabled = true;

    void Start()
    {
        // Ensure targetAngles array matches joint count
        if (targetAngles == null || targetAngles.Length != joints.Length)
            targetAngles = new float[joints.Length];

        // Configure each joint to stay connected and locked
        for (int i = 0; i < joints.Length; i++)
        {
            ArticulationBody joint = joints[i];
            if (joint == null) continue;

            // ? Keep the parent–child connection (don’t reparent!)
            // ? Align anchors so they're connected
            joint.anchorPosition = Vector3.zero;
            joint.parentAnchorPosition = Vector3.zero;

            // ? Lock all translation (no sliding)
            joint.linearLockX = ArticulationDofLock.LockedMotion;
            joint.linearLockY = ArticulationDofLock.LockedMotion;
            joint.linearLockZ = ArticulationDofLock.LockedMotion;

            // ? Allow only rotation around its configured twist axis
            joint.twistLock = ArticulationDofLock.LimitedMotion;
            joint.swingYLock = ArticulationDofLock.LockedMotion;
            joint.swingZLock = ArticulationDofLock.LockedMotion;

            // ? Prevent gravity pulling links apart
            joint.useGravity = false;

            // ? Ensure correct joint type
            if (joint.jointType != ArticulationJointType.FixedJoint)
                joint.jointType = ArticulationJointType.RevoluteJoint;
        }
    }

    void Update()
    {
        // Smoothly move each joint toward its target angle
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;
            var drive = joints[i].xDrive;
            drive.target = Mathf.MoveTowards(drive.target, targetAngles[i], speed * Time.deltaTime);
            joints[i].xDrive = drive;
        }

        // ?? Toggle robot control
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.F))
            controlEnabled = !controlEnabled;

        if (!controlEnabled) return; // ?? stop reading input if disabled

        // --- Robot manual control (use F1–F5 & Z–V) ---
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.F1)) targetAngles[0] += 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.F2)) targetAngles[1] += 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.F3)) targetAngles[2] += 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.F4)) targetAngles[3] += 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.F5)) targetAngles[4] += 20f * Time.deltaTime;

        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.Z)) targetAngles[0] -= 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.X)) targetAngles[1] -= 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.C)) targetAngles[2] -= 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.V)) targetAngles[3] -= 20f * Time.deltaTime;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.B)) targetAngles[4] -= 20f * Time.deltaTime;
    }
}
