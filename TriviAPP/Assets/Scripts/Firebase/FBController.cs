using UnityEngine;
// Firebase
using Firebase.Extensions;
using Firebase.Auth;
using Firebase;
using TMPro;

public class FBController : MonoBehaviour
{
    private FirebaseAuth auth;
    public TMP_Text _statusLogin;
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
        DependencyStatus status = task.Result;
     if (status == DependencyStatus.Available)
    {
        InitializeFirebase();
    }
      else
    {
        Debug.Log ("Could not resolve all Firebase dependencies: " + status);
    }
    });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeFirebase()
{
        Debug.Log("Setting up Firebase Auth");
        _statusLogin.text = "Setting up Firebase Auth";

    //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;

}
}
