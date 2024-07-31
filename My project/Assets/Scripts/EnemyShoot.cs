using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;
using KinematicCharacterController;
using Mirror.Examples.Basic;

public class EnemyShoot : NetworkBehaviour
{
    public enum ShotType{
        None,
        Projectile,
        Laser
    }

    [Header("Behaviour")]
    public ShotType shotType = ShotType.None;
    [SerializeField] private bool stopToShoot = false;


    [Header("Laser")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float laserCooldown;
    [SerializeField] private float laserDuration;
    [SerializeField] private float laserDamage;
    [SerializeField] private float laserTickRate;
    [SerializeField] private float laserRotateSpeed = 0.01f;

    [Header("Projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootDamage;
    [SerializeField] private float spawnForce;
    [SerializeField] private float shootCooldown;
    [SerializeField] private int magSize;
    [SerializeField] private float fireRate;
    [SerializeField] private float maxVelocityTrackingMultiplier = 1f;
    [SerializeField] private float maxVelocityToTrack = 1f;

    [Header("Other")]
    [SerializeField] private List<Transform> vfxStarts = new List<Transform>();
    private int currentAmmo;
    private float attackTimer = 0f;
    private Enemy enemy;
    private Quaternion shootRotation;
    private EnemyMotor motor;

    [Header("MuzzleFlash")]
    [SerializeField] private List<ParticleSystem> muzzleParticleSystems;

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void Start()
    {
        // just in case
        if (fireRate * magSize > shootCooldown)
        {
            fireRate = shootCooldown / magSize;
        }
        if (laserDuration > laserCooldown)
        {
            laserDuration = laserCooldown;
        }
        currentAmmo = magSize;
        enemy = GetComponent<Enemy>();
        motor = GetComponent<EnemyMotor>();

        // get reference to all the muzzle flash particle system vfx
        // so that we don't have to add it all ourselves
        foreach (Transform vfxStart in vfxStarts)
        {
            muzzleParticleSystems.Add(vfxStart.GetComponent<ParticleSystem>());
        }
    }

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void FixedUpdate()
    {
        if (shotType != ShotType.None)
            CheckShoot();
    }

    // RUNS ONLY ON SERVER
    private void CheckShoot()
    {
        if (attackTimer <= 0f) 
        {
            if (enemy.target != null && enemy.canSeeTarget)
            {
                if (enemy.currentState == EnemyState.Chase && enemy.currentAttackState == EnemyAttackState.Idle) 
                {
                    PickAttack();
                }
            }
        }
        else
        {
            attackTimer -= Time.deltaTime;
        }
    }

    // RUNS ONLY ON SERVER
    private void PickAttack()
    {
        if (shotType == ShotType.Projectile)
            StartCoroutine(ServerShoot());
        else if (shotType == ShotType.Laser)
            StartCoroutine(ServerLaser());
    }

    #region Laser
    // RUNS ONLY ON SERVER
    private IEnumerator ServerLaser()
    {
        enemy.ChangeAttackState(EnemyAttackState.Shoot);
        attackTimer = laserCooldown;
        RpcLaser(); // technically should avoid passing gameobjects as reference
        if (stopToShoot)
        {
            enemy.canMove = false;
        }

        float oldSpeed = motor.headRotateSpeed;
        motor.headRotateSpeed = laserRotateSpeed;
        yield return new WaitForSeconds(laserDuration);
        motor.headRotateSpeed = oldSpeed;

        enemy.canMove = true;
        enemy.ChangeAttackState(EnemyAttackState.Idle);
    }

    // RUNS ONLY ON CLIENTS
    [ClientRpc]
    private void RpcLaser()
    {
        Laser();
    }

    // RUNS ONLY ON CLIENTS
    private void Laser()
    {
        for (int i = 0; i < vfxStarts.Count; i++)
        {
            GameObject vfx = Instantiate(laserPrefab, vfxStarts[i].position, vfxStarts[i].rotation, vfxStarts[i]);
            vfx.GetComponent<Laser>().SetProjectileData(laserDamage, laserDuration, laserTickRate);
        }
        
    }
    #endregion

    #region Shoot
    // RUNS ONLY ON SERVER
    private IEnumerator ServerShoot() 
    {
        enemy.ChangeAttackState(EnemyAttackState.Shoot);
        attackTimer = shootCooldown;
        currentAmmo = magSize;
        //currentAmmo = Random.Range(1,magSize+1);
        InvokeRepeating("CalculateShoot", 0.0f, fireRate);
        
        if (stopToShoot)
        {
            enemy.canMove = false;
        }

        yield return new WaitForSeconds(fireRate * magSize); 

        enemy.canMove = true;
        enemy.ChangeAttackState(EnemyAttackState.Idle);
    }

    // RUNS ONLY ON SERVER
    private void CalculateShoot()
    {
        currentAmmo--;
        if (currentAmmo <= 0)
        {
            CancelInvoke(); // still runs through rest of function
        }

        Quaternion shootRotation = motor.head.rotation;
        if (enemy.target != null) // just in case
        {
            Vector3 playerVelocity = enemy.target.GetComponent<PlayerSetup>().velocity;
            float velocityTracking = Random.Range(0,maxVelocityTrackingMultiplier);
            if (playerVelocity.magnitude > maxVelocityToTrack)
            {
                playerVelocity = playerVelocity.normalized * maxVelocityToTrack;
            }
            Vector3 playerDirection = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0) - motor.head.position;
            Vector3 shootDirection = playerDirection + playerVelocity * velocityTracking;// * playerDirection.magnitude;
            shootRotation = Quaternion.LookRotation(shootDirection, motor.head.up);
        }
        RpcShoot(shootRotation);
    }

    // RUNS ONLY ON CLIENTS
    [ClientRpc]
    private void RpcShoot(Quaternion rot)
    {
        shootRotation = rot;
        LaunchProjectile();
    }

    // RUNS ONLY ON CLIENTS
    private void LaunchProjectile()
    {
        // KinematicCharacterMotor is disabled on remote clients
        for (int i = 0; i < vfxStarts.Count; i++)
        {
            GameObject vfx = Instantiate(projectilePrefab, vfxStarts[i].position, shootRotation);
            vfx.GetComponent<EnemyProjectile>().SetProjectileData(shootDamage, spawnForce);
            muzzleParticleSystems[i].Play();
        }
    }
    #endregion
}
