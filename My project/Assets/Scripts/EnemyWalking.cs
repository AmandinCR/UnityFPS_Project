using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyWalking : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform head;
    private Enemy enemy;
    private NavMeshAgent agent;

    [Header("Other")]
    [SerializeField] private LayerMask walkLineOfSightMask;
    [SerializeField] private float minFollowPlayerDistance;

    [Header("Disengage")]
    [SerializeField] private float disengageTime = 1.0f;
    private float disengageTimer;

    [Header("Aggresivness")]
    [SerializeField] private float followPercent = 0.5f; // 0 to 1 float
    [SerializeField] private float getClosePercent = 0.8f; // 0 to 1 float

    [Header("Chase")]
    [SerializeField] private float minChaseShift = 3f;
    [SerializeField] private float maxChaseShift = 6f;
    [SerializeField] private float pickChaseSpotCooldown = 10f;
    private float pickSpotTimer;
    private bool followingPlayer = false;

    [Header("Patrol")]
    [SerializeField] private float minPatrolShift = 3f;
    [SerializeField] private float maxPatrolShift = 6f;
    [SerializeField] private float pickPatrolSpotCooldown = 10f;

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
    void Start()
    {
        if (isServer) // just in case
        {
            agent = GetComponent<NavMeshAgent>();
            enemy = GetComponent<Enemy>();
            pickSpotTimer = pickPatrolSpotCooldown;
            disengageTimer = 0f;
            dashTimer = dashCooldown;
            agent.destination = transform.position;
        }
    }

    [ServerCallback]
    void FixedUpdate()
    {
        if (enemy.canMove)
        {
            CheckEngage();
            CheckDisengage();
            CheckPatrol();
            CheckDash();
        }
        else
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
            }
        }

        if (pickSpotTimer < Mathf.Max(pickPatrolSpotCooldown, pickChaseSpotCooldown))
        {
            pickSpotTimer += Time.fixedDeltaTime;
        }

        if (dashTimer < dashCooldown)
        {
            dashTimer += Time.fixedDeltaTime;
        }

        LookAtTarget();
    }

    private void LookAtTarget()
    {
        if (enemy.target == null) { return; }

        if (enemy.currentState == EnemyState.Chase)
        {
            head.LookAt(enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0));
        }
    }

    private void Move()
    {
        // destination is set manually elsewhere to unless following player for optimization
        if (followingPlayer)
        {
            agent.destination = enemy.target.transform.position;
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
        agent.enabled = false;
        enemy.canMove = false;
        enemy.ChangeAttackState(EnemyAttackState.Dash);
        
        //pick new position
        // REPETITIVE CODE....
        Vector3 startingPosition = transform.position;
        Vector3 endPosition = transform.position;
        if (toPlayer)
        {
            // get player position
            Vector3 playerPos = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0);

            // calculate new position near player
            Vector3 newPoint = playerPos + Random.onUnitSphere * dashToPlayerShift; // should be change so that its on a unit circle instead? y=0?
            Vector3 lineOfSight = newPoint - playerPos;
            
            // check we can see player from new position
            if (!Physics.Raycast(playerPos, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
            {
                endPosition = newPoint;
            }
        }
        else
        {
            // calculate new position to move to
            Vector3 newPoint = transform.position + Random.onUnitSphere * dashNearEnemyShift;
            Vector3 lineOfSight = newPoint - transform.position;

            // check we can see the new position
            if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
            {
                endPosition = newPoint;
            }
        }

        //lerp enemy to dashPosition
        for (float time=0; time<1; time += Time.deltaTime * dashSpeed)
        {
            transform.position = Vector3.Lerp(startingPosition, endPosition, time);
            yield return null;
        }

        enemy.ChangeAttackState(EnemyAttackState.Idle);
        agent.enabled = true;
        enemy.canMove = true;
        dashTimer = 0f;
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

        if (enemy.currentState == EnemyState.Chase)
        {
            PickSpotNearPlayer();
            Move();
        }
    }

    private void Engage()
    {
        agent.isStopped = false;
        enemy.ChangeState(EnemyState.Chase);
        disengageTimer = 0f;
    }

    private void PickSpotNearPlayer()
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
            followingPlayer = true;
            return;
        }

        // go to player or just get close
        float aggroValue = Random.value;
        if (aggroValue < followPercent)
        {
            followingPlayer = true;
        }
        else if (aggroValue < getClosePercent)
        {
            followingPlayer = false;

            // get player position
            Vector3 playerPos = enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0);

            // calculate new position near player
            float radius = Random.Range(minChaseShift, maxChaseShift);
            Vector3 newPoint = playerPos + Random.onUnitSphere * radius; // should be change so that its on a unit circle instead? y=0?
            Vector3 lineOfSight = newPoint - playerPos;

            // check we can see player from new position
            if (!Physics.Raycast(playerPos, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
            {
                agent.destination = newPoint;
            }
        }
        else
        {
            // REPETITIVE CODE....
            // calculate new position to move to
            float radius = Random.Range(minPatrolShift, maxPatrolShift);
            Vector3 newPoint = transform.position + Random.onUnitSphere * radius;
            Vector3 lineOfSight = newPoint - transform.position;

            // check we can see the new position
            if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
            {
                agent.destination = newPoint;
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
        // stop moving but don't stop agent so he can still patrol
        followingPlayer = false;
        agent.destination = transform.position;
        Move();

        enemy.ChangeState(EnemyState.Idle);
        head.rotation = transform.rotation;
        disengageTimer = 0f;
    }
#endregion

#region Patrol
    private void CheckPatrol()
    {
        if (enemy.canSeeTarget) { return;}

        // if we are not chasing the player
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

        // check we can see the new position
        if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
        {
            agent.destination = newPoint;
        }
    }
#endregion
}
