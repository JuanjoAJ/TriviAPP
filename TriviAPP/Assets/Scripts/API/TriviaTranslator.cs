using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class TriviaTranslator : MonoBehaviour
{
    [Header("Configuración Firebase")]
    [Tooltip("La URL completa de Realtime DB, sin '/' final")]
    [SerializeField] private string databaseUrl = "https://triviapp-8f3d8-default-rtdb.europe-west1.firebasedatabase.app";
    [Tooltip("El nombre exacto del nodo que contiene preguntas")]
    [SerializeField] private string questionsNode = "trivia_questions_es";

    private DatabaseReference dbRef;
    private bool isFirebaseReady = false;

    // Diccionario de mapeo de categorías en inglés a español
    private static readonly Dictionary<string, string> CategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "film_and_tv",         "Cine y TV" },
        { "ciencia",             "Ciencia" },
        { "historia",             "Historia" },
        { "música",               "Música" },
        { "society_and_culture", "Sociedad y Cultura" },
        { "general_knowledge",   "Conocimientos Generales" },
        { "food_and_drink",      "Comida y Bebida" },
        { "geografía",           "Geografía" },
        { "sport_and_leisure",   "Deportes y Ocio" },
        { "arts_and_literature", "Artes y Literatura" },
    };

    private void Awake()
    {
        Debug.Log("[TriviaTranslator] Comprobando dependencias de Firebase...");
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[TriviaTranslator] Dependencias NO satisfechas: {task.Result}");
                    return;
                }

                dbRef = FirebaseDatabase
                    .GetInstance(FirebaseApp.DefaultInstance, databaseUrl)
                    .RootReference;

                isFirebaseReady = true;
                Debug.Log($"[TriviaTranslator] Firebase listo. Leeremos de: {databaseUrl}/{questionsNode}");
            });
    }

    /// <summary>
    /// Obtiene una pregunta aleatoria de Firebase, incluyendo la categoría traducida.
    /// Callback signature: (string category, string question, List<string> options, int correctIndex)
    /// </summary>
    public IEnumerator GetTranslatedQuestion(Action<string, string, List<string>, int> callback)
    {
        // 1) Esperar inicialización (hasta 10s)
        float timer = 0f, timeout = 10f;
        while (!isFirebaseReady && timer < timeout)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        if (!isFirebaseReady)
        {
            Debug.LogError("[TriviaTranslator] Firebase NO se inicializó a tiempo.");
            yield break;
        }

        // 2) Leer nodo completo
        Debug.Log($"[TriviaTranslator] Solicitando preguntas en '{questionsNode}'...");
        var fetch = dbRef.Child(questionsNode).GetValueAsync();
        yield return new WaitUntil(() => fetch.IsCompleted);

        if (fetch.IsFaulted)
        {
            Debug.LogError("[TriviaTranslator] Error leyendo preguntas: " + fetch.Exception);
            yield break;
        }

        var snapshot = fetch.Result;
        if (snapshot == null || snapshot.ChildrenCount == 0)
        {
            Debug.LogError("[TriviaTranslator] ¡Sin datos o ruta equivocada!");
            yield break;
        }

        // 3) Elegir un child al azar
        var all = snapshot.Children.ToList();
        var chosen = all[UnityEngine.Random.Range(0, all.Count)];

        // 4) Mapear campos
        string rawCategory = chosen.Child("Category").Value?.ToString() ?? "";
        string category = CategoryMap.TryGetValue(rawCategory, out var esp)
            ? esp
            : (string.IsNullOrEmpty(rawCategory)
                ? "Sin categoría"
                : rawCategory.Replace('_', ' ').ToUpperInvariant());

        string questionText = chosen.Child("Question").Value?.ToString() ?? "";
        string correctAnswer = chosen.Child("CorrectAnswer").Value?.ToString() ?? "";
        var wrongSnaps = chosen.Child("IncorrectAnswers").Children;

        var options = wrongSnaps.Select(c => c.Value.ToString()).ToList();
        options.Add(correctAnswer);

        // Debug detallado de la clave y pregunta elegida
        Debug.Log($"[TriviaTranslator] Clave elegida: {chosen.Key} || Pregunta: {questionText}");

        // 5) Validar datos mínimos
        if (string.IsNullOrEmpty(questionText) ||
            string.IsNullOrEmpty(correctAnswer) ||
            options.Count < 2)
        {
            Debug.LogError("[TriviaTranslator] Datos incompletos: falta pregunta, respuesta correcta o opciones.");
            yield break;
        }

        // 6) Barajar y calcular índice correcto
        Shuffle(options);
        int correctIndex = options.IndexOf(correctAnswer);
        Debug.Log($"[TriviaTranslator] Categoría: {category} | Opciones: {string.Join(" | ", options)} (correct={correctIndex})");

        // 7) Invocar callback con categoría en español
        callback?.Invoke(category, questionText, options, correctIndex);
    }

    /// <summary>
    /// Obtiene una pregunta aleatoria FILTRADA por la clave de categoría (modo categorías).
    /// </summary>
    public IEnumerator GetCategoryQuestion(
        string categoryKey,
        Action<string, string, List<string>, int> callback
    )
    {


        // 1) Esperar inicialización
        float timer = 0f, timeout = 10f;
        while (!isFirebaseReady && timer < timeout)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        if (!isFirebaseReady)
        {
            Debug.LogError("[TriviaTranslator] Firebase NO se inicializó a tiempo.");
            yield break;
        }

        // 2) Hacer query filtrada
        string normalizedKey = categoryKey.ToLowerInvariant();
        var query = dbRef
            .Child(questionsNode)
            .OrderByChild("Category")
            .EqualTo(normalizedKey);


        var task = query.GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("[TriviaTranslator] Error en GetCategoryQuestion: " + task.Exception);
            yield break;
        }

        var all = task.Result.Children.ToList();
        if (all.Count == 0)
        {
            Debug.LogError($"[TriviaTranslator] No hay preguntas de categoría '{categoryKey}'");
            yield break;
        }

        // 3) Elegir una al azar y reusar el resto del mapeo
        var chosen = all[UnityEngine.Random.Range(0, all.Count)];

        // Reusa tu método interno para mapear DataSnapshot a callback
        InvokeMappedQuestion(chosen, callback);
    }

    // Extrae de tu InvokeCallback existente para no duplicar
    private void InvokeMappedQuestion(DataSnapshot snap, Action<string, string, List<string>, int> cb)
    {
        string rawCat = snap.Child("Category").Value?.ToString() ?? "";
        string category = CategoryMap.TryGetValue(rawCat, out var esp) ? esp : rawCat;
        string questionText = snap.Child("Question").Value?.ToString() ?? "";
        string correctAnswer = snap.Child("CorrectAnswer").Value?.ToString() ?? "";
        var wrongs = snap.Child("IncorrectAnswers")
                         .Children
                         .Select(c => c.Value.ToString())
                         .ToList();

        var options = new List<string>(wrongs) { correctAnswer };
        Shuffle(options);
        int correctIndex = options.IndexOf(correctAnswer);
        cb?.Invoke(category, questionText, options, correctIndex);
    }


    // Fisher–Yates shuffle
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
