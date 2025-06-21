using UnityEngine;
using UnityEngine.UI;

public class TimerBar : MonoBehaviour
{
    public Image fillImage; // La imagen que se va reduciendo
    public float totalTime = 10f; // Duración total del turno

    private float timeLeft;
    private bool running = false;

    public System.Action OnTimeOut; // Evento cuando el tiempo termina

    public void StartTimer()
    {
        timeLeft = totalTime;
        running = true;
        fillImage.fillAmount = 1f;
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        fillImage.fillAmount = Mathf.Clamp01(timeLeft / totalTime);

        if (timeLeft <= 0)
        {
            running = false;
            OnTimeOut?.Invoke(); // Llama a quien escuche este evento
        }
    }

    public void StopTimer()
    {
        running = false;
    }

    public float GetTimeLeft()
    {
        return timeLeft;
    }

}
