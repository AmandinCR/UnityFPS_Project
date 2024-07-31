using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public Canvas canvas;
    public Camera playerCam;

    private void Start()
    {
        Application.targetFrameRate = 144;
    }
}
