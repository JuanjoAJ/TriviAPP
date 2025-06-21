using Firebase;
using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class BaseDatosController : MonoBehaviour
{
    // Referencias UI
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField registerAlias;
    public TMP_InputField Input_Alias;
    public TextMeshProUGUI mensajeSistemaTexto;

    private DatabaseReference referenciaBaseDeDatos;
    private string avatarSeleccionado = "defaultAvatar";

    void Start()
    {
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        string userId = auth.CurrentUser?.UserId;

        referenciaBaseDeDatos = FirebaseDatabase.GetInstance("https://triviapp-8f3d8-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;

        if (!string.IsNullOrEmpty(userId))
        {
            referenciaBaseDeDatos.Child("usuarios").Child(userId).Child("avatar").GetValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        avatarSeleccionado = task.Result.Value.ToString();
                        Debug.Log("Avatar actual recuperado: " + avatarSeleccionado);
                    }
                });
        }
    }

    public void GrabarDatos()
    {
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        referenciaBaseDeDatos.Child("usuarios").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al verificar existencia del usuario: " + task.Exception);
                return;
            }

            if (task.Result.Exists)
            {
                Debug.Log("El usuario ya existe, no se creará otro.");
                MostrarMensaje("Ya estás registrado.");
            }
            else
            {
                if (registerEmail.text.Equals("") || registerAlias.text.Equals(""))
                {
                    Debug.Log("Introduce tu correo y alias");
                    MostrarMensaje("Faltan datos.");
                    return;
                }

                Grabar(registerAlias.text.Trim(), registerEmail.text.Trim());
            }
        });
    }

    public void Grabar(string alias, string email)
    {
        Debug.Log("Grabando datos...");
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        string aliasID = alias + GenerarCodigoUnico(3);
        Debug.Log("Alias generado: " + aliasID);

        Usuario nuevoUsuario = new Usuario(
            userId,
            email,
            aliasID,
            GenerarCodigoUnico(6),
            "email",
            avatarSeleccionado 
        );

        string json = JsonUtility.ToJson(nuevoUsuario);

        referenciaBaseDeDatos
            .Child("usuarios")
            .Child(nuevoUsuario.idUsuario)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Usuario registrado en la base de datos");
                }
                else
                {
                    Debug.LogError("Error al registrar el usuario: " + task.Exception);
                }
            });
    }

    private string GenerarCodigoUnico(int num)
    {
        string caracteres = "0123456789";
        System.Random random = new System.Random();
        string codigo = "";

        for (int i = 0; i < num; i++)
        {
            codigo += caracteres[random.Next(caracteres.Length)];
        }

        return codigo;
    }

    public void ModificarAliasYAvatar()
    {
        string nuevoAlias = Input_Alias.text.Trim();

        if (string.IsNullOrEmpty(nuevoAlias))
        {
            Debug.LogWarning("Por favor, introduce un alias");
            return;
        }

        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        Dictionary<string, object> actualizaciones = new Dictionary<string, object>();
        actualizaciones["alias"] = nuevoAlias;

        // Solo actualiza el avatar si ha sido modificado
        if (!string.IsNullOrEmpty(avatarSeleccionado) && avatarSeleccionado != "defaultAvatar")
        {
            actualizaciones["avatar"] = avatarSeleccionado;
        }

        referenciaBaseDeDatos
            .Child("usuarios")
            .Child(userId)
            .UpdateChildrenAsync(actualizaciones)
            .ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompletedSuccessfully)
                {
                    Debug.Log("Datos actualizados con éxito");
                    MostrarMensaje("Datos modificados con éxito");
                }
                else
                {
                    Debug.LogError("Error al actualizar los datos: " + updateTask.Exception);
                }
            });
    }

    private void MostrarMensaje(string mensaje)
    {
        mensajeSistemaTexto.text = mensaje;
        mensajeSistemaTexto.gameObject.SetActive(true);
        Invoke(nameof(EsconderMensaje), 2f);
    }

    private void EsconderMensaje()
    {
        mensajeSistemaTexto.gameObject.SetActive(false);
    }

    public class Usuario
    {
        public string idUsuario;
        public string email;
        public string alias;
        public string codigoUsuario;
        public string tipoLogin;
        public string avatar;
        public int puntuacionTotal;
        public string estadoConexion;
        public List<string> idAmigos;
        public List<string> idPartidasJugadas;

        public Usuario(string idUsuario, string email, string alias, string codigoUsuario, string tipoLogin, string avatar)
        {
            this.idUsuario = idUsuario;
            this.email = email;
            this.alias = alias;
            this.codigoUsuario = codigoUsuario;
            this.tipoLogin = tipoLogin;
            this.avatar = avatar;
            this.puntuacionTotal = 0;
            this.estadoConexion = "offline";
            this.idAmigos = new List<string>();
            this.idPartidasJugadas = new List<string>();
        }
    }
}
