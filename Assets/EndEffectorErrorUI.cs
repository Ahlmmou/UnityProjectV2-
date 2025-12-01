using UnityEngine;
using TMPro;

public class EndEffectorErrorUI : MonoBehaviour
{
    [Header("References")]
    public Transform endEffector;   // Drag gripper tip
    public Transform targetA;       // Drag TargetA
    public Transform targetB;       // Drag TargetB

    [Header("UI Elements")]
    public TMP_Text statusText;     // Top line: target message
    public TMP_Text resultText;     // Multi-line numerical output

    [Header("Keys")]
    public KeyCode targetAKey = KeyCode.C;
    public KeyCode targetBKey = KeyCode.V;
    public KeyCode measureKey = KeyCode.E;

    private Transform activeTarget;

    void Start()
    {
        // Validate assignments (helps debugging)
        if (statusText == null)
            Debug.LogError("[EndEffectorErrorUI] STATUS TEXT IS NOT ASSIGNED!");
        if (resultText == null)
            Debug.LogError("[EndEffectorErrorUI] RESULT TEXT IS NOT ASSIGNED!");
        if (endEffector == null)
            Debug.LogError("[EndEffectorErrorUI] END EFFECTOR NOT ASSIGNED!");
        if (targetA == null)
            Debug.LogError("[EndEffectorErrorUI] TARGET A NOT ASSIGNED!");
        if (targetB == null)
            Debug.LogError("[EndEffectorErrorUI] TARGET B NOT ASSIGNED!");

        // Pick default target
        activeTarget = targetA;

        ForceEnableUI();

        UpdateStatus("Target A Selected");
        resultText.text = "Awaiting movement…";
    }

    void Update()
    {
        // Switch target A
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(targetAKey))
        {
            Debug.Log("C pressed ? Target A selected");
            SetActiveTarget(targetA, "Target A Selected");
        }

        // Switch target B
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(targetBKey))
        {
            Debug.Log("V pressed ? Target B selected");
            SetActiveTarget(targetB, "Target B Selected");
        }

        // Measure error
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(measureKey))
        {
            Debug.Log("E pressed ? Calculating error");
            UpdateStatus("Movement Logged. Calculating Error…");
            ComputeAndDisplayError();
        }
    }

    void SetActiveTarget(Transform target, string msg)
    {
        activeTarget = target;
        UpdateStatus(msg);
        resultText.text = "Awaiting movement…";
    }

    void UpdateStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = msg;
        }
        else
        {
            Debug.LogWarning("[EndEffectorErrorUI] Tried to update STATUS TEXT but it is NULL.");
        }
    }

    void ForceEnableUI()
    {
        // In case the UI is disabled anywhere
        if (statusText != null)
            statusText.gameObject.SetActive(true);

        if (resultText != null)
            resultText.gameObject.SetActive(true);
    }

    void ComputeAndDisplayError()
    {
        if (endEffector == null || activeTarget == null)
        {
            Debug.LogError("[EndEffectorErrorUI] Cannot compute error, missing references.");
            return;
        }

        if (resultText == null)
        {
            Debug.LogError("[EndEffectorErrorUI] RESULT TEXT NOT ASSIGNED!");
            return;
        }

        Vector3 eePos = endEffector.position;
        Vector3 tgtPos = activeTarget.position;

        Vector3 error = tgtPos - eePos;
        float magnitude = error.magnitude;

        Vector3 axisAbs = new Vector3(
            Mathf.Abs(error.x),
            Mathf.Abs(error.y),
            Mathf.Abs(error.z)
        );

        // Make sure result text is visible
        resultText.gameObject.SetActive(true);

        resultText.text =
    $"<b>{activeTarget.name} ERROR RESULTS</b>\n\n" +
    $"<b>EE POSITION:</b>\n" +
    $"({eePos.x:F3}, {eePos.y:F3}, {eePos.z:F3})\n\n" +
    $"<b>TARGET POSITION:</b>\n" +
    $"({tgtPos.x:F3}, {tgtPos.y:F3}, {tgtPos.z:F3})\n\n" +
    $"<b>? VECTOR:</b>\n" +
    $"({error.x:F3}, {error.y:F3}, {error.z:F3}) m\n\n" +
    $"<b>DISTANCE |?|:</b>  {magnitude:F3} m\n\n" +
    $"<b>AXIS ERRORS:</b>\n" +
    $"X = {axisAbs.x:F3}\n" +
    $"Y = {axisAbs.y:F3}\n" +
    $"Z = {axisAbs.z:F3}";
    }
}
