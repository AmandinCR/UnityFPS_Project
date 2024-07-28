using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    public Transform vfxStart;
    public TextMeshProUGUI healthText;
    public Canvas canvas;
    public Camera playerCam;
    public Camera uiCam;

    private void Update()
    {
        uiCam.fieldOfView = playerCam.fieldOfView;
    }
}
