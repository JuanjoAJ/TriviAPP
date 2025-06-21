
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using Photon.Pun;
using Photon.Realtime;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class GameController : MonoBehaviourPunCallbacks
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
    [SerializeField] private Button commonAnswerButton;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;


    [Header("Timer")]
    public TimerBar timerBar;

    [Header("UI Jugadores")]
    [SerializeField] private PlayerSlotManager playerSlotManager;


    // Estado interno
    private string currentCategory;      // Categoría de la pregunta actual
    private int correctAnswerIndex;      // Índice de la respuesta correcta
    private bool answered = false;       // Para controlar que no se responda dos veces
    private int currentQuestionNumber = 1; // Contador de preguntas mostradas

    public static GameController Instance;

    private void Awake()
    {
        // Singleton para acceder fácilmente desde otras clases
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (timerBar == null) Debug.LogError("[GameController] TimerBar NO asignado.");
        if (commonAnswerButton == null) Debug.LogError("[GameController] commonAnswerButton NO asignado.");
        if (numberText == null) Debug.LogError("[GameController] numberText NO asignado.");
        if (questionText == null) Debug.LogError("[GameController] questionText NO asignado.");
        if (answerButtons == null || answerButtons.Count == 0)
            Debug.LogError("[GameController] answerButtons NO asignados.");

        bool trainingMode = GameSettings.trainingMode;
        bool categoryMode = GameSettings.categoryMode;
        string categoryKey = GameSettings.categoryKey;
        Debug.Log($"[GameController] trainingMode={trainingMode}, categoryMode={categoryMode}, categoryKey={categoryKey}");

        GameStats.Instance.ResetStats();

        if (trainingMode)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user != null)
            {
                string userId = user.UserId;
                var userRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(userId);

                userRef.GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        string alias = task.Result.Child("alias").Value?.ToString() ?? user.Email;
                        string avatar = task.Result.Child("avatar").Value?.ToString() ?? "defaultAvatar";

                        alias = Capitalizar(alias);
                        PhotonNetwork.NickName = alias;

                        var stats = GameStats.Instance.GetPlayer(alias);
                        stats.avatarName = avatar;

                        Debug.Log($"[Entrenamiento] Alias: {alias}, Avatar: {avatar}");

                        StartCoroutine(FetchLocalQuestions());
                    }
                    else
                    {
                        Debug.LogWarning("[GameController]  No se pudieron obtener los datos del usuario. Continuando como 'Entrenamiento'.");
                        FallbackTrainingSetup();
                    }
                });
            }
            else
            {
                FallbackTrainingSetup();
            }

            return; // Salimos para esperar Firebase
        }
        else
        {
            if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            {
                PhotonNetwork.NickName = "Jugador" + Random.Range(1000, 9999);
                Debug.LogWarning("[GameController] Nickname vacío. Asignado aleatorio.");
            }

            foreach (var pl in PhotonNetwork.PlayerList)
            {
                if (!string.IsNullOrEmpty(pl.NickName) && !pl.NickName.StartsWith("Jugador"))
                    GameStats.Instance.GetPlayer(pl.NickName);
            }

            commonAnswerButton.onClick.AddListener(OnCommonAnswerButtonPressed);
            commonAnswerButton.interactable = true;

            bool canStart = PhotonNetwork.CurrentRoom != null
                            && PhotonNetwork.CurrentRoom.PlayerCount >= 2
                            && PhotonNetwork.IsMasterClient;

            if (!canStart)
                Debug.Log("Esperando a MasterClient con ≥2 jugadores para lanzar la pregunta…");
            else
                StartCoroutine(FetchAndBroadcastQuestion());
        }
    }
    private void FallbackTrainingSetup()
    {
        PhotonNetwork.NickName = "Entrenamiento";
        GameStats.Instance.GetPlayer("Entrenamiento").avatarName = "defaultAvatar";
        StartCoroutine(FetchLocalQuestions());
    }

    private string Capitalizar(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    private IEnumerator FetchLocalQuestions()
    {
        var translator = FindFirstObjectByType<TriviaTranslator>();
        if (translator == null)
        {
            Debug.LogError("[GameController] No encontré TriviaTranslator.");
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

            yield return new WaitUntil(() => answered);
            answered = false;
            yield return new WaitForSeconds(delayBeforeNextQuestion);
        }

        FinishTraining();
    }

    private void FinishTraining()
    {
        // Muestra estadísticas por consola
        Debug.Log("[GameController] Entrenamiento terminado.");
        var p = GameStats.Instance.GetPlayer(PhotonNetwork.NickName);
        Debug.Log($"Correctas={p.correct}, Fallos={p.wrong}, Puntos={p.totalPoints}");
    }

    // =====================================
    // MODO ONLINE: CATEGORÍAS o CLÁSICO
    // =====================================
    private IEnumerator FetchAndBroadcastQuestion()
    {
        var translator = FindFirstObjectByType<TriviaTranslator>();
        if (translator == null)
        {
            Debug.LogError("[GameController] No encontré TriviaTranslator.");
            yield break;
        }

        if (GameSettings.categoryMode)
        {
            // Pregunta filtrada por categoría
            yield return StartCoroutine(
                translator.GetCategoryQuestion(
                    GameSettings.categoryKey,
                    (cat, ques, opts, idx) =>
                    {
                        currentCategory = cat;
                        photonView.RPC(
                            nameof(RPCSetupQuestion),
                            RpcTarget.AllBuffered,
                            ques, opts.ToArray(), idx
                        );
                    })
            );
        }
        else
        {
            // Pregunta aleatoria global
            yield return StartCoroutine(
                translator.GetTranslatedQuestion((cat, ques, opts, idx) =>
                {
                    currentCategory = cat;
                    photonView.RPC(
                        nameof(RPCSetupQuestion),
                        RpcTarget.AllBuffered,
                        ques, opts.ToArray(), idx
                    );
                })
            );
        }
    }

    // ===================
    // BOTÓN COMÚN CENTRAL
    // ===================
    public void OnCommonAnswerButtonPressed()
    {
        if (!PhotonNetwork.IsConnected || string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            Debug.LogWarning("[GameController] No estás conectado o el nombre es inválido.");
            return;
        }

        commonAnswerButton.interactable = false;
        // Solicita a MasterClient que inicie el turno de este jugador
        photonView.RPC(
            nameof(RequestStartTurn),
            RpcTarget.MasterClient,
            PhotonNetwork.NickName
        );
    }

    [PunRPC]
    void RequestStartTurn(string playerName, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Notifica a todos quién comienza
        photonView.RPC(
            nameof(StartTurnForPlayer),
            RpcTarget.AllBuffered,
            playerName
        );
    }

    [PunRPC]
    public void StartTurnForPlayer(string playerName)
    {
        TurnManager.Instance.SetActivePlayer(playerName, null);
        commonAnswerButton.interactable = false;

        EnableAnswerButtons(playerName == PhotonNetwork.NickName);

        //  Colorear el jugador activo
        ResaltarTurno(playerName);
    }


    // ===================================
    // CONFIGURAR PREGUNTA EN TODOS CLIENTES
    // ===================================
    [PunRPC]
    void RPCSetupQuestion(string question, string[] answers, int correctIdx)
        => SetupQuestion(question, answers.ToList(), correctIdx);

    public void SetupQuestion(string question, List<string> answers, int correctIdx)
    {
        // Reiniciar estado de respuesta
        answered = false;
        commonAnswerButton.interactable = true;
        EnableAnswerButtons(false);

        // Actualizar UI
        numberText.text = $"Pregunta {currentQuestionNumber} de {totalQuestions}";
        questionText.text = WebUtility.HtmlDecode(question);
        correctAnswerIndex = correctIdx;
        currentQuestionNumber++;

        // Configurar cada botón de respuesta
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int idx = i;
            var btn = answerButtons[i];
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = WebUtility.HtmlDecode(answers[i]);

            ResetButtonColor(btn);
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAnswerSelected(idx));
        }

        // Arrancar timer
        if (timerBar != null)
        {
            timerBar.StartTimer();
            timerBar.OnTimeOut = () =>
            {
                // Si se agota el tiempo, enviar -1 como respuesta
                photonView.RPC(
                    nameof(RPCProcessAnswer),
                    RpcTarget.AllBuffered,
                    -1,
                    PhotonNetwork.NickName
                );
            };
        }
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (answered) return;
        answered = true;
        commonAnswerButton.interactable = false;

        // Enviar la respuesta seleccionada
        photonView.RPC(
            nameof(RPCProcessAnswer),
            RpcTarget.AllBuffered,
            selectedIndex,
            PhotonNetwork.NickName
        );
    }

    // ===================================
    // PROCESAR RESPUESTA Y COLOREAR BOTONES
    // ===================================
    [PunRPC]
    void RPCProcessAnswer(int selectedIndex, string playerName)
    {
        // Detener timer
        if (timerBar != null) timerBar.StopTimer();

        // Colorear todos los botones: verde el correcto, rojo el seleccionado (si fue erróneo)
        for (int i = 0; i < answerButtons.Count; i++)
        {
            var btn = answerButtons[i];
            btn.interactable = false;
            if (i == correctAnswerIndex) SetButtonColor(btn, correctColor);
            else if (i == selectedIndex) SetButtonColor(btn, incorrectColor);
        }

        // Actualizar estadísticas
        var p = GameStats.Instance.GetPlayer(playerName);
        if (p != null && selectedIndex >= 0)
        {
            if (selectedIndex == correctAnswerIndex)
            {
                p.correct++;
                int pts = Mathf.Max(0, Mathf.RoundToInt(timerBar.totalTime - timerBar.GetTimeLeft()));
                p.totalPoints += pts * 100;
            }
            else
            {
                p.wrong++;
                float used = timerBar.totalTime - timerBar.GetTimeLeft();
                float ratio = Mathf.Clamp01(used / timerBar.totalTime);
                int penalty = Mathf.CeilToInt(basePenalty * (1f - ratio)) * 100;
                p.totalPoints = Mathf.Max(0, p.totalPoints - penalty);

                // Registrar fallo por categoría
                GameStats.Instance.AddCategoryFail(currentCategory);
            }
        }

        // Avanzar a la siguiente pregunta tras el delay
        StartCoroutine(LoadNextQuestionAfterDelay());
    }

    private IEnumerator LoadNextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNextQuestion);
        RestaurarColoresJugadores();

        // Si se acabaron las preguntas
        if (currentQuestionNumber > totalQuestions)
        {
            if (GameSettings.trainingMode) FinishTraining();
            else if (PhotonNetwork.IsMasterClient) FinishGame();
            yield break;
        }

        // Reset de turno y nueva pregunta si somos MasterClient
        TurnManager.Instance.ResetTurn();
        yield return new WaitForSeconds(1f);
        if (!GameSettings.trainingMode && PhotonNetwork.IsMasterClient)
            StartCoroutine(FetchAndBroadcastQuestion());
    }

    // ==============================
    // FINAL DE PARTIDA MULTIJUGADOR
    // ==============================
    public void FinishGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Recoger arrays de estadísticas
        GameStats.Instance.GetAllStatsArrays(
            out string[] names,
            out int[] corrects,
            out int[] wrongs,
            out int[] points
        );
        string worstCategory = GameStats.Instance.GetWorstCategory();
        string mejorJugador = GameStats.Instance.GetPlayerWithMostPoints();
        if (mejorJugador == PhotonNetwork.NickName)
        {
            GuardarPartidaGanadaFirebase();
        }
        // Enviar a todos los clientes
        photonView.RPC(
            nameof(RPC_ReceiveGlobalStats),
            RpcTarget.AllBuffered,
            names, corrects, wrongs, points, worstCategory
        );

        // Cerrar sala y cargar escena de ranking
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("GameRanking");
    }
    private void GuardarPartidaGanadaFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("Usuario no autenticado. No se puede registrar partida ganada.");
            return;
        }

        string userId = currentUser.UserId;

        var refGanadas = FirebaseDatabase.DefaultInstance
            .GetReference("usuarios")
            .Child(userId)
            .Child("estadisticas")
            .Child("ganadas");

        refGanadas.RunTransaction(mutable =>
        {
            int actual = int.TryParse(mutable.Value?.ToString(), out int val) ? val : 0;
            mutable.Value = actual + 1;
            return TransactionResult.Success(mutable);
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log(" Partida ganada registrada en Firebase.");
            else
                Debug.LogError(" Error al registrar partida ganada: " + task.Exception);
        });
    }

    [PunRPC]
    void RPC_ReceiveGlobalStats(
        string[] names,
        int[] corrects,
        int[] wrongs,
        int[] points,
        string worstCategory
    )
    {
        EndGameData.GlobalNames = names;
        EndGameData.GlobalCorrects = corrects;
        EndGameData.GlobalWrongs = wrongs;
        EndGameData.GlobalPoints = points;
        EndGameData.GlobalWorstCategory = worstCategory;
    }

    // ====================
    // UTILIDADES VISUALES
    // ====================
    public void EnableAnswerButtons(bool enabled)
    {
        foreach (var b in answerButtons)
            b.interactable = enabled;
    }

    private void SetButtonColor(Button btn, Color c)
    {
        if (btn.TryGetComponent<Image>(out var img)) img.color = c;
        var cb = btn.colors;
        cb.normalColor = c;
        cb.highlightedColor = c * 1.2f;
        btn.colors = cb;
    }

    private void ResetButtonColor(Button btn)
    {
        if (btn.TryGetComponent<Image>(out var img)) img.color = Color.white;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white * 1.2f;
        btn.colors = cb;
    }

    // =========================
    // CONTROL SALIDA TEMPRANA
    // =========================
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        int remaining = PhotonNetwork.CurrentRoom.PlayerCount;
        // Si baja de 2 jugadores, finaliza partida automáticamente
        if (!GameSettings.trainingMode && remaining <= 1 && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[GameController] Solo queda un jugador: finalizando partida automáticamente.");
            FinishGame();
        }
    }

    public void RestaurarColoresJugadores()
    {
        for (int i = 0; i < playerSlotManager.aliasSlots.Count; i++)
        {
            var aliasText = playerSlotManager.aliasSlots[i];
            var contenedor = playerSlotManager.containerSlots[i];

            if (!aliasText.gameObject.activeSelf || contenedor == null) continue;

            if (aliasText.text == PhotonNetwork.NickName)
            {
                contenedor.color = new Color32(255, 255, 100, 255); //  Jugador local
            }
            else
            {
                contenedor.color = Color.white; // ⚪ Otros
            }
        }
    }



    public void UpdatePlayerScoresUI()
    {
        for (int i = 0; i < playerSlotManager.aliasSlots.Count; i++)
        {
            var alias = playerSlotManager.aliasSlots[i].text;

            // Asumimos que TextScore está al mismo nivel que alias
            var scoreText = playerSlotManager.aliasSlots[i].transform
                .parent
                .Find("TextScore")?.GetComponent<TextMeshProUGUI>();

            if (scoreText == null) continue;

            var stats = GameStats.Instance.GetPlayer(alias);
            scoreText.text = $"Puntos: {stats.totalPoints}";
        }
    }

    public void ResaltarTurno(string playerName)
    {
        for (int i = 0; i < playerSlotManager.aliasSlots.Count; i++)
        {
            var aliasText = playerSlotManager.aliasSlots[i];
            var contenedor = playerSlotManager.containerSlots[i];

            if (!aliasText.gameObject.activeSelf || contenedor == null) continue;

            if (aliasText.text == playerName)
            {
                // Naranja si está en turno, da igual si es el local
                contenedor.color = new Color32(255, 170, 0, 255);
            }
            else if (aliasText.text == PhotonNetwork.NickName)
            {
                // Amarillo si es el local (pero no está en turno)
                contenedor.color = new Color32(255, 255, 100, 255);
            }
            else
            {
                contenedor.color = Color.white;
            }
        }
    }





}