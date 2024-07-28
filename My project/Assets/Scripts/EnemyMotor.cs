using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemyMotor : NetworkBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float farthestPlayerCheck = 100f;
    [SerializeField] private float followDistance = 15.0f;
    [SerializeField] private float findPlayersTime = 1.0f;
    private Enemy enemy;

    [SerializeField] private LayerMask chaseLineOfSightMask;
    [SerializeField] private float proximityChaseDistance = 4f;
    //[SerializeField] private float dotProdAngle = -1.0f;
    [SerializeField] public Transform head;
    public float headRotateSpeed = 0.05f;


    // SHOULD ONLY BE CALLED IN FIXEDUPDATE
    public void LookAtPosition(Vector3 pos)
    {
        Vector3 dir = pos - head.position;
        LookAtDirection(dir);
    }

    // SHOULD ONLY BE CALLED IN FIXEDUPDATE
    public void LookAtDirection(Vector3 dir)
    {
        dir = Vector3.RotateTowards(head.forward, dir, headRotateSpeed, 0f);
        head.LookAt(head.position + dir);
    }

    [ServerCallback]
    private void Start()
    {
        if (isServer) // just in case
        {
            enemy = GetComponent<Enemy>();
            SearchForPlayers();
        }
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        CheckCanSeeTarget();
    }

    private void CheckCanSeeTarget()
    {
        enemy.canSeeTarget = false;

        if (enemy.target == null)
        {
            return;
        }

        RaycastHit hit;
        Vector3 playerDirection = enemy.target.transform.position  + new Vector3(0,enemy.playerHeight,0) - transform.position;
        Physics.Raycast(transform.position, playerDirection, out hit, followDistance, chaseLineOfSightMask);
        //Debug.DrawRay(transform.position, playerDirection);
        
        if (hit.transform == null) { return; }
        
        // no walls between ai and player
        if (hit.transform.gameObject.layer == 6 || hit.transform.gameObject.layer == 7 || hit.transform.gameObject.layer == 9)
        {
            //canSeeTarget = Vector3.Dot(transform.forward, playerDirection.normalized) > dotProdAngle;
            enemy.canSeeTarget = true;
        }

        // if you are too close then enemy can see
        if (playerDirection.magnitude < proximityChaseDistance)
        {
            enemy.canSeeTarget = true;
        }
    }

    #region PlayerSearch
    public void SearchForPlayers()
    {
        InvokeRepeating("FindTarget", findPlayersTime, findPlayersTime);
    }

    public void StopSearchForPlayers()
    {
        CancelInvoke("FindTarget");
    }

    [ServerCallback]
    private void FindTarget() 
    {
        float closestDistance = Mathf.Infinity;
        enemy.target = null;
        foreach (PlayerSetup player in PlayerSetup.playerList) 
        {
            float distanceToTarget = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToTarget < closestDistance) 
            {
                if (distanceToTarget <= farthestPlayerCheck)
                {
                    enemy.target = player.gameObject;
                    closestDistance = distanceToTarget;
                }
            }
        }
    }
    #endregion
}
