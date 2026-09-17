using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Conecta botones de un UIDocument (UI Toolkit) con la carga de escenas.
/// Se coloca en el mismo GameObject que el UIDocument.
/// Las escenas referenciadas deben estar en Build Settings.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(100)] // corre después de UIDocument para que rootVisualElement exista
public class MenuNavigation : MonoBehaviour
{
    [Header("Marco Teórico")]
    [Tooltip("name del botón en el UXML que abre el Marco Teórico.")]
    [SerializeField] private string marcoTeoricoButton = "BtnMarcoTeorico";
    [Tooltip("Nombre de la escena del Marco Teórico (en Build Settings).")]
    [SerializeField] private string marcoTeoricoScene = "MarcoTeorico";

    [Header("Módulo 2D (Simulador)")]
    [Tooltip("name del botón en el UXML que abre el Módulo 2D.")]
    [SerializeField] private string moduloButton = "BtnModulo2D";
    [Tooltip("Nombre de la escena del Simulador 2D (en Build Settings).")]
    [SerializeField] private string moduloScene = "Simulador2D";

    [Header("Volver (opcional)")]
    [Tooltip("name del botón para regresar. Déjalo vacío si esta vista no tiene.")]
    [SerializeField] private string backButton = "BtnBack";
    [Tooltip("Escena a la que regresa. Vacío = no se conecta el botón volver.")]
    [SerializeField] private string backScene = "";

    [Header("Salir del aplicativo")]
    [Tooltip("name del botón que cierra la aplicación. Vacío = no se conecta.")]
    [SerializeField] private string quitButton = "BtnSalir";

    private Button mtButton;
    private Button modButton;
    private Button bkButton;
    private Button qtButton;

    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        var root = document != null ? document.rootVisualElement : null;
        if (root == null)
        {
            Debug.LogWarning("[MenuNavigation] rootVisualElement aún no está listo.");
            return;
        }

        if (!string.IsNullOrEmpty(marcoTeoricoButton))
        {
            mtButton = root.Q<Button>(marcoTeoricoButton);
            if (mtButton != null) mtButton.clicked += OpenMarcoTeorico;
            else Debug.LogWarning($"[MenuNavigation] No se encontró el botón '{marcoTeoricoButton}'.");
        }

        if (!string.IsNullOrEmpty(moduloButton))
        {
            modButton = root.Q<Button>(moduloButton);
            if (modButton != null) modButton.clicked += OpenModulo;
            else Debug.LogWarning($"[MenuNavigation] No se encontró el botón '{moduloButton}'.");
        }

        if (!string.IsNullOrEmpty(backButton) && !string.IsNullOrEmpty(backScene))
        {
            bkButton = root.Q<Button>(backButton);
            if (bkButton != null) bkButton.clicked += GoBack;
        }

        if (!string.IsNullOrEmpty(quitButton))
        {
            qtButton = root.Q<Button>(quitButton);
            if (qtButton != null) qtButton.clicked += QuitApp;
            else Debug.LogWarning($"[MenuNavigation] No se encontró el botón '{quitButton}'.");
        }
    }

    private void OnDisable()
    {
        if (mtButton != null) mtButton.clicked -= OpenMarcoTeorico;
        if (modButton != null) modButton.clicked -= OpenModulo;
        if (bkButton != null) bkButton.clicked -= GoBack;
        if (qtButton != null) qtButton.clicked -= QuitApp;
    }

    private void OpenMarcoTeorico()
    {
        if (!string.IsNullOrEmpty(marcoTeoricoScene))
            SceneManager.LoadScene(marcoTeoricoScene);
    }

    private void OpenModulo()
    {
        if (!string.IsNullOrEmpty(moduloScene))
            SceneManager.LoadScene(moduloScene);
    }

    private void GoBack()
    {
        if (!string.IsNullOrEmpty(backScene))
            SceneManager.LoadScene(backScene);
    }

    /// <summary>
    /// Cierra el aplicativo. En el Editor, Application.Quit() no hace nada,
    /// así que se detiene el Play Mode para poder probar el botón.
    /// </summary>
    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
