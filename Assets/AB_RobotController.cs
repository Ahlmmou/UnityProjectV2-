using UnityEngine;
using UnityEngine.UI;

public class AB_RobotController : MonoBehaviour
{
    [Header("Assign rotating joints in order (Link1 .. EndEff) – DO NOT include the fixed base")]
    public ArticulationBody[] joints;   //

    [Header("Motion Speeds")]
    [Tooltip("Degrees per second when holding an inc/dec key (without Shift/Alt).")]
    public float speedDegPerSec = 10f;   // low-ish default
    [Tooltip("Hold Shift to go faster (multiplies speed).")]
    public float shiftMultiplier = 1.5f; // modest boost
    [Tooltip("Hold Alt to go slower (multiplies speed).")]
    public float altMultiplier = 0.5f;   // fine control

    [Header("Joint Selection")]
    [Tooltip("Shortcut keys to select each joint (X,C,V,B,N by default). Optional; leave empty to disable shortcuts.")]
    public KeyCode[] selectJointKeys = { KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N };
    [Tooltip("Degrees to rotate per arrow-key press for the currently selected joint.")]
    public float arrowStepDegrees = 5f;
    [Tooltip("Default selected joint (0-based index). Set -1 to start with no selection.")]
    public int defaultSelectedJoint = 0;

    [Header("Keys (Primary Axis) – T/Y/U/I/O = Right, G/H/J/K/L = Left")]
    public KeyCode[] increaseKeys = { KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O };
    public KeyCode[] decreaseKeys = { KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

    [Header("Keys (Vertical Axis) – Optional")]
    public KeyCode[] increaseKeysVertical; // Assign keys here for individual vertical control if needed
    public KeyCode[] decreaseKeysVertical;

    [Header("Extras")]
    public bool clampToLimits = true;          // keep target inside xDrive limits
    public KeyCode zeroAllKey = KeyCode.Space; // zero all joint targets

    [Header("Selection Highlight")]
    [Tooltip("Renderers to tint for each joint (one per joint). Leave empty to skip highlights.")]
    public Renderer[] jointRenderers;
    public Color highlightColor = Color.yellow;
    [Tooltip("Emission multiplier for highlight color (makes it pop even in dark scenes).")]
    public float highlightEmission = 6f;

    [Header("UI Display")]
    [Tooltip("Optional UI Text to display selected joint info. Leave empty to disable.")]
    public Text jointInfoText;

    private Vector2[] targets; // x=primary (degrees), y=vertical (degrees)
    private int selectedJointIndex = -1;
    private Color[] originalColors;

    void Start()
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("Assign 'joints' (Link1..EndEff). Do not include the fixed base.");
            enabled = false; return;
        }

        targets = new Vector2[joints.Length];
        CacheOriginalColors();

        // Initialize targets from current joint positions (radians -> degrees)
        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;
            var pose = ab.jointPosition; // radians
            
            // Primary Axis (Index 0)
            float xVal = (pose.dofCount > 0) ? pose[0] * Mathf.Rad2Deg : 0f;
            
            // Spin Axis (Index 1) - Swing Y for spinning the joint
            float yVal = (pose.dofCount > 1) ? pose[1] * Mathf.Rad2Deg : 0f;

