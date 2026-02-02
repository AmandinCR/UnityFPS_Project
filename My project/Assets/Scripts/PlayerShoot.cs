using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KinematicCharacterController.Examples;
using Mirror;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private Transform vfxStart;
    private Transform cam;
    [SerializeField] private float damage = 0f;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float shotCooldown = 0.5f;
    private float shotTimer = 0f;
    public bool canShoot = false;
    private PlayerItems items;

    [Header("Custom Projectile")]
    [SerializeField] private bool custom = false;
    [SerializeField] private GameObject customProjectilePrefab;

    [Header("Muzzle")]
    private ParticleSystem muzzleFlash;

    void Start()
    {
        cam = Camera.main.transform;
        items = GetComponent<PlayerItems>();
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
    
    #region Projectiles
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

        CmdShoot(pos, path, cam.right);
        DoShoot(pos, path, cam.right);
    }

    // RUNS ONLY ON SERVER (HOST TECHNICALLY)
    [Command] 
    void CmdShoot(Vector3 pos, Vector3 path, Vector3 cameraRight)
    {
        RpcShoot(pos, path, cameraRight);
    }

    // RUNS ONLY ON REMOTE CLIENT
    [ClientRpc(includeOwner = false)]
    void RpcShoot(Vector3 pos, Vector3 path, Vector3 cameraRight) 
    {
        if (!isLocalPlayer) // just in case
        {
            DoShoot(pos, path, cameraRight);
        }
    }

    // RUNS ON ALL CLIENTS
    [SerializeField] private float cameraShift = 0.2f;
    private void DoShoot(Vector3 pos, Vector3 path, Vector3 cameraRight)
    {
        if (custom)
        {
            // // This is for being able to split on first shot
            // for (int i = 0; i < items.splitters+1; i++) {
            //     Vector3 newPath = path.normalized-cameraRight*cameraShift*i + cameraRight*cameraShift*items.splitters/2;
            //     Quaternion rot = Quaternion.LookRotation(newPath, Vector3.up);
            //     GameObject vfx = Instantiate(customProjectilePrefab, pos, rot);
            //     CustomProjectile projectile = vfx.GetComponent<CustomProjectile>();
            //     projectile.isLocalPlayer = isLocalPlayer;
            //     projectile.owner = GetComponent<PlayerSetup>();
            //     projectile.SetProjectileData(damage, newPath, items, cameraShift);
            // }
            Quaternion rot = Quaternion.LookRotation(path, Vector3.up);
            GameObject vfx = Instantiate(customProjectilePrefab, pos, rot);
            CustomProjectile projectile = vfx.GetComponent<CustomProjectile>();
            projectile.isLocalPlayer = isLocalPlayer;
            projectile.owner = GetComponent<PlayerSetup>();
            projectile.SetProjectileData(damage, path, items, cameraShift);
            PlayMuzzleFlash();
        }
    }

    private void PlayMuzzleFlash()
    {
        muzzleFlash.Play();
    }

    #endregion
}
