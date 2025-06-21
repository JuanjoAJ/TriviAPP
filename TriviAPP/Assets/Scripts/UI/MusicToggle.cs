using UnityEngine;

public class MusicToggle : MonoBehaviour
{
    public GameObject iconSound; // Hijo con el icono de sonido
    public GameObject iconMute;  // Hijo con el icono de silencio

    private void Start()
    {
        Debug.Log("🎵 MusicToggle Start");
        UpdateIcons();
    }

    public void ToggleMusic()
    {
        if (AudioManager.instance == null) return;

        // Accedemos directamente al estado actual de mute y lo invertimos
        bool currentlyMuted = AudioManager.instance.IsMusicMuted();
        bool newMuteState = !currentlyMuted;

        Debug.Log($"🔁 Cambiando estado de la música. ¿Actualmente silenciado? {currentlyMuted} → Nuevo estado: {newMuteState}");

        AudioManager.instance.SetMusicMuted(newMuteState);
        UpdateIcons();
    }


    private void UpdateIcons()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("⚠️ AudioManager no disponible en UpdateIcons");
            return;
        }

        bool isMusicOn = AudioManager.instance.IsMusicOn();
        Debug.Log($"🎚 Actualizando iconos. Música activa: {isMusicOn}");

        if (iconSound == null || iconMute == null)
        {
            Debug.LogError("❌ iconSound o iconMute no asignados en el Inspector");
            return;
        }

        iconSound.SetActive(isMusicOn);
        iconMute.SetActive(!isMusicOn);
    }
}
