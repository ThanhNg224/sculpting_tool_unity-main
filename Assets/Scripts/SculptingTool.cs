using System.Collections.Generic;
using UnityEngine;

public class SculptingTool : MonoBehaviour
{
    [Header("Brush Settings")]
    [Tooltip("Radius of the sculpting effect in world units.")]
    public float sculptRadius = 1f;
    [Tooltip("Strength of the sculpting effect (vertex displacement).")]
    public float sculptStrength = 0.1f;

    [Header("Camera Reference")]
    [Tooltip("Optional camera. If empty, uses Camera.main.")]
    [SerializeField] private Camera sculptCamera;

    [Header("Audio")]
    [Tooltip("AudioSource used to play sculpting sounds.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sound when using Push mode.")]
    [SerializeField] private AudioClip pushSound;
    [Tooltip("Sound when using Pull mode.")]
    [SerializeField] private AudioClip pullSound;
    [Tooltip("Sound when using Grab mode.")]
    [SerializeField] private AudioClip grabSound;
    [Tooltip("Sound when using Pinch mode.")]
    [SerializeField] private AudioClip pinchSound;
    [Tooltip("Sound when using Smooth mode.")]
    [SerializeField] private AudioClip smoothSound;

    // Internal references
    private GameObject targetObject;  // Object being sculpted
    private Mesh mesh;                // Instantiated mesh
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private Vector3[] originalNormals;

    private enum SculptMode { Push, Pull, Grab, Pinch, Smooth }
    [SerializeField] private SculptMode currentMode = SculptMode.Push;

    private bool isSculpting = false;
    private bool isSculptModeActive = false;

    // For Undo functionality
    private Stack<Vector3[]> undoStack = new Stack<Vector3[]>();

    private void Awake()
    {
        // Fallback to Camera.main if user did not assign any camera
        if (!sculptCamera)
        {
            sculptCamera = Camera.main;
            if (!sculptCamera)
            {
                Debug.LogError("No camera assigned and no MainCamera found. SculptingTool raycasts won't work.");
            }
        }
    }

    private void Update()
    {
        if (isSculptModeActive)
        {
            HandleInput();
        }
    }

