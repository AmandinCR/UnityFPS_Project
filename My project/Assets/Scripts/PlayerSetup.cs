using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    public float health = 100f;
    public float points = 0f;

    private NetworkManager manager;
    public readonly static List<PlayerSetup> playerList = new List<PlayerSetup>();
    public static PlayerSetup localPlayer;
    private GameObject cam;
    private TextMeshProUGUI healthText;
    public Vector3 velocity;
    private Vector3 previousPosition;

    //private SurfCharacter motor;
    private KinematicCharacterMotor motor;

    private void Start()
    {
        if (isLocalPlayer)
            localPlayer = this;
        
        playerList.Add(this);

        // hopefully the layers don't change lol
        motor = GetComponent<KinematicCharacterMotor>();
        //motor = GetComponent<SurfCharacter>();

        if (!isLocalPlayer) {
            motor.enabled = false;
            GetComponent<ExampleCharacterController>().enabled = false;
            this.gameObject.layer = 7;
        }
        else
        {
            motor.enabled = true;
            GetComponent<ExampleCharacterController>().enabled = true;
            manager = NetworkManager.singleton;
            cam = GameObject.FindGameObjectWithTag("MainCamera");

            healthText = cam.GetComponent<CameraSetup>().healthText;
            healthText.text = health.ToString();
            this.gameObject.layer = 6;
        }
    }

    private void Update() {
        if (isLocalPlayer) {
            QuitGame();
        }

        UpdateVelocity();
    }

    private void UpdateVelocity()
    {
        velocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
    }

    private void QuitGame()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            Application.Quit();
        }
    }

    private void OnDestroy() 
    {
        playerList.Remove(this);
    }

    // TakeDamage should only be run locally so the player is always correct
    public void TakeDamage(float damage) 
    {
        if (isLocalPlayer)
        {
            RemoveHealth(damage);
            CmdTakeDamage(damage);
        }
    }

    [Command(requiresAuthority = false)] 
    private void CmdTakeDamage(float damage) 
    {
        RpcTakeDamage(damage);
    }

    [ClientRpc] 
    private void RpcTakeDamage(float damage) 
    {
        // localplayer already lost health
        if (!isLocalPlayer)
        {
            RemoveHealth(damage);
        }

        if (health <= 0.0f) {
            Die();
        }
    }

    private void RemoveHealth(float damage)
    {
        health -= damage;
        if (isLocalPlayer)
        {
            healthText.text = health.ToString();
        }
    }

    private void Die() {
        health = 100f;

        if (isLocalPlayer) {
            Vector3 spawn = manager.GetStartPosition().position;
            transform.position = spawn;
        }
    }
}
