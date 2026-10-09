using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Dibuja por instancias miles de copias de unas pocas mallas (matas de grama, arbustos, piedras) sin
/// un GameObject por copia: un solo componente por capa, que cada cámara dibuja con
/// Graphics.RenderMeshInstanced. Lo monta el generador (StationGenerator, "Vegetación y piedras").
///
/// Para que corra en una gráfica integrada:
///  - Nivel de detalle por distancia (<see cref="distances"/>): de cerca la malla buena, de lejos una más
///    simple (otra malla o un nivel de Mesh LOD), y más allá de la última distancia, nada.
///  - Las copias van en celdas de <see cref="CellSize"/> m: las celdas fuera de cámara ni se miran.
///  - En calidad "Baja" (AutoQuality) se acortan las distancias y se dibuja solo una parte de las copias
///    (siempre las mismas, para que no parpadeen).
///  - Sombras solo en los niveles que lo piden (los arbustos de cerca; la grama y las piedras, no).
/// Se ve también en el Editor (ExecuteAlways), en la Scene View y en las capturas.
/// </summary>
[ExecuteAlways]
public class InstancedScatter : MonoBehaviour
{
    [Serializable]
    public class Band
    {
        public Mesh mesh;
        [Tooltip("Nivel de Mesh LOD de la malla (−1 = la malla entera).")]
        public int meshLod = -1;
        public bool castShadows;
    }

    [Serializable]
    public class Variant
    {
        public string name;
        [Tooltip("Uno por submalla.")]
        public Material[] materials;
        [Tooltip("Un nivel por cada distancia de 'distances', del más cercano al más lejano.")]
        public Band[] bands;
        [Tooltip("Lleva la malla a su sitio: centrada, apoyada en Y = 0 y a escala 1 = su tamaño real.")]
        public Matrix4x4 pivot = Matrix4x4.identity;
    }

    public ScatterData data;
    public List<Variant> variants = new List<Variant>();
    [Tooltip("Hasta dónde se usa cada nivel de detalle (m). La última es hasta dónde se dibuja.")]
    public float[] distances = { 10f, 25f, 50f };
    [Tooltip("En calidad Baja, las distancias × esto.")]
    public float lowDistanceScale = 0.6f;
    [Tooltip("En calidad Baja, qué parte de las copias se dibuja.")]
    [Range(0f, 1f)] public float lowDensity = 0.5f;

    const float CellSize = 24f;
    const int MaxPerCall = 1023;

    Matrix4x4[][] matrices;  // [variante][copia]
    Vector3[][] positions;
    Cell[] cells;
    List<Matrix4x4>[][] visible; // [variante][nivel]
    readonly Plane[] planes = new Plane[6];

    struct Cell
    {
        public Bounds bounds;
        public int[][] items; // [variante] → índices de copia
    }

    void OnEnable()
    {
        Build();
        RenderPipelineManager.beginCameraRendering += Draw;
    }

    void OnDisable() => RenderPipelineManager.beginCameraRendering -= Draw;

    void OnValidate() { if (isActiveAndEnabled) Build(); }

