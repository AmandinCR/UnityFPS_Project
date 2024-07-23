using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using DG.Tweening;
using Steamworks;

public class PlayerFOV : NetworkBehaviour
{
    private Camera cam;
    private KinematicCharacterMotor characterMotor;
    [SerializeField] private float maxVelocityFOV;
    [SerializeField] private float minVelocityFOV;
    [SerializeField] private float maxFOVChange;
    [SerializeField] private float baseFOV;
    [SerializeField] private float fovTransitionSpeed;
    private float targetFOV;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            this.enabled = false;
        }

        characterMotor = GetComponent<KinematicCharacterMotor>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    private void Update()
    {
        // don't change fov in the air, it feels weird
        if (!characterMotor.GroundingStatus.IsStableOnGround && cam.fieldOfView == baseFOV + maxFOVChange) 
        {
            return;
        }

        Vector3 currentVel = characterMotor.BaseVelocity;
        currentVel.y = 0f;
        targetFOV = baseFOV;
        if (currentVel.magnitude > maxVelocityFOV)
        {
            targetFOV = baseFOV + maxFOVChange;
        }

        /* for continuous fov changes per velocity
        if (currentVel.magnitude < minVelocityFOV) 
            targetFOV = baseFOV;
        else
            targetFOV =  baseFOV + maxFOVChange * Mathf.Min(currentVel.magnitude / maxVelocityFOV, 1);
        */

        if (targetFOV > cam.fieldOfView)
        {
            cam.fieldOfView += Time.deltaTime*fovTransitionSpeed;
            if (cam.fieldOfView > targetFOV) 
            {
                cam.fieldOfView = targetFOV;
            }
        }
        else if (targetFOV < cam.fieldOfView)
        {
            cam.fieldOfView -= Time.deltaTime*fovTransitionSpeed;
            if (cam.fieldOfView < targetFOV) 
            {
                cam.fieldOfView = targetFOV;
            }
        }
        
    }
}
