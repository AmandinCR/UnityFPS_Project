using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using Mirror;
using UnityEngine;

public class PlayerLook : NetworkBehaviour
{
    [HideInInspector] public ExampleCharacterCamera CharacterCamera;
    public Transform cameraTarget;
    private GameObject cam;
    public Transform bodyTransform;

    private const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";

    private void Start()
    {
        if (!isLocalPlayer) {
            this.enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        cam = GameObject.FindGameObjectWithTag("MainCamera");
        CharacterCamera = cam.GetComponent<ExampleCharacterCamera>();
        CharacterCamera.SetFollowTransform(cameraTarget);
        CharacterCamera.IgnoredColliders.Clear();
        CharacterCamera.IgnoredColliders.AddRange(this.GetComponentsInChildren<Collider>());
    }

    private void Update()
    {
        if (!isLocalPlayer) {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer) {
            return;
        }

        HandleCameraInput();
    }

    private void HandleCameraInput()
    {
        // Create the look input vector for the camera
        float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
        float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
        Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

        // Prevent moving the camera while the cursor isn't locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookInputVector = Vector3.zero;
        }

        CharacterCamera.UpdateWithInput(Time.deltaTime, 0.0f, lookInputVector);
    }
}
