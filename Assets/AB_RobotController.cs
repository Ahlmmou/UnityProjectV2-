using UnityEngine;

public class AB_RobotController : MonoBehaviour
{
    [Header("Assign rotating joints in order (Link1 .. EndEff) � DO NOT include the fixed base")]
    public ArticulationBody[] joints;   //

    [Header("Motion Speeds")]
    [Tooltip("Degrees per second when holding an inc/dec key (without Shift/Alt).")]
    public float speedDegPerSec = 10f;   // low-ish default
    [Tooltip("Hold Shift to go faster (multiplies speed).")]
    public float shiftMultiplier = 1.5f; // modest boost
    [Tooltip("Hold Alt to go slower (multiplies speed).")]
    public float altMultiplier = 0.5f;   // fine control

    [Header("Keys (+/- per joint) � T/Y/U/I/O = right, G/H/J/K/L = left")]
    public KeyCode[] increaseKeys = { KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O };
    public KeyCode[] decreaseKeys = { KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

    [Header("Extras")]
    public bool clampToLimits = true;          // keep target inside xDrive limits
    public KeyCode zeroAllKey = KeyCode.Space; // zero all joint targets

    private float[] targets; // degrees

    void Start()
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("Assign 'joints' (Link1..EndEff). Do not include the fixed base.");
            enabled = false; return;
        }

        targets = new float[joints.Length];

        // Initialize targets from current joint positions (radians -> degrees)
        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;
            var pose = ab.jointPosition; // radians
            targets[i] = (pose.dofCount > 0 ? pose[0] : 0f) * Mathf.Rad2Deg;
        }
    }

    void Update()
    {
        if (targets == null || joints == null) return;

        float mult = 1f;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftShift) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightShift)) mult *= shiftMultiplier;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftAlt) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightAlt)) mult *= altMultiplier;

        float step = speedDegPerSec * mult * Time.deltaTime;

        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;

            bool inc = (i < increaseKeys.Length) && increaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(increaseKeys[i]);
            bool dec = (i < decreaseKeys.Length) && decreaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(decreaseKeys[i]);

            if (inc && !dec) targets[i] += step;
            if (dec && !inc) targets[i] -= step;

            var d = ab.xDrive; // struct
            if (clampToLimits && (!Mathf.Approximately(d.lowerLimit, 0f) || !Mathf.Approximately(d.upperLimit, 0f)))
                targets[i] = Mathf.Clamp(targets[i], d.lowerLimit, d.upperLimit);

            d.target = targets[i]; // degrees
            ab.xDrive = d;
        }

        // Zero all joints (optional)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(zeroAllKey))
        {
            for (int i = 0; i < joints.Length; i++)
            {
                var ab = joints[i]; if (!ab) continue;
                var d = ab.xDrive;
                float zero = Mathf.Clamp(0f, d.lowerLimit, d.upperLimit);
                d.target = zero;
                ab.xDrive = d;
                targets[i] = zero;
            }
        }
    }
}
