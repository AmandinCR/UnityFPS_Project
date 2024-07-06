using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.Billiards;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public PlayerSetup owner;
    private float damage = 0f;
    private float spawnForce = 0f;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider projectileCol;
    [HideInInspector] public Collider playerCol;
    [SerializeField] private float timeToDestroy;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GameObject meshes;

    private void Awake() 
    {
        projectileCol.enabled = false;
    }

    private void Start()
    {
        Physics.IgnoreCollision(projectileCol, playerCol);
        projectileCol.enabled = true;
        rb.AddForce(transform.forward * spawnForce);
        StartCoroutine(SelfDestruct(timeToDestroy));
    }

    public void SetProjectileData(float dam, float spa, bool grav)
    {
        damage = dam;
        spawnForce = spa;
        rb.useGravity = grav;
    }

    private void OnCollisionEnter(Collision co) 
    {
        if (isLocalPlayer) // deal damage locally
        {
            if (co.transform.root.gameObject.layer == 8) // enemy layer
            {
                if (co.transform.tag == "HitBox") // enemy hitbox
                {
                    co.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
                }
                else
                {
                    return; // avoid enemy locomotion collider
                }
            }
        }
        
        StopAllCoroutines();
        StartCoroutine(DestroyProjectile());
    }

    private IEnumerator SelfDestruct(float time) 
    {
        yield return new WaitForSeconds(time);
        StartCoroutine(DestroyProjectile());
    }

    private IEnumerator DestroyProjectile()
    {
        rb.velocity = Vector3.zero;
        meshes.SetActive(false);
        yield return new WaitForSeconds(2 * trailRenderer.time);
        Destroy(this.gameObject);
    }
}
