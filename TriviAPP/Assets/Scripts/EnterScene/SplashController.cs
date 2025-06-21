using System.Collections;      
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashController : MonoBehaviour
{
    [Tooltip("Tiempo en segundos que permanece esta escena antes de pasar a la siguiente")]
    public float delay = 5f;

    [Tooltip("Nombre de la siguiente escena")]
    public string nextSceneName = "LoginScene";

    private void Start()
    {
        StartCoroutine(WaitAndLoad());
    }

    private IEnumerator WaitAndLoad()
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(nextSceneName);
    }
}
