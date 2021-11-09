using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    float panSpeed = 20f;
    float scrollSpeed = 20f;
    float panBorderThickness = 10f;
    float minZoom = 10f;
    float maxZoom = 300f;
    public bool mouseMovement = false;
    public Terrain terrain;
    Vector3 cameraInsets = new Vector3(10, 0, 10);

    // Update is called once per frame 
    void Update()
    {
        if (mouseMovement && Input.GetKey(KeyCode.Escape)) mouseMovement = false;
        if (!mouseMovement && Input.GetMouseButton(0)) mouseMovement = true;

        Vector3 position = transform.position; 
        if (Input.GetKey(KeyCode.UpArrow) || (mouseMovement && Input.mousePosition.y >= Screen.height - panBorderThickness))
            position.z += panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow) || (mouseMovement && Input.mousePosition.y <= panBorderThickness))
            position.z -= panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow) || (mouseMovement && Input.mousePosition.x >= Screen.width - panBorderThickness))
            position.x += panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow) || (mouseMovement && Input.mousePosition.x <= panBorderThickness))
            position.x -= panSpeed * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        position.y -= scroll * 25 * scrollSpeed * Time.deltaTime;

        position.x = Mathf.Clamp(position.x, cameraInsets.x, terrain.terrainData.size.x - cameraInsets.x);
        position.z = Mathf.Clamp(position.z, cameraInsets.z, terrain.terrainData.size.z - cameraInsets.z);
        position.y = Mathf.Clamp(position.y, minZoom, maxZoom);

        transform.position = position;
    }
}
