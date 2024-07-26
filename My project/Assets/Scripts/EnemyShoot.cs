using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;
using KinematicCharacterController;
using Mirror.Examples.Basic;

public class EnemyShoot : NetworkBehaviour
{
    [Header("Laser")]
    public bool canLaser = true;
    [SerializeField] private bool stopToLaser = false;
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float laserCooldown;
    [SerializeField] private float laserDuration;
    [SerializeField] private float laserDamage;
    [SerializeField] private float laserTickRate;
    [SerializeField] private float laserPercent = 0.5f;
    [SerializeField] private float laserRotateSpeed = 1f;

    [Header("Projectiles")]
    public bool canShoot = true;
    [SerializeField] private bool stopToShoot = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootDamage;
    [SerializeField] private float spawnForce;
    [SerializeField] private float shootCooldown;
    [SerializeField] private int magSize;
    [SerializeField] private float fireRate;
    [SerializeField] private float attackRange;
    [SerializeField] private Transform vfxStart;
    [SerializeField] private float maxVelocityTrackingMultiplier = 1f;
    [SerializeField] private float maxVelocityToTrack = 1f;
    private int currentAmmo;
    private float attackTimer = 0f;
    private Enemy enemy;
    private Quaternion shootRotation;
    private EnemyMotor motor;

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
    }

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void FixedUpdate()
    {
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

    private void PickAttack()
    {
        float attackValue = Random.value;
        if (attackValue < laserPercent)
        {
            if (canLaser)
                StartCoroutine(ServerLaser());
        }
        else
        {
            if (canShoot)
                StartCoroutine(ServerShoot());
        }
    }

    #region Laser
    // RUNS ONLY ON SERVER
    private IEnumerator ServerLaser()
    {
        enemy.ChangeAttackState(EnemyAttackState.Shoot);
        attackTimer = laserCooldown;
        RpcLaser(); // technically should avoid passing gameobjects as reference
        if (stopToLaser)
        {
            enemy.canMove = false;
        }

        float time = 0f;
        while (time < laserDuration)
        {
            Vector3 targetDirection = vfxStart.forward;
            Vector3 ourDirection = vfxStart.forward;
            if (enemy.target != null)
                targetDirection = (enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0) - vfxStart.position).normalized;
            float step = laserRotateSpeed * Time.deltaTime;
            Vector3 newDirection = Vector3.RotateTowards(ourDirection, targetDirection, step, 0.0f);
            motor.LookAtDirection(newDirection);

            time += Time.deltaTime;
            yield return null;
        }

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
        GameObject vfx = Instantiate(laserPrefab, vfxStart.position, vfxStart.rotation, vfxStart);
        vfx.GetComponent<Laser>().SetProjectileData(laserDamage, laserDuration, laserTickRate);
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

    private void CalculateShoot()
    {
        currentAmmo--;
        if (currentAmmo <= 0)
        {
            CancelInvoke(); // still runs through rest of function
        }

        Quaternion shootRotation = vfxStart.rotation;
        if (enemy.target != null) // just in case
        {
            Vector3 playerVelocity = enemy.target.GetComponent<PlayerSetup>().velocity;
            float velocityTracking = Random.Range(0,maxVelocityTrackingMultiplier);
            if (playerVelocity.magnitude > maxVelocityToTrack)
            {
                playerVelocity = playerVelocity.normalized * maxVelocityToTrack;
            }
            Vector3 playerDirection = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0) - vfxStart.position;
            Vector3 shootDirection = playerDirection + playerVelocity * velocityTracking;// * playerDirection.magnitude;
            shootRotation = Quaternion.LookRotation(shootDirection, vfxStart.up);
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
        GameObject vfx = Instantiate(projectilePrefab, vfxStart.position, shootRotation);
        vfx.GetComponent<EnemyProjectile>().SetProjectileData(shootDamage, spawnForce);
    }
    #endregion
}
