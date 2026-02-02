using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Key : NetworkBehaviour
{
    [SerializeField] private int keyAmount = 1;

    private void OnTriggerStay(Collider co)
    {
        if (Input.GetKey(KeyCode.E))
        {
            if (co.gameObject.layer == 6) // local player layer
            {
                OnItemPickup(co);
                
                // needs to NOT be run on every call since it's on GetKey not GetKeyDown
                Destroy(this.gameObject);
            }
        }
    }

    private void OnItemPickup(Collider co)
    {
        co.GetComponent<PlayerInventory>().AddItem(keyAmount);
    }
}


