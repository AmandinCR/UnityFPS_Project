using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;

public class CustomProjectile : MonoBehaviour
{
#region Variables
    [SerializeField] private LayerMask raycastLayerMask;
    [SerializeField] private bool fixedUpdateVisual = false;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GameObject meshes;
    
    [Header("Bullet Properties")]
    [SerializeField] private float selfDestructTime = 5f;
    [SerializeField] private float bulletSpeed = 1f;
    private float damage;
    private PlayerItems items;
    private Vector3 camForward;
    private RaycastHit projectileHit;
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public PlayerSetup owner;

#endregion

    public void SetProjectileData(float dam, Vector3 trueDirection, PlayerItems itemData)
    {
        damage = dam;
        items = itemData;
        camForward = trueDirection;
    }

    // Gets called when the projectile hits anything in raycastLayerMask
    private void OnHit()
    {
        if (projectileHit.transform.root.gameObject.layer == 8) // enemy layer
        {
            OnEnemyHit();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(DestroyProjectile());
        }
    }

    private void OnEnemyHit() 
    {
        // only deal damage locally
        if (isLocalPlayer)
        {
            projectileHit.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
        }
        CheckPierce();
    }

#region Item Effects
    private int pierceCount = 0;
    private void CheckPierce()
    {
        pierceCount++;
        transform.LookAt(transform.position + camForward, transform.up); // too lazy to find the right function
        if (pierceCount > items.penetrators) {
            StopAllCoroutines();
            StartCoroutine(DestroyProjectile());
        }
        else
        {
            hitSomething = false;
        }
    }
#endregion

    private IEnumerator DestroyProjectile()
    {
        meshes.SetActive(false);
        yield return new WaitForSeconds(trailRenderer.time);
        Destroy(this.gameObject);
    }

    private IEnumerator SelfDestruct() 
    {
        yield return new WaitForSeconds(selfDestructTime);
        StartCoroutine(DestroyProjectile());
    }
    
#region Projectile Collision and Movement
    private bool hitSomething = false;
    private float timeLeft;
    private Vector3 start;
    private Vector3 end;
    private bool shotStarted = false;
    private void Start()
    {
        StartCoroutine(SelfDestruct());
    }

    private void FixedUpdate()
    {
        if (hitSomething) {return;}

        // check in front of the bullet for collisions
        float raycastDistance = 2 * bulletSpeed * Time.fixedDeltaTime;
        float moveDistance = bulletSpeed * Time.fixedDeltaTime;
        // check behind if we missed something
        if (Physics.Raycast(transform.position, transform.forward, out projectileHit, raycastDistance, raycastLayerMask))
        {
            hitSomething = true;
        }

        // move projectile in update or fixedupdate
        if (fixedUpdateVisual)
        {
            if (hitSomething)
            {
                transform.position = projectileHit.point;
            }
            else
            {
                transform.position += transform.forward * moveDistance;
            }
        }
        else
        {
            timeLeft = Time.fixedDeltaTime;
            start = transform.position;
            shotStarted = true;
            if (hitSomething)
            {
                end = projectileHit.point;
            }
            else
            {
                end = transform.position + transform.forward * moveDistance;
            }
            
        }
        
        if (hitSomething)
        {
            OnHit();
        }
    }

    private void Update()
    {
        if (fixedUpdateVisual) {return;}
        if (hitSomething) {return;}
        if (!shotStarted) {return;}

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0)
        {
            timeLeft = 0;
        }
        transform.position = Vector3.Lerp(start, end, 1 - timeLeft);
    }
#endregion
}
