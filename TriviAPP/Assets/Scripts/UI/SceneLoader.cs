using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Metodo para cambiar de escena
    public void LoadScene(string sceneName)
    {
        Debug.Log("¡Clic!");
        SceneManager.LoadScene(sceneName);
    }
}
