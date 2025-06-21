using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAudio : MonoBehaviour
{
    [Header("Configuración")]
    public AudioClip customSound; // Opcional: sonido específico para este botón
    public bool playDefaultClick = true; // Usar sonido de click por defecto
    public bool autoSetup = true; // Configurar automáticamente

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (autoSetup && button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    public void PlaySound()
    {
        if (AudioManager.instance == null) return;

        if (customSound != null)
        {
            AudioManager.instance.PlaySFX(customSound);
        }
        else if (playDefaultClick)
        {
            AudioManager.instance.PlayClickSound();
        }
    }

    // Métodos extra por si los necesitas desde Inspector
    public void PlayClickSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayClickSound();
        }
    }

    public void PlayCustomSound()
    {
        if (customSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(customSound);
        }
    }
}