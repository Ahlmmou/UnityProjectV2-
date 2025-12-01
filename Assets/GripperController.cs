using UnityEngine;

public class GripperController : MonoBehaviour
{
    [Header("Setup")]
    public Transform graspPoint; // empty at tool tip
    public float graspRadius = 0.06f;
    public LayerMask grabbableMask;

    [Header("Tuning")]
    public float releaseThrowMultiplier = 1.0f;

    [Header("State (read-only)")]
    public Rigidbody heldBody;

    Vector3 lastPos;
    Vector3 eeVelocity;

    void Awake()
    {
        if (!graspPoint) graspPoint = this.transform;
        lastPos = graspPoint.position;
    }

    void Update()
    {
        eeVelocity = (graspPoint.position - lastPos) / Mathf.Max(Time.deltaTime, 1e-6f);
        lastPos = graspPoint.position;

        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Space))
        {
            if (heldBody) Release();
            else TryGrasp();
        }
    }

    public bool TryGrasp()
    {
        if (heldBody) return false;

        Collider[] hits = Physics.OverlapSphere(graspPoint.position, graspRadius, grabbableMask, QueryTriggerInteraction.Ignore);
        Rigidbody best = null;
        float bestDist = float.MaxValue;
        foreach (var c in hits)
        {
            var rb = c.attachedRigidbody;
            if (rb == null) continue;
            float d = Vector3.Distance(rb.worldCenterOfMass, graspPoint.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = rb;
            }
        }
        if (best)
        {
            Attach(best);
            return true;
        }
        return false;
    }

    void Attach(Rigidbody rb)
    {
        heldBody = rb;
        heldBody.isKinematic = true;
        heldBody.transform.SetParent(graspPoint, worldPositionStays: true);
        heldBody.transform.position = graspPoint.position;
    }

    public void Release()
    {
        if (!heldBody) return;
        heldBody.transform.SetParent(null, worldPositionStays: true);
        heldBody.isKinematic = false;
        heldBody.velocity = eeVelocity * releaseThrowMultiplier;
        heldBody = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!graspPoint) graspPoint = this.transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(graspPoint.position, graspRadius);
    }
}
