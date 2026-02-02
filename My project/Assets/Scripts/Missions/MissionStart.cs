using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissionStart : NetworkBehaviour
{
    [SerializeField] private MissionManager missionManager;

    [ServerCallback]
    private void OnTriggerEnter(Collider co)
    {
        if (missionManager.missionStarted) { return; }

        if (co.transform.root.gameObject.layer == 6 || co.transform.root.gameObject.layer == 7)
        {
            missionManager.StartMission();
        }
    }
}
