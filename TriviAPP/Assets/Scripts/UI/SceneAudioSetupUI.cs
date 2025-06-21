using UnityEngine;

public class SceneAudioSetupUI : MonoBehaviour
{
    [Header("Música de esta escena")]
    public AudioClip sceneMusic; // Arrastra aquí la música de esta escena (o deja vacío)

    [Header("Configuración de Volumen")]
    [Range(0f, 1f)]
    public float sceneVolume = -1f; // -1 = usar volumen por defecto, 0-1 = volumen específico
    public bool useCustomVolume = false; // Marcar para usar volumen personalizado

    [Header("Configuración")]
    public bool autoChangeMusic = true; // Cambiar música automáticamente al cargar escena
    public bool resumeDefaultIfNoMusic = true; // Reanudar música por defecto si no hay música específica

    void Start()
    {
        SetupSceneAudio();
    }

    void SetupSceneAudio()
    {
        // Crear AudioManager si no existe
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("⚠️ Creando AudioManager automáticamente...");
            CreateAudioManager();
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Configurar volumen de esta escena
        if (useCustomVolume)
        {
            AudioManager.instance.SetSceneVolume(sceneVolume);
            Debug.Log($"🎚️ Volumen personalizado para {sceneName}: {sceneVolume}");
        }
        else
        {
            AudioManager.instance.UseDefaultVolume();
            Debug.Log($"🎚️ Usando volumen por defecto en {sceneName}");
        }

        // Si hay música específica para esta escena
        if (sceneMusic != null)
        {
            AudioManager.instance.RegisterSceneMusic(sceneName, sceneMusic);

            if (autoChangeMusic)
            {
                AudioManager.instance.PlayMusic(sceneMusic);
            }

            Debug.Log($"🎵 Música configurada para escena: {sceneName}");
        }
        else if (autoChangeMusic && resumeDefaultIfNoMusic)
        {
            // Si no hay música específica pero autoChangeMusic está activo,
            // intentar reanudar música por defecto
            Debug.Log($"🔄 Escena sin música específica, intentando reanudar por defecto...");
            AudioManager.instance.ResumeDefaultMusicIfNeeded();
        }
        else if (!useCustomVolume || sceneVolume > 0f)
        {
            // Si no hay configuración automática pero queremos música (volumen > 0),
            // asegurar que algo esté sonando
            AudioManager.instance.ResumeDefaultMusicIfNeeded();
            Debug.Log($"🔄 Asegurando música en escena sin configuración específica");
        }
        else
        {
            Debug.Log($"⏸️ Escena sin configuración de música automática");
        }
    }

    void CreateAudioManager()
    {
        GameObject audioManagerObj = new GameObject("AudioManager");
        audioManagerObj.AddComponent<AudioManager>();
    }

    // MÉTODOS PÚBLICOS PARA USAR DESDE INSPECTOR DE BOTONES
    public void PlayClickSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayClickSound();
        }
    }

    public void ToggleMute()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicMuted(!AudioManager.instance.IsMusicMuted());
        }
    }

    public void ChangeToSceneMusic()
    {
        if (sceneMusic != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlayMusic(sceneMusic);
        }
    }

    // MÉTODOS PARA CONTROL DE VOLUMEN
    public void SetCustomVolume(float volume)
    {
        if (AudioManager.instance != null)
        {
            sceneVolume = Mathf.Clamp01(volume);
            useCustomVolume = true;
            AudioManager.instance.SetSceneVolume(sceneVolume);
        }
    }

    public void UseDefaultVolume()
    {
        if (AudioManager.instance != null)
        {
            useCustomVolume = false;
            AudioManager.instance.UseDefaultVolume();
        }
    }

    public void SilenceMusic()
    {
        SetCustomVolume(0f);
    }
}