// Assets/Scripts/Game/ExitPanelManager.cs
using UnityEngine;
using UnityEngine.UI;

public class ExitPanelManager : MonoBehaviour
{
    [Header("Panel de confirmación")]
    [Tooltip("Panel que aparece al pulsar Salir")]
    public GameObject panelConfirmExit;
    [Tooltip("Efecto de desenfoque de fondo (opcional)")]
    public GameObject panelBlurEffect;

    [Header("Botones")]
    public Button btnExit;   // Botón que abre el panel
    public Button btnYes;    // Botón de "Sí, salir"
    public Button btnNo;     // Botón de "No, cancelar"

    void Start()
    {
        panelConfirmExit.SetActive(false);
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(false);

        // Asignar listeners a los botones
        if (btnExit != null)
            btnExit.onClick.AddListener(ShowExitPanel);

        if (btnYes != null)
            btnYes.onClick.AddListener(ConfirmExit);

        if (btnNo != null)
            btnNo.onClick.AddListener(HideExitPanel);
    }

    /// <summary>
    /// Muestra el panel de confirmación (y el desenfoque si existe).
    /// </summary>
    void ShowExitPanel()
    {
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(true);

        panelConfirmExit.SetActive(true);
    }

    /// <summary>
    /// Oculta el panel de confirmación (y el desenfoque si existe).
    /// </summary>
    void HideExitPanel()
    {
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(false);

        panelConfirmExit.SetActive(false);
    }

    /// <summary>
    /// Confirmación de salir: busca el PhotonPrueba y ejecuta la salida.
    /// </summary>
    void ConfirmExit()
    {
        // Requiere Unity 2023.1+ para FindFirstObjectByType
        PhotonPrueba photonPrueba = FindFirstObjectByType<PhotonPrueba>();
        if (photonPrueba != null)
        {
            photonPrueba.LeaveRoomAndReturnToMenu();
        }
        else
        {
            Debug.LogWarning("No se encontró ningún PhotonPrueba en la escena.");
        }
    }
}
