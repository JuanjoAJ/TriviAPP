using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton que mapea avatarName → Sprite.
/// avatarName viene de Firebase ("girl1", "boy2", etc.)
/// </summary>
public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance;

    [System.Serializable]
    public struct AvatarEntry
    {
        public string avatarName;        // ← antes era "playerName"
        public Sprite avatarSprite;
    }

    [Header("Lista de avatares por nombre de avatar (ej: monstruo_3)")]
    public List<AvatarEntry> avatars = new List<AvatarEntry>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Sprite GetAvatarFor(string avatarName)
    {
        foreach (var entry in avatars)
        {
            if (entry.avatarName == avatarName)
                return entry.avatarSprite;
        }
        return null;
    }
}