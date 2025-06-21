using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;   // ¡Este NUNCA se desactiva!
    [SerializeField] private GameObject miniMenuPanel;   // Overlay lateral
    [SerializeField] private GameObject categoryPanel;   // Panel de categorías
    [SerializeField] private GameObject exitPanel; // Panel de confirmación de salida


    private void Start()
    {
        if (mainMenuPanel == null)
        {
            Debug.LogError("❌ mainMenuPanel está NULL en MenuManager", this);
        }
        else
        {
            mainMenuPanel.SetActive(true);
        }

        miniMenuPanel?.SetActive(false);
        categoryPanel?.SetActive(false);
    }


    /// <summary>
    /// Abre únicamente el menú lateral, sin tocar mainMenuPanel
    /// </summary>
    public void ShowMiniMenu()
    {
        Debug.Log("🔄 Mostrar miniMenuPanel");
        // hacemos que esté encima en la jerarquía (opcional)
        miniMenuPanel.transform.SetAsLastSibling();

        // activamos/desactivamos
        miniMenuPanel.SetActive(true);
        categoryPanel.SetActive(false);
    }

    /// <summary>
    /// Abre únicamente el panel de categorías, sin tocar mainMenuPanel
    /// </summary>
    public void ShowCategoriesPanel()
    {
        Debug.Log("🔢 Mostrar categoryPanel");
        categoryPanel.transform.SetAsLastSibling();

        categoryPanel.SetActive(true);
        miniMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Oculta ambos overlays (miniMenu y categorías), mainMenuPanel sigue activo
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("🔙 Volver a solo mainMenuPanel");
        miniMenuPanel.SetActive(false);
        categoryPanel.SetActive(false);
    }

    // --------------------------
    // Rutas de escena / botones
    // --------------------------

    public void GoToEditProfile()
    {
        Debug.Log("⚙️ Ir a EditProfile");
        SceneManager.LoadScene("EditProfile");
    }

    public void GoToFriendsScene()
    {
        Debug.Log("➡️ Ir a FriendsScene");
        SceneManager.LoadScene("FriendsScene");
    }

    public void GoToGlobalRanking()
    {
        Debug.Log("📊 Ir a GlobalRanking");
        SceneManager.LoadScene("GlobalRanking");
    }


    public void OnClickTrainingMode()
    {
        GameSettings.trainingMode = true;
        GameSettings.categoryMode = false;
        SceneManager.LoadScene("GameActionTraining");
    }

    public void OnClickMultiplayerMode()
    {
        GameSettings.trainingMode = false;
        GameSettings.categoryMode = false;
        SceneManager.LoadScene("GameAction");
    }

    /// <summary>
    /// Botón de cada categoría: asigna la clave y lanza la partida en modo categorías.
    /// </summary>
    public void OnClickCategory(string categoryKey)
    {
        Debug.Log($"🎯 Categoría seleccionada: {categoryKey}");
        GameSettings.trainingMode = false;
        GameSettings.categoryMode = true;
        GameSettings.categoryKey = categoryKey;
        SceneManager.LoadScene("GameLoad");
    }

    public void BackFromCategories()
    {
        // Vuelve al menú principal
        categoryPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    public void ShowExitPanel()
    {
        Debug.Log("🚪 Mostrar exitPanel");
        exitPanel.transform.SetAsLastSibling();
        exitPanel.SetActive(true);
    }

    /// <summary>
    /// Cancela la salida y oculta el panel de confirmación
    /// </summary>
    public void HideExitPanel()
    {
        Debug.Log("❌ Cancelar salida");
        exitPanel.SetActive(false);
    }

    /// <summary>
    /// Cierra completamente el juego (solo funciona en build)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("⏹ Cerrando el juego...");
        Application.Quit();

#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
