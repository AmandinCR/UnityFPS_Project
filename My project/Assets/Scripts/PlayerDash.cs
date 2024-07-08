using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KinematicCharacterController.Examples;
using UnityEngine;
using Mirror;
using KinematicCharacterController;

public class PlayerDash : NetworkBehaviour
{
    private ExampleCharacterController characterController;
    private KinematicCharacterMotor characterMotor;
    [SerializeField] private float groundDashForce;
    [SerializeField] private float airDashForce;
    [SerializeField] private float dashUpwardForce;
    [SerializeField] private bool useCameraForward = false;
    private float dashTimer;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashDuration = 0.3f;
    private Transform cam;
    private Transform forward;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            this.enabled = false;
        }
        characterController = GetComponent<ExampleCharacterController>();
        characterMotor = GetComponent<KinematicCharacterMotor>();
        dashTimer = dashCooldown;
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

    private void Update()
    {
        if (!isLocalPlayer) { return;}

        CheckDash();
    }

    private void CheckDash()
    {
        if (dashTimer < dashCooldown)
        {
            dashTimer += Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Dash();
            }
        }
    }

    private void Dash()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (useCameraForward)
        {
            forward = cam;
        }
        else
        {
            forward = transform;
        }

        Vector3 direction = new Vector3();
        if (verticalInput == 0 && horizontalInput == 0)
        {
            direction = forward.forward;
        }
        else
        {
            direction = forward.forward * verticalInput + forward.right * horizontalInput;
        }

        // add a stronger force if you are on the ground?
        Vector3 force = Vector3.zero;
        if (characterMotor.GroundingStatus.IsStableOnGround)
        {
            force = groundDashForce * direction.normalized + dashUpwardForce * transform.up;
        }
        else
        {
            force = airDashForce * direction.normalized + dashUpwardForce * transform.up;
        }

        
        Vector3 currentVel = characterMotor.BaseVelocity;
        Vector3 removeUpVel = Vector3.zero;
        characterMotor.BaseVelocity = removeUpVel;
        
        characterController.Motor.ForceUnground(0.1f);
        characterController.AddVelocity(force);
        
        dashTimer = 0f;
        StartCoroutine(DashEffects());
    }

    private IEnumerator DashEffects()
    {
        characterController.Gravity = Vector3.zero;
        characterController.JumpUpSpeed = 0f;

        yield return new WaitForSeconds(dashDuration);
        
        // should not manually set this
        characterController.JumpUpSpeed = 12f;
        characterController.Gravity = new Vector3(0,-20f,0);
    }
}
