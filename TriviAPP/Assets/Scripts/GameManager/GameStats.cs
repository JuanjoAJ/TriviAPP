using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance;

    [System.Serializable]
    public class PlayerStats
    {
        public string playerName;
        public int correct = 0;
        public int wrong = 0;
        public int totalPoints = 0;
        public string avatarName;
    }

    private Dictionary<string, int> categoryFails = new Dictionary<string, int>();
    private Dictionary<string, PlayerStats> allPlayers = new Dictionary<string, PlayerStats>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public PlayerStats GetPlayer(string playerName, string avatarName = null)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Se intentó registrar o acceder a un jugador con nombre vacío o nulo.");
            return null;
        }

        if (!allPlayers.ContainsKey(playerName))
            allPlayers[playerName] = new PlayerStats { playerName = playerName };

        if (avatarName != null)
            allPlayers[playerName].avatarName = avatarName;

        return allPlayers[playerName];
    }

    public List<PlayerStats> GetAllPlayersSorted()
    {
        return allPlayers.Values
                         .OrderByDescending(p => p.totalPoints)
                         .ToList();
    }

    public PlayerStats GetLocalPlayer()
    {
        return GetPlayer(TurnManager.Instance.localPlayerName);
    }

    public string GetPlayerWithMostPoints()
    {
        var best = allPlayers.Values.OrderByDescending(p => p.totalPoints).FirstOrDefault();
        return best != null ? best.playerName : "-";
    }

    public string GetPlayerWithMostFails()
    {
        var worst = allPlayers.Values.OrderByDescending(p => p.wrong).FirstOrDefault();
        return worst != null ? worst.playerName : "-";
    }

    public void AddCategoryFail(string category)
    {
        if (string.IsNullOrEmpty(category)) return;
        if (!categoryFails.ContainsKey(category))
            categoryFails[category] = 0;
        categoryFails[category]++;
    }

    public string GetWorstCategory()
    {
        if (categoryFails.Count == 0)
            return "Sin datos aún";

        return categoryFails
            .OrderByDescending(kv => kv.Value)
            .First().Key;
    }

    public void ResetStats()
    {
        allPlayers.Clear();
        categoryFails.Clear();
    }

    public void GetAllStatsArrays(out string[] names, out int[] corrects, out int[] wrongs, out int[] points)
    {
        var sorted = GetAllPlayersSorted();
        int n = sorted.Count;
        names = new string[n];
        corrects = new int[n];
        wrongs = new int[n];
        points = new int[n];

        for (int i = 0; i < n; i++)
        {
            var p = sorted[i];
            names[i] = p.playerName;
            corrects[i] = p.correct;
            wrongs[i] = p.wrong;
            points[i] = p.totalPoints;
        }
    }

    public void RegisterPhotonPlayersWithFirebaseOnly()
    {
        foreach (var pl in PhotonNetwork.PlayerList)
        {
            if (pl.CustomProperties.ContainsKey("firebaseId"))
            {
                GetPlayer(pl.NickName);
            }
            else
            {
                Debug.LogWarning($"Jugador {pl.NickName} no tiene firebaseId, se omite.");
            }
        }
    }
    public void RemovePlayer(string playerName)
    {
        if (allPlayers.ContainsKey(playerName))
            allPlayers.Remove(playerName);
    }


}
