using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);

    private const int MAX_PLAYERS = 2;

    [SerializeField] private GameObject ticTacToeBoardPrefab;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInputField;

    private GameObject newTicTacToeBoard;

    private async void Start() 
    {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log(clientId + " joined.");

        if(NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == MAX_PLAYERS)
        {
            SpawnTicTacToeBoard();
        }
    }

    private void SpawnTicTacToeBoard()
    {
        newTicTacToeBoard = Instantiate(ticTacToeBoardPrefab);
        newTicTacToeBoard.GetComponent<NetworkObject>().Spawn();
    }

    private void Awake() 
    {
        Instance = this;
    }

    public async void StartHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = joinCode;

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
        
    }

    public async void StartClient()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInputField.text);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void ShowMessage(string message)
    {
        if(message.Equals("Won"))
        {
            resultText.text = "YOU WON!";
            resultPanel.SetActive(true);
            //Show panel with text that Opponent won
            ShowOpponentMessage("YOU LOSE!");
        }
        else if(message.Equals("Draw"))
        {
            resultText.text = "DRAW!";
            resultPanel.SetActive(true);
            ShowOpponentMessage("DRAW!");
        }
    }

    private void ShowOpponentMessage(string message)
    {
        if(IsHost)
        {
            OpponentMessageClientRpc(message);
        }
        else
        {
            OpponentMessageServerRpc(message);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpponentMessageServerRpc(string message)
    {
        resultText.text = message;
        resultPanel.SetActive(true);
    }

    [ClientRpc]
    private void OpponentMessageClientRpc(string message)
    {   
        if(IsHost) return;

        resultText.text = message;
        resultPanel.SetActive(true);
    }

    public void OnPlayAgainButtonClick()
    {
        if(!IsHost)
        {
            OnPlayAgainButtonClickServerRpc();
            resultPanel.SetActive(false);
        }
        else
        {
            Destroy(newTicTacToeBoard);
            SpawnTicTacToeBoard();
            OnPlayAgainButtonClickClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnPlayAgainButtonClickServerRpc()
    {
        Destroy(newTicTacToeBoard);
        SpawnTicTacToeBoard();
        resultPanel.SetActive(false);
    }

    [ClientRpc]
    private void OnPlayAgainButtonClickClientRpc()
    {
        resultPanel.SetActive(false);
    }
}
