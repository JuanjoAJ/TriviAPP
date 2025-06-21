using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class TrainingRankingController : MonoBehaviour
{
    [Header("Entrada única de jugador")]
    [SerializeField] private Image img_Avatar1;
    [SerializeField] private TextMeshProUGUI txt_PlayerName1;

    [Header("Panel de estadísticas")]
    [SerializeField] private TextMeshProUGUI txt_CorrectAnswers;
    [SerializeField] private TextMeshProUGUI txt_WrongAnswers;

    [Header("Fallback")]
    [SerializeField] private Sprite defaultAvatar;

    private void Start()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("[TrainingRanking] No hay usuario logueado");
            CargarFallback();
            return;
        }

        string userId = user.UserId;
        var dbRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId);

        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                string alias = task.Result.Child("alias").Value?.ToString() ?? "Entrenamiento";
                string avatarKey = task.Result.Child("avatar").Value?.ToString() ?? "defaultAvatar";

                alias = Capitalizar(alias);

                // Transferencia de estadísticas
                var original = GameStats.Instance.GetPlayer("Entrenamiento");
                var p = GameStats.Instance.GetPlayer(alias);

                if (original != null)
                {
                    p.correct = original.correct;
                    p.wrong = original.wrong;
                    p.totalPoints = original.totalPoints;

                    // Limpieza del jugador "Entrenamiento"
                    GameStats.Instance.RemovePlayer("Entrenamiento");
                }

                p.avatarName = avatarKey;

                txt_PlayerName1.text = alias;
                var sprite = AvatarManager.Instance?.GetAvatarFor(avatarKey);
                img_Avatar1.sprite = sprite != null ? sprite : defaultAvatar;

                txt_CorrectAnswers.text = $"Aciertos: {p.correct}";
                txt_WrongAnswers.text = $"Fallos: {p.wrong}";

                Debug.Log($"[TrainingRanking] ✅ Alias={alias}, Avatar={avatarKey}, Correctas={p.correct}, Fallos={p.wrong}");
            }
            else
            {
                Debug.LogWarning("[TrainingRanking] ❌ No se pudieron cargar los datos desde Firebase");
                CargarFallback();
            }
        });
    }

    private void CargarFallback()
    {
        var p = GameStats.Instance.GetPlayer("Entrenamiento");
        if (p == null)
        {
            Debug.LogWarning("[TrainingRanking] No existe PlayerStat para 'Entrenamiento'");
            return;
        }

        txt_PlayerName1.text = p.playerName;
        var sprite = AvatarManager.Instance?.GetAvatarFor(p.avatarName);
        img_Avatar1.sprite = sprite != null ? sprite : defaultAvatar;
        txt_CorrectAnswers.text = $"Aciertos: {p.correct}";
        txt_WrongAnswers.text = $"Fallos: {p.wrong}";
    }

    private string Capitalizar(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
