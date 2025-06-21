using UnityEngine;

public class AvatarClickHandler : MonoBehaviour
{
    [SerializeField]  public Animator avatarAnimator;
    [SerializeField]  public string playerName;

    public void OnClickAvatar()
    {
        if (TurnManager.Instance.CanPlayerBuzz(playerName))
        {
            avatarAnimator.SetTrigger("PlayBounce"); // Solo este avatar
            TurnManager.Instance.SetActivePlayer(playerName, avatarAnimator);

        }
    }
}
