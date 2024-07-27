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
    private EnemyMotor motor;

    [Header("Other")]
    [SerializeField] private float checkTerrainRadius = 0.1f;
    [SerializeField] private LayerMask flyLineOfSightMask;
    [SerializeField] private float flyLocationEpsilon = 0.5f;
    [SerializeField] private float minFollowPlayerDistance = 40f;

    [Header("Disengage")]
    private float disengageTimer;
    [SerializeField] private float disengageTime = 1.0f;

    [Header("Aggresivness, must sum to less than 1")]
    [SerializeField] private float getClosePercent = 0.5f;

    [Header("Chase")]
    [SerializeField] private float minChaseShift = 3f;
    [SerializeField] private float maxChaseShift = 6f;
    [SerializeField] private float chaseSpeed = 2f;
    private float pickSpotTimer;

    [Header("Patrol")]
    [SerializeField] private bool canPatrol = false;
    [SerializeField] private float minPatrolShift = 3f;
    [SerializeField] private float maxPatrolShift = 6f;
    [SerializeField] private float pickPatrolSpotCooldown = 10f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float pickChaseSpotCooldown = 2f;
    private Vector3 flyLocation;

    [Header("Dash")]
    public bool canRetreatDash = true;
    public bool canAttackDash = true;
    [SerializeField] private float dashToPlayerShift = 2f;
    [SerializeField] private float dashNearEnemyShift = 10f;
    [SerializeField] private float dashSpeed = 1f;
    [SerializeField] private float dashCooldown = 15f;
    [SerializeField] private float dashHealthTrigger = 50f;
    [SerializeField] private float dashPlayerDistanceTrigger = 10f;
    private float dashTimer;

    [ServerCallback]
    private void Start()
    {
        if (isServer) // just in case
        {
            enemy = GetComponent<Enemy>();
            motor = GetComponent<EnemyMotor>();
            flyLocation = transform.position;
            pickSpotTimer = pickPatrolSpotCooldown;
            dashTimer = dashCooldown;
            disengageTimer = 0f;
        }
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (enemy.canMove)
        {
            CheckEngage();
            CheckDisengage();
            if (canPatrol)
                CheckPatrol();
            CheckDash();
        }
        UpdateTimers();
        if (enemy.currentAttackState == EnemyAttackState.Idle)
            LookAtTarget();
    }

    private void UpdateTimers()
    {
        if (pickSpotTimer < Mathf.Max(pickPatrolSpotCooldown, pickChaseSpotCooldown))
        {
            pickSpotTimer += Time.fixedDeltaTime;
        }

        if (dashTimer < dashCooldown)
        {
            dashTimer += Time.fixedDeltaTime;
        }
    }

    private void LookAtTarget()
    {
        if (enemy.target == null) { return; }

        if (enemy.currentState == EnemyState.Chase)
        {
            motor.LookAtPosition(enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0));
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

#region Dash
    private void CheckDash()
    {
        if (enemy.currentState != EnemyState.Chase) { return; } 
        if (dashTimer < dashCooldown) { return; }

        if (enemy.health <= dashHealthTrigger)
        {
            if (canRetreatDash)
                StartCoroutine(Dash(false));
        }
        else if ((enemy.target.transform.position - transform.position).magnitude < dashPlayerDistanceTrigger)
        {
            if (canAttackDash)
                StartCoroutine(Dash(true));
        }
    }

    private IEnumerator Dash(bool toPlayer)
    {
        // pick new position
        Vector3 startingPosition = transform.position;
        Vector3 endPosition;
        if (toPlayer)
        {
            endPosition = PickSpotNearPlayer(dashToPlayerShift);
        }
        else
        {
            endPosition = PickSpotNearEnemy(dashNearEnemyShift);
        }

        // only dash if we found a place to dash to
        if (endPosition != startingPosition)
        {
            rb.isKinematic = true;
            enemy.canMove = false;
            enemy.ChangeAttackState(EnemyAttackState.Dash);

            // lerp enemy to dashPosition
            for (float time=0; time<1; time += Time.deltaTime * dashSpeed)
            {
                transform.position = Vector3.Lerp(startingPosition, endPosition, time);
                yield return null;
            }
            
            enemy.ChangeAttackState(EnemyAttackState.Idle);
            rb.isKinematic = false;
            enemy.canMove = true;
            dashTimer = 0f;
        }
    }
#endregion

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
            ApproachPlayer();
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

    private void ApproachPlayer()
    {
        if (pickSpotTimer < pickChaseSpotCooldown)
        {
            return;
        }
        pickSpotTimer = 0f;

        // if player is far away then just go to him first
        Vector3 playerDistance = enemy.target.transform.position - transform.position;
        if (playerDistance.magnitude > minFollowPlayerDistance)
        {
            float radius = Random.Range(minChaseShift, maxChaseShift);
            flyLocation = PickSpotNearPlayer(radius);
            return;
        }

        // movement options
        // 1. move close to player
        // 2. move close to ourselves
        float aggroValue = Random.value;
        if (aggroValue < getClosePercent)
        {
            float radius = Random.Range(minChaseShift, maxChaseShift);
            flyLocation = PickSpotNearPlayer(radius);
        }
        else
        {
            float radius = Random.Range(minPatrolShift, maxPatrolShift);
            flyLocation = PickSpotNearEnemy(radius);
        }
    }
#endregion

#region Disengage
    private void CheckDisengage()
    {
        if (enemy.currentState == EnemyState.Chase) // chasing player
        {
            if (enemy.target == null) // target too far
            {
                Disengage();
            }
            else if (!enemy.canSeeTarget) // lost sight
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
            Patrol();
            Move();
            //PickSpotNearEnemy();
            //Move();
        }
    }

    private void Patrol()
    {
        if (pickSpotTimer < pickPatrolSpotCooldown)
        {
            return;
        }
        pickSpotTimer = 0f;

        // calculate new position to move to
        float radius = Random.Range(minPatrolShift, maxPatrolShift);
        flyLocation = PickSpotNearEnemy(radius);
        motor.LookAtPosition(flyLocation);
    }
#endregion

#region PickSpots
    // should be change so that its on a unit circle instead of unit sphere? y=0?
    private Vector3 PickSpotNearPlayer(float radius)
    {
        // get player position
        Vector3 playerPos = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0);

        // calculate new position near player
        Vector3 newPoint = playerPos + Random.onUnitSphere * radius; // should be change so that its on a unit circle instead? y=0?
        Vector3 lineOfSight = newPoint - playerPos;

        // check new position is not in a wall
        if (!Physics.CheckSphere(newPoint, checkTerrainRadius, flyLineOfSightMask))
        {
            // check we can see the new position
            if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, flyLineOfSightMask))
            {
                return newPoint;
            }
            else
            {
                return transform.position;
            }
        }
        else
        {
            return transform.position;
        }
    }

    private Vector3 PickSpotNearEnemy(float radius)
    {
        // calculate new position to move to
        Vector3 newPoint = transform.position + Random.onUnitSphere * radius;
        Vector3 lineOfSight = newPoint - transform.position;

        // check new position is not in a wall
        if (!Physics.CheckSphere(newPoint, checkTerrainRadius, flyLineOfSightMask))
        {
            // check we can see the new position
            if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, flyLineOfSightMask))
            {
                return newPoint;
            }
            else
            {
                return transform.position;
            }
        }
        else
        {
            return transform.position;
        }
    }
#endregion
}
