using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    float panSpeed = 20f;
    float scrollSpeed = 20f;
    float panBorderThickness = 10f;
    float minZoom = 15f;
    float maxZoom = 60f;
    public bool mouseMovement = false;

    private void Start()
    {
#if UNITY_WEBGL
        mouseMovement = true;
#endif
    }

    // Update is called once per frame 
    void Update()
    {
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

        position.y = Mathf.Clamp(position.y, minZoom, maxZoom);

        transform.position = position;
    }
}
