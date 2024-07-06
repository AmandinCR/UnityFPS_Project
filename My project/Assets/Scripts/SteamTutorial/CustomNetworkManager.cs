using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private PlayerObjectController gamePlayerPrefab;
    public List<PlayerObjectController> gamePlayers {get;} = new List<PlayerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby") 
        {
            PlayerObjectController playerInstance = Instantiate(gamePlayerPrefab);
            playerInstance.connectionID = conn.connectionId;
            playerInstance.playerIDnumber = gamePlayers.Count + 1;
            playerInstance.playerSteamID = (ulong) SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby2.Instance.CurrentLobbyID, gamePlayers.Count);
        
            NetworkServer.AddPlayerForConnection(conn, playerInstance.gameObject);
        }
        //base.OnServerAddPlayer(conn);
    }
}
