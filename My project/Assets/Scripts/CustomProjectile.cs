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
    private Vector3 path;
    private RaycastHit projectileHit;
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public PlayerSetup owner;
    private int pierceCount;
    private float cameraShift;
    private int bounceCount;

#endregion

    public void SetProjectileData(float dam, Vector3 trueDirection, PlayerItems itemData, float camShift, int pierceCountStart = 0, int bounceCountStart = 0)
    {
        initialDamage = dam;
        damage = initialDamage;
        items = itemData;
        path = trueDirection;
        cameraShift = camShift;
        pierceCount = pierceCountStart;
        bounceCount = bounceCountStart;
    }

    // Gets called when the projectile hits anything in raycastLayerMask
    private void OnHit()
    {
        CheckExplosion();
        if (projectileHit.transform.root.gameObject.layer == 8) // enemy layer
        {
            OnEnemyHit();
        }
        // else
        // {
        //     StopAllCoroutines();
        //     StartCoroutine(DestroyProjectile());
        // }
        else {
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
        
    }

#region Item Effects
    
    [SerializeField] private float penetrationShift = 0.5f;
    [SerializeField] private float pierceDamageMultiplier = 0.5f;
    [SerializeField] private GameObject customProjectilePrefab;
    private void CheckPierce()
    {
        pierceCount++;
        damage = damage + initialDamage*pierceDamageMultiplier;
        if (pierceCount <= items.penetrators) 
        {
            transform.position += transform.forward * penetrationShift;
            for (int i = 0; i < items.splitters+1; i++) {
                Vector3 newPath = path.normalized-transform.right*cameraShift*i + transform.right*cameraShift*items.splitters/2;
                Quaternion rot = Quaternion.LookRotation(newPath, Vector3.up);
                GameObject vfx = Instantiate(customProjectilePrefab, transform.position, rot);
                CustomProjectile projectile = vfx.GetComponent<CustomProjectile>();
                projectile.isLocalPlayer = isLocalPlayer;
                projectile.owner = owner;
                projectile.SetProjectileData(damage, newPath, items, cameraShift, pierceCountStart: pierceCount);
            }
        }
        // StopAllCoroutines();
        // StartCoroutine(DestroyProjectile());
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
