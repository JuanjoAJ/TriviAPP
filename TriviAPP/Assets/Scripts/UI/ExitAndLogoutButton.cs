using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Photon.Pun;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;



public class ExitAndLogoutButton : MonoBehaviour
{
    [Header("Confirm Panel Setup")]
    [Tooltip("El panel que pregunta '¿Seguro que quieres salir?'")]
    public GameObject confirmPanel;
    [Tooltip("Botón 'Sí' dentro del panel")]
    public Button btnYes;
    [Tooltip("Botón 'No' dentro del panel")]
    public Button btnNo;

    [Header("Opciones de logout")]
    [Tooltip("Si quieres desconectar también de Photon")]
    public bool disconnectPhoton = true;

    void Awake()
    {
        // Ocultamos el panel al principio
        confirmPanel.SetActive(false);

        // Conectamos los listeners
        btnYes.onClick.AddListener(HandleYes);
        btnNo.onClick.AddListener(HandleNo);
    }

    /// <summary>
    /// Llamar a este método desde el OnClick() del botón de la puerta.
    /// </summary>
    public void OnExitPress()
    {
        // Mostramos el panel de confirmación
        confirmPanel.SetActive(true);
    }

    private void HandleNo()
    {
        // Oculta el panel y no hace nada más
        confirmPanel.SetActive(false);
    }

    private void HandleYes()
    {
        // Ocultamos el panel
        confirmPanel.SetActive(false);

        // ⬇️ NUEVO: establecer estado "offline" en Firebase
        ActualizarEstadoConexion("offline");

        // 1) Firebase sign out
        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log("Usuario de Firebase desconectado.");
        }

        // 2) Photon disconnect
        if (disconnectPhoton && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("PhotonNetwork desconectado.");
        }

        // 3) Quit application (en Editor no hará nada)
        Debug.Log("Saliendo de la aplicación...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ActualizarEstadoConexion(string estado)
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null) return;

        var dbRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(user.UserId);
        dbRef.Child("estadoConexion").SetValueAsync(estado)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("Estado de conexión actualizado a: " + estado);
                }
                else
                {
                    Debug.LogError("Error al actualizar estado: " + task.Exception);
                }
            });
    }

    private void OnApplicationQuit()
    {
        Debug.Log(" Aplicación cerrada. Estableciendo offline...");
        ActualizarEstadoConexion("offline");

        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log("Sesión cerrada automáticamente.");
        }

        if (disconnectPhoton && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("Photon desconectado automáticamente.");
        }
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log(" App en segundo plano. Usuario offline.");
            ActualizarEstadoConexion("offline");
        }
        else
        {
            if (FirebaseAuth.DefaultInstance.CurrentUser != null)
            {
                Debug.Log(" App retomada. Usuario online.");
                ActualizarEstadoConexion("online");
            }
        }
    }

}
