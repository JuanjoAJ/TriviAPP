using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;  // para PhotonNetwork

/// <summary>
/// Muestra un panel de confirmación para salir del juego y navega de vuelta al menú principal.
/// Diseñado para la escena GameAction.
/// </summary>
public class ExitGameManager : MonoBehaviour
{
    [Header("Panel de confirmación")]
    [Tooltip("Panel que aparece al pulsar Salir")]
    public GameObject panelConfirmExit;
    [Tooltip("Efecto de desenfoque de fondo (opcional)")]
    public GameObject panelBlurEffect;

    [Header("Botones")]
    public Button btnExit;   // Botón que abre el panel
    public Button btnYes;    // Botón de "Yes, exit"
    public Button btnNo;     // Botón de "No, cancel"

    void Start()
    {
        panelConfirmExit.SetActive(false);
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(false);

        if (btnExit != null)
            btnExit.onClick.AddListener(ShowExitPanel);
        if (btnYes != null)
            btnYes.onClick.AddListener(ConfirmExit);
        if (btnNo != null)
            btnNo.onClick.AddListener(HideExitPanel);
    }

    void ShowExitPanel()
    {
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(true);
        panelConfirmExit.SetActive(true);
    }

    void HideExitPanel()
    {
        if (panelBlurEffect != null)
            panelBlurEffect.SetActive(false);
        panelConfirmExit.SetActive(false);
    }

    void ConfirmExit()
    {
        // Oculta el panel antes de salir
        HideExitPanel();

        // Si estamos en una sala Photon, la dejamos
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("MainMenuScene");
            return;
        }

        // Si estamos conectados pero no en sala, desconectamos
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // Carga el menú principal
        SceneManager.LoadScene("MainMenuScene");
    }
}