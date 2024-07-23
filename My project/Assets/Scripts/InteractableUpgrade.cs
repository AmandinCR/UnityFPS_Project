using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class InteractableUpgrade : MonoBehaviour
{
    [SerializeField] private ItemTypes type;
    [SerializeField] private ItemScriptableObject[] items = new ItemScriptableObject[10];
    [SerializeField] private MeshRenderer meshRend;

    private ItemScriptableObject item;
    private void Start() {
        for (int i = 0; i < items.Length; i++) {
            if (items[i].type == type) {
                item = items[i];
                break;
            }
        }
        meshRend.material = item.matt;
    }

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
        co.GetComponent<PlayerItems>().ChangeItems(type);
    }
}
