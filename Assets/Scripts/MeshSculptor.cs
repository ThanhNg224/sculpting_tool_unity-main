using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshSculptor : MonoBehaviour
{
    public enum BrushMode
    {
        Push,
        Pull,
        Smooth
    }

    [Header("Sculpt Settings")]
    [Tooltip("Which mode the brush operates in.")]
    public BrushMode brushMode = BrushMode.Push;

    [Tooltip("Radius around the hand point that gets affected.")]
    public float sculptRadius = 0.25f;

    [Tooltip("How strong each frame's sculpt operation is.")]
    public float sculptStrength = 1.0f;

    [Tooltip("Optional random noise factor to make sculpts look more organic.")]
    public float noiseAmplitude = 0.0f; // 0 = off, try 0.01 or 0.02 for subtle variation

    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] baseVertices;     // original mesh vertices
    private Vector3[] workingVertices;  // the modifiable array we tweak each frame

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;

        // Copy the original vertices so we can deform them without losing them
        baseVertices = mesh.vertices;
        workingVertices = mesh.vertices;
    }

    /// <summary>
    /// Main entry point to deform the mesh around the given world-space point.
    /// </summary>
    public void SculptAtPoint(Vector3 worldPoint)
    {
        // Convert world space point -> local space
        Vector3 localSculptPoint = transform.InverseTransformPoint(worldPoint);

        switch (brushMode)
        {
            case BrushMode.Push:
            case BrushMode.Pull:
                DoPushPull(localSculptPoint);
                break;
            case BrushMode.Smooth:
                DoSmooth(localSculptPoint);
                break;
        }

        // Update the mesh
        mesh.vertices = workingVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Reset the mesh to its original shape if you want a "clear" or "reset" button.
    /// </summary>
    public void ResetMesh()
    {
        workingVertices = (Vector3[])baseVertices.Clone();
        mesh.vertices = workingVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Push or Pull vertices near the sculpt point.
    /// </summary>
    private void DoPushPull(Vector3 localSculptPoint)
    {
        // If brushMode == Pull, invert direction
        float directionFactor = (brushMode == BrushMode.Pull) ? -1f : 1f;

        for (int i = 0; i < workingVertices.Length; i++)
        {
            Vector3 vertPos = workingVertices[i];
            float dist = Vector3.Distance(localSculptPoint, vertPos);
            if (dist < sculptRadius)
            {
                float falloff = 1.0f - (dist / sculptRadius);
                // A quick way to give a sharper or softer falloff:
                // e.g. power of 2 -> sharper
                falloff = Mathf.Pow(falloff, 2.0f);

                // Direction from sculpt point to this vertex
                Vector3 dir = (vertPos - localSculptPoint).normalized * directionFactor;

                // Optional: add a bit of noise for more organic look
                if (noiseAmplitude > 0f)
                {
                    float nx = (Mathf.PerlinNoise(Time.time, i * 0.1f) - 0.5f) * 2f;
                    float ny = (Mathf.PerlinNoise(i * 0.1f, Time.time) - 0.5f) * 2f;
                    float nz = (Mathf.PerlinNoise(i * 0.1f, i * 0.1f) - 0.5f) * 2f;
                    dir += new Vector3(nx, ny, nz) * noiseAmplitude;
                }

                // Apply the deformation
                vertPos += dir * sculptStrength * falloff;
                workingVertices[i] = vertPos;
            }
        }
    }

    /// <summary>
    /// Smooth the mesh near the sculpt point by averaging vertex positions
    /// with their neighbors. (Simplistic approach)
    /// </summary>
    private void DoSmooth(Vector3 localSculptPoint)
    {
        // For basic smoothing, we can average vertices around the target area
        // A more advanced method would find adjacency, but let's do a naive approach.
        // We'll store changes in a temp array so we don't overwrite as we iterate.
        Vector3[] tempVerts = (Vector3[])workingVertices.Clone();

        for (int i = 0; i < workingVertices.Length; i++)
        {
            float dist = Vector3.Distance(localSculptPoint, workingVertices[i]);
            if (dist < sculptRadius)
            {
                // find neighbors in a small radius
                Vector3 average = Vector3.zero;
                int count = 0;

                // We'll do a quick local neighborhood check - naive approach
                // This is not the most efficient for big meshes, but fine for demos.
                for (int j = 0; j < workingVertices.Length; j++)
                {
                    float distToNeighbor = Vector3.Distance(workingVertices[i], workingVertices[j]);
                    if (distToNeighbor < (sculptRadius * 0.3f)) // smaller neighborhood
                    {
                        average += workingVertices[j];
                        count++;
                    }
                }

                if (count > 0)
                {
                    average /= count;
                    // Lerp the vertex position toward the average for smoothing
                    float falloff = 1.0f - (dist / sculptRadius);
                    falloff = Mathf.Pow(falloff, 2.0f);

                    // Move partly toward average to avoid over-smoothing in one frame
                    tempVerts[i] = Vector3.Lerp(workingVertices[i], average, sculptStrength * 0.01f * falloff);
                }
            }
        }

        // Copy changes back
        tempVerts.CopyTo(workingVertices, 0);
    }
}
