using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyWalking : NetworkBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private float minGetCloseToPlayerDistance;
    [SerializeField] private float chanceToFollow = 0.5f; // 0 to 1 float
    [SerializeField] private float chanceToGetClose = 0.3f; // 0 to 1 float
    [SerializeField] private float chanceToStayAway = 0.3f; // 0 to 1 float
    [SerializeField] private float minNearPlayerRadius = 3f;
    [SerializeField] private float maxNearPlayerRadius = 6f;
    [SerializeField] private float minNearEnemyRadius = 3f;
    [SerializeField] private float maxNearEnemyRadius = 6f;
    [SerializeField] private float pickChaseSpotCooldown = 10f;
    public bool canRetreatDash = true;
    public bool canAttackDash = true;

    [Header("References")]
    private Enemy enemy;
    private NavMeshAgent agent;
    private EnemyMotor motor;

    [Header("Other")]
    [SerializeField] private LayerMask walkLineOfSightMask;
    [SerializeField] private float navMeshRadiusCheck = 2.0f;

    [Header("Disengage")]
    [SerializeField] private float disengageTime = 1.0f;
    private float disengageTimer;

    [Header("Chase")]
    private float pickSpotTimer;
    private bool followingPlayer = false;

    [Header("Patrol")]
    [SerializeField] private bool canPatrol = false;
    [SerializeField] private float pickPatrolSpotCooldown = 10f;

    [Header("Dash")]
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
            motor = GetComponent<EnemyMotor>();
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
            if (canPatrol)
                CheckPatrol();
            CheckDash();
        }
        else
        {
            if (agent.enabled)
                agent.isStopped = true;
        }
        UpdateTimers();
        //if (enemy.currentAttackState == EnemyAttackState.Idle)
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
            
            agent.enabled = false;
            enemy.canMove = false;
            enemy.ChangeAttackState(EnemyAttackState.Dash);

            // lerp enemy to dashPosition
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
    }
#endregion

#region Engage
    private void FollowPlayer()
    {
        // destination is set manually elsewhere unless following player for optimization
        if (followingPlayer)
        {
            if (agent.enabled)
                agent.destination = enemy.target.transform.position;
        }
    }

    private void CheckEngage()
    {
        if (enemy.target == null) { return; }

        if (enemy.canSeeTarget)
        {
            Engage();
        }

        if (enemy.currentState == EnemyState.Chase)
        {
            ApproachPlayer();
            FollowPlayer();
        }
    }

    private void Engage()
    {
        if (agent.enabled)
            agent.isStopped = false;
        disengageTimer = 0f;
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
        if (playerDistance.magnitude > minGetCloseToPlayerDistance)
        {
            float radius = Random.Range(minNearPlayerRadius, maxNearPlayerRadius);
            if (agent.enabled)
                agent.destination = PickSpotNearPlayer(radius);
            return;
        }

        // movement options
        // 1. follow player
        // 2. move close to player
        // 3. move close to ourselves
        float aggroValue = Random.value;
        if (aggroValue < chanceToFollow)
        {
            followingPlayer = true;
        }
        else if (aggroValue < chanceToFollow + chanceToGetClose)
        {
            followingPlayer = false;
            float radius = Random.Range(minNearPlayerRadius, maxNearPlayerRadius);
            if (agent.enabled)
                agent.destination = PickSpotNearPlayer(radius);
        }
        else if (aggroValue < chanceToFollow + chanceToGetClose + chanceToStayAway)
        {
            float radius = Random.Range(minNearEnemyRadius, maxNearEnemyRadius);
            if (agent.enabled)
                agent.destination = PickSpotNearEnemy(radius);
        }
        else
        {
            return;
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
        // stop moving but don't stop agent so he can still patrol
        followingPlayer = false;
        if (agent.enabled)
            agent.destination = transform.position;

        enemy.ChangeState(EnemyState.Idle);
        disengageTimer = 0f;
    }
#endregion

#region Patrol
    private void CheckPatrol()
    {
        //if (enemy.canSeeTarget) { return;}

        // if we are not chasing the player
        if (enemy.currentState == EnemyState.Idle)
        {
            enemy.ChangeState(EnemyState.Patrol);
        }

        if (enemy.currentState == EnemyState.Patrol)
        {
            Patrol();
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
        float radius = Random.Range(minNearEnemyRadius, maxNearEnemyRadius);
        if (agent.enabled)
            agent.destination = PickSpotNearEnemy(radius);
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

        // check we can see player from new position
        if (!Physics.Raycast(playerPos, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPoint, out hit, navMeshRadiusCheck, NavMesh.AllAreas))
            {
                return hit.position;
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

        // check we can see the new position
        if (!Physics.Raycast(transform.position, lineOfSight, lineOfSight.magnitude, walkLineOfSightMask))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPoint, out hit, navMeshRadiusCheck, NavMesh.AllAreas))
            {
                return hit.position;
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
