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
    private Vector3 walkLocation;

    [Header("Other")]
    [SerializeField] private LayerMask walkLineOfSightMask;

    [Header("Disengage")]
    [SerializeField] private float disengageTime = 1.0f;
    private float disengageTimer = 0f;

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

    [ServerCallback]
    void Start()
    {
        if (isServer) // just in case
        {
            agent = GetComponent<NavMeshAgent>();
            enemy = GetComponent<Enemy>();
            pickSpotTimer = pickPatrolSpotCooldown;
            walkLocation = transform.position;
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
        }
        else
        {
            agent.isStopped = true;
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
            head.LookAt(enemy.target.transform.position + new Vector3(0,enemy.playerHeight,0));
        }
    }

    private void Move()
    {
        if (followingPlayer)
        {
            walkLocation = enemy.target.transform.position;
        }
        agent.destination = walkLocation;
    }

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
    
    [SerializeField] private float minFollowPlayerDistance;


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
                walkLocation = newPoint;
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
                walkLocation = newPoint;
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
        walkLocation = transform.position;
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
            walkLocation = newPoint;
        }
    }
#endregion
}
