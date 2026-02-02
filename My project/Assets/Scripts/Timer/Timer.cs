using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class Timer : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    private TextMeshProUGUI timerText;
    private float currentTime;
    private GameObject cam;

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera");
        timerText = cam.GetComponent<CameraSetup>().timerText;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                RanOutOfTime();  // clients should NOT run this function...
                EndTimer();
            }  
            timerText.text = currentTime.ToString("0");
        }
    }

    public void StartTimer(float startTime)
    {
        currentTime = startTime;
    }

    [ServerCallback]
    private void RanOutOfTime()
    {
        missionManager.EndMission(false);
    }

    public void EndTimer()
    {
        currentTime = 0;
        timerText.text = "";
    }
}
