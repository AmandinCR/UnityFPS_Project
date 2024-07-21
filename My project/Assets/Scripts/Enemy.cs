using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressBar healthBar;
    [HideInInspector] public GameObject target;
    [HideInInspector] public EnemyManager enemyManager;

    [Header("States")]
    public EnemyState currentState;
    public EnemyAttackState currentAttackState;
    public bool canSeeTarget = false;
    public bool canMove = true;

    [Header("Parameters")]
    [SerializeField] private float maxHealth = 100f;
    public float health = 100f;
    [SerializeField] private float pointWorth = 1;
    public float playerHeight = 1f;

    private void Start()
    {
        currentState = EnemyState.Idle;
        currentAttackState = EnemyAttackState.Idle;
        health = maxHealth;
        SetupHealthBar();
    }

    [ServerCallback]
    public void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    [ServerCallback]
    public void ChangeAttackState(EnemyAttackState newState)
    {
        currentAttackState = newState;
    }

    private void SetupHealthBar()
    {
        CameraSetup mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraSetup>();
        healthBar.GetComponent<FaceCamera>().cam = mainCam.playerCam;
        healthBar.transform.SetParent(mainCam.canvas.transform);
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
        healthBar.SetDamage(damage, health / maxHealth);
        //healthBar.SetProgress(health / maxHealth);

        if (health <= 0.0f) 
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die() {
        if (isServer)
        {
            canMove = false;
        }
        //Destroy(healthBar.gameObject);
        
        yield return new WaitForSeconds(1f);

        if (isServer)
        {  
            enemyManager.enemiesAlive--;
            NetworkServer.Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        Destroy(healthBar.gameObject);
    }
    #endregion
}
