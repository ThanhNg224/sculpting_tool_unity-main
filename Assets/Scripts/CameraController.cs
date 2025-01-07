using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Focus & Distances")]
    [Tooltip("Pivot or target that this camera orbits around.")]
    public Transform focusPoint;      // The point the camera orbits around
    public float minDistance = 2f;    // Minimum zoom distance
    public float maxDistance = 50f;   // Maximum zoom distance

    [Header("Speeds")]
    public float orbitSpeed = 10000f; // Speed of orbiting (degrees/sec)
    public float panSpeed = 0.1f;     // Speed of panning (world units per pixel)
    public float zoomSpeed = 10f;     // Speed of zoom (world units per scroll unit)

    private float currentDistance;     // Current distance from the focus point
    private Vector3 lastMousePosition; // Last recorded mouse position
    private Vector3 offset;           // Initial offset between the camera and the focus point

    private Camera cam;

    void Awake()
    {
        // Ensure we have a local camera reference
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No Camera component found, and no MainCamera in scene.");
            }
        }
    }

    void Start()
    {
        // If focusPoint was not assigned, create one so we never get a null ref
        if (focusPoint == null)
        {
            GameObject newFocus = new GameObject("FocusPoint");
            // Place focus a little in front of the camera as a fallback
            newFocus.transform.position = transform.position + transform.forward * 10f;
            focusPoint = newFocus.transform;

            Debug.LogWarning("No focusPoint assigned. Creating a new GameObject named 'FocusPoint'.");
        }

        // Initialize the camera's distance and offset from the focus point
        currentDistance = Vector3.Distance(transform.position, focusPoint.position);
        offset = transform.position - focusPoint.position;
    }

    void Update()
    {
        // Orbiting (Alt + Left Mouse Button)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            OrbitCamera();
        }
        // Panning (Middle Mouse Button)
        else if (Input.GetMouseButton(2))
        {
            PanCamera();
        }
        // Zooming (Mouse Scroll Wheel)
        else if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.0001f)
        {
            ZoomCamera();
        }

        // Store the mouse position for next frame
        lastMousePosition = Input.mousePosition;
    }

    // Handle Orbiting (Alt + Left Mouse Button)
    private void OrbitCamera()
    {
        // Get the mouse movement delta
        Vector3 delta = Input.mousePosition - lastMousePosition;

        // Convert mouse movement into rotation angles
        float horizontalRotation = (delta.x * orbitSpeed * Time.deltaTime) / Screen.width;
        float verticalRotation = (-delta.y * orbitSpeed * Time.deltaTime) / Screen.height;

        // Rotate the 'offset' vector around the focus point
        offset = Quaternion.AngleAxis(horizontalRotation, Vector3.up) * offset;
        offset = Quaternion.AngleAxis(verticalRotation, transform.right) * offset;

        // Update camera position
        transform.position = focusPoint.position + offset;
        transform.LookAt(focusPoint);
    }

    // Handle Panning (Middle Mouse Button)
    private void PanCamera()
    {
        Vector3 delta = Input.mousePosition - lastMousePosition;

        // Pan relative to current camera orientation
        Vector3 panMovement = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0f);
        transform.Translate(panMovement, Space.Self);

        // Optionally keep focusPoint in sync with the camera, so orbit doesn't "lose" it:
        focusPoint.Translate(panMovement, Space.Self);

        // Recompute offset (distance) after panning
        offset = transform.position - focusPoint.position;
    }

    // Handle Zooming (Mouse Scroll Wheel)
    private void ZoomCamera()
    {
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
        AdjustZoom(scrollAmount * zoomSpeed);
    }

    // Adjust the zoom distance based on input
    private void AdjustZoom(float deltaZoom)
    {
        print("thang");
        currentDistance = Mathf.Clamp(currentDistance + deltaZoom, minDistance, maxDistance);
        offset = offset.normalized * currentDistance;

        // Update position relative to the focus point
        transform.position = focusPoint.position + offset;
    }
}
