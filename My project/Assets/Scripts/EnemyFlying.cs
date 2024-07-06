using System.Collections;
using System.Collections.Generic;
using Mirror;
using Org.BouncyCastle.Security;
using UnityEngine;

public class EnemyFlying : NetworkBehaviour
{
    private Enemy enemy;
    [SerializeField] private float disengageTime = 1.0f;
    private float disengageTimer = 0f;
    [SerializeField] private float minShift = 3f;
    [SerializeField] private float maxShift = 6f;
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float checkTerrainRadius = 0.1f;
    private float pickSpotTimer;
    [SerializeField] private float pickChaseSpotCooldown = 2f;
    private Vector3 flyLocation;
    [SerializeField] private LayerMask flyLineOfSightMask;
    [SerializeField] private float flyLocationEpsilon = 0.5f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float minPatrolShift = 3f;
    [SerializeField] private float maxPatrolShift = 6f;
    [SerializeField] private float pickPatrolSpotCooldown = 10f;
    [SerializeField] private float patrolSpeed = 1f;

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
        CheckEngage();
        CheckDisengage();

        CheckPatrol();

        if (pickSpotTimer < Mathf.Max(pickPatrolSpotCooldown, pickChaseSpotCooldown))
        {
            pickSpotTimer += Time.fixedDeltaTime;
        }
    }

    private void CheckPatrol()
    {
        if (enemy.canSeeTarget) { return;}

        if (enemy.currentState == EnemyState.Idle)
        {
            enemy.ChangeState(EnemyState.Patrol);
        }

        if (enemy.currentState == EnemyState.Patrol)
        {
            PickPatrolSpot();
            Move();
        }
    }

    private void LookAtTarget()
    {
        transform.LookAt(enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0));
    }

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
            LookAtTarget();
            Move();
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

    private void Engage()
    {
        // start chasing
        if (enemy.currentState != EnemyState.Chase)
        {
            enemy.ChangeState(EnemyState.Chase);
        }
    }

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
        float radius = Random.Range(minShift, maxShift);
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
        //Debug.DrawRay(origin, lineOfSight, Color.green, 10f, false);
    }

    private void PickPatrolSpot()
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
}
