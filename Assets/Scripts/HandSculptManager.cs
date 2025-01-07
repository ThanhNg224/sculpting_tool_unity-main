using UnityEngine;

public class HandSculptManager : MonoBehaviour
{
    [Header("Hand Tracking")]
    public Transform handTransform;  // e.g., your "Point(8)"

    [Header("Sculptor Reference")]
    public MeshSculptor sculptor;    // reference your subdivided cube

    [Header("Sculpting Toggle")]
    public bool isSculpting = false;

    private void Update()
    {
        if (!isSculpting) return;
        if (handTransform == null || sculptor == null) return;

        // Grab the hand's position from your existing tracking
        Vector3 handPos = handTransform.position;

        // Now call the sculptor's function
        sculptor.SculptAtPoint(handPos);
    }

    // UI button hooks
    public void StartSculpting()
    {
        isSculpting = true;
        Debug.Log("Hand Sculpting Started!");
    }

    public void StopSculpting()
    {
        isSculpting = false;
        Debug.Log("Hand Sculpting Stopped!");
    }

    // Additional UI to switch brush modes
    public void SetPushMode()
    {
        if (sculptor != null)
        {
            sculptor.brushMode = MeshSculptor.BrushMode.Push;
            Debug.Log("Brush Mode: PUSH");
        }
    }

    public void SetPullMode()
    {
        if (sculptor != null)
        {
            sculptor.brushMode = MeshSculptor.BrushMode.Pull;
            Debug.Log("Brush Mode: PULL");
        }
    }

    public void SetSmoothMode()
    {
        if (sculptor != null)
        {
            sculptor.brushMode = MeshSculptor.BrushMode.Smooth;
            Debug.Log("Brush Mode: SMOOTH");
        }
    }
}
