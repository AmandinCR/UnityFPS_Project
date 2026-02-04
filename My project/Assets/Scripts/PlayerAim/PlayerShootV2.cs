using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerShootV2 : NetworkBehaviour
{
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private Transform vfxStart;
    private Transform cam;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float shotCooldown = 0.5f;
    private float shotTimer = 0f;
    public bool canShoot = false;

    [Header("Custom Projectile")]
    [SerializeField] private GameObject customProjectilePrefab;

    [Header("Muzzle")]
    private ParticleSystem muzzleFlash;

    void Start()
    {
        cam = Camera.main.transform;
        muzzleFlash = vfxStart.GetComponent<ParticleSystem>();

        // maybe should be in a different file...
        // this is so that the gun rotates smoothly from camera perspective
        if (isLocalPlayer)
        {
            Vector3 pos = vfxStart.localPosition;
            vfxStart.SetParent(cam);
            vfxStart.localPosition = pos;
        }
            
    }

    // RUNS ONLY ON LOCAL CLIENT
    void Update()
    {
        if (isLocalPlayer) {
            if (canShoot)
            {
                if (Input.GetMouseButtonDown(0)) 
                {
                    if (shotTimer <= 0)
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
    }

    // RUNS ONLY ON LOCAL CLIENT
    [ClientCallback]
    private void Shoot() 
    {
        ProjectileShoot();
    }
    
    // RUNS ONLY ON LOCAL PLAYER
    private void ProjectileShoot()
    {
        RaycastHit hit;
        Vector3 pos = vfxStart.position;
        Vector3 path = cam.forward;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxRaycastDistance, aimLayerMask)) 
        {
            path = hit.point - pos;
        }


        CmdShoot(pos, path);
        DoShoot(pos, path);
    }

    // RUNS ONLY ON SERVER (HOST TECHNICALLY)
    [Command] 
    void CmdShoot(Vector3 pos, Vector3 path)
    {
        RpcShoot(pos, path);
    }

    // RUNS ONLY ON REMOTE CLIENT
    [ClientRpc(includeOwner = false)]
    void RpcShoot(Vector3 pos, Vector3 path) 
    {
        if (!isLocalPlayer) // just in case
        {
            DoShoot(pos, path);
        }
    }

    // RUNS ON ALL CLIENTS
    [SerializeField] private float cameraShift = 0.2f;
    private void DoShoot(Vector3 pos, Vector3 path)
    {
        Quaternion rot = Quaternion.LookRotation(path, Vector3.up);
        GameObject vfx = Instantiate(customProjectilePrefab, pos, rot);
        CustomProjectileV2 projectile = vfx.GetComponent<CustomProjectileV2>();
        projectile.isLocalPlayer = isLocalPlayer;
        projectile.owner = GetComponent<PlayerSetup>();
        projectile.SetProjectileData(damage);
        PlayMuzzleFlash();
    }

    private void PlayMuzzleFlash()
    {
        muzzleFlash.Play();
    }
}
