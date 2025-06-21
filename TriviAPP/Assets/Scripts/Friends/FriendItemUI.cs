using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendItemUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI pointText;
    public TextMeshProUGUI idText;
    public Button deleteButton;


    public void Configurar(string alias, string puntos, string id, Sprite avatarSprite, System.Action onDelete)
    {
        nameText.text = alias;
        pointText.text = $"{puntos}";
        idText.text = $"ID: {id}";
        avatarImage.sprite = avatarSprite;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => onDelete?.Invoke());
    }
}
