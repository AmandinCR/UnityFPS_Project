using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomProjectileV2 : MonoBehaviour
{
    [SerializeField] private LayerMask raycastLayerMask;
    [SerializeField] private bool fixedUpdateVisual = false;
    [SerializeField] private GameObject meshes;

    [Header("Hit Effect")]
    [SerializeField] private ParticleSystem hitEffect;
    
    [Header("Bullet Properties")]
    [SerializeField] private float selfDestructTime = 5f;
    [SerializeField] private float bulletSpeed = 1f;
    [SerializeField] private float delayDestroyTime = 0.1f;
    
    private float damage;
    private RaycastHit projectileHit;
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public PlayerSetup owner;

    private bool hitSomething = false;
    private float timeLeft;
    private Vector3 start;
    private Vector3 end;
    private bool shotStarted = false;

    public void SetProjectileData(float dam)
    {
        damage = dam;
    }

    // Gets called when the projectile hits anything in raycastLayerMask
    private void OnHit()
    {
        // play the on hit vfx
        hitEffect.Play();

        if (projectileHit.transform.root.gameObject.layer == 8) // enemy layer
        {
            OnEnemyHit();
        } 
        else 
        {
            OnWallHit();
        }
        StopAllCoroutines();
        StartCoroutine(DestroyProjectile());
    }

    private void OnEnemyHit() 
    {
        // only deal damage locally
        if (isLocalPlayer)
        {
            projectileHit.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
        }
    }

    private void OnWallHit() 
    {
        return;
    }

    public IEnumerator DestroyProjectile()
    {
        meshes.SetActive(false);
        yield return new WaitForSeconds(delayDestroyTime);
        Destroy(this.gameObject);
    }

    private IEnumerator SelfDestruct() 
    {
        yield return new WaitForSeconds(selfDestructTime);
        StartCoroutine(DestroyProjectile());
    }
    

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
                end = projectileHit.point + transform.forward;
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
}
