using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrainingController : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float delayBeforeNextQuestion = 2f;
    [SerializeField] private int totalQuestions = 3;
    [Tooltip("Puntos máximos a restar por respuesta incorrecta")]
    [SerializeField] private int basePenalty = 5;

    [Header("UI de pregunta")]
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private List<Button> answerButtons;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;

    [Header("Timer")]
    public TimerBar timerBar;

    [Header("Avatar único")]
    [SerializeField] private Image singleAvatarImage;

    private string currentCategory;
    private int correctAnswerIndex;
    private bool answered = false;
    private int currentQuestionNumber = 1;

    private void Start()
    {
        // Validaciones iniciales
        if (numberText == null) Debug.LogError("[TrainingController] numberText NO asignado.");
        if (questionText == null) Debug.LogError("[TrainingController] questionText NO asignado.");
        if (answerButtons == null || answerButtons.Count == 0)
            Debug.LogError("[TrainingController] answerButtons NO asignados o lista vacía.");
        if (timerBar == null) Debug.LogError("[TrainingController] timerBar NO asignado.");
        if (singleAvatarImage == null) Debug.LogError("[TrainingController] singleAvatarImage NO asignado.");

        // Iniciar el flujo de entrenamiento
        StartCoroutine(FetchLocalQuestions());
    }

    private IEnumerator FetchLocalQuestions()
    {
        var translator = Object.FindFirstObjectByType<TriviaTranslator>();
        if (translator == null)
        {
            Debug.LogError("[TrainingController] No encontré TriviaTranslator en la escena.");
            yield break;
        }

        while (currentQuestionNumber <= totalQuestions)
        {
            yield return StartCoroutine(
                translator.GetTranslatedQuestion((cat, ques, opts, idx) =>
                {
                    currentCategory = cat;
                    SetupQuestion(ques, opts.ToList(), idx);
                })
            );

            // Esperar respuesta
            yield return new WaitUntil(() => answered);
            answered = false;
            yield return new WaitForSeconds(delayBeforeNextQuestion);
        }

        FinishTraining();
    }

    private void SetupQuestion(string question, List<string> answers, int correctIdx)
    {
        // Comprueba referencias antes de seguir
        if (numberText == null || questionText == null || answerButtons == null || answerButtons.Count == 0)
        {
            Debug.LogError("[TrainingController] No puedo configurar la pregunta: referencias UI faltantes.");
            return;
        }

        // Actualiza el contador y el texto
        numberText.text = $"Pregunta {currentQuestionNumber} de {totalQuestions}";
        questionText.text = WebUtility.HtmlDecode(question);
        correctAnswerIndex = correctIdx;
        currentQuestionNumber++;

        // Asegura que tenemos suficientes botones para las opciones
        if (answers.Count > answerButtons.Count)
        {
            Debug.LogWarning($"[TrainingController] Hay más opciones ({answers.Count}) que botones ({answerButtons.Count}).");
        }

        // Configura cada botón según exista una opción
        for (int i = 0; i < answerButtons.Count; i++)
        {
            var btn = answerButtons[i];
            btn.onClick.RemoveAllListeners();
            if (i < answers.Count)
            {
                int idx = i;
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = WebUtility.HtmlDecode(answers[i]);

                btn.onClick.AddListener(() => OnAnswerSelected(idx));
                ResetButtonColor(btn);
                btn.interactable = true;
            }
            else
            {
                // Si no hay más opciones, desactiva botones sobrantes
                btn.gameObject.SetActive(false);
            }
        }

        // Lanza el temporizador
        if (timerBar != null)
        {
            timerBar.StartTimer();
            timerBar.OnTimeOut = () => OnAnswerSelected(-1);
        }
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (answered) return;
        answered = true;

        // Para el temporizador
        timerBar?.StopTimer();

        // Colorea y bloquea los botones
        for (int i = 0; i < answerButtons.Count; i++)
        {
            var btn = answerButtons[i];
            btn.interactable = false;
            if (i == correctAnswerIndex) SetButtonColor(btn, correctColor);
            else if (i == selectedIndex) SetButtonColor(btn, incorrectColor);
        }

        // Acumula estadística para “Entrenamiento”
        var stats = GameStats.Instance.GetPlayer("Entrenamiento");
        if (stats == null)
            stats = GameStats.Instance.GetPlayer("Entrenamiento"); // GetPlayer crea si no existe

        if (selectedIndex == correctAnswerIndex)
            stats.correct++;
        else
        {
            stats.wrong++;
            float used = timerBar.totalTime - timerBar.GetTimeLeft();
            float ratio = Mathf.Clamp01(used / timerBar.totalTime);
            int penalty = Mathf.CeilToInt(basePenalty * (1f - ratio));
            stats.totalPoints = Mathf.Max(0, stats.totalPoints - penalty * 100);
        }

        // Próxima pregunta tras el retraso
        StartCoroutine(NextAfterDelay());
    }

    private IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNextQuestion);
        // El bucle de FetchLocalQuestions continuará
    }

    private void FinishTraining()
    {
        Debug.Log("[TrainingController] Entrenamiento finalizado");
        var stats = GameStats.Instance.GetPlayer("Entrenamiento");
        Debug.Log($"[TrainingController] Resultados → C:{stats.correct}, F:{stats.wrong}, P:{stats.totalPoints}");
        // Aquí podrías mostrar un panel de resultados
        SceneManager.LoadScene("GameRankingTraining1");
    }

    private void SetButtonColor(Button btn, Color col)
    {
        if (btn.TryGetComponent<Image>(out var img))
            img.color = col;
        var cb = btn.colors;
        cb.normalColor = col;
        cb.highlightedColor = col * 1.2f;
        btn.colors = cb;
    }

    private void ResetButtonColor(Button btn)
    {
        if (btn.TryGetComponent<Image>(out var img))
            img.color = Color.white;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white * 1.2f;
        btn.colors = cb;
    }
}