    /// <summary>
    /// Handles mouse input for starting, continuing, and stopping sculpt actions.
    /// </summary>
    private void HandleInput()
    {
        // Start sculpt on left mouse down
        if (Input.GetMouseButtonDown(0))
        {
            TryStartSculpt();
        }

        // Continue sculpting while left mouse is held
        if (isSculpting && Input.GetMouseButton(0))
        {
            ContinueSculpt();
        }

        // End sculpt on left mouse up
        if (Input.GetMouseButtonUp(0))
        {
            isSculpting = false;
            // Optional: update the collider after the stroke to improve performance
            UpdateCollider();
        }

        // Undo action (Ctrl + Z)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            UndoLastAction();
        }
    }

    /// <summary>
    /// Called on mouse down to attempt starting a sculpt (raycast hit).
    /// </summary>
    private void TryStartSculpt()
    {
        if (!sculptCamera) return;

        Ray ray = sculptCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            // If clicking a new target, re-initialize
            if (hitObject != targetObject)
            {
                InitializeNewTarget(hitObject);
            }

            if (mesh != null) // We have a valid mesh
            {
                SaveMeshState();
                isSculpting = true;

                // Play a one-shot sound depending on the current sculpt mode
                PlaySculptSound();

                // Perform an immediate sculpt at the hit point
                SculptAtPoint(hit.point);
            }
            else
            {
                Debug.LogWarning($"[SculptingTool] '{hitObject.name}' has no valid mesh. Cannot sculpt.");
            }
        }
    }

    /// <summary>
    /// Called every frame while the mouse is held down to continue sculpting.
    /// </summary>
    private void ContinueSculpt()
    {
        if (!sculptCamera) return;

        Ray ray = sculptCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SculptAtPoint(hit.point);
        }
    }

    /// <summary>
    /// Sets up references for a newly selected target object (MeshFilter + MeshCollider).
    /// </summary>
    private void InitializeNewTarget(GameObject newTarget)
    {
        targetObject = newTarget;

        MeshFilter mf = targetObject.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh)
        {
            // No valid mesh
            Debug.LogWarning($"[SculptingTool] {targetObject.name} has no MeshFilter or a null mesh.");
            ClearVertexData();
            return;
        }

        // Instantiate a unique mesh so we don't modify the original asset
        mesh = Instantiate(mf.sharedMesh);
        mesh.MarkDynamic();
        mesh.name = targetObject.name + "_SculptMesh";

        // Assign the mesh back to the MeshFilter
        mf.mesh = mesh;

        // Store original vertices and normals
        originalVertices = mesh.vertices;
        modifiedVertices = (Vector3[])originalVertices.Clone();
        originalNormals = mesh.normals;

        // Refresh collider to match new mesh instance
        UpdateCollider();

        Debug.Log($"[SculptingTool] Initialized new target: {targetObject.name}");
    }

    /// <summary>
    /// Clears the mesh references so we don't keep sculpting a null object.
    /// </summary>
    private void ClearVertexData()
    {
        mesh = null;
        originalVertices = null;
        modifiedVertices = null;
        originalNormals = null;
    }

    /// <summary>
    /// Sculpt operation at the given world-space point.
    /// </summary>
    private void SculptAtPoint(Vector3 hitPoint)
    {
        if (!targetObject || mesh == null || modifiedVertices == null)
        {
            Debug.LogWarning("[SculptingTool] SculptAtPoint called but no valid mesh/target!");
            return;
        }

        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            Vector3 worldVertex = targetObject.transform.TransformPoint(modifiedVertices[i]);
            float dist = Vector3.Distance(worldVertex, hitPoint);

            if (dist < sculptRadius)
            {
                float influence = (1f - dist / sculptRadius) * sculptStrength;

                switch (currentMode)
                {
                    case SculptMode.Push:
                        HandlePush(i, influence);
                        break;
                    case SculptMode.Pull:
                        HandlePull(i, influence);
                        break;
                    case SculptMode.Grab:
                        HandleGrab(i, hitPoint, influence);
                        break;
                    case SculptMode.Pinch:
                        HandlePinch(i, hitPoint, influence);
                        break;
                    case SculptMode.Smooth:
                        HandleSmooth(i, influence);
                        break;
                }
            }
        }

        UpdateMesh();
    }

    /// <summary>
    /// Normal-based push (moves vertex inward along its local normal).
    /// </summary>
    private void HandlePush(int index, float influence)
    {
        if (originalNormals == null || index >= originalNormals.Length) return;

        Vector3 worldNormal = targetObject.transform.TransformDirection(originalNormals[index]).normalized;
        modifiedVertices[index] -= worldNormal * influence;
    }

    /// <summary>
    /// Normal-based pull (moves vertex outward along its local normal).
    /// </summary>
    private void HandlePull(int index, float influence)
    {
        if (originalNormals == null || index >= originalNormals.Length) return;

        Vector3 worldNormal = targetObject.transform.TransformDirection(originalNormals[index]).normalized;
        modifiedVertices[index] += worldNormal * influence;
    }

    /// <summary>
    /// "Grab" moves the vertex toward the brush center point.
    /// </summary>
    private void HandleGrab(int index, Vector3 hitPoint, float influence)
    {
        Vector3 worldVertex = targetObject.transform.TransformPoint(modifiedVertices[index]);
        Vector3 dir = (hitPoint - worldVertex).normalized;
        modifiedVertices[index] += dir * influence;
    }

    /// <summary>
    /// "Pinch" pulls vertices toward the center of the brush (inward).
    /// </summary>
    private void HandlePinch(int index, Vector3 hitPoint, float influence)
    {
        Vector3 worldVertex = targetObject.transform.TransformPoint(modifiedVertices[index]);
        Vector3 dir = (hitPoint - worldVertex).normalized;
        modifiedVertices[index] -= dir * influence;
    }

    /// <summary>
    /// Smooths a vertex by averaging its position with neighbors in the sculpt radius.
    /// </summary>
    private void HandleSmooth(int index, float influence)
    {
        Vector3 currentPos = modifiedVertices[index];
        Vector3 sumPositions = Vector3.zero;
        int neighborCount = 0;

        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            if (i == index) continue;

            Vector3 worldPosI = targetObject.transform.TransformPoint(modifiedVertices[i]);
            Vector3 worldPosCurrent = targetObject.transform.TransformPoint(currentPos);
            float dist = Vector3.Distance(worldPosI, worldPosCurrent);

            if (dist < sculptRadius)
            {
                sumPositions += modifiedVertices[i];
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            Vector3 avg = sumPositions / neighborCount;
            modifiedVertices[index] = Vector3.Lerp(currentPos, avg, influence);
        }
    }

    /// <summary>
    /// Saves the current mesh state for undo.
    /// </summary>
    private void SaveMeshState()
    {
        if (modifiedVertices != null)
        {
            undoStack.Push((Vector3[])modifiedVertices.Clone());
        }
    }

    /// <summary>
    /// Undo the last sculpt action.
    /// </summary>
    private void UndoLastAction()
    {
        if (undoStack.Count > 0)
        {
            modifiedVertices = undoStack.Pop();
            UpdateMesh();
            Debug.Log("[SculptingTool] Undo: reverted last sculpt action.");
        }
        else
        {
            Debug.Log("[SculptingTool] No more undos available.");
        }
    }

    /// <summary>
    /// Updates the mesh with modified vertices, recalculates normals & bounds.
    /// </summary>
    private void UpdateMesh()
    {
        if (mesh == null || modifiedVertices == null)
        {
            Debug.LogWarning("[SculptingTool] UpdateMesh called but mesh or vertices are null.");
            return;
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Refreshes the MeshCollider to match the updated mesh.
    /// </summary>
    private void UpdateCollider()
    {
        if (!targetObject || !mesh) return;

        MeshCollider col = targetObject.GetComponent<MeshCollider>();
        if (col)
        {
            col.sharedMesh = null;
            col.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Plays a one-shot sound effect based on the current sculpt mode,
    /// with debug logs to confirm what is happening.
    /// </summary>
    private void PlaySculptSound()
    {
        // If no AudioSource is assigned, skip
        if (!audioSource)
        {
            Debug.LogWarning("[SculptingTool] AudioSource is missing! Cannot play sound.");
            return;
        }

        AudioClip clipToPlay = null;

        switch (currentMode)
        {
            case SculptMode.Push:
                clipToPlay = pushSound;
                break;
            case SculptMode.Pull:
                clipToPlay = pullSound;
                break;
            case SculptMode.Grab:
                clipToPlay = grabSound;
                break;
            case SculptMode.Pinch:
                clipToPlay = pinchSound;
                break;
            case SculptMode.Smooth:
                clipToPlay = smoothSound;
                break;
        }

        if (!clipToPlay)
        {
            Debug.LogWarning($"[SculptingTool] No clip assigned for {currentMode} mode!");
            return;
        }

        // Log which sound we are about to play
        Debug.Log($"[SculptingTool] Playing clip '{clipToPlay.name}' for {currentMode} mode.");
        audioSource.PlayOneShot(clipToPlay);
    }

    // --- Public methods for UI or other scripts ---

    public void SetSculptMode(string modeName)
    {
        try
        {
            currentMode = (SculptMode)System.Enum.Parse(typeof(SculptMode), modeName, true);
            Debug.Log($"[SculptingTool] Sculpt mode set to: {currentMode}");
        }
        catch
        {
            Debug.LogWarning($"[SculptingTool] Invalid sculpt mode: {modeName}");
        }
    }

    public void ToggleSculptMode(bool isActive)
    {
        isSculptModeActive = isActive;
        Debug.Log($"[SculptingTool] Sculpting mode {(isActive ? "enabled" : "disabled")}");
    }

    public void ToggleObjectMode(bool isActive)
    {
        if (isActive)
        {
            isSculptModeActive = false;
            Debug.Log("[SculptingTool] Sculpting mode disabled due to Object Mode activation");
        }
        Debug.Log($"[SculptingTool] Object mode {(isActive ? "enabled" : "disabled")}");
    }
}
