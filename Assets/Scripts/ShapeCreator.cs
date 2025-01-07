using System.Collections.Generic;
using UnityEngine;

public class ShapeCreator : MonoBehaviour
{
    // Existing Shape Prefabs
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject cylinderPrefab;
    public GameObject capsulePrefab;
    public GameObject planePrefab;

    // New Hand Prefab
    public GameObject handPrefab;

    // Stack to track created shapes
    private Stack<GameObject> createdShapes = new Stack<GameObject>();

    // Reference to Sculpting Tool (Assuming it's another script/component)
    public SculptingTool sculptingTool;

    // Method to Create Cube
    public void CreateCube()
    {
        Debug.Log("Creating Cube");
        GameObject newCube = Instantiate(cubePrefab, new Vector3(10, 1, 10), Quaternion.identity);
        if (newCube != null)
        {
            EnsureMeshCollider(newCube);
            createdShapes.Push(newCube); // Add to undo stack
            Debug.Log("Cube created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create Cube.");
        }
    }

    // Method to Create Sphere
    public void CreateSphere()
    {
        Debug.Log("Creating Sphere");
        GameObject newSphere = Instantiate(spherePrefab, new Vector3(0, 1, 0), Quaternion.identity);
        if (newSphere != null)
        {
            EnsureMeshCollider(newSphere);
            createdShapes.Push(newSphere); // Add to undo stack
            Debug.Log("Sphere created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create Sphere.");
        }
    }

    // Method to Create Cylinder
    public void CreateCylinder()
    {
        Debug.Log("Creating Cylinder");
        GameObject newCylinder = Instantiate(cylinderPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        if (newCylinder != null)
        {
            EnsureMeshCollider(newCylinder);
            createdShapes.Push(newCylinder); // Add to undo stack
            Debug.Log("Cylinder created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create Cylinder.");
        }
    }

    // Method to Create Capsule
    public void CreateCapsule()
    {
        Debug.Log("Creating Capsule");
        GameObject newCapsule = Instantiate(capsulePrefab, new Vector3(0, 1, 0), Quaternion.identity);
        if (newCapsule != null)
        {
            EnsureMeshCollider(newCapsule);
            createdShapes.Push(newCapsule); // Add to undo stack
            Debug.Log("Capsule created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create Capsule.");
        }
    }

    // Method to Create Plane
    public void CreatePlane()
    {
        Debug.Log("Creating Plane");
        GameObject newPlane = Instantiate(planePrefab, new Vector3(0, 1, 0), Quaternion.identity);
        if (newPlane != null)
        {
            EnsureMeshCollider(newPlane);
            createdShapes.Push(newPlane); // Add to undo stack
            Debug.Log("Plane created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create Plane.");
        }
    }

    // **New Method to Create Hand with Enhanced Debugging**
    public void CreateHand()
    {
        if (handPrefab != null)
        {
            if (!handPrefab.activeInHierarchy)
            {
                handPrefab.SetActive(true);
                //Debug.Log("Hand prefab activated.");
            }
            else
            {
                handPrefab.SetActive(false);
                //Debug.Log("Hand prefab is already active.");
            }
        }
        else
        {
            Debug.LogError("Hand prefab reference is not assigned.");
        }
    }

    // Ensures the GameObject has a MeshCollider
    private void EnsureMeshCollider(GameObject obj)
    {
        // Check if a MeshCollider is already present; if not, add one.
        if (obj.GetComponent<MeshCollider>() == null)
        {
            obj.AddComponent<MeshCollider>();
            Debug.Log($"MeshCollider added to {obj.name}.");
        }
        else
        {
            Debug.Log($"{obj.name} already has a MeshCollider.");
        }
    }

    // Method to Toggle Sculpting Mode
    public void ToggleSculptingMode(bool isEnabled)
    {
        if (sculptingTool != null)
        {
            sculptingTool.enabled = isEnabled;
            Debug.Log($"Sculpting Mode {(isEnabled ? "Enabled" : "Disabled")}.");
        }
        else
        {
            Debug.LogError("SculptingTool reference is not assigned.");
        }
    }

    // Optional: Method to Undo the Last Created Shape
    public void UndoLastShape()
    {
        if (createdShapes.Count > 0)
        {
            GameObject lastShape = createdShapes.Pop();
            if (lastShape != null)
            {
                Destroy(lastShape);
                Debug.Log($"{lastShape.name} has been destroyed (Undo Last Shape).");
            }
            else
            {
                Debug.LogError("Last shape in stack is null.");
            }
        }
        else
        {
            Debug.Log("No shapes to undo.");
        }
    }
}
