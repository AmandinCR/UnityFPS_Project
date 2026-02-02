using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Door : NetworkBehaviour
{
    [SerializeField] private int keyRequirement = 1;

    private void OnTriggerStay(Collider co)
    {
        if (Input.GetKey(KeyCode.E))
        {
            if (co.gameObject.layer == 6) // local player layer
            {
                if (co.GetComponent<PlayerInventory>().keys >= keyRequirement)
                {
                    OnDoorOpen(co);
                
                    // needs to NOT be run on every call since it's on GetKey not GetKeyDown
                    Destroy(this.gameObject);
                }
            }
        }
    }

    private void OnDoorOpen(Collider co)
    {
        co.GetComponent<PlayerInventory>().RemoveItem(keyRequirement);
    }
}
