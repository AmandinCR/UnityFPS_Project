using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using TMPro;

public class PlayerInventory : NetworkBehaviour
{
    public int keys = 0;
    private TextMeshProUGUI keyText;
    private GameObject cam;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera");
        keyText = cam.GetComponent<CameraSetup>().keyText;
    }

    public void AddItem(int key) 
    {
        CmdAddItems(key);
    }

    [Command(requiresAuthority = false)]
    private void CmdAddItems(int key) 
    {
        RpcAddItems(key);
    }

    [ClientRpc]
    private void RpcAddItems(int key) 
    {
        keys += key;

        // this function is already only called by local player because we check with layers but
        // for future proof this might be good
        if (isLocalPlayer)
        {
            UpdateKeyText();
        }
    }

    public void RemoveItem(int key) 
    {
        

        CmdRemoveItems(key);
    }

    [Command(requiresAuthority = false)]
    private void CmdRemoveItems(int key) 
    {
        RpcRemoveItems(key);
    }

    [ClientRpc]
    private void RpcRemoveItems(int key) 
    {
        keys -= key;

        // this function is already only called by local player because we check with layers but
        // for future proof this might be good
        if (isLocalPlayer)
        {
            UpdateKeyText();
        }
    }

    private void UpdateKeyText()
    {
        keyText.text = keys.ToString();
    }
}