            targets[i] = new Vector2(xVal, yVal);
        }

        if (defaultSelectedJoint >= 0 && defaultSelectedJoint < joints.Length && joints[defaultSelectedJoint] != null)
        {
            selectedJointIndex = defaultSelectedJoint;
            ApplyHighlight(-1, selectedJointIndex);
        }
    }

    void Update()
    {
        if (targets == null || joints == null) return;

        HandleJointSelectionInput();

        float mult = 1f;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftShift) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightShift)) mult *= shiftMultiplier;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftAlt) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightAlt)) mult *= altMultiplier;

        float step = speedDegPerSec * mult * Time.deltaTime;

        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;

            // Primary Axis (Existing)
            bool inc = (i < increaseKeys.Length) && increaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(increaseKeys[i]);
            bool dec = (i < decreaseKeys.Length) && decreaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(decreaseKeys[i]);

            if (inc && !dec) targets[i].x += step;
            if (dec && !inc) targets[i].x -= step;

            // Spin Axis (Swing Y)
            if (increaseKeysVertical != null && decreaseKeysVertical != null)
            {
                bool incV = (i < increaseKeysVertical.Length) && increaseKeysVertical[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(increaseKeysVertical[i]);
                bool decV = (i < decreaseKeysVertical.Length) && decreaseKeysVertical[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(decreaseKeysVertical[i]);

                if (incV && !decV) targets[i].y += step;
                if (decV && !incV) targets[i].y -= step;
            }

            ApplyTargetToJoint(i);
        }

        HandleArrowKeySteps();

        // Zero all joints (optional)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(zeroAllKey))
        {
            for (int i = 0; i < joints.Length; i++)
            {
                var ab = joints[i]; if (!ab) continue;
                var d = ab.xDrive;
                float zeroX = Mathf.Clamp(0f, d.lowerLimit, d.upperLimit);
                d.target = zeroX;
                ab.xDrive = d;

                // Zero Spin axis if exists
                var d2 = ab.yDrive;
                float zeroY = Mathf.Clamp(0f, d2.lowerLimit, d2.upperLimit);
                d2.target = zeroY;
                ab.yDrive = d2;

                targets[i] = new Vector2(zeroX, zeroY);
            }
        }

        // Update UI display
        UpdateJointInfoUI();
    }

    private void HandleJointSelectionInput()
    {
        if (selectJointKeys == null || selectJointKeys.Length == 0)
        {
            Debug.LogWarning("AB_RobotController: selectJointKeys array is empty!");
            return;
        }

        for (int i = 0; i < joints.Length && i < selectJointKeys.Length; i++)
        {
            if (selectJointKeys[i] == KeyCode.None) continue;
            
            // TEMPORARY: Use Input.GetKeyDown directly to bypass GlobalInputManager
            if (Input.GetKeyDown(selectJointKeys[i]))
            {
                Debug.LogWarning($"AB_RobotController: Key {selectJointKeys[i]} pressed for joint {i}");
                
                if (joints[i] == null)
                {
                    Debug.LogWarning($"AB_RobotController: Joint {i} is null, cannot select!");
                    continue;
                }

                Debug.LogWarning($"AB_RobotController: Selecting joint {i} (was {selectedJointIndex})");
                ApplyHighlight(selectedJointIndex, i);
                selectedJointIndex = i;
                break;
            }
        }
    }

    private void HandleArrowKeySteps()
    {
        // Primary Axis (Q/E) - controls SELECTED joint
        if (selectedJointIndex >= 0 && selectedJointIndex < joints.Length && joints[selectedJointIndex] != null)
        {
            float deltaX = 0f;
            if (Input.GetKeyDown(KeyCode.Q)) deltaX += arrowStepDegrees;
            if (Input.GetKeyDown(KeyCode.E)) deltaX -= arrowStepDegrees;

            if (!Mathf.Approximately(deltaX, 0f))
            {
                targets[selectedJointIndex].x += deltaX;
                ApplyTargetToJoint(selectedJointIndex);
            }
        }

        // Spin Axis (F/R) - ALWAYS controls joint 0 (base) regardless of selection
        if (joints.Length > 0 && joints[0] != null)
        {
            float deltaY = 0f;
            if (Input.GetKeyDown(KeyCode.F)) deltaY += arrowStepDegrees;
            if (Input.GetKeyDown(KeyCode.R)) deltaY -= arrowStepDegrees;

            if (!Mathf.Approximately(deltaY, 0f))
            {
                targets[0].y += deltaY;
                ApplyTargetToJoint(0);
            }
        }
    }

    private void ApplyTargetToJoint(int jointIndex)
    {
        if (jointIndex < 0 || jointIndex >= joints.Length) return;
        var ab = joints[jointIndex]; if (!ab) return;

        // --- Primary Axis (xDrive) ---
        var dX = ab.xDrive;
        if (clampToLimits && (!Mathf.Approximately(dX.lowerLimit, 0f) || !Mathf.Approximately(dX.upperLimit, 0f)))
            targets[jointIndex].x = Mathf.Clamp(targets[jointIndex].x, dX.lowerLimit, dX.upperLimit);
        
        dX.target = targets[jointIndex].x;
        ab.xDrive = dX;

        // --- Spin Axis (yDrive / Swing Y) ---
        // Controls the spin/rotation around the joint's Y-axis.
        // Make sure Swing Y is set to "Limited" in Unity Inspector for this to work.
        
        var dY = ab.yDrive;
        if (clampToLimits && (!Mathf.Approximately(dY.lowerLimit, 0f) || !Mathf.Approximately(dY.upperLimit, 0f)))
            targets[jointIndex].y = Mathf.Clamp(targets[jointIndex].y, dY.lowerLimit, dY.upperLimit);

        dY.target = targets[jointIndex].y;
        ab.yDrive = dY;
    }

    private void CacheOriginalColors()
    {
        if (jointRenderers == null || jointRenderers.Length == 0) return;
        int len = jointRenderers.Length;
        originalColors = new Color[len];
        for (int i = 0; i < len; i++)
        {
            originalColors[i] = GetRendererColor(jointRenderers[i], Color.white);
        }
    }


    private static Color GetRendererColor(Renderer r, Color fallback)
    {
        if (!r) return fallback;
        
        // Try to get color from shared material first (more reliable)
        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
        {
            return r.sharedMaterial.color;
        }
        
        // Fallback to material instance
        if (r.material != null && r.material.HasProperty("_Color"))
        {
            return r.material.color;
        }
        
        return fallback;
    }

    private static void SetRendererColor(Renderer r, Color c)
    {
        if (!r) return;
        
        // Use MaterialPropertyBlock for runtime color changes (doesn't modify the asset)
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", c);
        mpb.SetColor("_EmissionColor", c * Mathf.Max(1f, c.grayscale * 2f)); // boost visibility
        r.SetPropertyBlock(mpb);
    }

    private void ApplyHighlight(int prevIndex, int newIndex)
    {
        if (jointRenderers == null || jointRenderers.Length == 0)
        {
            Debug.LogWarning("AB_RobotController: jointRenderers array is empty or null. Please assign renderers in the Inspector to enable highlighting.");
            return;
        }

        Debug.LogWarning($"AB_RobotController: ApplyHighlight called - prev: {prevIndex}, new: {newIndex}");

        // Restore previous
        if (prevIndex >= 0 && prevIndex < jointRenderers.Length && jointRenderers[prevIndex])
        {
            Color baseColor = (originalColors != null && prevIndex < originalColors.Length) ? originalColors[prevIndex] : Color.white;
            SetRendererColor(jointRenderers[prevIndex], baseColor);
            Debug.LogWarning($"AB_RobotController: Restored joint {prevIndex} to color {baseColor}");
        }

        // Apply new
        if (newIndex >= 0 && newIndex < jointRenderers.Length && jointRenderers[newIndex])
        {
            SetRendererColor(jointRenderers[newIndex], highlightColor);
            Debug.LogWarning($"AB_RobotController: Highlighted joint {newIndex} with color {highlightColor}");
        }
        else if (newIndex >= 0)
        {
            Debug.LogWarning($"AB_RobotController: Cannot highlight joint {newIndex} - renderer not assigned or out of range.");
        }
    }

    private void UpdateJointInfoUI()
    {
        if (jointInfoText == null) return;

        string displayText = "";

        // Selected joint info (primary axis only)
        if (selectedJointIndex >= 0 && selectedJointIndex < joints.Length)
        {
            string jointName = (selectedJointIndex == 0) ? "BASE" : selectedJointIndex.ToString();
            float primaryAngle = targets[selectedJointIndex].x;
            displayText += $"<color=lime>Selected Joint: {jointName}\nPrimary (Q/E): {primaryAngle:F1}°</color>";
        }

        // Base spin angle (always displayed independently)
        if (joints.Length > 0 && joints[0] != null)
        {
            float baseSpinAngle = targets[0].y;
            if (displayText.Length > 0) displayText += "\n";
            displayText += $"<color=cyan>Base Spin (F/R): {baseSpinAngle:F1}°</color>";
        }

        jointInfoText.text = displayText;
    }
}
