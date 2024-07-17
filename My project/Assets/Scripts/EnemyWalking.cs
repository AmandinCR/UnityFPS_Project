using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyWalking : NetworkBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] private float disengageTime = 1.0f;
    [SerializeField] private Transform head;
    private float disengageTimer = 0f;
    private Enemy enemy;
    public bool canMove = true;

    [ServerCallback]
    void Start()
    {
        if (isServer) // just in case
        {
            agent = GetComponent<NavMeshAgent>();
            enemy = GetComponent<Enemy>();
        }
    }

    [ServerCallback]
    void FixedUpdate()
    {
        if (canMove)
        {
            CheckEngage();
            CheckDisengage();
            LookAtTarget();
        }
        else
        {
            agent.isStopped = true;
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
            Move();
        }
        
    }

    private void LookAtTarget()
    {
        if (enemy.target == null) { return; }

        if (enemy.currentState == EnemyState.Chase)
        {
            head.LookAt(enemy.target.transform.position);
        }
    }

    private void Move()
    {
        agent.destination = enemy.target.transform.position;
    }

    private void Engage()
    {
        agent.isStopped = false;
        enemy.ChangeState(EnemyState.Chase);
        disengageTimer = 0f;
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
        agent.isStopped = true;
        enemy.ChangeState(EnemyState.Idle);
        head.rotation = transform.rotation;
        disengageTimer = 0f;
    }
}
