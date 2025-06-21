using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class AgregarAmigo : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoMiID;          // Muestra el código del usuario actual
    public TMP_InputField inputIDAmigo;        // Campo para escribir el ID del amigo
    public TextMeshProUGUI mensajeTexto;       // Mensaje de feedback (errores, éxito)

    private void Start()
    {
        // Muestra el código del usuario actual
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            FirebaseDatabase.DefaultInstance
                .GetReference("usuarios")
                .Child(userId)
                .Child("codigoUsuario")
                .GetValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        textoMiID.text = task.Result.Value.ToString();
                    }
                    else
                    {
                        textoMiID.text = "Sin ID";
                    }
                });
        }
    }

    // Botón "Agregar"
    public void AlClicAgregarAmigo()
    {
        string codigo = inputIDAmigo.text.Trim();

        if (string.IsNullOrEmpty(codigo))
        {
            MostrarMensaje("Introduce un código válido.");
            return;
        }

        AgregarAmigoPorCodigo(codigo);
    }

    // Agregar amigo por códigoUsuario
    public void AgregarAmigoPorCodigo(string codigoAmigo)
    {
        if (string.IsNullOrEmpty(codigoAmigo))
        {
            MostrarMensaje("Introduce un código válido.");
            return;
        }

        string miUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        FirebaseDatabase.DefaultInstance.GetReference("usuarios")
            .OrderByChild("codigoUsuario")
            .EqualTo(codigoAmigo.Trim())
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error al buscar el código del amigo: " + task.Exception);
                    MostrarMensaje("Error al buscar el código.");
                    return;
                }

                if (!task.Result.Exists)
                {
                    MostrarMensaje("No se encontró ningún usuario con ese código.");
                    return;
                }

                foreach (var snapshot in task.Result.Children)
                {
                    string idAmigo = snapshot.Key;

                    if (idAmigo == miUserId)
                    {
                        MostrarMensaje("No puedes agregarte a ti mismo.");
                        return;
                    }

                    DatabaseReference miRef = FirebaseDatabase.DefaultInstance
                        .GetReference("usuarios").Child(miUserId).Child("idAmigos");

                    miRef.GetValueAsync().ContinueWithOnMainThread(checkTask =>
                    {
                        if (checkTask.IsFaulted)
                        {
                            Debug.LogError("Error al verificar amigos existentes: " + checkTask.Exception);
                            MostrarMensaje("Error al comprobar amigos.");
                            return;
                        }

                        bool yaEsAmigo = false;

                        if (checkTask.Result != null && checkTask.Result.Exists)
                        {
                            foreach (var amigo in checkTask.Result.Children)
                            {
                                if (amigo.Value.ToString() == idAmigo)
                                {
                                    yaEsAmigo = true;
                                    break;
                                }
                            }
                        }

                        if (yaEsAmigo)
                        {
                            MostrarMensaje("Ese usuario ya es tu amigo.");
                            return;
                        }

                        // Agregar amigo
                        string nuevaClave = miRef.Push().Key;
                        miRef.Child(nuevaClave).SetValueAsync(idAmigo).ContinueWithOnMainThread(writeTask =>
                        {
                            if (writeTask.IsCompletedSuccessfully)
                            {
                                MostrarMensaje("¡Amigo agregado con éxito!");
                                Debug.Log("Amigo agregado: " + idAmigo);

                                // Amistad recíproca
                                AgregarAmistadReciproca(idAmigo, miUserId);
                            }
                            else
                            {
                                MostrarMensaje("Error al agregar amigo.");
                                Debug.LogError("Error al guardar amigo: " + writeTask.Exception);
                            }
                        });
                    });
                }
            });
    }

    // Agrega el usuario actual como amigo en la cuenta del otro
    private void AgregarAmistadReciproca(string amigoId, string yoId)
    {
        DatabaseReference refAmigo = FirebaseDatabase.DefaultInstance
            .GetReference("usuarios").Child(amigoId).Child("idAmigos");

        refAmigo.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error al verificar amistad recíproca: " + task.Exception);
                return;
            }

            bool yaExiste = false;

            if (task.Result != null && task.Result.Exists)
            {
                foreach (var item in task.Result.Children)
                {
                    if (item.Value.ToString() == yoId)
                    {
                        yaExiste = true;
                        break;
                    }
                }
            }

            if (!yaExiste)
            {
                string nuevaClave = refAmigo.Push().Key;
                refAmigo.Child(nuevaClave).SetValueAsync(yoId);
            }
        });
    }

    private void MostrarMensaje(string mensaje)
    {
        mensajeTexto.text = mensaje;
        mensajeTexto.gameObject.SetActive(true);
        CancelInvoke(nameof(EsconderMensaje));
        Invoke(nameof(EsconderMensaje), 3f);
    }

    private void EsconderMensaje()
    {
        mensajeTexto.gameObject.SetActive(false);
    }
}
