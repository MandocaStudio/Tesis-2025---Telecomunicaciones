using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD del recorrido 3D (Modulo3DView.uxml). Enseña el aviso de "Haz clic para recorrer" con el
/// cursor suelto y el punto de mira mientras se camina, poniendo o quitando .m3d-hud--walking en
/// la raíz. Le dice además al FirstPersonWalker qué clics caen sobre un botón, para que el clic en
/// Atrás no bloquee el cursor. El botón en sí lo cablea MenuNavigation, como en las demás vistas.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(100)] // corre después de UIDocument para que rootVisualElement exista
public class Modulo3DHud : MonoBehaviour
{
    const string WalkingClass = "m3d-hud--walking";

    [Tooltip("El caminante de la escena. Vacío = se busca solo.")]
    [SerializeField] FirstPersonWalker walker;

    VisualElement root;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement?.Q("Root");
        if (root == null)
        {
            Debug.LogWarning("[Modulo3DHud] No se encontró 'Root' en el UXML.");
            return;
        }

        if (walker == null) walker = FindAnyObjectByType<FirstPersonWalker>();
        if (walker == null)
        {
            Debug.LogWarning("[Modulo3DHud] No hay FirstPersonWalker en la escena.");
            return;
        }

        walker.IsPointerOverUi = IsOverButton;
        walker.WalkingChanged += OnWalkingChanged;
        OnWalkingChanged(walker.Walking);
    }

    void OnDisable()
    {
        if (walker == null) return;
        walker.WalkingChanged -= OnWalkingChanged;
        walker.IsPointerOverUi = null;
    }

    void OnWalkingChanged(bool walking) => root.EnableInClassList(WalkingClass, walking);

    /// <summary>
    /// Solo cuentan los botones: el título y los avisos son decorativos y un clic sobre ellos
    /// debe volver a la escena. La posición llega con el origen abajo (Input System) y el panel
    /// la quiere con el origen arriba.
    /// </summary>
    bool IsOverButton(Vector2 screenPosition)
    {
        var panel = root?.panel;
        if (panel == null) return false;
        var point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
        for (var e = panel.Pick(point); e != null; e = e.parent)
            if (e is Button) return true;
        return false;
    }
}
