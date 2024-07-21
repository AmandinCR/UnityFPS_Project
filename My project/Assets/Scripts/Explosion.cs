using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{  
    
    [SerializeField] private GameObject explosionVFX;
    [HideInInspector] public PlayerSetup owner;
    [SerializeField] private float timeToDestroy = 1.0f;

    public float damage = 0;

    private void Start() {
        StartCoroutine(SelfDestruct());
    }

    private void FixedUpdate() {
        GetComponent<Collider>().enabled=false;    
    }

    private IEnumerator SelfDestruct() {
        yield return new WaitForSeconds(timeToDestroy);
        Destroy(this.gameObject);
    }

    private void OnTriggerStay(Collider co) {
        if (damage != 0) {
            co.transform.root.GetComponent<Enemy>().TakeDamage(owner, damage);
        }
    }


}
