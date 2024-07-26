using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    private float damage;
    private float duration;
    private float laserTickRate;
    [SerializeField] private float maxLaserDistanceCheck;
    [SerializeField] private LayerMask laserMask;

    private void Start()
    {
        StartCoroutine(SelfDestruct());
        //InvokeRepeating("CheckDamage", laserTickRate, laserTickRate);
    }

    private void CheckDamage()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxLaserDistanceCheck, laserMask))
        {
            if (hit.transform.root.gameObject.layer == 6 || hit.transform.root.gameObject.layer == 7) // layer
            {
                if (hit.transform.tag == "HitBox") // hitbox
                {
                    // take damage only runs on local player
                    hit.transform.root.GetComponent<PlayerSetup>().TakeDamage(damage);
                }
            }
        }
    }

    public void SetProjectileData(float dam, float dur, float tick)
    {
        damage = dam;
        duration = dur;
        laserTickRate = tick;
        InvokeRepeating("CheckDamage", 0.0f, laserTickRate);
    }

    private IEnumerator SelfDestruct() 
    {
        yield return new WaitForSeconds(duration);
        Destroy(this.gameObject);
    }
}
