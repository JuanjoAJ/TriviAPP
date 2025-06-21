using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class DeleteUserManager : MonoBehaviour
{
    public GameObject panelConfirmDelete;

    public void OnClickDeleteAccount()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("⚠ No hay usuario autenticado.");
            return;
        }

        string userId = user.UserId;

        // 1️ Eliminar datos del usuario en Realtime Database
        FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId)
            .RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError(" Error al borrar datos en Firebase DB: " + task.Exception);
                    return;
                }

                Debug.Log(" Datos en la base de datos eliminados.");

                // 2️ Borrar cuenta de autenticación
                user.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                {
                    if (deleteTask.IsFaulted || deleteTask.IsCanceled)
                    {
                        Debug.LogError(" Error al eliminar cuenta: " + deleteTask.Exception);
                        return;
                    }

                    Debug.Log(" Cuenta eliminada correctamente.");
                    SceneManager.LoadScene("LoginScene"); 
                });
            });
    }

    public void OnClickCancel()
    {
        panelConfirmDelete.SetActive(false);
    }
}
