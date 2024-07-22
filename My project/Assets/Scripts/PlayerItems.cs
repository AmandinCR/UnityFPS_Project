using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerItems : NetworkBehaviour
{
    public int penetrators = 0;
    public int exploders = 0;
    public int splitters = 0;

    public void ChangeItems(ItemTypes item) {
        CmdChangeItems(item);
    }

    [Command(requiresAuthority = false)]
    private void CmdChangeItems(ItemTypes item) {
        RpcChangeItems(item);
    }

    [ClientRpc]
    private void RpcChangeItems(ItemTypes item) {
        if (item == ItemTypes.penetrators) {
            penetrators++;
        } else if (item == ItemTypes.exploders) {
            exploders++;
        } else if (item == ItemTypes.splitters) {
            splitters++;
        }
    }

}
