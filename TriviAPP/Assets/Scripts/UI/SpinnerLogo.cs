using UnityEngine;

public class ScriptSpinner : MonoBehaviour
{
    public float speedRotation = 100f; // Velocidad en grados por segundo

    void Update()
    {
        transform.Rotate(Vector3.forward * -speedRotation * Time.deltaTime);
    }
}
