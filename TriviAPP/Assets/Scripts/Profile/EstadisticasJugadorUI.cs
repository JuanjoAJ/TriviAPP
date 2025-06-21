using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;

public class EstadisticasJugadorUI : MonoBehaviour
{
    public TMP_Text txtAlias;
    public TMP_Text txtID;
    public Image imgAvatar;

    public TMP_Text[] txtWinsArray; // Text_WinsResult, etc.

    void Start()
    {
        CargarDatosJugador();
    }

    void CargarDatosJugador()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("⚠ Usuario no autenticado");
            return;
        }

        FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(user.UserId)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.Result.Exists)
                {
                    Debug.LogWarning(" No se encontró el usuario");
                    return;
                }

                var data = task.Result;

                string alias = data.Child("alias").Value?.ToString() ?? "Sin alias";
                string id = data.Child("codigoUsuario").Value?.ToString() ?? "-------";
                string avatar = data.Child("avatar").Value?.ToString() ?? "default";
                string puntos = data.Child("puntuacionTotal").Value?.ToString() ?? "0";

                int ganadas = int.TryParse(data.Child("estadisticas/ganadas").Value?.ToString(), out int g) ? g : 0;
                int perdidas = int.TryParse(data.Child("estadisticas/perdidas").Value?.ToString(), out int p) ? p : 0;
                int acertadas = int.TryParse(data.Child("estadisticas/preguntasAcertadas").Value?.ToString(), out int a) ? a : 0;
                int falladas = int.TryParse(data.Child("estadisticas/preguntasFalladas").Value?.ToString(), out int f) ? f : 0;

                txtAlias.text = alias;
                txtID.text = $"ID: {id}";

                var sprite = AvatarManager.Instance.GetAvatarFor(avatar);
                if (sprite != null)
                    imgAvatar.sprite = sprite;

                // Asignar estadísticas
                if (txtWinsArray.Length >= 5)
                {
                    txtWinsArray[0].text = $"{ganadas} ganadas";
                    txtWinsArray[1].text = $"{perdidas} perdidas";
                    txtWinsArray[2].text = $"{acertadas} acertadas";
                    txtWinsArray[3].text = $"{falladas} falladas";
                    txtWinsArray[4].text = $"{puntos} puntos";
                }
            });
    }
}