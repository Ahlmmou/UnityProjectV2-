using UnityEngine;
using TMPro;

public class EndEffectorCollisionStatus : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text collisionText;

    private void Start()
    {
        if (collisionText != null)
        {
            collisionText.text = "No collision detected";
        }
        else
        {
            Debug.LogWarning($"[{name}] CollisionText is not assigned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{name}] Trigger ENTER with {other.name}, tag={other.tag}");

        if (other.CompareTag("Hazard"))
        {
            if (collisionText != null)
                collisionText.text = "Collision detected";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[{name}] Trigger EXIT with {other.name}, tag={other.tag}");

        if (other.CompareTag("Hazard"))
        {
            if (collisionText != null)
                collisionText.text = "No collision detected";
        }
    }
}
