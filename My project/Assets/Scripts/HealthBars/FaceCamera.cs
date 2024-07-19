using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [HideInInspector] public Camera cam;

    private void Update()
    {
        if (cam != null)
            transform.LookAt(cam.transform, Vector3.up);
    }
}
