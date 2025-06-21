using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class InternetTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CheckConnection());
    }

    IEnumerator CheckConnection()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://opentdb.com/api.php?amount=1");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(" ¡Conexión a internet funcionando!");
            Debug.Log(" Pregunta recibida: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError(" Error de conexión: " + request.error);
        }
    }
}
