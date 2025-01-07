using UnityEngine;
using UnityEngine.UI; // For UI elements

public class ObjectSelector : MonoBehaviour
{
    public GameObject axisPrefab;      // Prefab for move axes
    public GameObject gizmoPrefab;     // Prefab for scale gizmo
    public GameObject rotationUI;      // UI panel for rotation inputs
    public InputField xInputField;     // Input field for X rotation
    public InputField yInputField;     // Input field for Y rotation
    public InputField zInputField;     // Input field for Z rotation

    public Color selectionColor = Color.yellow; // Color for selected outline

    private GameObject axisInstance;   // Instance of move axes
    private GameObject gizmoInstance;  // Instance of scale gizmo
    private GameObject selectedObject; // Currently selected object

    private bool isMoveMode = false;   // Whether in Move Mode
    private bool isScaleMode = false;  // Whether in Scale Mode
    private bool isRotateMode = false; // Whether in Rotate Mode

    private Vector3 dragAxis;          // Axis being dragged (Move/Scale mode)
    private Plane manipulationPlane;   // Plane used for dragging calculations
    private Vector3 initialMousePosition; // Initial mouse position during drag
    private bool isDragging = false;   // Whether an object is being dragged

    private Plane movementPlane;       // Plane for movement calculations
    private Vector3 offsetToMouse;     // Offset between object and mouse position on the plane

    void Update()
    {
        HandleSelection();
        HandleModeSwitch();
        HandleDrag();
        UpdateAxisPosition();
        HandleDelete();
    }

    // Handles selecting or deselecting an object with the left mouse button
    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0) && !isMoveMode && !isScaleMode && !isRotateMode)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject clickedObject = hit.collider.gameObject;

                if (selectedObject == clickedObject)
                {
                    return; // Do nothing if the same object is clicked
                }

                if (selectedObject != null)
                {
                    DeselectObject();
                }

                selectedObject = clickedObject;
                EnableOutline(selectedObject);

                Debug.Log($"Selected: {selectedObject.name}");
            }
            else if (selectedObject != null)
            {
                DeselectObject();
            }
        }
    }

    private void EnableOutline(GameObject obj)
    {
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }

        outline.OutlineColor = selectionColor;
        outline.OutlineWidth = 5f;
    }

    private void DeselectObject()
    {
        if (selectedObject != null)
        {
            Outline outline = selectedObject.GetComponent<Outline>();
            if (outline != null)
            {
                Destroy(outline);
            }
            selectedObject = null;
        }
    }

    // Handles toggling between Select, Move, Rotate, and Scale modes
    private void HandleModeSwitch()
    {
        if (selectedObject != null)
        {
            if (Input.GetKeyDown(KeyCode.W)) // Move Mode
            {
                EnterMoveMode();
            }
            else if (Input.GetKeyDown(KeyCode.R)) // Scale Mode
            {
                EnterScaleMode();
            }
            else if (Input.GetKeyDown(KeyCode.E)) // Rotate Mode
            {
                EnterRotateMode();
            }
            else if (Input.GetKeyDown(KeyCode.Q)) // Select Mode
            {
                ExitAllModes();
            }
        }
    }

    private void EnterMoveMode()
    {
        isMoveMode = true;
        isScaleMode = false;
        isRotateMode = false;

        if (axisInstance == null)
        {
            axisInstance = Instantiate(axisPrefab, selectedObject.transform.position, Quaternion.identity);
        }

        Destroy(gizmoInstance);
        rotationUI.SetActive(false);
        Debug.Log("Switched to Move Mode");
    }

    private void EnterScaleMode()
    {
        isScaleMode = true;
        isMoveMode = false;
        isRotateMode = false;

        if (gizmoInstance == null)
        {
            gizmoInstance = Instantiate(gizmoPrefab, selectedObject.transform.position, Quaternion.identity);
        }

        Destroy(axisInstance);
        rotationUI.SetActive(false);
        Debug.Log("Switched to Scale Mode");
    }

    private void EnterRotateMode()
    {
        isRotateMode = true;
        isMoveMode = false;
        isScaleMode = false;

        Destroy(axisInstance);
        Destroy(gizmoInstance);

        rotationUI.SetActive(true);

        Vector3 currentRotation = selectedObject.transform.eulerAngles;
        xInputField.text = currentRotation.x.ToString("F1");
        yInputField.text = currentRotation.y.ToString("F1");
        zInputField.text = currentRotation.z.ToString("F1");

        Debug.Log("Switched to Rotate Mode");
    }

    private void ExitAllModes()
    {
        isMoveMode = false;
        isScaleMode = false;
        isRotateMode = false;

        Destroy(axisInstance);
        Destroy(gizmoInstance);
        rotationUI.SetActive(false);

        Debug.Log("Switched to Select Mode");
    }

    // Handles dragging in Move and Scale modes
    private void HandleDrag()
    {
        if ((isMoveMode || isScaleMode) && selectedObject != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.CompareTag("X_Axis"))
                    {
                        dragAxis = Vector3.right;
                        manipulationPlane = new Plane(Vector3.up, selectedObject.transform.position);
                    }
                    else if (hit.collider.CompareTag("Y_Axis"))
                    {
                        dragAxis = Vector3.up;
                        manipulationPlane = new Plane(Vector3.right, selectedObject.transform.position);
                    }
                    else if (hit.collider.CompareTag("Z_Axis"))
                    {
                        dragAxis = Vector3.forward;
                        manipulationPlane = new Plane(Vector3.up, selectedObject.transform.position);
                    }

                    if (dragAxis != Vector3.zero)
                    {
                        Ray dragRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (manipulationPlane.Raycast(dragRay, out float enter))
                        {
                            initialMousePosition = dragRay.GetPoint(enter);
                            isDragging = true;
                        }
                    }
                }
            }

            if (Input.GetMouseButton(0) && isDragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (manipulationPlane.Raycast(ray, out float enter))
                {
                    Vector3 currentMousePosition = ray.GetPoint(enter);
                    Vector3 movementDelta = currentMousePosition - initialMousePosition;
                    float dragAmount = Vector3.Dot(movementDelta, dragAxis);

                    if (isMoveMode)
                    {
                        selectedObject.transform.position += dragAxis * dragAmount;
                    }
                    else if (isScaleMode)
                    {
                        Vector3 scaleChange = dragAxis * dragAmount;
                        selectedObject.transform.localScale += scaleChange;
                    }

                    initialMousePosition = currentMousePosition;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                dragAxis = Vector3.zero;
            }
        }
    }

    // Updates the axis or gizmo position to keep it visible
    private void UpdateAxisPosition()
    {
        if (axisInstance != null && selectedObject != null)
        {
            axisInstance.transform.position = selectedObject.transform.position;
        }

        if (gizmoInstance != null && selectedObject != null)
        {
            gizmoInstance.transform.position = selectedObject.transform.position;
        }
    }

    public void ApplyRotation()
    {
        if (selectedObject != null && isRotateMode)
        {
            float x = float.Parse(xInputField.text);
            float y = float.Parse(yInputField.text);
            float z = float.Parse(zInputField.text);

            selectedObject.transform.eulerAngles = new Vector3(x, y, z);

            Debug.Log($"Applied Rotation: X={x}, Y={y}, Z={z}");
        }
    }

    // Handles deleting the selected object
    private void HandleDelete()
    {
        if (selectedObject != null && Input.GetKeyDown(KeyCode.Delete))
        {
            Destroy(selectedObject);
            selectedObject = null;
            Debug.Log("Object deleted");
        }
    }
}
