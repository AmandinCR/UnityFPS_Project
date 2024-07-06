using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;
using KinematicCharacterController;
using Mirror.Examples.Basic;

public class EnemyShoot : NetworkBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float damage;
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

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void Start()
    {
        // just in case
        if (fireRate * magSize > shootCooldown)
        {
            fireRate = shootCooldown / magSize;
        }
        currentAmmo = magSize;
        enemy = GetComponent<Enemy>();
    }

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void FixedUpdate()
    {
        CheckAttack();
    }

    private void CheckAttack()
    {
        if (attackTimer <= 0f) 
        {
            if (enemy.target != null) // just in case
            {
                if (enemy.currentState == EnemyState.Chase && enemy.currentState != EnemyState.Attack) 
                {
                    ServerShoot();
                }
            }
        }
        else
        {
            attackTimer -= Time.deltaTime;
        }
    }

    // RUNS ONLY ON SERVER
    private void ServerShoot() 
    {
        attackTimer = shootCooldown;
        Quaternion shootRotation = vfxStart.rotation;
        if (enemy.target != null)
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
        Shoot();
    }
    
    // RUNS ONLY ON CLIENTS
    private void Shoot()
    {
        currentAmmo = Random.Range(1,magSize+1);
        InvokeRepeating("LaunchProjectile", 0.0f, fireRate);
    }

    // RUNS ONLY ON CLIENTS
    private void LaunchProjectile()
    {
        currentAmmo--;
        if (currentAmmo <= 0)
        {
            CancelInvoke(); // still runs through rest of function
        }

        // KinematicCharacterMotor is disabled on remote clients
        GameObject vfx = Instantiate(projectilePrefab, vfxStart.position, shootRotation);
        vfx.GetComponent<EnemyProjectile>().SetProjectileData(damage, spawnForce);
    }
}
