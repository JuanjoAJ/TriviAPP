using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class MostrarAvatarFirebase : MonoBehaviour
{
    public Image avatarImage;

    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.GetInstance("https://triviapp-8f3d8-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
    }

    private void OnEnable()
    {
        RecargarAvatar();
    }

    public void RecargarAvatar()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (dbRef == null) dbRef = FirebaseDatabase.GetInstance("https://triviapp-8f3d8-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;

        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;

            dbRef.Child("usuarios").Child(userId).Child("avatar").GetValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        string nombreAvatar = task.Result.Value.ToString();
                        Sprite sprite = AvatarManager.Instance.GetAvatarFor(nombreAvatar);

                        if (sprite != null)
                        {
                            avatarImage.sprite = sprite;
                            Debug.Log("Avatar recargado correctamente.");
                        }
                        else
                        {
                            Debug.LogWarning("No se encontró sprite para: " + nombreAvatar);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No se encontró avatar en Firebase.");
                    }
                });
        }
    }

}
