using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Accessibility;

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
    
    private float initialDamage;
    private float damage;
    private PlayerItems items;
    private Vector3 bulletForward;
    private RaycastHit projectileHit;
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public PlayerSetup owner;
    [SerializeField] private GameObject customProjectilePrefab;
    private int pierceCount;
    private float splitShift;
    private int bounceCount;

#endregion

    public void SetProjectileData(float dam, Vector3 trueDirection, PlayerItems itemData, float camShift, int pierceCountStart = 0, int bounceCountStart = 0)
    {
        initialDamage = dam;
        damage = initialDamage;
        items = itemData;
        bulletForward = trueDirection;
        splitShift = camShift;
        pierceCount = pierceCountStart;
        bounceCount = bounceCountStart;
    }

    // Gets called when the projectile hits anything in raycastLayerMask
    private void OnHit()
    {
        CalculateDamage();
        CheckExplosion();
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
        CheckPierce();
    }

    private void OnWallHit() {
        CheckBounce();
    }

    private void CreateBullet(Vector3 foward, float newInitialDamage) {
        Quaternion rot = Quaternion.LookRotation(foward, Vector3.up);
        GameObject vfx = Instantiate(customProjectilePrefab, transform.position, rot);
        CustomProjectile projectile = vfx.GetComponent<CustomProjectile>();
        projectile.isLocalPlayer = isLocalPlayer;
        projectile.owner = owner;
        projectile.SetProjectileData(newInitialDamage, foward, items, splitShift, pierceCountStart: pierceCount, bounceCountStart: bounceCount);

    }


#region Item Effects

    [SerializeField] private float pierceDamageMultiplier = 0.5f;
    [SerializeField] private float bounceDamageMultiplier = 0.0f;
    private void CalculateDamage() {
        damage = initialDamage;
        damage *= 1 + pierceCount*pierceDamageMultiplier; 
        damage *= 1 + bounceCount*bounceDamageMultiplier;
    }
    

    private void CheckBounce() {
        bounceCount++;
        if (bounceCount <= items.bouncers)
        {
            Vector3 bouncedForward = Vector3.Reflect(bulletForward,projectileHit.normal);
            CheckSplits(bouncedForward);
            //CreateBullet(bouncedForward);
        }
    }
    [SerializeField] private float penetrationShift = 0.5f;
    
    private void CheckPierce()
    {
        pierceCount++;
        if (pierceCount <= items.penetrators) 
        {
            transform.position += transform.forward * penetrationShift;
            CheckSplits(bulletForward);
        }
    }

    private int splitAmount = 2;
    [SerializeField] private float splitDamageMultiplier = 0.4f;
    private void CheckSplits(Vector3 forward) {
        if (pierceCount + bounceCount <= splitAmount) 
        {
            // Creates a pool of damage scaling up by splitDamageMultiplier then distributes it equally across each bullet
            float splitInitialDamage = initialDamage * (1+splitDamageMultiplier*items.splitters) / items.splitters;    
            
            for (int i = 0; i < items.splitters+1; i++)                                                                 
            {
                Vector3 newPath = forward.normalized-transform.right*splitShift*i + transform.right*splitShift*items.splitters/2;
                CreateBullet(newPath, splitInitialDamage);
            }
        }
        else 
        {
            CreateBullet(forward, initialDamage);
        }

    }

    [SerializeField] private float explosionDamageMultiplier = 0.5f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionRadiusMultiplier = 1;
    private void CheckExplosion() {
        
        if (items.exploders > 0) {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = (1 + (items.exploders-1)*explosionRadiusMultiplier)*Vector3.one;
            Explosion myExplosion = explosion.GetComponent<Explosion>();
            myExplosion.damage = damage*explosionDamageMultiplier;
            myExplosion.owner = owner;
        }
    }

#endregion

    public IEnumerator DestroyProjectile()
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
#endregion
}
