using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ventana PVI > Estación 3D > Constructor. Elige el layout, qué partes levantar, muestra las
/// comprobaciones contra el plano y lanza StationGenerator.
/// </summary>
public class StationBuilderWindow : EditorWindow
{
    [SerializeField] StationLayout layout;
    StationGenerator.Options options;
    Vector2 scroll;

    [MenuItem("PVI/Estación 3D/Constructor")]
    static void Open() => GetWindow<StationBuilderWindow>("Constructor de la estación");

    void OnEnable()
    {
        options = StationGenerator.Options.Load();
        if (layout == null)
            layout = StationGenerator.FindRoots().Select(r => r.layout).FirstOrDefault(l => l != null)
                     ?? AssetDatabase.LoadAssetAtPath<StationLayout>(StationGenerator.DefaultLayoutPath);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        layout = (StationLayout)EditorGUILayout.ObjectField("Layout", layout, typeof(StationLayout), false);
        EditorGUILayout.LabelField("Escena destino", EditorSceneManager.GetActiveScene().name);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Partes", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        options.terrain   = EditorGUILayout.Toggle("Terreno", options.terrain);
        options.fence     = EditorGUILayout.Toggle("Cerca perimetral", options.fence);
        options.building  = EditorGUILayout.Toggle("Edificio", options.building);
        using (new EditorGUI.DisabledScope(!options.building))
            options.roof  = EditorGUILayout.Toggle("   Losa de techo", options.roof);
        options.antennas  = EditorGUILayout.Toggle("Antenas", options.antennas);
        if (EditorGUI.EndChangeCheck()) options.Save();

        if (layout == null)
        {
            EditorGUILayout.HelpBox($"Elige un StationLayout. El de la estación es {StationGenerator.DefaultLayoutPath}.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Comprobación contra el plano", EditorStyles.boldLabel);
        DrawSums(layout.building);
        DrawAntennaHeights(layout);
        var issues = layout.Validate();
        if (issues.Count == 0)
            EditorGUILayout.HelpBox("Todo cuadra: filas, fondos, puertas y posiciones dentro de la parcela.", MessageType.Info);
        foreach (string issue in issues)
            EditorGUILayout.HelpBox(issue, MessageType.Warning);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            bool exists = StationGenerator.FindRoots().Count > 0;
            if (GUILayout.Button(exists ? "Regenerar" : "Generar", GUILayout.Height(28)))
                StationGenerator.Generate(layout, options);
            using (new EditorGUI.DisabledScope(!exists))
                if (GUILayout.Button("Borrar lo generado"))
                    StationGenerator.Clear();
        }

        EditorGUILayout.HelpBox(
            $"Todo lo generado cuelga de '{StationGenerator.RootName}' y se borra al regenerar. " +
            "El detalle a mano (ProBuilder, atrezo) va FUERA de ese objeto.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>Las sumas del plano a la vista: "26 + 20 + 20 = 66 m ✓".</summary>
    static void DrawSums(BuildingSpec b)
    {
        foreach (var row in b.rows)
        {
            float sum = row.rooms.Sum(r => r.width);
            string parts = string.Join(" + ", row.rooms.Select(r => r.width.ToString("0.##")));
            EditorGUILayout.LabelField(row.name, $"{parts} = {sum:0.##} m {Mark(sum, b.size.x)}");
        }
        float depth = b.rows.Sum(r => r.depth);
        string depths = string.Join(" + ", b.rows.Select(r => r.depth.ToString("0.##")));
        EditorGUILayout.LabelField("Fondo", $"{depths} = {depth:0.##} m {Mark(depth, b.size.y)}");
    }

    static string Mark(float value, float expected) =>
        Mathf.Abs(value - expected) <= 0.01f ? "✓" : $"✗ (debe ser {expected:0.##})";

    /// <summary>
    /// Altura total de cada antena calculada con su geometría, frente a la "≈" del plano, y el botón
    /// que recalcula los pedestales para que coincidan (el plano no da la altura del pedestal).
    /// </summary>
    static void DrawAntennaHeights(StationLayout layout)
    {
        bool anyOff = false;
        foreach (var a in layout.antennas)
        {
            float h = AntennaGeometry.OverallHeight(a, layout.antennaDesign);
            string plan = a.overallHeight > 0f ? $"plano ≈ {a.overallHeight:0.#} m" : "plano: sin rotular";
            string mark = a.overallHeight > 0f ? (Mathf.Abs(h - a.overallHeight) <= StationLayout.HeightTolerance ? " ✓" : " ✗") : "";
            EditorGUILayout.LabelField(a.name, $"alto {h:0.00} m · {plan}{mark}");
            anyOff |= a.overallHeight > 0f && Mathf.Abs(h - a.overallHeight) > 0.01f;
        }

        using (new EditorGUI.DisabledScope(!anyOff))
            if (GUILayout.Button("Ajustar pedestales a la altura del plano"))
                FitPedestals(layout);
    }

    public static void FitPedestals(StationLayout layout)
    {
        Undo.RecordObject(layout, "Ajustar pedestales");
        foreach (var a in layout.antennas)
        {
            if (a.overallHeight <= 0f || !a.HasPedestal) continue;
            float fitted = AntennaGeometry.PedestalHeightFor(a, layout.antennaDesign, a.overallHeight);
            if (fitted <= 0f)
            {
                Debug.LogWarning($"[Estación] '{a.name}' ya pasa de {a.overallHeight:0.#} m sin pedestal: baja la montura.", layout);
                continue;
            }
            a.pedestalHeight = Mathf.Round(fitted * 100f) / 100f;
        }
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssetIfDirty(layout);
    }
}
