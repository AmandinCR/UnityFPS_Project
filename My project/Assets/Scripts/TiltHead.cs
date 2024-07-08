using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TiltHead : NetworkBehaviour
{
    [SerializeField] private GameObject head;
    private Transform cam;

    private void Start()
    {
        if (!isLocalPlayer) {
            this.enabled = false;
            return;
        }
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

    private void Update() 
    {
        if (cam != null)
        {
            head.transform.rotation = cam.rotation;
        }
    }
}
