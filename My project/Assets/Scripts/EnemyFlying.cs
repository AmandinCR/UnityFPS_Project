using System.Collections;
using System.Collections.Generic;
using Mirror;
using Org.BouncyCastle.Security;
using UnityEngine;

public class EnemyFlying : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    private Enemy enemy;

    [Header("Other")]
    [SerializeField] private float checkTerrainRadius = 0.1f;
    [SerializeField] private LayerMask flyLineOfSightMask;
    [SerializeField] private float flyLocationEpsilon = 0.5f;

    [Header("Disengage")]
    private float disengageTimer = 0f;
    [SerializeField] private float disengageTime = 1.0f;

    [Header("Chase")]
    [SerializeField] private float minChaseShift = 3f;
    [SerializeField] private float maxChaseShift = 6f;
    [SerializeField] private float chaseSpeed = 2f;
    private float pickSpotTimer;

    [Header("Patrol")]
    [SerializeField] private float minPatrolShift = 3f;
    [SerializeField] private float maxPatrolShift = 6f;
    [SerializeField] private float pickPatrolSpotCooldown = 10f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float pickChaseSpotCooldown = 2f;
    private Vector3 flyLocation;

    [ServerCallback]
    private void Start()
    {
        if (isServer) // just in case
        {
            enemy = GetComponent<Enemy>();
            flyLocation = transform.position;
            pickSpotTimer = pickPatrolSpotCooldown;
        }
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (enemy.canMove)
        {
            CheckEngage();
            CheckDisengage();
            CheckPatrol();
        }

        if (pickSpotTimer < Mathf.Max(pickPatrolSpotCooldown, pickChaseSpotCooldown))
        {
            pickSpotTimer += Time.fixedDeltaTime;
        }

        LookAtTarget();
    }

    private void LookAtTarget()
    {
        if (enemy.target == null) { return; }

        if (enemy.currentState == EnemyState.Chase)
        {
            transform.LookAt(enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0));
        }
    }

    private void Move()
    {
        Vector3 flyDirection = flyLocation - transform.position;
        if (flyDirection.magnitude > flyLocationEpsilon)
        {
            if (enemy.currentState == EnemyState.Chase)
            {
                rb.AddForce(flyDirection.normalized * chaseSpeed);
            }
            else if (enemy.currentState == EnemyState.Patrol)
            {
                rb.AddForce(flyDirection.normalized * patrolSpeed);
            }
        }
    }

#region Engage
    private void CheckEngage()
    {
        if (enemy.target == null) { return; }

        if (enemy.canSeeTarget)
        {
            Engage();
        }

        // we are chasing the player
        if (enemy.currentState == EnemyState.Chase)
        {
            PickSpotNearPlayer();
            Move();
        }
    }

    private void Engage()
    {
        // start chasing
        if (enemy.currentState != EnemyState.Chase)
        {
            enemy.ChangeState(EnemyState.Chase);
        }
    }

    private void PickSpotNearPlayer()
    {
        if (pickSpotTimer < pickChaseSpotCooldown)
        {
            return;
        }
        pickSpotTimer = 0f;

        // get player position
        Vector3 playerPos = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0);

        // calculate new position near player
        float radius = Random.Range(minChaseShift, maxChaseShift);
        Vector3 newPoint = playerPos + Random.onUnitSphere * radius;
        Vector3 lineOfSight = newPoint - playerPos;

        // check new position is not in a wall
        if (!Physics.CheckSphere(newPoint, checkTerrainRadius, flyLineOfSightMask))
        {
            // check we can see player from new position
            if (!Physics.Raycast(playerPos, lineOfSight, lineOfSight.magnitude, flyLineOfSightMask))
            {
                flyLocation = newPoint;
            }
        }
    }
#endregion

#region Disengage
    private void CheckDisengage()
    {
        if (enemy.currentState == EnemyState.Chase) // chasing player
        {
            if (enemy.target == null || !enemy.canSeeTarget) // lost sight or player too far
            {
                // chasing for too long
                if (disengageTimer >= disengageTime)
                {
                    Disengage();
                }
                else
                {
                    disengageTimer += Time.deltaTime;
                }
            }
        }
    }

    private void Disengage()
    {
        enemy.ChangeState(EnemyState.Idle);
        disengageTimer = 0f;
    }
#endregion

#region Patrol
    private void CheckPatrol()
    {
        if (enemy.canSeeTarget) { return;}

        if (enemy.currentState == EnemyState.Idle)
        {
            enemy.ChangeState(EnemyState.Patrol);
        }

        if (enemy.currentState == EnemyState.Patrol)
        {
            PickSpotNearEnemy();
            Move();
        }
    }

    private void PickSpotNearEnemy()
    {
        if (pickSpotTimer < pickPatrolSpotCooldown)
        {
            return;
        }
        pickSpotTimer = 0f;

        // calculate new position to move to
        float radius = Random.Range(minPatrolShift, maxPatrolShift);
        Vector3 newPoint = transform.position + Random.onUnitSphere * radius;
        Vector3 lineOfSight = newPoint - transform.position;

        // check new position is not in a wall
        if (!Physics.CheckSphere(newPoint, checkTerrainRadius, flyLineOfSightMask))
        {
            // check we can see the new position
            if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, flyLineOfSightMask))
            {
                flyLocation = newPoint;
                transform.LookAt(newPoint);
            }
        }
    }
#endregion
}
