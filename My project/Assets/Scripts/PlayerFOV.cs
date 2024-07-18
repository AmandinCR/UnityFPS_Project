using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using DG.Tweening;

public class PlayerFOV : NetworkBehaviour
{
    private Camera cam;
    private KinematicCharacterMotor characterMotor;
    [SerializeField] private float velocityFOVChange;
    [SerializeField] private float fastFOV;
    [SerializeField] private float slowFOV;
    [SerializeField] private float fovTransitionSpeed;
    private float fovChangeTimer = 0f;
    private bool goingFast = false;

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
        Vector3 currentVel = characterMotor.BaseVelocity;
        currentVel.y = 0f;
        if (!goingFast)
        {
            if (currentVel.magnitude >= velocityFOVChange)
            {
                if (cam.fieldOfView != fastFOV)
                {
                    DOFov(fastFOV);
                    goingFast = true;
                }
            }
        }

        if (fovChangeTimer <= 0f)
        {
            if (goingFast)
            {
                if (currentVel.magnitude < velocityFOVChange)
                {
                    if (cam.fieldOfView != slowFOV)
                    {
                        DOFov(slowFOV);
                        goingFast = false;
                    }
                }
            }
        }
        else
        {
            fovChangeTimer -= Time.deltaTime;
        }
    }

    private void DOFov(float newFOV)
    {
        fovChangeTimer = fovTransitionSpeed;
        cam.DOFieldOfView(newFOV, fovTransitionSpeed);
    }
}
