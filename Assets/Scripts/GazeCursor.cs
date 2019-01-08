using UnityEngine;

public class GazeCursor : MonoBehaviour
{
    //Private Variables
    public GameObject FocusedObject = null; // The object which user is staring at
    //Cached variables
    private Renderer cursorMeshRenderer; // Using this to disable cursor
    private RaycastHit hitInfo;
    private Vector3 gazeOrigin;
    private Vector3 gazeDirection;
    private Camera mainCamera;
    public static GazeCursor Instance { get; private set; }

    void Start ()
    {
        cursorMeshRenderer = gameObject.GetComponent<Renderer>();
        mainCamera = Camera.main;
        Instance = this;
    }

    void Update()
    {
        gazeOrigin = mainCamera.transform.position;
        gazeDirection = mainCamera.transform.forward;
        if (Physics.Raycast(gazeOrigin, gazeDirection, out hitInfo))
        {
            cursorMeshRenderer.enabled = true;
            gameObject.transform.position = hitInfo.point;
            gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            //Find Target
            if (hitInfo.collider.gameObject.CompareTag("Target"))
                FocusedObject = hitInfo.collider.gameObject;
            else
                FocusedObject = null;
        }
        else
        {
            cursorMeshRenderer.enabled = false;
            FocusedObject = null;
        }
    }

    public GameObject getFocusedObject()
    {
        return FocusedObject;
    }

}
