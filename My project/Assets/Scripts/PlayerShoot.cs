using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KinematicCharacterController.Examples;
using Mirror;
using Mirror.Examples.Basic;
using StinkySteak.SimulationTimer;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
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
    [SerializeField] private bool hitScan = false;
    [SerializeField] private TrailRenderer hitScanTrail;
    private int pierceCount = 0;
    [SerializeField] private float hitScanTravelDelay;

    [Header("Custom Projectile")]
    [SerializeField] private bool custom = false;
    [SerializeField] private GameObject customProjectilePrefab;

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
                    OnEnemyHit(hit, pos, dir);
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

    private void OnEnemyHit(RaycastHit hit, Vector3 pos, Vector3 dir)
    {
        StartCoroutine(FakeBulletTravel(hit, pos, dir));
    }

    private IEnumerator FakeBulletTravel(RaycastHit hit, Vector3 pos, Vector3 dir)
    {
        yield return new WaitForSeconds(hitScanTravelDelay);

        // apply on hit effects
        DealHitScanDamage(hit);
        CheckPierce(hit.point, dir);
    }
    private void DealHitScanDamage(RaycastHit hit)
    {
        PlayerSetup owner = GetComponent<PlayerSetup>();
        hit.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
    }

    private void CheckPierce(Vector3 hit, Vector3 dir)
    {
        pierceCount++;
        if (pierceCount <= items.penetrators)
        {
            HitScanShoot(hit, dir, hit);
        }
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
        StartCoroutine(SpawnTrail(trail, vfxPos, hit));
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 start, Vector3 end)
    {
        float timeToTravel = hitScanTravelDelay;
        if ((start - end).magnitude < 5f)
        {
            timeToTravel = timeToTravel / 2f;
        }

        for(float t = 0; t < 1; t += Time.deltaTime / timeToTravel)
        {
            trail.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        Destroy(trail.gameObject, trail.time);
    }
    #endregion

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
            for (int i = 0; i < items.splitters+1; i++) {
                Vector3 newPath = path.normalized-cameraRight*cameraShift*i + cameraRight*cameraShift*items.splitters/2;
                Quaternion rot = Quaternion.LookRotation(newPath, Vector3.up);
                GameObject vfx = Instantiate(customProjectilePrefab, pos, rot);
                CustomProjectile projectile = vfx.GetComponent<CustomProjectile>();
                projectile.isLocalPlayer = isLocalPlayer;
                projectile.owner = GetComponent<PlayerSetup>();
                projectile.SetProjectileData(damage, newPath, items, cameraShift);
            }
        }
        // else
        // {
        //     GameObject vfx = Instantiate(projectilePrefab, pos, rot);
        //     Projectile projectile = vfx.GetComponent<Projectile>();
        //     projectile.playerCol = col;
        //     projectile.owner = GetComponent<PlayerSetup>();
        //     projectile.isLocalPlayer = isLocalPlayer;
        //     projectile.SetProjectileData(damage, spawnForce, gravity, cameraForward, items);
        // }
    }

    #endregion
}
