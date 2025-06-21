using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using ExitGames.Client.Photon;

public class PlayerSlotManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Sprite defaultAvatar;

    [Header("Slots UI (máx. 4 jugadores)")]
    public List<Image> avatarSlots;                     // Imagen del monstruo
    public List<TextMeshProUGUI> aliasSlots;            // Texto del alias
                                                        // public List<TextMeshProUGUI> scoreSlots;            // Texto de puntuación
    public List<Image> containerSlots;                  // Contenedor (Avatar, Avatar(1), ...)

    [Header("Avatares disponibles")]
    public List<Sprite> avatarSprites;
    public List<string> avatarNames;

    private Dictionary<string, Sprite> avatarDict;

    void Start()
    {
        // Verificación de seguridad
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("❌ No hay usuario logueado.");
            MostrarAliasDebug(0, "[Sin login]");
            return;
        }

        Debug.Log("✅ Usuario logueado: " + FirebaseAuth.DefaultInstance.CurrentUser.UserId);

        avatarDict = new Dictionary<string, Sprite>();
        for (int i = 0; i < avatarNames.Count; i++)
        {
            avatarDict[avatarNames[i]] = avatarSprites[i];
        }

        if (GameSettings.trainingMode)
            CargarJugadorEntrenamiento();
        else
            RellenarSlotsJugadores();
    }

    void RellenarSlotsJugadores()
    {
        // Ocultar todos los slots inicialmente
        for (int i = 0; i < avatarSlots.Count; i++)
        {
            if (i < containerSlots.Count) containerSlots[i].gameObject.SetActive(false);
            if (i < aliasSlots.Count) aliasSlots[i].gameObject.SetActive(false);
            if (i < avatarSlots.Count) avatarSlots[i].gameObject.SetActive(false);
            // if (i < scoreSlots.Count)
            // {
            //     scoreSlots[i].gameObject.SetActive(false);
            //     scoreSlots[i].text = "";
            // }
        }

        int index = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("firebaseId", out object firebaseIdObj))
            {
                string userId = firebaseIdObj.ToString();
                int slotIndex = index;

                if (slotIndex < containerSlots.Count) containerSlots[slotIndex].gameObject.SetActive(true);
                if (slotIndex < aliasSlots.Count) aliasSlots[slotIndex].gameObject.SetActive(true);
                if (slotIndex < avatarSlots.Count) avatarSlots[slotIndex].gameObject.SetActive(true);
                // if (slotIndex < scoreSlots.Count)
                // {
                //     scoreSlots[slotIndex].gameObject.SetActive(true);
                //     scoreSlots[slotIndex].text = "Puntos: 0";
                // }

                CargarDatosFirebase(userId, slotIndex);
                index++;
            }

            if (index >= avatarSlots.Count)
                break;
        }
    }

    void CargarDatosFirebase(string userId, int slotIndex)
    {
        var dbRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId);

        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                string json = task.Result.GetRawJsonValue();
                Jugador jugador = JsonUtility.FromJson<Jugador>(json);

                string alias = Capitalizar(jugador.alias);
                string avatarKey = jugador.avatar;

                if (slotIndex < aliasSlots.Count) aliasSlots[slotIndex].text = alias;
                if (slotIndex < avatarSlots.Count)
                {
                    avatarSlots[slotIndex].sprite = avatarDict.TryGetValue(avatarKey, out var sprite)
                        ? sprite
                        : defaultAvatar;
                }

                // if (slotIndex < scoreSlots.Count)
                // {
                //     scoreSlots[slotIndex].gameObject.SetActive(true);
                //     scoreSlots[slotIndex].text = "Puntos: 0";
                // }

                if (FirebaseAuth.DefaultInstance.CurrentUser.UserId == userId)
                {
                    PhotonNetwork.NickName = alias;

                    ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                    {
                        { "alias", alias },
                        { "avatar", avatarKey }
                    };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);


                    if (GameController.Instance != null)
                        GameController.Instance.RestaurarColoresJugadores();

                }

                var stats = GameStats.Instance.GetPlayer(alias);
                stats.avatarName = avatarKey;
            }
            else
            {
                MostrarAliasDebug(slotIndex, "[Error al cargar Firebase]");
            }
        });
    }

    void CargarJugadorEntrenamiento()
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var dbRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId);

        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                Jugador jugador = JsonUtility.FromJson<Jugador>(task.Result.GetRawJsonValue());

                string alias = Capitalizar(jugador.alias);
                string avatarKey = jugador.avatar;

                aliasSlots[0].text = alias;
                avatarSlots[0].sprite = avatarDict.TryGetValue(avatarKey, out var sprite) ? sprite : defaultAvatar;
                aliasSlots[0].gameObject.SetActive(true);
                avatarSlots[0].gameObject.SetActive(true);
                // scoreSlots[0].gameObject.SetActive(true);
                // scoreSlots[0].text = "Puntos: 0";

                var stats = GameStats.Instance.GetPlayer(alias);
                stats.avatarName = avatarKey;
            }
            else
            {
                MostrarAliasDebug(0, "[Error al cargar datos]");
            }
        });
    }

    void MostrarAliasDebug(int slotIndex, string mensaje)
    {
        if (slotIndex < aliasSlots.Count) aliasSlots[slotIndex].text = mensaje;
        if (slotIndex < avatarSlots.Count)
        {
            avatarSlots[slotIndex].sprite = defaultAvatar;
            avatarSlots[slotIndex].gameObject.SetActive(true);
        }
        // if (slotIndex < scoreSlots.Count)
        // {
        //     scoreSlots[slotIndex].gameObject.SetActive(true);
        //     scoreSlots[slotIndex].text = "Puntos: 0";
        // }
        if (slotIndex < containerSlots.Count)
        {
            containerSlots[slotIndex].gameObject.SetActive(true);
        }
    }

    string Capitalizar(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
