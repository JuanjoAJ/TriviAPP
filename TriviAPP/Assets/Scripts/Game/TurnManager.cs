using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public string currentPlayer; // El que ganó el turno
    public string localPlayerName = "jugador4"; // PRUEBA LOCAL

    public Animator currentAvatarAnimator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanPlayerBuzz(string playerName)
    {
        // Evita que jugadores vacíos ganen turno
        return string.IsNullOrEmpty(currentPlayer) && !string.IsNullOrEmpty(playerName);
    }

    public void SetActivePlayer(string playerName, Animator avatarAnim)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("SetActivePlayer recibió un nombre vacío o nulo.");
            return;
        }

        currentPlayer = playerName;
        currentAvatarAnimator = avatarAnim;

        Debug.Log($" {playerName} ha pulsado — Local: {localPlayerName}");

        if (playerName == localPlayerName)
        {
            Debug.Log(" Es el jugador local. Activamos respuestas.");
            GameController.Instance?.EnableAnswerButtons(true);
        }
        else
        {
            Debug.Log(" No es el jugador local. Bloqueamos respuestas.");
            GameController.Instance?.EnableAnswerButtons(false);
        }
    }

    public void ResetTurn()
    {
        currentPlayer = null;
        currentAvatarAnimator = null;
        GameController.Instance?.EnableAnswerButtons(false);
    }

    public void DisableAllAvatarButtons()
    {
        var handlers = FindObjectsByType<AvatarClickHandler>(FindObjectsSortMode.None);
        foreach (var handler in handlers)
        {
            var btn = handler.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }
            else
            {
                // Buscamos el botón en el padre o hijos (ej: ButtonLogImg)
                var found = handler.GetComponentInParent<Button>() ?? handler.GetComponentInChildren<Button>();
                if (found != null)
                {
                    found.interactable = false;
                }
                else
                {
                    Debug.LogWarning($" El objeto {handler.name} no tiene un botón asignado.");
                }
            }
        }
    }
}
