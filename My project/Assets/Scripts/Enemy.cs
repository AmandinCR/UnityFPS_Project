using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private float maxHealth = 100f;

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
    [SerializeField] private float deathDelayTime = 1f;
    [HideInInspector] public float health;
    public float playerHeight = 1f;

    private void Start()
    {
        currentState = EnemyState.Idle;
        currentAttackState = EnemyAttackState.Idle;
        health = maxHealth;
        SetupHealthBar();
        SetLayerAllChildren(this.transform);
    }

    private void SetLayerAllChildren(Transform root)
    {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children)
        {
            // enemy layer i hope...
            child.gameObject.layer = 8;
        }
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
        CameraSetup mainCam = Camera.main.GetComponent<CameraSetup>();
        healthBar.transform.SetParent(mainCam.canvas.transform);
        //healthBar.GetComponent<FaceCamera>().cam = mainCam.playerCam;
        //healthBar.player = PlayerSetup.localPlayer.transform;
    }

    #region Damaged
    public void TakeDamage(PlayerSetup player, float damage) 
    {
        if (health <= 0.0f) { return;}
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
        
        yield return new WaitForSeconds(deathDelayTime);

        if (isServer)
        {  
            enemyManager.enemiesAlive--;
            NetworkServer.Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (healthBar != null) // just in case i guess
        {
            Destroy(healthBar.gameObject);
        }
    }
    #endregion
}
