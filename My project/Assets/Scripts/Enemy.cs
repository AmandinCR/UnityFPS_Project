using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private ProgressBar healthBar;
    [SerializeField] private float maxHealth = 100f;
    private float health = 100f;
    [SerializeField] private float pointWorth = 1;
    [HideInInspector] public GameObject target;
    public bool canSeeTarget = false;
    public EnemyState currentState;
    [SerializeField] private EnemyState initState = EnemyState.Idle;
    [HideInInspector] public EnemyManager enemyManager;
    public float playerHeight = 1f;

    private void Start() 
    {
        currentState = initState;
        health = maxHealth;
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == EnemyState.Attack && newState == EnemyState.Chase) 
        { 
            return; // don't allow to move and attack
        }

        currentState = newState;
    }

    public void SetupHealthBar(Canvas canvas, Camera cam)
    {
        healthBar.transform.SetParent(canvas.transform);
        healthBar.GetComponent<FaceCamera>().Camera = cam;
    }

    #region Damaged
    public void TakeDamage(PlayerSetup player, float damage) 
    {
        if (health <= 0.0f) { return;}

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
        healthBar.SetProgress(health / maxHealth, 3);

        if (health <= 0.0f) 
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die() {
        Destroy(healthBar.gameObject);

        if (isServer)
        {  
            GetComponent<EnemyWalking>().canMove = false;
            yield return new WaitForSeconds(1f);
            enemyManager.enemiesAlive--;
            NetworkServer.Destroy(this.gameObject);
        }
    }
    #endregion
}
