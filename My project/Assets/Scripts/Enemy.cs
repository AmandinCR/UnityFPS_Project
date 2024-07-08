using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    public float health = 100f;
    public float pointWorth = 1;
    public GameObject target;
    public bool canSeeTarget = false;
    public EnemyState currentState;
    public EnemyState initState = EnemyState.Idle;
    public EnemyManager enemyManager;
    public float playerHeight = 1f;

    private void Start() 
    {
        currentState = initState;
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == EnemyState.Attack && newState == EnemyState.Chase) 
        { 
            return; // don't allow to move and attack
        }

        currentState = newState;
    }

    #region Damaged
    public void TakeDamage(PlayerSetup player, float damage) 
    {
        if (health - damage <= 0.0f) 
        {
            player.points += pointWorth;
        }
        CmdTakeDamage(damage);
    }

    [Command(requiresAuthority = false)] 
    private void CmdTakeDamage(float damage) 
    {
        RemoveHealth(damage);
        RpcTakeDamage(damage);

        if (health <= 0.0f) 
        {
            StartCoroutine(Die());
        }

        // start chasing player if damaged
        ChangeState(EnemyState.Chase);
    }

    [ClientRpc] 
    private void RpcTakeDamage(float damage) 
    {
        // host is a client and server (so don't double damage)
        if (!isServer) 
        {
            RemoveHealth(damage);
        }
    }

    private void RemoveHealth(float damage)
    {
        health -= damage;
    }

    private IEnumerator Die() {
        if (isServer) // just in case
        {  
            yield return new WaitForSeconds(1f);
            enemyManager.enemiesAlive--;
            NetworkServer.Destroy(this.gameObject);
        }
    }


    #endregion
}
