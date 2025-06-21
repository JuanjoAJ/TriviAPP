using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    [Header("Configuración de Audio")]
    public float musicVolume = 0.2f;
    public float sfxVolume = 1f;

    [Header("Clips de Audio")]
    public AudioClip defaultMusic;
    public AudioClip clickSound;

    // Diccionario para almacenar músicas por escena
    private Dictionary<string, AudioClip> sceneMusic = new Dictionary<string, AudioClip>();

    // Control de volumen por escena
    private float currentSceneVolume = -1f; // -1 significa usar volumen por defecto

    void Awake()
    {
        // Singleton mejorado
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudio();
    }

    void InitializeAudio()
    {
        // Crear AudioSource para música
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        // Crear AudioSource para efectos
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        // Reproducir música por defecto
        if (defaultMusic != null && musicSource.clip == null)
        {
            PlayMusic(defaultMusic);
        }

        Debug.Log("🎵 AudioManager inicializado correctamente");
    }

    // MÉTODOS DE MÚSICA
    public void PlayMusic(AudioClip newMusic)
    {
        if (newMusic == null) return;

        if (musicSource.clip != newMusic)
        {
            musicSource.clip = newMusic;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
            Debug.Log($"🎼 Música cambiada a: {newMusic.name}");
        }

        // Asegurar que el volumen esté correcto
        UpdateMusicVolume();
    }

    public void SetSceneVolume(float volume)
    {
        currentSceneVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = currentSceneVolume;
            Debug.Log($"🎚️ Volumen de escena establecido a: {currentSceneVolume}");
        }
    }

    public void UseDefaultVolume()
    {
        currentSceneVolume = -1f;
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            Debug.Log($"🎚️ Usando volumen por defecto: {musicVolume}");
        }
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            float targetVolume = currentSceneVolume >= 0f ? currentSceneVolume : musicVolume;
            musicSource.volume = targetVolume;
        }
    }

    public void ResumeDefaultMusicIfNeeded()
    {
        Debug.Log($"🔍 ResumeDefaultMusicIfNeeded - clip: {(musicSource.clip != null ? musicSource.clip.name : "null")}, isPlaying: {musicSource.isPlaying}");

        if (musicSource != null && defaultMusic != null)
        {
            if (musicSource.clip == null)
            {
                // No hay clip, asignar y reproducir música por defecto
                PlayMusic(defaultMusic);
                Debug.Log("🔄 Reanudando música por defecto (sin clip)");
            }
            else if (!musicSource.isPlaying)
            {
                // Hay clip pero no se está reproduciendo, reproducir
                musicSource.Play();
                UpdateMusicVolume();
                Debug.Log("🔄 Reanudando reproducción de música existente");
            }
            else if (musicSource.clip != defaultMusic)
            {
                // Se está reproduciendo otra música, cambiar a por defecto
                PlayMusic(defaultMusic);
                Debug.Log("🔄 Cambiando a música por defecto");
            }
            else
            {
                Debug.Log("✅ Música por defecto ya se está reproduciendo");
                UpdateMusicVolume();
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No se puede reanudar: musicSource o defaultMusic es null");
        }
    }

    public void RegisterSceneMusic(string sceneName, AudioClip music)
    {
        if (music != null)
        {
            sceneMusic[sceneName] = music;
        }
    }

    public void PlaySceneMusic(string sceneName)
    {
        if (sceneMusic.ContainsKey(sceneName))
        {
            PlayMusic(sceneMusic[sceneName]);
        }
    }

    // MÉTODOS DE EFECTOS
    public void PlayClickSound()
    {
        if (clickSound != null)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // MÉTODOS DE CONTROL
    public void SetMusicMuted(bool isMuted)
    {
        if (musicSource != null)
        {
            musicSource.mute = isMuted;
        }

        if (sfxSource != null)
        {
            sfxSource.mute = isMuted;
        }

        Debug.Log($"🔇 Audio {(isMuted ? "silenciado" : "activado")}");
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        // Solo actualizar si no hay volumen de escena específico
        if (currentSceneVolume < 0f)
        {
            UpdateMusicVolume();
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // MÉTODOS DE CONSULTA
    public bool IsMusicMuted()
    {
        return musicSource != null ? musicSource.mute : false;
    }

    public bool IsMusicOn()
    {
        return !IsMusicMuted();
    }

    public bool IsVolumeAtZero()
    {
        return musicSource != null && musicSource.volume == 0f;
    }

    public float GetCurrentVolume()
    {
        return musicSource != null ? musicSource.volume : 0f;
    }

    // EVENTOS DE ESCENA
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        PlaySceneMusic(scene.name);
        Debug.Log($"🎬 Escena cargada: {scene.name}");
    }
}