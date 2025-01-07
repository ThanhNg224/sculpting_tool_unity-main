using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform focusPoint;       // The point the camera orbits around
    public float orbitSpeed = 100f;    // Speed of orbiting (degrees per second)
    public float panSpeed = 0.1f;      // Speed of panning (world units per pixel)
    public float zoomSpeed = 10f;      // Speed of zooming (world units per scroll unit)
    public float minDistance = 2f;     // Minimum zoom distance
    public float maxDistance = 50f;    // Maximum zoom distance

    private float currentDistance;     // Current distance from the focus point
    private Vector3 lastMousePosition; // Last recorded mouse position

    void Start()
    {
        if (focusPoint == null)
        {
            Debug.LogError("Focus point is not assigned in the inspector.");
            return;
        }

        // Initialize the camera's distance from the focus point
        currentDistance = Vector3.Distance(transform.position, focusPoint.position);
    }

    void Update()
    {
        // Orbiting (Alt + Left Mouse Button)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)) // Alt + Left Mouse
        {
            OrbitCamera();
        }

        // Panning (Middle Mouse Button)
        else if (Input.GetMouseButton(2)) // Middle Mouse Button
        {
            PanCamera();
        }

        // Zooming (Mouse Scroll Wheel)
        else if (Input.GetAxis("Mouse ScrollWheel") != 0) // Mouse Scroll Wheel
        {
            ZoomCamera();
        }

        // Update the last mouse position for future frame calculations
        lastMousePosition = Input.mousePosition;
    }

    // Handle Orbiting (Alt + Left Mouse Button)
    private void OrbitCamera()
    {
        // Get the mouse movement delta
        Vector3 delta = Input.mousePosition - lastMousePosition;

        // Calculate the horizontal and vertical rotation based on the mouse movement
        float horizontalRotation = delta.x * orbitSpeed * Time.deltaTime;
        float verticalRotation = -delta.y * orbitSpeed * Time.deltaTime;

        // Rotate around the focus point dynamically
        Quaternion horizontalRotationQuat = Quaternion.AngleAxis(horizontalRotation, Vector3.up);
        Quaternion verticalRotationQuat = Quaternion.AngleAxis(verticalRotation, transform.right);

        // Calculate the new position
        Vector3 direction = (transform.position - focusPoint.position).normalized * currentDistance;
        direction = horizontalRotationQuat * direction;
        direction = verticalRotationQuat * direction;

        transform.position = focusPoint.position + direction;

        // Always look at the focus point
        transform.LookAt(focusPoint);
    }

    // Handle Panning (Middle Mouse Button)
    private void PanCamera()
    {
        Vector3 delta = Input.mousePosition - lastMousePosition;

        // Pan the camera based on the mouse movement
        Vector3 panMovement = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0);
        transform.Translate(panMovement, Space.Self);

        // Update the focus point to match the new camera position
        focusPoint.position += transform.TransformDirection(panMovement);
    }

    // Handle Zooming (Mouse Scroll Wheel)
    private void ZoomCamera()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        AdjustZoom(scroll * zoomSpeed);
    }

    // Adjust the zoom distance based on input
    private void AdjustZoom(float deltaZoom)
    {
        currentDistance = Mathf.Clamp(currentDistance - deltaZoom, minDistance, maxDistance);

        // Update camera position based on zoom
        Vector3 direction = (transform.position - focusPoint.position).normalized;
        transform.position = focusPoint.position + direction * currentDistance;
    }
}