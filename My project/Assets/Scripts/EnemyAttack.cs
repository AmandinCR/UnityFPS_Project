using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemyAttack : NetworkBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool stopToAttack = false;

    [Header("Parameters")]
    [SerializeField] private float damage;
    [SerializeField] private float cooldown;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackDuration;
    private float attackTimer = 0f;
    private Enemy enemy;

    [Header("Slash")]
    [SerializeField] private ParticleSystem slashEffect;

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
        if (canAttack)
            CheckAttack();
    }

    // RUNS ONLY ON SERVER
    private void CheckAttack()
    {
        if (attackTimer <= 0f) 
        {
            if (enemy.currentAttackState == EnemyAttackState.Idle)
            {
                if (enemy.target != null) 
                {
                    float distanceToTarget = Vector3.Distance(transform.position, enemy.target.transform.position);
                    if (distanceToTarget <= attackRange) {
                        StartCoroutine(ServerAttack(enemy.target));
                    }
                }
            }
        }
        else
        {
            attackTimer -= Time.deltaTime;
        }
    }

    // RUNS ONLY ON SERVER
    private IEnumerator ServerAttack(GameObject target) 
    {
        attackTimer = cooldown;
        enemy.ChangeAttackState(EnemyAttackState.Attack);
        RpcAttack(target);
        if (stopToAttack)
        {
            enemy.canMove = false;
        }

        yield return new WaitForSeconds(attackDuration);

        enemy.ChangeAttackState(EnemyAttackState.Idle);
        enemy.canMove = true;
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
        slashEffect.Play();

        // TakeDamage only runs on the local player
        target.GetComponent<PlayerSetup>().TakeDamage(damage);
    }
}