    /// <summary>Desempaqueta las copias y las reparte en celdas. Se llama al activar el componente.</summary>
    public void Build()
    {
        cells = null;
        if (data == null || variants.Count == 0) return;
        int n = Mathf.Min(variants.Count, data.sets.Count);
        matrices = new Matrix4x4[n][];
        positions = new Vector3[n][];
        visible = new List<Matrix4x4>[n][];
        var byCell = new Dictionary<Vector2Int, (Bounds bounds, List<int>[] items)>();

        for (int v = 0; v < n; v++)
        {
            matrices[v] = ScatterData.Unpack(data.sets[v], variants[v].pivot);
            positions[v] = new Vector3[matrices[v].Length];
            int bandCount = variants[v].bands?.Length ?? 0;
            visible[v] = new List<Matrix4x4>[bandCount];
            for (int b = 0; b < bandCount; b++) visible[v][b] = new List<Matrix4x4>();

            var meshBounds = variants[v].bands != null && bandCount > 0 && variants[v].bands[0].mesh != null
                ? variants[v].bands[0].mesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            for (int i = 0; i < matrices[v].Length; i++)
            {
                var m = matrices[v][i];
                var p = (Vector3)m.GetColumn(3);
                positions[v][i] = p;
                var key = new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));
                var world = TransformBounds(m, meshBounds);
                if (!byCell.TryGetValue(key, out var cell))
                {
                    var lists = new List<int>[n];
                    for (int k = 0; k < n; k++) lists[k] = new List<int>();
                    cell = (world, lists);
                }
                cell.bounds.Encapsulate(world);
                cell.items[v].Add(i);
                byCell[key] = cell;
            }
        }

        cells = new Cell[byCell.Count];
        int c = 0;
        foreach (var kv in byCell)
        {
            var items = new int[n][];
            for (int v = 0; v < n; v++) items[v] = kv.Value.items[v].ToArray();
            cells[c++] = new Cell { bounds = kv.Value.bounds, items = items };
        }
    }

    void Draw(ScriptableRenderContext context, Camera cam)
    {
        if (cells == null || distances == null || distances.Length == 0) return;
        if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
        if ((cam.cullingMask & (1 << gameObject.layer)) == 0) return;

        bool low = AutoQuality.IsLow;
        float scale = low ? lowDistanceScale : 1f;
        float far = distances[distances.Length - 1] * scale;
        Vector3 eye = cam.transform.position;
        GeometryUtility.CalculateFrustumPlanes(cam, planes);

        for (int v = 0; v < visible.Length; v++)
            foreach (var list in visible[v]) list.Clear();

        foreach (var cell in cells)
        {
            if (cell.bounds.SqrDistance(eye) > far * far) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, cell.bounds)) continue;
            for (int v = 0; v < visible.Length; v++)
            {
                int bands = visible[v].Length;
                foreach (int i in cell.items[v])
                {
                    if (low && Thin(v, i) > lowDensity) continue;
                    float d = Vector3.Distance(eye, positions[v][i]);
                    for (int b = 0; b < bands && b < distances.Length; b++)
                        if (d < distances[b] * scale) { visible[v][b].Add(matrices[v][i]); break; }
                }
            }
        }

        for (int v = 0; v < visible.Length; v++)
        {
            var variant = variants[v];
            for (int b = 0; b < visible[v].Length; b++)
            {
                var list = visible[v][b];
                var band = variant.bands[b];
                if (list.Count == 0 || band.mesh == null) continue;
                for (int s = 0; s < band.mesh.subMeshCount && s < variant.materials.Length; s++)
                {
                    if (variant.materials[s] == null) continue;
                    var rp = new RenderParams(variant.materials[s])
                    {
                        camera = cam,
                        layer = gameObject.layer,
                        shadowCastingMode = band.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        receiveShadows = true,
                        forceMeshLod = band.meshLod,
                    };
                    for (int start = 0; start < list.Count; start += MaxPerCall)
                        Graphics.RenderMeshInstanced(rp, band.mesh, s, list, Mathf.Min(MaxPerCall, list.Count - start), start);
                }
            }
        }
    }

    /// <summary>Número fijo entre 0 y 1 por copia: decide cuáles se quedan en calidad Baja.</summary>
    static float Thin(int variant, int index)
    {
        uint h = (uint)(index * 2654435761u) ^ (uint)(variant * 40503u);
        h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
        return (h & 0xFFFFFF) / 16777216f;
    }

    static Bounds TransformBounds(Matrix4x4 m, Bounds b)
    {
        var center = m.MultiplyPoint3x4(b.center);
        var e = b.extents;
        var ax = m.MultiplyVector(new Vector3(e.x, 0f, 0f));
        var ay = m.MultiplyVector(new Vector3(0f, e.y, 0f));
        var az = m.MultiplyVector(new Vector3(0f, 0f, e.z));
        var extents = new Vector3(Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                                  Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                                  Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
        return new Bounds(center, extents * 2f);
    }
}
