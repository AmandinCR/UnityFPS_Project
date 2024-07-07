using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KinematicCharacterController.Examples;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private bool hitScan = false;
    [SerializeField] private LayerMask aimLayerMask;
    private Transform vfxStart;
    private Transform cam;
    private Collider col;
    [SerializeField] private float damage = 0f;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float shotCooldown = 0.5f;
    private float shotTimer = 0f;
    public bool canShoot = false;
    private PlayerItems items;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float spawnForce = 0f;
    [SerializeField] private bool gravity = false;

    [Header("HitScan")]
    [SerializeField] private TrailRenderer hitScanTrail;
    [SerializeField] private float bulletSpeed = 1f;
    private int pierceCount = 0;

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        vfxStart = cam.GetComponent<CameraSetup>().vfxStart;
        col = GetComponent<Collider>();
        items = GetComponent<PlayerItems>();
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
        if (hitScan)
        {
            pierceCount = 0;
            HitScanShoot(cam.position, cam.forward, vfxStart.position);
        }
        else
        {
            ProjectileShoot();
        }
    }

    #region HitScan
    // RUNS ONLY ON LOCAL CLIENT
    private void HitScanShoot(Vector3 pos, Vector3 dir, Vector3 vfxPos)
    {
        // this function runs immediately on localplayer
        RaycastHit hit;
        if (Physics.Raycast(pos, dir, out hit, maxRaycastDistance, aimLayerMask))
        {
            if (hit.transform.root.gameObject.layer == 8) // enemy layer
            {
                if (hit.transform.tag == "HitBox") // enemy hitbox
                {
                    OnHit(hit, pos, dir);
                }
            }
        }
        else
        {
            hit.point = pos + maxRaycastDistance * dir;
        }

        CmdHitScanShoot(hit.point, vfxPos);
        DoHitScanShot(hit.point, vfxPos);
    }

    private void OnHit(RaycastHit hit, Vector3 pos, Vector3 dir)
    {
        PlayerSetup owner = GetComponent<PlayerSetup>();
        hit.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
        float bulletDistance = (hit.point - pos).magnitude;
        CheckPierce(hit.point, dir, bulletDistance);
    }

    private void CheckPierce(Vector3 hit, Vector3 dir, float bulletDistance)
    {
        pierceCount++;
        if (pierceCount <= items.penetrators)
        {
            StartCoroutine(FakeBulletRestart(hit, dir, bulletDistance));
        }
    }

    private IEnumerator FakeBulletRestart(Vector3 hit, Vector3 dir, float bulletDistance)
    {
        yield return new WaitForSeconds(bulletDistance / bulletSpeed);
        HitScanShoot(hit, dir, hit);
    }

    // RUNS ONLY ON SERVER (HOST TECHNICALLY)
    [Command] 
    void CmdHitScanShoot(Vector3 hit, Vector3 vfxPos)
    {
        RpcHitScanShoot(hit, vfxPos);
    }

    // RUNS ONLY ON REMOTE CLIENT
    [ClientRpc(includeOwner = false)]
    void RpcHitScanShoot(Vector3 hit, Vector3 vfxPos) 
    {
        if (!isLocalPlayer) // just in case
        {
            DoHitScanShot(hit, vfxPos);
        }
    }

    // RUNS ON ALL CLIENTS
    private void DoHitScanShot(Vector3 hit, Vector3 vfxPos)
    {
        // Do visual effect of the shot
        TrailRenderer trail = Instantiate(hitScanTrail, vfxPos, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, hit));
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hit)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(trail.transform.position, hit);
        float remainingDistance = distance;
        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit, 1 - (remainingDistance / distance));
            remainingDistance -= bulletSpeed * Time.deltaTime;
            yield return null;
        }
        trail.transform.position = hit;
        Destroy(trail.gameObject, trail.time);
    }
    #endregion

    #region Projectiles
    // RUNS ONLY ON LOCAL PLAYER
    private void ProjectileShoot()
    {
        RaycastHit hit;
        Vector3 pos = vfxStart.position;
        Quaternion rot = vfxStart.rotation;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxRaycastDistance, aimLayerMask)) 
        {
            rot = Quaternion.LookRotation(hit.point - pos, Vector3.up);
        }
        CmdShoot(pos, rot, cam.forward);
        DoShoot(pos, rot, cam.forward);
    }

    // RUNS ONLY ON SERVER (HOST TECHNICALLY)
    [Command] 
    void CmdShoot(Vector3 pos, Quaternion rot, Vector3 cameraForward)
    {
        RpcShoot(pos, rot, cameraForward);
    }

    // RUNS ONLY ON REMOTE CLIENT
    [ClientRpc(includeOwner = false)]
    void RpcShoot(Vector3 pos, Quaternion rot, Vector3 cameraForward) 
    {
        if (!isLocalPlayer) // just in case
        {
            DoShoot(pos, rot, cameraForward);
        }
    }

    // RUNS ON ALL CLIENTS
    private void DoShoot(Vector3 pos, Quaternion rot, Vector3 cameraForward)
    {
        // Do visual effect of the shot
        GameObject vfx = Instantiate(projectilePrefab, pos, rot);
        Projectile projectile = vfx.GetComponent<Projectile>();
        projectile.playerCol = col;
        projectile.owner = GetComponent<PlayerSetup>();
        projectile.isLocalPlayer = isLocalPlayer;
        projectile.SetProjectileData(damage, spawnForce, gravity, cameraForward, items);
    }

    #endregion
}
