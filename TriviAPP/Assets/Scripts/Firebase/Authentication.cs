using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using System.Threading.Tasks;


public class NewMonoBehaviourScript : MonoBehaviour
{
    private static BaseDatosController baseDatosController;
    private DatabaseReference referenciaBaseDeDatos;
    public LoginScreenManager loginScreenManager;

    private string avatarSeleccionado = "defaultAvatar";


    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject forgotPasswordPanel;

    // Login
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;

    // Registro
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField registerAlias;

    // Recuperar contraseña
    public TMP_InputField forgotPassword;

    // Modificar perfil
    public TMP_InputField Input_Email;
    public TMP_InputField Input_Password;
    public TMP_InputField Input_ConfirmPassword;


    [Header("Mensajes de feedback")]
    public Text mensajeTextoRegistro;
    public Text mensajeTextoLogin;


    FirebaseAuth auth;
    FirebaseUser user;



    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                referenciaBaseDeDatos = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase ha sido configurado correctamente.");
            }
            else
            {
                Debug.LogError($"No se pudo resolver las dependencias de Firebase: {dependencyStatus}");
            }
        });
    }

    void InicializarFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        referenciaBaseDeDatos = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void OnDestroy()
    {
        auth = null;
        Debug.Log("Firebase ha sido configurado.");
    }

    public void AlClicBotonCrearCuenta()
    {
        IntentarRegistrarCorreoFirebase(registerEmail.text, registerPassword.text, registerAlias.text);
    }

    void IntentarRegistrarCorreoFirebase(string email, string password, string alias)
    {
        if (string.IsNullOrEmpty(email))
        {
            Debug.Log("Correo electrónico no proporcionado");
            MostrarMensaje("Faltan datos");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            Debug.Log("Contraseña vacía");
            MostrarMensaje("Faltan datos");
            return;
        }
        if (string.IsNullOrEmpty(alias))
        {
            Debug.Log("Alias vacío");
            MostrarMensaje("Faltan datos");
            return;
        }

        // Verificación de alias único
        referenciaBaseDeDatos.Child("usuarios").OrderByChild("alias").EqualTo(alias.Trim()).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al verificar existencia del alias: " + task.Exception);
                MostrarMensaje("Error al verificar alias.");
                return;
            }

            if (task.Result.Exists)
            {
                Debug.Log("El alias ya está en uso, por favor elige otro.");
                MostrarMensaje("El alias ya está en uso, por favor elige otro.");
            }
            else
            {
                // Crear el usuario en Firebase Auth
                auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.Log("Acción cancelada");
                        MostrarMensaje("Acción cancelada.");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        string mensajeError = "Error al registrar.";
                        if (task.Exception != null && task.Exception.InnerExceptions.Count > 0)
                        {
                            var firebaseEx = task.Exception.InnerExceptions[0] as Firebase.FirebaseException;
                            if (firebaseEx != null)
                            {
                                string error = firebaseEx.Message;

                                if (error.Contains("email address is already in use"))
                                    mensajeError = "Ya existe una cuenta con ese correo electrónico.";
                                else if (error.Contains("The email address is badly formatted"))
                                    mensajeError = "El formato del correo electrónico no es válido.";
                                else if (error.Contains("Password should be at least"))
                                    mensajeError = "La contraseña debe tener al menos 6 caracteres.";
                                else
                                    mensajeError = "Error: " + firebaseEx.Message;
                            }
                        }

                        Debug.LogError("Error al registrar: " + mensajeError);
                        MostrarMensaje(mensajeError);
                        return;
                    }

                    FirebaseUser nuevoUsuario = task.Result.User;
                    Debug.Log("Usuario creado: " + nuevoUsuario.UserId);

                    GrabarDatos(email, alias);
                    MostrarMensaje("¡Cuenta creada con éxito!");
                    LimpiarFormularioRegistro();
                });
            }
        });
    }


    public void GrabarDatos(string email, string alias)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("CurrentUser es NULL, no se puede continuar.");
            return;
        }

        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        referenciaBaseDeDatos.Child("usuarios").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al verificar existencia del usuario: " + task.Exception);
                MostrarMensaje("Usuario no existe");
                return;
            }

            if (task.Result.Exists)
            {
                Debug.Log("El usuario ya existe, no se creara otro.");
                MostrarMensaje("El usuario ya existe, no se creara otro.");
            }
            else
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(alias))
                {
                    Debug.Log("Introduce tu nombre y alias");
                    MostrarMensaje("Introduce tu nombre y alias");
                    return;
                }

                Grabar(alias.Trim(), email.Trim());
            }
        });
    }

    public void Grabar(string alias, string email)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("CurrentUser es NULL, no se puede grabar datos.");
            return;
        }

        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // Usa el avatar actualmente seleccionado
        Usuario nuevoUsuario = new Usuario(
            userId,
            email,
            alias,
            GenerarCodigoUnico(),
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
                    Debug.Log("Datos Grabados en Real Time Database " + nuevoUsuario.idUsuario);
                }
                else
                {
                    Debug.LogError("Error al registrar el usuario: " + task.Exception);
                }
            });
    }


    private string GenerarCodigoUnico()
    {
        string caracteres = "0123456789";
        System.Random random = new System.Random();
        string codigo = "";

        for (int i = 0; i < 6; i++)
        {
            codigo += caracteres[random.Next(caracteres.Length)];
        }

        return codigo;
    }

    void LimpiarFormularioRegistro()
    {
        registerAlias.text = "";
        registerEmail.text = "";
        registerPassword.text = "";
    }

    public void AlClicBotonIniciarSesion()
    {
        IniciarSesionFirebase(loginEmail.text, loginPassword.text);
    }

    public void IniciarSesionFirebase(string email, string password)
    {
        if (email == "" || password == "")
        {
            Debug.Log("Correo o contraseña vacíos");
            MostrarMensaje("Correo o contraseña vacíos", true);
            return;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        Credential credential = EmailAuthProvider.GetCredential(email, password);

        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                string mensaje = "Error al iniciar sesión";

                if (task.Exception != null)
                {
                    foreach (var inner in task.Exception.Flatten().InnerExceptions)
                    {
                        if (inner is FirebaseException firebaseEx)
                        {
                            string errorMsg = firebaseEx.Message;

                            if (errorMsg.Contains("INVALID_PASSWORD"))
                                mensaje = "Contraseña incorrecta.";
                            else if (errorMsg.Contains("EMAIL_NOT_FOUND"))
                                mensaje = "El correo no está registrado.";
                            else if (errorMsg.Contains("INVALID_EMAIL"))
                                mensaje = "Correo electrónico inválido.";
                            else if (errorMsg.Contains("NETWORK_ERROR"))
                                mensaje = "Sin conexión. Revisa tu red.";
                            else
                                mensaje = "No se pudo iniciar sesión. Revisa tus datos.";
                        }
                    }
                }

                Debug.Log("Error al iniciar sesión: " + task.Exception);
                MostrarMensaje(mensaje, true);
                return;
            }

            AuthResult result = task.Result;
            FirebaseUser user = result.User;
            EstablecerEstadoConexion("online");

            Debug.LogFormat("Usuario conectado: {0} ({1})", user.Email, user.UserId);

            // Verificar si hay un email pendiente y sincronizar
            VerificarYActualizarEmailVerificado(user);

            loginScreenManager.GoToMainMenuScene();
        });
    }
    private void VerificarYActualizarEmailVerificado(FirebaseUser user)
    {
        user.ReloadAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                Debug.LogWarning("No se pudo recargar el usuario para verificar email.");
                return;
            }

            if (user.IsEmailVerified)
            {
                string userId = user.UserId;
                string correoActual = user.Email;

                referenciaBaseDeDatos.Child("usuarios").Child(userId).Child("emailPendienteVerificacion")
                    .GetValueAsync().ContinueWithOnMainThread(pendienteTask =>
                    {
                        if (pendienteTask.IsCompleted && pendienteTask.Result.Exists)
                        {
                            string emailPendiente = pendienteTask.Result.Value.ToString();

                            if (correoActual == emailPendiente)
                            {
                                referenciaBaseDeDatos.Child("usuarios").Child(userId).Child("email").SetValueAsync(correoActual);
                                referenciaBaseDeDatos.Child("usuarios").Child(userId).Child("emailPendienteVerificacion").RemoveValueAsync();

                                Debug.Log("Correo verificado y sincronizado con la base de datos.");
                            }
                            else
                            {
                                Debug.Log("El email actual no coincide con el pendiente. No se actualiza.");
                            }
                        }
                    });
            }
        });
    }

    public void AlClicBotonRestablecerContrasena()
    {
        string email = forgotPassword.text;

        if (string.IsNullOrEmpty(email))
        {
            Debug.Log("Por favor, introduce tu correo electrónico.");
            MostrarMensaje("Por favor, introduce tu correo electrónico.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Acción cancelada.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("Error al enviar el correo de recuperación: " + task.Exception);
                return;
            }

            Debug.Log("Correo de recuperación enviado a: " + email);
            loginScreenManager.ShowLoginPanel();
        });
    }

    private void MostrarMensaje(string mensaje, bool esLogin = false)
    {
        Text destino = esLogin ? mensajeTextoLogin : mensajeTextoRegistro;

        if (destino == null)
        {
            Debug.LogWarning("No se asignó el campo de mensaje.");
            return;
        }

        destino.text = mensaje;
        destino.gameObject.SetActive(true);

        CancelInvoke(nameof(EsconderMensaje));
        Invoke(nameof(EsconderMensaje), 3f);
    }

    private void EsconderMensaje()
    {
        if (mensajeTextoLogin != null) mensajeTextoLogin.gameObject.SetActive(false);
        if (mensajeTextoRegistro != null) mensajeTextoRegistro.gameObject.SetActive(false);
    }


    private void EstablecerEstadoConexion(string estado)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        DatabaseReference referenciaBD = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId);

        referenciaBD.Child("estadoConexion").SetValueAsync(estado).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Estado de conexión actualizado a: " + estado);
            }
            else
            {
                Debug.LogError("Error al actualizar estado de conexión: " + task.Exception);
            }
        });
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
            this.avatar = string.IsNullOrEmpty(avatar) ? "defaultAvatar" : avatar;
            this.puntuacionTotal = 0;
            this.estadoConexion = "offline";
            this.idAmigos = new List<string>();
            this.idPartidasJugadas = new List<string>();
        }

    }



}