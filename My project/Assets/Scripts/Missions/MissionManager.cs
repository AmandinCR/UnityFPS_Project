using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class MissionManager : NetworkBehaviour
{
    [SerializeField] private float missionTime = 20f;
    [SerializeField] private Timer timer;
    [SerializeField] private float resultDisplayTime = 5f;
    [HideInInspector] public bool missionStarted = false;
    [HideInInspector] public bool missionSuccess = false;
    private TextMeshProUGUI missionText;
    private GameObject cam;

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera");
        missionText = cam.GetComponent<CameraSetup>().missionText;
    }


    [ServerCallback]
    public void StartMission()
    {
        missionSuccess = false;
        missionStarted = true;

        RpcStartMission();
    }

    [ClientRpc]
    private void RpcStartMission()
    {
        // is this not starting the timer on the server ???
        timer.StartTimer(missionTime);
    }

    [ServerCallback]
    public void EndMission(bool success)
    {
        missionStarted = false;
        missionSuccess = success;

        RpcEndMission(success);
    }

    [ClientRpc]
    private void RpcEndMission(bool success)
    {
        timer.EndTimer();
        
        StartCoroutine(MissionDisplayResult(success));
    }

    private IEnumerator MissionDisplayResult(bool success)
    {
        if (success)
        {
            missionText.text = "Mission Success!";
        } else
        {
            missionText.text = "Mission Failed";
        }

        yield return new WaitForSeconds(resultDisplayTime);

        missionText.text = "";
    }
}
