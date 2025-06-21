using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;

public class EditProfileController : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField aliasInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("UI")]
    public TMP_Text mensajeText;
    public Button updateButton;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private DatabaseReference referenciaBaseDeDatos;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        referenciaBaseDeDatos = FirebaseDatabase.DefaultInstance.RootReference;

        updateButton.onClick.AddListener(() => StartCoroutine(ActualizarPerfil()));
    }

    void MostrarMensaje(string mensaje)
    {
        mensajeText.text = mensaje;
        Debug.Log("Mensaje UI: " + mensaje);
    }

    IEnumerator ActualizarPerfil()
    {
        string nuevoAlias = aliasInput.text.Trim();
        string nuevoEmail = emailInput.text.Trim();
        string nuevaPassword = passwordInput.text;
        string confirmarPassword = confirmPasswordInput.text;

        if (!string.IsNullOrEmpty(nuevaPassword) || !string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            if (nuevaPassword != confirmarPassword)
            {
                MostrarMensaje("Las contraseñas no coinciden.");
                yield break;
            }
        }

        bool cambios = false;
        Dictionary<string, object> actualizaciones = new Dictionary<string, object>();

        // Verificación y actualización del alias
        if (!string.IsNullOrEmpty(nuevoAlias))
        {
            var aliasCheck = referenciaBaseDeDatos.Child("usuarios").OrderByChild("alias").EqualTo(nuevoAlias).GetValueAsync();
            yield return new WaitUntil(() => aliasCheck.IsCompleted);

            bool aliasEnUsoPorOtro = false;
            if (aliasCheck.Result.Exists)
            {
                foreach (var usuario in aliasCheck.Result.Children)
                {
                    if (usuario.Key != currentUser.UserId)
                    {
                        aliasEnUsoPorOtro = true;
                        break;
                    }
                }
            }

            if (aliasEnUsoPorOtro)
            {
                MostrarMensaje("Alias ya está en uso.");
                yield break;
            }
            else
            {
                actualizaciones["alias"] = nuevoAlias;
                cambios = true;
            }
        }

        // Verificación y actualización del correo
        if (!string.IsNullOrEmpty(nuevoEmail) && nuevoEmail != currentUser.Email)
        {
            var emailCheck = referenciaBaseDeDatos.Child("usuarios").OrderByChild("email").EqualTo(nuevoEmail).GetValueAsync();
            yield return new WaitUntil(() => emailCheck.IsCompleted);

            bool emailEnUsoPorOtro = false;
            if (emailCheck.Result.Exists)
            {
                foreach (var usuario in emailCheck.Result.Children)
                {
                    if (usuario.Key != currentUser.UserId)
                    {
                        emailEnUsoPorOtro = true;
                        break;
                    }
                }
            }

            if (emailEnUsoPorOtro)
            {
                MostrarMensaje("El correo ya está registrado.");
                yield break;
            }

            var emailUpdateTask = currentUser.SendEmailVerificationBeforeUpdatingEmailAsync(nuevoEmail);
            yield return new WaitUntil(() => emailUpdateTask.IsCompleted);

            if (emailUpdateTask.Exception != null)
            {
                MostrarMensaje("Error al enviar correo de verificación.");
                yield break;
            }

            actualizaciones["emailPendienteVerificacion"] = nuevoEmail;
            cambios = true;
        }

        // Actualización de contraseña
        if (!string.IsNullOrEmpty(nuevaPassword))
        {
            var passwordUpdateTask = currentUser.UpdatePasswordAsync(nuevaPassword);
            yield return new WaitUntil(() => passwordUpdateTask.IsCompleted);

            if (passwordUpdateTask.Exception != null)
            {
                MostrarMensaje("Error al actualizar la contraseña.");
                yield break;
            }

            cambios = true;
        }

        // Aplicar actualizaciones en la base de datos
        if (actualizaciones.Count > 0)
        {
            referenciaBaseDeDatos
                .Child("usuarios")
                .Child(currentUser.UserId)
                .UpdateChildrenAsync(actualizaciones)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Debug.Log("Datos actualizados correctamente.");
                    }
                    else
                    {
                        Debug.LogError("Error al actualizar datos: " + task.Exception);
                        MostrarMensaje("Error al guardar los cambios.");
                    }
                });
        }

        if (cambios)
        {
            MostrarMensaje("Perfil actualizado correctamente.");
        }
        else
        {
            MostrarMensaje("No se realizaron cambios.");
        }
    }
}
