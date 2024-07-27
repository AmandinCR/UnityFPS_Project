using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{

    private float damage = 0f;
    private float spawnForce = 0f;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float timeToDestroy;
    [SerializeField] private GameObject meshes;
    [SerializeField] private float trailDelayTime = 0.1f;


    private void Start()
    {
        //Physics.IgnoreCollision(projectileCol, playerCol);
        rb.AddForce(transform.forward * spawnForce);
        StartCoroutine(SelfDestruct(timeToDestroy));
    }

    public void SetProjectileData(float dam, float spa)
    {
        damage = dam;
        spawnForce = spa;
    }

    // we hit a physical object
    private void OnCollisionEnter(Collision co)
    {
        if (co.transform.root.gameObject.layer == 6 || co.transform.root.gameObject.layer == 7) // layer
        {
            if (co.transform.tag != "HitBox") // hitbox
            {
                return;
            }
        }

        StopAllCoroutines();
        StartCoroutine(DestroyProjectile());
    }

    // we hit a player hitbox
    private void OnTriggerEnter(Collider co) 
    {
        if (co.transform.root.gameObject.layer == 6 || co.transform.root.gameObject.layer == 7) // layer
        {
            if (co.transform.tag == "HitBox") // hitbox
            {
                // take damage only runs on local player
                co.transform.root.GetComponent<PlayerSetup>().TakeDamage(damage);
            }
            else
            {
                return; // avoid other colliders
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
        yield return new WaitForSeconds(trailDelayTime);
        Destroy(this.gameObject);
    }
}
