using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Steamworks;

public class PlayerObjectController : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int connectionID;
    [SyncVar] public int playerIDnumber;
    [SyncVar] public ulong playerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string playerName;

    private CustomNetworkManager manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    public override void OnStartAuthority()
    {
        Debug.Log("here?");
        CmdSetPlayerName(SteamFriends.GetPersonaName());
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        Debug.Log("here?");
        Manager.gamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.gamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command] private void CmdSetPlayerName(string playerName)
    {
        this.PlayerNameUpdate(this.playerName, playerName);
    }

    private void PlayerNameUpdate(string oldValue, string newValue) 
    {
        if (isServer) //host
        {
            this.playerName = newValue;
        }

        if (isClient) //client
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }
}
