using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class AvatarController : MonoBehaviour
{
    [Header("Referencias en la UI")]
    public Image avatarActualUI; // Imagen que muestra el avatar actual
    public Text mensajeTexto; // Texto que muestra confirmaci�n

    [Header("Avatares disponibles")] //test
    public List<Sprite> listaDeAvatares;
    public List<string> nombresDeAvatares;

    private Dictionary<string, Sprite> diccionarioAvatares = new Dictionary<string, Sprite>();

    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.GetInstance("https://triviapp-8f3d8-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;

        // Crear diccionario (nombre -> sprite)
        for (int i = 0; i < listaDeAvatares.Count; i++)
        {
            diccionarioAvatares[nombresDeAvatares[i]] = listaDeAvatares[i];
        }

        CargarAvatarDelUsuario();
    }

    public void SeleccionarAvatar(string nombreAvatar)
    {
        if (diccionarioAvatares.ContainsKey(nombreAvatar))
        {
            avatarActualUI.sprite = diccionarioAvatares[nombreAvatar];
            GuardarAvatarEnFirebase(nombreAvatar);
        }
    }

    private void GuardarAvatarEnFirebase(string nombreAvatar)
    {
        string userId = auth.CurrentUser.UserId;

        Dictionary<string, object> actualizacion = new Dictionary<string, object>();
        actualizacion["avatar"] = nombreAvatar;

        dbRef.Child("usuarios").Child(userId).UpdateChildrenAsync(actualizacion)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("�Avatar actualizado correctamente!");
                    MostrarMensaje("�Avatar seleccionado correctamente!");
                }
                else
                {
                    Debug.LogError("Error al actualizar avatar: " + task.Exception);
                    MostrarMensaje("Error al seleccionar avatar.");
                }
            });
    }

    private void CargarAvatarDelUsuario()
    {
        string userId = auth.CurrentUser.UserId;

        dbRef.Child("usuarios").Child(userId).Child("avatar").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    if (task.Result.Exists)
                    {
                        string nombreAvatar = task.Result.Value.ToString();
                        Debug.Log("📦 Avatar obtenido de Firebase: " + nombreAvatar);

                        if (diccionarioAvatares.ContainsKey(nombreAvatar))
                        {
                            avatarActualUI.sprite = diccionarioAvatares[nombreAvatar];
                            Debug.Log("✅ Avatar actualizado correctamente.");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ No se encontró el sprite para: " + nombreAvatar);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("❗Campo 'avatar' no existe para este usuario.");
                    }
                }
                else
                {
                    Debug.LogError("⛔ Error al obtener avatar: " + task.Exception);
                }
            });
    }



    private void MostrarMensaje(string mensaje)
    {
        mensajeTexto.text = mensaje;
        mensajeTexto.gameObject.SetActive(true);
        Invoke(nameof(EsconderMensaje), 2f);
    }

    private void EsconderMensaje()
    {
        mensajeTexto.gameObject.SetActive(false);
    }
}
