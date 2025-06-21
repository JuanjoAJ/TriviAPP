using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase;
using Firebase.Extensions;
using System.Linq;

public class GlobalRankingUI : MonoBehaviour
{
    [Header("Medallas (posición 1, 2, 3)")]
    public List<Sprite> medallasTop3;

    [Header("Prefabs y referencias UI")]
    public Transform contenedorRanking;
    public GameObject rankingEntryPrefab;
    public AvatarManager avatarManager;

    private GameObject entryDelUsuario;

    private void Start()
    {
        Debug.Log(" Iniciando GlobalRankingUI...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Debug.Log(" Firebase disponible.");
                Debug.Log($" Usuario actual: {FirebaseAuth.DefaultInstance.CurrentUser}");

                if (FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    Debug.Log(" Usuario autenticado, cargando ranking...");
                    CargarRankingGlobal();
                }
                else
                {
                    Debug.LogWarning(" Usuario no logueado. No se puede cargar el ranking.");
                }
            }
            else
            {
                Debug.LogError($" No se pudo resolver las dependencias de Firebase: {dependencyStatus}");
            }
        });
    }


    void CargarRankingGlobal()
    {
        var currentUserID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        Debug.Log(" Solicitando datos de ranking a Firebase...");

        FirebaseDatabase.DefaultInstance.GetReference("usuarios")
            .OrderByChild("puntuacionTotal")
            .LimitToLast(100)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError(" Error al cargar ranking global: " + task.Exception);
                    return;
                }

                Debug.Log(" Datos recibidos del ranking.");

                var usuarios = new List<DataSnapshot>(task.Result.Children);

                var usuariosOrdenados = usuarios
                    .Select(u => new
                    {
                        Snapshot = u,
                        Puntos = int.TryParse(u.Child("puntuacionTotal").Value?.ToString(), out int val) ? val : 0
                    })
                    .OrderByDescending(u => u.Puntos)
                    .ToList();

                int userIndex = usuariosOrdenados.FindIndex(u => u.Snapshot.Key == currentUserID);

                // Limpiar entradas anteriores
                foreach (Transform child in contenedorRanking)
                    Destroy(child.gameObject);

                if (usuariosOrdenados.Count == 0)
                {
                    Debug.LogWarning(" No hay usuarios con puntuación.");
                    return;
                }

                // Mostrar Top 10
                int topLimit = Mathf.Min(10, usuariosOrdenados.Count);
                for (int i = 0; i < topLimit; i++)
                    CrearEntrada(usuariosOrdenados[i].Snapshot, i + 1, usuariosOrdenados[i].Snapshot.Key == currentUserID);

                // Si el usuario está FUERA del top 10, mostrar +/-2 de su posición
                if (userIndex >= 10)
                {
                    int start = Mathf.Max(10, userIndex - 2);
                    int end = Mathf.Min(userIndex + 2, usuariosOrdenados.Count - 1);

                    for (int i = start; i <= end; i++)
                        CrearEntrada(usuariosOrdenados[i].Snapshot, i + 1, usuariosOrdenados[i].Snapshot.Key == currentUserID);
                }

                if (entryDelUsuario != null)
                    StartCoroutine(ScrollToEntry(entryDelUsuario.GetComponent<RectTransform>()));
            });
    }
    void CrearEntrada(DataSnapshot usuarioSnapshot, int posicion, bool esUsuarioActual)
    {
        string alias = usuarioSnapshot.Child("alias").Value?.ToString() ?? "-";
        string avatarKey = usuarioSnapshot.Child("avatar").Value?.ToString() ?? "";
        int puntuacion = int.TryParse(usuarioSnapshot.Child("puntuacionTotal").Value?.ToString(), out int val) ? val : 0;

        GameObject entry = Instantiate(rankingEntryPrefab, contenedorRanking);
        entry.name = $"Entry_{alias}_{posicion}";
        entry.SetActive(true);
        SetAllChildrenActive(entry.transform);

        var medallaObj = entry.transform.Find("Img_PositionMedal");
        var numeroObj = medallaObj?.Find("Txt_PositionNumber");

        if (posicion <= 3 && medallaObj != null)
        {
            if (posicion - 1 < medallasTop3.Count && medallasTop3[posicion - 1] != null)
            {
                medallaObj.GetComponent<Image>().enabled = true;
                medallaObj.GetComponent<Image>().sprite = medallasTop3[posicion - 1];
                if (numeroObj != null) numeroObj.gameObject.SetActive(false);
            }
        }
        else if (numeroObj != null)
        {
            numeroObj.gameObject.SetActive(true);
            numeroObj.GetComponent<TMP_Text>().text = $"#{posicion}";
            if (medallaObj != null)
            {
                var img = medallaObj.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }
        }

        var avatarObj = entry.transform.Find("Img_Avatar");
        if (avatarObj != null)
        {
            Sprite avatar = avatarManager.GetAvatarFor(avatarKey);
            if (avatar != null) avatarObj.GetComponent<Image>().sprite = avatar;
        }

        var nombreObj = entry.transform.Find("Txt_PlayerName");
        if (nombreObj != null)
            nombreObj.GetComponent<TMP_Text>().text = alias;

        var puntosObj = entry.transform.Find("Txt_PlayerScore");
        if (puntosObj != null)
            puntosObj.GetComponent<TMP_Text>().text = puntuacion.ToString();

        if (esUsuarioActual)
        {
            var fondo = entry.GetComponent<Image>();
            if (fondo != null) fondo.color = new Color(1f, 1f, 0.5f);
            if (nombreObj != null) nombreObj.GetComponent<TMP_Text>().color = Color.black;
            if (puntosObj != null) puntosObj.GetComponent<TMP_Text>().color = Color.black;
            if (numeroObj != null && numeroObj.gameObject.activeSelf)
                numeroObj.GetComponent<TMP_Text>().color = Color.black;

            entryDelUsuario = entry;
        }
    }

    void SetAllChildrenActive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(true);
            SetAllChildrenActive(child);
        }
    }

    IEnumerator ScrollToEntry(RectTransform target)
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        ScrollRect scrollRect = contenedorRanking.GetComponentInParent<ScrollRect>();
        RectTransform content = scrollRect.content;
        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        float pivotOffset = 0.5f;
        float normalizedPos = Mathf.Clamp01(1 - ((target.localPosition.y + target.rect.height * pivotOffset) / (contentHeight - viewportHeight)));

        scrollRect.verticalNormalizedPosition = normalizedPos;
    }
}
