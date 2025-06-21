using UnityEngine;
using UnityEngine.UI;

public class BlurPanelManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelAvatarSelection;
    public GameObject panelConfirmDelete;
    public GameObject panelStatistics;
    public GameObject panelEditProfile;
    public GameObject panelBlurEffect;
    private CanvasGroup editProfileCanvasGroup;

    [Header("Buttons")]
    public Button btnOpenAvatarSelection;
    public Button btnCancelAvatarSelection;
    public Button btnAcceptAvatarSelection;

    public Button btnDeleteAccount;
    public Button btnConfirmDelete;
    public Button btnCancelDelete;

    public Button btnOpenStatistics;
    public Button btnCloseStatistics;

    public Button btnEditAvatar;

    void Start()
    {
        editProfileCanvasGroup = panelEditProfile.GetComponent<CanvasGroup>();

        panelAvatarSelection.SetActive(false);
        panelConfirmDelete.SetActive(false);
        panelStatistics.SetActive(false);
        if (panelBlurEffect) panelBlurEffect.SetActive(false);

        if (btnOpenAvatarSelection)
            btnOpenAvatarSelection.onClick.AddListener(() => ShowPanel(panelAvatarSelection));
        if (btnCancelAvatarSelection)
            btnCancelAvatarSelection.onClick.AddListener(() => ClosePanel(panelAvatarSelection));
        if (btnAcceptAvatarSelection)
            btnAcceptAvatarSelection.onClick.AddListener(AcceptAvatarSelection);

        if (btnDeleteAccount)
            btnDeleteAccount.onClick.AddListener(() => ShowPanel(panelConfirmDelete));
        if (btnConfirmDelete)
            btnConfirmDelete.onClick.AddListener(ConfirmDelete);
        if (btnCancelDelete)
            btnCancelDelete.onClick.AddListener(() => ClosePanel(panelConfirmDelete));

        if (btnOpenStatistics)
            btnOpenStatistics.onClick.AddListener(() => ShowPanel(panelStatistics));
        if (btnCloseStatistics)
            btnCloseStatistics.onClick.AddListener(() => ClosePanel(panelStatistics));

        if (btnEditAvatar)
            btnEditAvatar.onClick.AddListener(() => ShowPanel(panelAvatarSelection));
    }

    private void ShowPanel(GameObject panel)
    {
        if (panelBlurEffect) panelBlurEffect.SetActive(true);
        panel.SetActive(true);

        if (editProfileCanvasGroup)
        {
            editProfileCanvasGroup.alpha = 0.4f;
            editProfileCanvasGroup.interactable = false;
            editProfileCanvasGroup.blocksRaycasts = false;
        }

        Debug.Log($" Se mostró el panel: {panel.name}");
    }

    private void ClosePanel(GameObject panel)
    {
        if (panelBlurEffect) panelBlurEffect.SetActive(false);
        panel.SetActive(false);

        if (editProfileCanvasGroup)
        {
            editProfileCanvasGroup.alpha = 1f;
            editProfileCanvasGroup.interactable = true;
            editProfileCanvasGroup.blocksRaycasts = true;
        }

        Debug.Log($" Se cerró el panel: {panel.name}");

        if (panel == panelAvatarSelection)
        {
            MostrarAvatarFirebase[] scripts = panelEditProfile.GetComponentsInChildren<MostrarAvatarFirebase>(true);
            foreach (var avatarScript in scripts)
            {
                Debug.Log($" Recargando avatar desde ClosePanel en: {avatarScript.gameObject.name}");
                avatarScript.RecargarAvatar();
            }
        }
    }

    private void AcceptAvatarSelection()
    {
        Debug.Log(" Avatar seleccionado y se aceptó el cambio.");
        ClosePanel(panelAvatarSelection);

        MostrarAvatarFirebase[] scripts = panelEditProfile.GetComponentsInChildren<MostrarAvatarFirebase>(true);
        foreach (var avatarScript in scripts)
        {
            Debug.Log($" Recargando avatar desde AcceptAvatarSelection en: {avatarScript.gameObject.name}");
            avatarScript.RecargarAvatar();
        }
    }

    private void ConfirmDelete()
    {
        Debug.Log(" Confirmación de eliminación de cuenta.");
        ClosePanel(panelConfirmDelete);
    }
}
