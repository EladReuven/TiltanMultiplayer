using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OnlineGameManager : MonoBehaviourPunCallbacks
{
    public const string NETWORK_PLAYER_PREFAB_NAME = "NetworkPlayerObject";

    private const string GAME_STARTED_RPC = nameof(GameStarted);
    private const string COUNTDOWN_STARTED_RPC = nameof(CountdownStarted);
    private const string ASK_FOR_RANDOM_SPAWN_POINT_RPC = nameof(AskForRandomSpawnPoint);
    private const string SPAWN_PLAYER_CLIENT_RPC = nameof(SpawnPlayer);
    
    public bool hasGameStarted = false;

    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Button startGameButtonUI;
    [SerializeField] private SpawnPoint[] spawnPoints;

    private PlayerController localPlayerController;

    private bool isCountingForStartGame;
    private float timeLeftForStartGame = 0;
    
    public void StartGameCountdown()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int countdownRandomTime = Random.Range(3, 8);
            photonView.RPC(COUNTDOWN_STARTED_RPC,
                RpcTarget.AllViaServer, countdownRandomTime );
            startGameButtonUI.interactable = false;
        }
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        Debug.Log("Masterclient has been switched!" + Environment.NewLine
        + "Masterclient is now actor number " + newMasterClient.ActorNumber);
    }
    
    #region RPCS

    [PunRPC]
    void CountdownStarted(int countdownTime)
    {
        isCountingForStartGame = true;
        timeLeftForStartGame = countdownTime;
        countdownText.gameObject.SetActive(true);
    }
    
    [PunRPC]
    void GameStarted()
    {
        hasGameStarted = true;
        localPlayerController.canControl = true;
        isCountingForStartGame = false;
        Debug.Log("Game Started!!! WHOW");
    }

    [PunRPC]
    void AskForRandomSpawnPoint(PhotonMessageInfo messageInfo)
    {
        List<SpawnPoint> availableSpawnPoints = new List<SpawnPoint>();
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if(!spawnPoint.taken)
             availableSpawnPoints.Add(spawnPoint);
        }

        SpawnPoint chosenSpawnPoint =
            availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        chosenSpawnPoint.taken = true;
        
        bool[] takenSpawnPoints = new bool[spawnPoints.Length];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            takenSpawnPoints[i] = spawnPoints[i].taken;
        }
        photonView.RPC(SPAWN_PLAYER_CLIENT_RPC,
            messageInfo.Sender, chosenSpawnPoint.ID,
            takenSpawnPoints);
    }

    [PunRPC]
    void SpawnPlayer(int spawnPointID, bool[] takenSpawnPoints)
    {
        SpawnPoint spawnPoint = GetSpawnPointByID(spawnPointID);
        localPlayerController =
            PhotonNetwork.Instantiate(NETWORK_PLAYER_PREFAB_NAME, 
                    spawnPoint.transform.position, 
                    spawnPoint.transform.rotation)
                .GetComponent<PlayerController>();
        
        for (int i = 0; i < takenSpawnPoints.Length; i++)
        {
            spawnPoints[i].taken = takenSpawnPoints[i];
        }
        
    }
    
    #endregion

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // localPlayerController =
            //     PhotonNetwork.Instantiate(NETWORK_PLAYER_PREFAB_NAME, 
            //             spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].position, 
            //             spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].rotation)
            //         .GetComponent<PlayerController>();
            photonView.RPC(ASK_FOR_RANDOM_SPAWN_POINT_RPC, RpcTarget.MasterClient);
            if (PhotonNetwork.IsMasterClient)
            {
                startGameButtonUI.interactable = true;
            }
        }
    }

    private void Update()
    {
        if (isCountingForStartGame)
        {
            timeLeftForStartGame -= Time.deltaTime;
            countdownText.text = Mathf.Ceil(timeLeftForStartGame).ToString();
            if (timeLeftForStartGame <= 0)
            {
                isCountingForStartGame = false;
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC(GAME_STARTED_RPC, RpcTarget.AllViaServer);
                }
            }
        }
    }

    private void OnValidate()
    {
        int currentID = 0;
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            spawnPoint.ID = currentID++;
        }
    }

    private SpawnPoint GetSpawnPointByID(int targetID)
    {
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.ID == targetID)
                return spawnPoint;
        }

        return null;
    }


}