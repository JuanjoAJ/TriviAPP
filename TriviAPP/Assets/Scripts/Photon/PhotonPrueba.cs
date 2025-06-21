// Assets/Scripts/Game/PhotonPrueba.cs
using System.Collections;
using Firebase.Auth;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonPrueba : MonoBehaviourPunCallbacks
{
    [Header("UI de estado")]
    public TextMeshProUGUI textTimer;
    public TextMeshProUGUI textAttempt;
    public TextMeshProUGUI textPlayerCount;

    [Header("Configuración de conexión")]
    [SerializeField] private float connectionAttemptTime = 15f;
    [SerializeField] private int maxAttempts = 3;

    private int currentAttempt = 0;
    private bool gameStarted = false;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            // Ya estamos en sala (volver a jugar)
            BeginWaitingForPlayers();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            TryJoinRandomOrCreate();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        TryJoinRandomOrCreate();
    }


    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonPrueba] Entrado en sala: {PhotonNetwork.CurrentRoom.Name}");

        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null && !string.IsNullOrEmpty(currentUser.UserId))
        {
            string userId = currentUser.UserId;

            // ✅ Esperar si no está completamente dentro de la sala
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[PhotonPrueba] Aún no en sala completamente. Reintentando asignar firebaseId en 0.5s...");
                Invoke(nameof(ReintentarSetFirebaseId), 0.5f);
                return;
            }

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "firebaseId", userId }
        };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log("[PhotonPrueba] Publicado firebaseId: " + userId);
        }
        else
        {
            Debug.LogWarning("[PhotonPrueba] FirebaseAuth aún no está listo. Reintentando en 0.5s...");
            Invoke(nameof(ReintentarSetFirebaseId), 0.5f);
        }

        BeginWaitingForPlayers();
    }


    void ReintentarSetFirebaseId()
    {
        var currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null && !string.IsNullOrEmpty(currentUser.UserId))
        {
            string userId = currentUser.UserId;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["firebaseId"] = userId;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log("[PhotonPrueba] Reintentado y publicado firebaseId: " + userId);
        }
        else
        {
            Debug.LogError("[PhotonPrueba] No se pudo publicar el firebaseId.");
        }
    }


    private void BeginWaitingForPlayers()
    {
        UpdateLocalPlayerCount();
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(CheckConnectionAttempts());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateLocalPlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLocalPlayerCount();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient && !gameStarted)
            StartCoroutine(CheckConnectionAttempts());
    }

    IEnumerator CheckConnectionAttempts()
    {
        while (currentAttempt < maxAttempts && !gameStarted)
        {
            currentAttempt++;
            float timer = connectionAttemptTime;
            while (timer > 0f && !gameStarted)
            {
                int t = Mathf.CeilToInt(timer);
                int p = PhotonNetwork.CurrentRoom.PlayerCount;
                if (PhotonNetwork.IsMasterClient)
                    photonView.RPC(nameof(UpdateRemoteStatus), RpcTarget.All, t, currentAttempt, p);
                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                StartGame();
                yield break;
            }
        }

        photonView.RPC(nameof(UpdateRemoteStatus), RpcTarget.Others, 0, 0, 0);
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenuScene");
    }

    void StartGame()
    {
        gameStarted = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("GameAction");
    }

    void UpdateLocalPlayerCount()
    {
        int p = PhotonNetwork.CurrentRoom.PlayerCount;
        photonView.RPC(nameof(UpdateRemoteStatus), RpcTarget.All, 0, currentAttempt, p);
    }

    [PunRPC]
    void UpdateRemoteStatus(int timerValue, int attemptValue, int playerCount)
    {
        if (textTimer != null) textTimer.text = timerValue > 0 ? $"Tiempo restante: {timerValue}s" : "";
        if (textAttempt != null) textAttempt.text = attemptValue > 0 ? $"Intento: {attemptValue}/{maxAttempts}" : "";
        if (textPlayerCount != null) textPlayerCount.text = $"Conectados: {playerCount}";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// Llamar desde UI “Salir” para abandonar y volver al menú.
    /// </summary>
    public void LeaveRoomAndReturnToMenu()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        else
            SceneManager.LoadScene("MainMenuScene");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        bool isCategory = GameSettings.categoryMode;
        string categoryKey = GameSettings.categoryKey;
        string prefix = isCategory ? $"category_{categoryKey}" : "classic";
        string roomName = $"{prefix}_{Random.Range(1000, 10000)}";

        var options = new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,
            IsVisible = true,
            CustomRoomProperties = isCategory
                ? new ExitGames.Client.Photon.Hashtable { { "categoryKey", categoryKey } }
                : null,
            CustomRoomPropertiesForLobby = isCategory
                ? new string[] { "categoryKey" }
                : new string[0]
        };

        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        Debug.Log($"[PhotonPrueba] No había sala, creando: {roomName}");
    }

    private void TryJoinRandomOrCreate()
    {
        bool isCategory = GameSettings.categoryMode;
        string categoryKey = GameSettings.categoryKey;

        if (isCategory)
        {
            // Intentar unirse a una sala con la misma categoría
            PhotonNetwork.JoinRandomRoom(
                new ExitGames.Client.Photon.Hashtable
                {
                { "categoryKey", categoryKey }
                },
                0 // maxPlayers (0 = cualquiera)
            );
        }
        else
        {
            // Sin categoría, intenta unirse a cualquier sala clásica
            PhotonNetwork.JoinRandomRoom();
        }
    }
}
