using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.Billiards;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using Unity.Mathematics;

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

    private Vector3 camForward;
    private PlayerItems items;

    private int pierceCount = 0;

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

    public void SetProjectileData(float dam, float spa, bool grav, Vector3 trueDirection, PlayerItems itemData)
    {
        damage = dam;
        spawnForce = spa;
        rb.useGravity = grav;
        items = itemData;
        camForward = trueDirection;
    }

    private void OnTriggerEnter(Collider co) 
    {
        CheckDamage(co);
        CheckPiercing(co);
    }

    private void CheckDamage(Collider co) 
    {
        if (co.transform.root.gameObject.layer == 8) // enemy layer
        {
            if (isLocalPlayer) 
            {
                if (co.transform.tag == "HitBox") // enemy hitbox
                {
                    co.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
                }
            }
        } 
    }

    private void CheckPiercing(Collider co) 
    {
        if (co.transform.root.gameObject.layer == 8) // enemy layer
        {
            pierceCount++;
            rb.velocity = Vector3.zero;
            rb.AddForce(camForward * spawnForce);
            //Debug.Log(pierceCount);
        } 
        
        if (pierceCount > items.penetrators) {
            StopAllCoroutines();
            StartCoroutine(DestroyProjectile());
        }
    }

    private void OnCollisionEnter(Collision co) 
    {
        // Debug.Log(co.gameObject.name);
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
        yield return new WaitForSeconds(trailRenderer.time);
        Destroy(this.gameObject);
    }
}
