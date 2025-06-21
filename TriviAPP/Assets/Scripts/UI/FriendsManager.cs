using UnityEngine;
using UnityEngine.UI;

public class FriendsManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    public GameObject addFriendPanel;      // Panel para agregar amigos
    public GameObject viewFriendsPanel;    // Panel para ver lista de amigos

    [Header("Elementos de UI")]
    public Toggle toggleView;              // Toggle para alternar entre los paneles

    private void Start()
    {
        // Escuchar el cambio del toggle
        toggleView.onValueChanged.AddListener(SwitchView);

        // Mostrar el panel de agregar amigo por defecto
        addFriendPanel.SetActive(true);
        viewFriendsPanel.SetActive(false);
    }

    // Cambiar de vista según el estado del toggle
    private void SwitchView(bool isViewingFriends)
    {
        addFriendPanel.SetActive(!isViewingFriends);
        viewFriendsPanel.SetActive(isViewingFriends);
    }
}