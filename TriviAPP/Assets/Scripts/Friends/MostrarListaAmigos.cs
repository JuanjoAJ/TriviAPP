using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MostrarListaAmigos : MonoBehaviour
{
    [Header("UI")]
    public ScrollRect scrollRect;                // Scroll principal
    public GameObject friendItemPrefab;          // Prefab del amigo
    public TMP_InputField inputBusqueda;         // Campo para buscar alias o ID

    private Transform contenedorAmigos;          // Content del ScrollRect
    private Dictionary<string, GameObject> amigosInstanciados = new();

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>();

        if (scrollRect != null)
            contenedorAmigos = scrollRect.content;
        else
            Debug.LogError(" ScrollRect no encontrado. Asigna uno manualmente.");
    }

    private void OnEnable()
    {
        if (inputBusqueda != null)
            inputBusqueda.onValueChanged.AddListener(FiltrarAmigos);
    }

    private void OnDisable()
    {
        if (inputBusqueda != null)
            inputBusqueda.onValueChanged.RemoveListener(FiltrarAmigos);
    }

    public void MostrarListaDeAmigos()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogWarning("⚠ Usuario no autenticado.");
            return;
        }

        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var referencia = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId).Child("idAmigos");

        referencia.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists)
            {
                Debug.Log("No hay amigos guardados.");
                return;
            }

            foreach (Transform child in contenedorAmigos)
            {
                Debug.Log($" Destruyendo item previo: {child.name}");
                Destroy(child.gameObject);
            }

            amigosInstanciados.Clear();

            foreach (var amigoRef in task.Result.Children)
            {
                string amigoId = amigoRef.Value.ToString();
                Debug.Log($" Consultando datos de amigo con ID: {amigoId}");

                FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(amigoId)
                    .GetValueAsync().ContinueWithOnMainThread(snapshotTask =>
                    {
                        if (!snapshotTask.Result.Exists)
                        {
                            Debug.LogWarning($"⚠ No se encontró el usuario {amigoId}");
                            return;
                        }

                        string alias = snapshotTask.Result.Child("alias").Value?.ToString() ?? "SinAlias";
                        string puntos = snapshotTask.Result.Child("puntuacionTotal").Value?.ToString() ?? "0";
                        string avatarName = snapshotTask.Result.Child("avatar").Value?.ToString() ?? "defaultAvatar";
                        string codigo = snapshotTask.Result.Child("codigoUsuario").Value?.ToString() ?? "------";

                        Debug.Log($" Usuario cargado: {alias}, Puntos: {puntos}, Código: {codigo}, Avatar: {avatarName}");

                        Sprite avatarSprite = CargarSpriteDesdeNombre(avatarName);
                        if (avatarSprite == null)
                            Debug.LogWarning($"⚠ No se encontró sprite para: {avatarName}");

                        GameObject nuevo = Instantiate(friendItemPrefab, contenedorAmigos);
                        nuevo.SetActive(true);  // Asegúrate de activarlo

                        Debug.Log($"📦 Prefab instanciado: {nuevo.name}, Activo: {nuevo.activeSelf}");

                        amigosInstanciados[amigoId] = nuevo;

                        var ui = nuevo.GetComponent<FriendItemUI>();
                        if (ui == null)
                        {
                            Debug.LogError(" El prefab no tiene el componente FriendItemUI");
                            return;
                        }

                        ui.Configurar(alias, puntos + " pts", codigo, avatarSprite, () => EliminarAmigo(amigoId));

                        FiltrarAmigos(inputBusqueda?.text ?? "");
                    });
            }
        });
    }

    public void EliminarAmigo(string amigoId)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser == null) return;

        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var refUsuario = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId).Child("idAmigos");

        refUsuario.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists) return;

            foreach (var item in task.Result.Children)
            {
                if (item.Value.ToString() == amigoId)
                {
                    refUsuario.Child(item.Key).RemoveValueAsync().ContinueWithOnMainThread(removeTask =>
                    {
                        if (removeTask.IsCompletedSuccessfully)
                        {
                            Debug.Log(" Amigo eliminado correctamente");

                            if (amigosInstanciados.ContainsKey(amigoId))
                            {
                                Destroy(amigosInstanciados[amigoId]);
                                amigosInstanciados.Remove(amigoId);
                            }

                            EliminarAmigoReciproco(amigoId, userId);
                        }
                        else
                        {
                            Debug.LogError(" Error al eliminar amigo: " + removeTask.Exception);
                        }
                    });

                    break;
                }
            }
        });
    }

    private void EliminarAmigoReciproco(string otroId, string miId)
    {
        var refAmigo = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(otroId).Child("idAmigos");

        refAmigo.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists) return;

            foreach (var item in task.Result.Children)
            {
                if (item.Value.ToString() == miId)
                {
                    refAmigo.Child(item.Key).RemoveValueAsync();
                    break;
                }
            }
        });
    }

    private Sprite CargarSpriteDesdeNombre(string avatarName)
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogWarning(" AvatarManager no está en la escena.");
            return null;
        }

        Sprite sprite = AvatarManager.Instance.GetAvatarFor(avatarName);
        return sprite;
    }

    private void FiltrarAmigos(string texto)
    {
        string filtro = texto.Trim().ToLower();

        foreach (var par in amigosInstanciados)
        {
            var ui = par.Value.GetComponent<FriendItemUI>();
            string alias = ui.nameText.text.ToLower();
            string id = ui.idText.text.ToLower();

            bool visible = alias.Contains(filtro) || id.Contains(filtro);
            Debug.Log($"Filtro: {filtro} | Alias: {alias} | ID: {id} | Visible: {visible}");

            par.Value.SetActive(visible);
        }
    }
}
