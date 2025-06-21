using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class FriendsRankingUI : MonoBehaviour
{
    public Transform contenedorRanking;
    public GameObject rankingEntryPrefab;
    public AvatarManager avatarManager;
    public List<Sprite> medallasTop3;

    private void Start()
    {
        if (RankingManager.Instance != null)
        {
            if (rankingEntryPrefab == null)
                rankingEntryPrefab = RankingManager.Instance.rankingEntryPrefab;
            if (avatarManager == null)
                avatarManager = RankingManager.Instance.avatarManager;
            if (contenedorRanking == null)
                contenedorRanking = RankingManager.Instance.contenedorRanking;
            if (medallasTop3 == null || medallasTop3.Count < 3)
                medallasTop3 = RankingManager.Instance.medallasTop3;
        }

        if (contenedorRanking == null)
            contenedorRanking = GameObject.Find("Content")?.transform;
        if (avatarManager == null)
            avatarManager = Object.FindFirstObjectByType<AvatarManager>();
        if (rankingEntryPrefab == null)
            rankingEntryPrefab = Resources.Load<GameObject>("Panel_PlayerEntry");

        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
            CargarRankingDeAmigos();
    }

    void CargarRankingDeAmigos()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null) return;

        string userId = currentUser.UserId;
        var amigosRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId).Child("idAmigos");

        amigosRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            List<string> idsAmigos = new();
            if (task.Result.Exists)
            {
                foreach (var amigo in task.Result.Children)
                    idsAmigos.Add(amigo.Value.ToString());
            }

            List<(string id, string alias, int puntos, string avatar)> datos = new();
            int completados = 0;

            foreach (string id in idsAmigos)
            {
                FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(id)
                    .GetValueAsync().ContinueWithOnMainThread(t =>
                    {
                        completados++;
                        if (t.Result.Exists)
                        {
                            string alias = t.Result.Child("alias").Value?.ToString() ?? "-";
                            string avatar = t.Result.Child("avatar").Value?.ToString() ?? "default";
                            int puntos = int.TryParse(t.Result.Child("puntuacionTotal").Value?.ToString(), out int val) ? val : 0;
                            datos.Add((id, alias, puntos, avatar));
                        }

                        if (completados == idsAmigos.Count)
                        {
                            FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId)
                                .GetValueAsync().ContinueWithOnMainThread(userTask =>
                                {
                                    if (userTask.Result.Exists)
                                    {
                                        string alias = userTask.Result.Child("alias").Value?.ToString() ?? "Yo";
                                        string avatar = userTask.Result.Child("avatar").Value?.ToString() ?? "default";
                                        int puntos = int.TryParse(userTask.Result.Child("puntuacionTotal").Value?.ToString(), out int val) ? val : 0;
                                        datos.Add((userId, alias, puntos, avatar));
                                    }

                                    var datosOrdenados = datos.OrderByDescending(d => d.puntos).ToList();
                                    MostrarEntradas(datosOrdenados);
                                });
                        }
                    });
            }

            if (idsAmigos.Count == 0)
            {
                FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId)
                    .GetValueAsync().ContinueWithOnMainThread(userTask =>
                    {
                        if (userTask.Result.Exists)
                        {
                            string alias = userTask.Result.Child("alias").Value?.ToString() ?? "Yo";
                            string avatar = userTask.Result.Child("avatar").Value?.ToString() ?? "default";
                            int puntos = int.TryParse(userTask.Result.Child("puntuacionTotal").Value?.ToString(), out int val) ? val : 0;
                            var soloUsuario = new List<(string id, string alias, int puntos, string avatar)>
                            {
                                (userId, alias, puntos, avatar)
                            };
                            MostrarEntradas(soloUsuario);
                        }
                    });
            }
        });
    }

    void MostrarEntradas(List<(string id, string alias, int puntos, string avatar)> amigosOrdenados)
    {
        foreach (Transform child in contenedorRanking)
            Destroy(child.gameObject);

        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
        int userIndex = amigosOrdenados.FindIndex(a => a.id == currentUserId);

        Debug.Log($" Usuario actual está en la posición {userIndex + 1} del ranking.");

        HashSet<int> posicionesAMostrar = new();
        for (int i = 0; i < Mathf.Min(10, amigosOrdenados.Count); i++)
            posicionesAMostrar.Add(i);

        if (userIndex >= 10)
        {
            int start = Mathf.Max(10, userIndex - 2);
            int end = Mathf.Min(amigosOrdenados.Count - 1, userIndex + 2);
            for (int i = start; i <= end; i++)
                posicionesAMostrar.Add(i);
        }

        for (int i = 0; i < amigosOrdenados.Count; i++)
        {
            if (!posicionesAMostrar.Contains(i)) continue;

            var amigo = amigosOrdenados[i];
            GameObject entry = Instantiate(rankingEntryPrefab, contenedorRanking);
            entry.name = $"Amigo_{amigo.alias}_{i + 1}";
            entry.SetActive(true);
            SetAllChildrenActive(entry.transform);

            var nombre = entry.transform.Find("Txt_PlayerName");
            var score = entry.transform.Find("Txt_PlayerScore");
            var avatarImg = entry.transform.Find("Img_Avatar");
            var medallaImg = entry.transform.Find("Img_PositionMedal");
            var numPos = medallaImg?.Find("Txt_PositionNumber");

            if (nombre) nombre.GetComponent<TMP_Text>().text = amigo.alias;
            if (score) score.GetComponent<TMP_Text>().text = amigo.puntos.ToString();
            if (avatarImg)
            {
                var sprite = avatarManager.GetAvatarFor(amigo.avatar);
                if (sprite != null)
                    avatarImg.GetComponent<Image>().sprite = sprite;
            }

            if (i < 3 && medallasTop3.Count > i)
            {
                if (medallaImg != null)
                {
                    var img = medallaImg.GetComponent<Image>();
                    img.enabled = true;
                    img.sprite = medallasTop3[i];
                }
                if (numPos != null)
                    numPos.gameObject.SetActive(false);
            }
            else
            {
                if (medallaImg != null)
                    medallaImg.GetComponent<Image>().enabled = false;
                if (numPos != null)
                {
                    numPos.gameObject.SetActive(true);
                    numPos.GetComponent<TMP_Text>().text = $"#{i + 1}";
                }
            }

            if (amigo.id == currentUserId)
            {
                var fondo = entry.GetComponent<Image>();
                if (fondo != null) fondo.color = new Color(1f, 1f, 0.5f);

                if (nombre != null) nombre.GetComponent<TMP_Text>().color = Color.black;
                if (score != null) score.GetComponent<TMP_Text>().color = Color.black;
                if (numPos != null && numPos.gameObject.activeSelf)
                    numPos.GetComponent<TMP_Text>().color = Color.black;
            }
        }

        Debug.Log(" Ranking de amigos mostrado con lógica de top 10 + entorno del jugador.");
    }

    void SetAllChildrenActive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(true);
            SetAllChildrenActive(child);
        }
    }
}