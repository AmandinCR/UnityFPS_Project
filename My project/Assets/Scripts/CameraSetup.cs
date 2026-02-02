using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI timerText;
    public Canvas canvas;
    public Camera playerCam;
    public TextMeshProUGUI missionText;
    public TextMeshProUGUI keyText;

    private void Start()
    {
        Application.targetFrameRate = 144;
    }
}
