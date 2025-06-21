using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class GameRankingUI : MonoBehaviourPunCallbacks
{
    [Header("Entradas del Ranking")]
    [SerializeField] private GameObject[] panels;
    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private Image[] imgPositions;
    [SerializeField] private Sprite[] positionSprites;
    [SerializeField] private Image[] imgAvatars;
    [SerializeField] private TMP_Text[] txtPlayerNames;
    [SerializeField] private TMP_Text[] txtPlayerScores;

    [Header("Mis estadísticas")]
    [SerializeField] private TMP_Text txtCorrectAnswers;
    [SerializeField] private TMP_Text txtWrongAnswers;
    [SerializeField] private TMP_Text txtPoints;

    [Header("Estadísticas de partida")]
    [SerializeField] private TMP_Text txtBestPlayer;
    [SerializeField] private TMP_Text txtWorstPlayer;
    [SerializeField] private TMP_Text txtWorstCategory;

    [Header("Controles")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnPlayAgain;

    private void Start()
    {
        SincronizarAvataresYAliasDesdePhoton();
        PopulateRanking();
        PopulateStats();
        GuardarMisEstadisticasEnFirebase();

        btnBack.onClick.AddListener(OnBackToMenu);
        btnPlayAgain.onClick.AddListener(OnPlayAgain);
        btnPlayAgain.interactable = true;
    }

    private void PopulateRanking()
    {
        var roomPlayers = PhotonNetwork.PlayerList;
        Debug.Log($"[GameRankingUI] Jugadores en sala: {roomPlayers.Length}, Panels: {panels.Length}");

        var statsList = roomPlayers
            .Select(pl => GameStats.Instance.GetPlayer(pl.NickName))
            .OrderByDescending(s => s.totalPoints)
            .ToList();

        int count = Mathf.Min(panels.Length, statsList.Count);
        for (int i = 0; i < panels.Length; i++)
        {
            if (i < count)
            {
                var s = statsList[i];
                panels[i].SetActive(true);

                if (i < positionSprites.Length && imgPositions[i] != null)
                {
                    imgPositions[i].sprite = positionSprites[i];
                    imgPositions[i].gameObject.SetActive(true);
                }

                txtPlayerNames[i].text = s.playerName;
                txtPlayerScores[i].text = s.totalPoints.ToString();

                if (imgAvatars != null && i < imgAvatars.Length && imgAvatars[i] != null)
                {
                    Sprite sprite = AvatarManager.Instance?.GetAvatarFor(s.avatarName);
                    imgAvatars[i].sprite = sprite != null ? sprite : defaultAvatar;
                    imgAvatars[i].gameObject.SetActive(true);
                }

                Debug.Log($"Jugador {s.playerName} usa avatarKey = {s.avatarName}");
            }
            else
            {
                panels[i].SetActive(false);
            }
        }
    }

    private void PopulateStats()
    {
        var me = GameStats.Instance.GetPlayer(PhotonNetwork.NickName);
        if (me == null)
        {
            Debug.LogWarning($"No se encontraron estadísticas para el jugador con el nombre: {PhotonNetwork.NickName}");
            return;
        }

        txtCorrectAnswers.text = $"Aciertos: {me.correct}";
        txtWrongAnswers.text = $"Fallos:   {me.wrong}";
        txtPoints.text = $"Puntos:   {me.totalPoints}";

        txtBestPlayer.text = $"Mejor jugador: {GameStats.Instance.GetPlayerWithMostPoints()}";
        txtWorstPlayer.text = $"Con más fallos: {GameStats.Instance.GetPlayerWithMostFails()}";

        var worstCat = EndGameData.GlobalWorstCategory;
        txtWorstCategory.text = $"Peor categoría: {(string.IsNullOrEmpty(worstCat) ? "Sin datos aún" : worstCat)}";

    }

    private void OnBackToMenu()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnPlayAgain()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            Debug.Log("[GameRankingUI] Ahora eres el MasterClient.");
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { ["Ready"] = true });

        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;
        PhotonNetwork.LoadLevel("GameLoad");
    }



    private void GuardarMisEstadisticasEnFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("Usuario no autenticado. No se puede guardar estadísticas.");
            return;
        }

        string userId = currentUser.UserId;
        var stats = GameStats.Instance.GetPlayer(PhotonNetwork.NickName);
        if (stats == null)
        {
            Debug.LogWarning("No se encontraron stats del jugador local.");
            return;
        }

        string mejorJugador = GameStats.Instance.GetPlayerWithMostPoints();
        bool esGanador = PhotonNetwork.NickName == mejorJugador;

        var statsRef = FirebaseDatabase.DefaultInstance
            .GetReference("usuarios")
            .Child(userId)
            .Child("estadisticas");

        var rootRef = FirebaseDatabase.DefaultInstance
            .GetReference("usuarios")
            .Child(userId);

        // 1. Guardar puntuación de esta partida (directamente)
        statsRef.Child("puntuacionPartida").SetValueAsync(stats.totalPoints);

        // 2. Actualizar puntuación total (sumar la de esta partida)
        rootRef.Child("puntuacionTotal").RunTransaction(mutable =>
        {
            int actual = int.TryParse(mutable.Value?.ToString(), out int val) ? val : 0;
            mutable.Value = actual + stats.totalPoints;
            return TransactionResult.Success(mutable);
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log($" puntuacionPartida: {stats.totalPoints}, puntuacionTotal actualizada.");
            else
                Debug.LogError(" Error al actualizar puntuacionTotal: " + task.Exception);
        });

        // 3. Ganadas o Perdidas
        string campoResultado = esGanador ? "ganadas" : "perdidas";
        statsRef.Child(campoResultado).RunTransaction(mutable =>
        {
            int actual = int.TryParse(mutable.Value?.ToString(), out int val) ? val : 0;
            mutable.Value = actual + 1;
            return TransactionResult.Success(mutable);
        });

        // 4. Preguntas acertadas
        statsRef.Child("preguntasAcertadas").RunTransaction(mutable =>
        {
            int actual = int.TryParse(mutable.Value?.ToString(), out int val) ? val : 0;
            mutable.Value = actual + stats.correct;
            return TransactionResult.Success(mutable);
        });

        // 5. Preguntas falladas
        statsRef.Child("preguntasFalladas").RunTransaction(mutable =>
        {
            int actual = int.TryParse(mutable.Value?.ToString(), out int val) ? val : 0;
            mutable.Value = actual + stats.wrong;
            return TransactionResult.Success(mutable);
        });
    }


    private void SincronizarAvataresYAliasDesdePhoton()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            string alias = player.NickName;
            GameStats.Instance.GetPlayer(alias);

            string avatar = "defaultAvatar";
            if (player.CustomProperties.TryGetValue("avatar", out object avatarObj))
                avatar = avatarObj.ToString();

            var stats = GameStats.Instance.GetPlayer(alias);
            stats.avatarName = avatar;
        }
    }
}
