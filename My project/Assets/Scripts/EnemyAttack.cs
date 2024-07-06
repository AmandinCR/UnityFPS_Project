using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemyAttack : NetworkBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private float cooldown;
    [SerializeField] private float attackRange;
    private float attackTimer = 0f;
    private Enemy enemy;

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    // RUNS ONLY ON SERVER
    [ServerCallback]
    private void FixedUpdate() 
    {
        CheckAttack();
    }

    // RUNS ONLY ON SERVER
    private void CheckAttack()
    {
        if (attackTimer <= 0f) 
        {
            if (enemy.currentState == EnemyState.Attack)
            {
                enemy.ChangeState(EnemyState.Idle);
            }

            if (enemy.target != null) {
                float distanceToTarget = Vector3.Distance(transform.position, enemy.target.transform.position);
                if (distanceToTarget <= attackRange) {
                    ServerAttack(enemy.target);
                }
            }
        }
        else
        {
            attackTimer -= Time.deltaTime;
        }
    }

    // RUNS ONLY ON SERVER
    private void ServerAttack(GameObject target) 
    {
        attackTimer = cooldown;
        enemy.ChangeState(EnemyState.Attack);
        RpcAttack(target);
    }

    // RUNS ONLY ON CLIENTS
    [ClientRpc]
    private void RpcAttack(GameObject target)
    {
        Attack(target);
    }
    
    // RUNS ONLY ON CLIENTS
    private void Attack(GameObject target)
    {
        // TakeDamage only runs on the local player
        target.GetComponent<PlayerSetup>().TakeDamage(damage);
    }
}
