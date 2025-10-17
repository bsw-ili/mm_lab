using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class pickup_new : MonoBehaviour
{
    Vector3 cubeScreenPos;
    Vector3 offset;

    Vector3 start_position;
    Quaternion start_rotation;
    public int x, y, z;

    private GameObject runing;

    void Start()
    {
        start_position = transform.position;
        start_rotation = transform.rotation;
    }

    void OnMouseDown()
    {
        StartCoroutine(DragObject());
    }


    IEnumerator DragObject()
    {
        cubeScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cubeScreenPos.z);
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        offset = transform.position - mousePos;

        while (Input.GetMouseButton(0))
        {
            Vector3 curMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cubeScreenPos.z);
            curMousePos = Camera.main.ScreenToWorldPoint(curMousePos);

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(x, y, z), 0.99f);
            transform.position = curMousePos + offset;

            yield return new WaitForFixedUpdate();
        }

        if (Input.GetMouseButtonUp(0))
        {
            transform.position = start_position;
            transform.rotation = start_rotation;
        }
    }
}
