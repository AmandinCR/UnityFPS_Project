using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.Examples;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private LayerMask aimLayerMask;
    private Transform vfxStart;
    private Transform cam;
    private Collider col;

    [SerializeField] private float damage = 0f;
    [SerializeField] private float spawnForce = 0f;
    [SerializeField] private bool gravity = false;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float shotCooldown = 0.5f;
    private float shotTimer = 0f;

    // for testing
    public bool canShoot = false;

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        vfxStart = cam.GetComponent<CameraSetup>().vfxStart;
        col = GetComponent<Collider>();
    }

    // RUNS ONLY ON LOCAL CLIENT
    void Update()
    {
        if (isLocalPlayer) {
            if (Input.GetMouseButtonDown(0)) 
            {
                if (shotTimer <= 0 && canShoot)
                {
                    Shoot();
                    shotTimer = shotCooldown;
                }
            }
            if (shotTimer > 0)
            {
                shotTimer -= Time.deltaTime;
            }
        }
    }

    // RUNS ONLY ON LOCAL CLIENT
    [ClientCallback] 
    private void Shoot() 
    {
        RaycastHit hit;
        Vector3 pos = vfxStart.position;
        Quaternion rot = vfxStart.rotation;
        bool didHit = false;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxRaycastDistance, aimLayerMask)) 
        {
            didHit = true;
        }

        CmdShoot(hit.point, didHit, pos, rot);
        DoShoot(hit.point, didHit, pos, rot);
    }

    // RUNS ONLY ON SERVER (HOST TECHNICALLY)
    [Command] 
    void CmdShoot(Vector3 hit, bool didHit, Vector3 pos, Quaternion rot)
    {
        RpcShoot(hit, didHit, pos, rot);
    }

    // RUNS ONLY ON REMOTE CLIENT
    [ClientRpc(includeOwner = false)]
    void RpcShoot(Vector3 hit, bool didHit, Vector3 pos, Quaternion rot) 
    {
        if (!isLocalPlayer) // just in case
        {
            DoShoot(hit, didHit, pos, rot);
        }
    }

    void DoShoot(Vector3 hit, bool didHit, Vector3 pos, Quaternion rot)
    {
        if (didHit) 
        {
            rot = Quaternion.LookRotation(hit - pos, Vector3.up);
        }
        
        // Do visual effect of the shot
        GameObject vfx = Instantiate(projectilePrefab, pos, rot);
        Projectile projectile = vfx.GetComponent<Projectile>();
        projectile.playerCol = col;
        projectile.owner = GetComponent<PlayerSetup>();
        projectile.isLocalPlayer = isLocalPlayer;
        projectile.SetProjectileData(damage, spawnForce, gravity);
    }
}
