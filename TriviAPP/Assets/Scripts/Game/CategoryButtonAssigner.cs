using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonsAssigner : MonoBehaviour
{
    [Header("Configuración de categorías")]
    [Tooltip("Transform que contiene todos los botones de categoría como hijos")]
    [SerializeField] private Transform categoryButtonsContainer;
    [Tooltip("Referencia al MenuManager para invocar OnClickCategory")]
    [SerializeField] private MenuManager menuManager;

    [Tooltip("Lista de claves de categoría en el mismo orden que los botones")]
    [SerializeField]
    private string[] categoryKeys = new[]
    {
        "arts_and_literature",
        "ciencia",
        "society_and_culture",
        "geografía",
        "música",
        "film_and_tv",
        "sport_and_leisure",
        "general_knowledge",
        "food_and_drink",
        "historia"
    };

    private void Start()
    {
        // Obtiene todos los botones hijos de categoryButtonsContainer
        var buttons = categoryButtonsContainer.GetComponentsInChildren<Button>();

        // Para cada botón, asigna el listener correspondiente
        for (int i = 0; i < buttons.Length; i++)
        {
            // Si no hay clave para este índice, salimos
            if (i >= categoryKeys.Length) break;

            string key = categoryKeys[i];  // captura local para el cierre
            buttons[i].onClick.AddListener(() =>
            {
                menuManager.OnClickCategory(key);
            });
        }
    }
}
