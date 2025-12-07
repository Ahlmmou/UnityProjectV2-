using UnityEngine;
using System.IO;
using System.Text;

public class ManualTrackingLogger : MonoBehaviour
{
    [Header("References")]
    public Transform target;               // Moving sphere (zigzag)
    public Transform endEffector;          // Robot EE transform
    public ArticulationBody[] joints;      // All robot joints in order

    [Header("Logging Settings")]
    public string fileName = "manual_tracking_log.csv";
    public float logInterval = 0.02f;      // 50 Hz

    private bool isLogging = false;
    private float startTime = 0f;
    private float timer = 0f;

    private string fullPath;

    void Start()
    {
        if (target == null || endEffector == null || joints == null || joints.Length == 0)
        {
            Debug.LogError("ManualTrackingLogger: Missing references!");
            enabled = false;
            return;
        }

        // Logs folder at:
        // C:\Users\HP\My project\Assets\Logs
        string dir = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(dir);

        fullPath = Path.Combine(dir, fileName);

        // Write header line
        using (StreamWriter writer = new StreamWriter(fullPath, false, Encoding.UTF8))
        {
            // time + sphere xyz + EE xyz + all joint angles
            string header = "time,targetX,targetY,targetZ,eeX,eeY,eeZ";

            for (int i = 0; i < joints.Length; i++)
                header += $",q{i}";

            writer.WriteLine(header);
        }

        Debug.Log("ManualTrackingLogger: Logging to " + fullPath);
    }

    void Update()
    {
        // Start logging on S
        if (!isLogging && Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.S))
        {
            isLogging = true;
            startTime = Time.time;
            timer = 0f;
            Debug.Log("ManualTrackingLogger: Logging STARTED");
        }

        // Stop logging on K (optional)
        if (isLogging && Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.K))
        {
            isLogging = false;
            Debug.Log("ManualTrackingLogger: Logging STOPPED");
        }

        if (!isLogging)
            return;

        timer += Time.deltaTime;
        if (timer < logInterval)
            return;

        timer = 0f;
        WriteOneLine();
    }

    void WriteOneLine()
    {
        float t = Time.time - startTime;

        Vector3 tgt = target.position;
        Vector3 ee = endEffector.position;

        using (StreamWriter writer = new StreamWriter(fullPath, true, Encoding.UTF8))
        {
            // time + positions
            string line =
                $"{t:F4},{tgt.x:F4},{tgt.y:F4},{tgt.z:F4},{ee.x:F4},{ee.y:F4},{ee.z:F4}";

            // joint angles from ArticulationBody.xDrive.target
            for (int i = 0; i < joints.Length; i++)
            {
                var j = joints[i];
                float q = j.xDrive.target;
                line += $",{q:F4}";
            }

            writer.WriteLine(line);
        }
    }
}
