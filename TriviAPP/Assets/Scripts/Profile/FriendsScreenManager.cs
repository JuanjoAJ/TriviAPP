using UnityEngine;

public class FriendsScreenManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject addFriendPanel;
    [SerializeField] private GameObject viewFriendsPanel;

    private void Start()
    {
        addFriendPanel.SetActive(true);
        viewFriendsPanel.SetActive(false);
    }



public void ShowAddFriendsPanel()
    {
        Debug.Log("? Mostrando Panel de Agregar Amigos");
        panelSwitch(true);
    }

    public void ShowViewFriendsPanel()
    {
        Debug.Log("? Mostrando Panel de Ver Amigos");
        panelSwitch(false);
    }

    private void panelSwitch(bool showAddFriends)
    {
        addFriendPanel.SetActive(showAddFriends);
        viewFriendsPanel.SetActive(!showAddFriends);
    }

}
