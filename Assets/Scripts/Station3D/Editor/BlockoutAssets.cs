using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Malla y materiales que usa el blockout. Se crean la primera vez en
/// Assets/Data/Station3D/Blockout/ y a partir de ahí se REUTILIZAN: regenerar no los pisa, así que
/// el acabado visual puede editar esos materiales (o sustituirlos) sin que se pierda.
///
/// Las mallas que dependen de las medidas (terreno, vías, techos, postes) van aparte, en
/// Assets/Data/Station3D/Generado/: se reescriben en cada generación conservando su GUID, para que la
/// escena no guarde geometría dentro del .unity.
/// </summary>
public static class BlockoutAssets
{
    public const string Folder = "Assets/Data/Station3D/Blockout";
    public const string GeneratedFolder = "Assets/Data/Station3D/Generado";

    static readonly HashSet<string> produced = new HashSet<string>();

    /// <summary>Empieza una generación: lleva la cuenta de qué mallas generadas se escriben.</summary>
    public static void BeginGenerated() => produced.Clear();

    /// <summary>
    /// Cierra una generación completa: borra las mallas generadas que ya no salen del layout (un
    /// edificio renombrado o quitado). Solo tiene sentido si se generaron todas las partes.
    /// </summary>
    public static void EndGenerated()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { GeneratedFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!produced.Contains(path)) AssetDatabase.DeleteAsset(path);
        }
    }

    /// <summary>
    /// Guarda una malla generada en su asset (por su nombre), o reescribe la que ya había
    /// conservando el GUID. Devuelve la malla del asset.
    /// </summary>
    public static Mesh SaveGenerated(Mesh mesh)
    {
        string path = $"{GeneratedFolder}/{mesh.name}.asset";
        produced.Add(path);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            EnsureFolder(GeneratedFolder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        return existing;
    }

    /// <summary>Nombre de archivo a partir de un nombre del plano ("Oficinas / Administración" → "Oficinas_Administracion").</summary>
    public static string Slug(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in name.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
            else if ((c == ' ' || c == '/' || c == '·' || c == '-' || c == '_') && sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
        }
        return sb.ToString().Trim('_');
    }

    /// <summary>
    /// Cubo de 1 m con el pivote en el centro de la cara INFERIOR (x, z en [-0,5, 0,5]; y en [0, 1]).
    /// Con él, localScale = medidas reales en metros y localPosition = punto de apoyo en el suelo.
    /// </summary>
    public static Mesh UnitBlock() => LoadOrCreate("BloqueUnidad", BuildUnitBlock);

    /// <summary>
    /// Tronco de cono de 1 m de alto, con el pivote en el centro de la base y el eje en +Y.
    /// Radios en metros para un diámetro de 1: (0,5, 0,5) es un cilindro.
    /// </summary>
    public static Mesh Frustum(string name, float bottomRadius, float topRadius) =>
        LoadOrCreate(name, () => BuildFrustum(name, bottomRadius, topRadius));

    /// <summary>
    /// Paraboloide z = r² / (4f) de 1 m de diámetro, con el vértice en el origen y abierto hacia +Z,
    /// visible por las dos caras. Escalado uniformemente sigue siendo un paraboloide con el mismo f/D,
    /// así que una sola malla sirve para todos los platos que compartan f/D.
    /// </summary>
    public static Mesh Paraboloid(float focalRatio)
    {
        string name = "Paraboloide_fD" + focalRatio.ToString("0.###", CultureInfo.InvariantCulture);
        return LoadOrCreate(name, () => BuildParaboloid(name, focalRatio));
    }

    /// <summary>Esfera de 1 m de diámetro con el pivote en su punto más bajo (y en [0, 1]).</summary>
    public static Mesh Sphere() => LoadOrCreate("EsferaUnidad", BuildSphere);

    static Mesh LoadOrCreate(string name, Func<Mesh> build)
    {
        string path = $"{Folder}/{name}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;

        EnsureFolder(Folder);
        mesh = build();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// Material URP/Lit por nombre. Si ya existe se devuelve tal cual, para no pisar lo que se haya
    /// retocado a mano, salvo con <paramref name="overwrite"/> (menú "Reaplicar colores del acabado").
    /// </summary>
    public static Material Material(string name, Color color, float smoothness = 0.15f, float metallic = 0f,
                                    bool transparent = false, bool overwrite = false)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = mat == null;
        if (!created && !overwrite) return mat;

        if (created)
        {
            EnsureFolder(Folder);
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        if (transparent)
        {
            // Las mismas propiedades que pone el Inspector de URP al elegir Surface Type = Transparent.
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        if (created) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>
    /// Grama corta, 512 px: verde oscuro, verde y paja seca, con grano por píxel. Es la del campo
    /// abierto. Se crea una vez (Grama.png) y luego se reutiliza.
    /// </summary>
    public static Texture2D GrassTexture() => NoiseTexture("Grama", 7,
        (0f, new Color(0.30f, 0.40f, 0.17f)), (0.5f, new Color(0.45f, 0.53f, 0.24f)), (1f, new Color(0.64f, 0.61f, 0.35f)));

    /// <summary>
    /// Suelo de la parcela: "tierra ocre + manchas de grama" (plano). La mayor parte es tierra; lo
    /// más bajo del ruido sale como manchas de grama verde y seca. Tierra.png.
    /// </summary>
    public static Texture2D SoilTexture() => NoiseTexture("Tierra", 23,
        (0f, new Color(0.35f, 0.42f, 0.20f)), (0.26f, new Color(0.47f, 0.48f, 0.26f)),
        (0.40f, new Color(0.62f, 0.54f, 0.35f)), (0.58f, new Color(0.70f, 0.57f, 0.38f)),
        (1f, new Color(0.79f, 0.67f, 0.48f)));

    /// <summary>Sabana seca de fuera de la cerca: paja con algo de verde oliva. Sabana.png.</summary>
    public static Texture2D SavannaTexture() => NoiseTexture("Sabana", 31,
        (0f, new Color(0.38f, 0.44f, 0.21f)), (0.5f, new Color(0.58f, 0.55f, 0.31f)), (1f, new Color(0.71f, 0.64f, 0.41f)));

    /// <summary>
    /// Textura de suelo de 512 px, generada por código y sin costuras al repetirse: el ruido sale de
    /// rejillas que dividen exacto el tamaño y se leen en módulo, y se colorea con una rampa de
    /// colores (posición 0–1 → color). Se crea una vez y luego se reutiliza.
    /// </summary>
    static Texture2D NoiseTexture(string file, int seed, params (float at, Color color)[] ramp)
    {
        string path = $"{Folder}/{file}.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;

        const int size = 512;
        var rnd = new System.Random(seed);
        float[] Grid(int cells)
        {
            var g = new float[cells * cells];
            for (int i = 0; i < g.Length; i++) g[i] = (float)rnd.NextDouble();
            return g;
        }
        float Sample(float[] g, int cells, float u, float v)
        {
            float x = u * cells, y = v * cells;
            int x0 = (int)x, y0 = (int)y;
            float fx = Mathf.SmoothStep(0f, 1f, x - x0), fy = Mathf.SmoothStep(0f, 1f, y - y0);
            float At(int i, int j) => g[(j % cells) * cells + (i % cells)];
            return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), fx),
                              Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), fx), fy);
        }
        float[] large = Grid(4), medium = Grid(16), fine = Grid(64);
        Color Ramp(float n)
        {
            for (int i = 1; i < ramp.Length; i++)
                if (n <= ramp[i].at)
                    return Color.Lerp(ramp[i - 1].color, ramp[i].color, Mathf.InverseLerp(ramp[i - 1].at, ramp[i].at, n));
            return ramp[ramp.Length - 1].color;
        }

        var img = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float n = 0.5f * Sample(large, 4, u, v) + 0.3f * Sample(medium, 16, u, v) + 0.2f * Sample(fine, 64, u, v);
            n = Mathf.Clamp01((n - 0.5f) * 1.8f + 0.5f);
            pixels[y * size + x] = Ramp(n) * (0.92f + 0.16f * (float)rnd.NextDouble());
        }
        img.SetPixels(pixels);
        EnsureFolder(Folder);
        File.WriteAllBytes(path, img.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(img);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 8;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>
    /// Grano fino de la grama para el detail map de URP Lit: gris centrado en 0,5 (que URP multiplica
    /// por 2, así que en promedio no oscurece ni aclara) con matas y briznas. Se repite a pocos
    /// metros encima de la textura grande y rompe la cuadrícula que se vería con una sola escala.
    /// </summary>
    public static Texture2D GrassDetailTexture()
    {
        string path = Folder + "/GramaDetalle.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;

        const int size = 256, cells = 16;
        var rnd = new System.Random(11);
        var grid = new float[cells * cells];
        for (int i = 0; i < grid.Length; i++) grid[i] = (float)rnd.NextDouble();
        float Clumps(float u, float v)
        {
            float x = u * cells, y = v * cells;
            int x0 = (int)x, y0 = (int)y;
            float fx = Mathf.SmoothStep(0f, 1f, x - x0), fy = Mathf.SmoothStep(0f, 1f, y - y0);
            float At(int i, int j) => grid[(j % cells) * cells + (i % cells)];
            return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), fx),
                              Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), fx), fy);
        }

        var img = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float g = 0.5f + 0.10f * (Clumps(x / (float)size, y / (float)size) - 0.5f) * 2f
                           + 0.08f * ((float)rnd.NextDouble() - 0.5f) * 2f;
            pixels[y * size + x] = new Color(g, g, g);
        }
        img.SetPixels(pixels);
        EnsureFolder(Folder);
        File.WriteAllBytes(path, img.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(img);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 8;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>
    /// Anillo de cerros alrededor de la parcela (origen en su centro, base en Y = 0). Suben desde el
    /// radio interior y su altura varía con senos de frecuencia entera en el ángulo, así que el
    /// anillo cierra sin costura. Se reescribe en su asset (Cerros.asset) conservando el GUID; con
    /// la misma semilla sale idéntico.
    /// </summary>
    public static Mesh Hills(EnvironmentSpec e)
    {
        const int segments = 192, rings = 20;
        var rnd = new System.Random(e.hillsSeed);
        float p1 = (float)rnd.NextDouble() * 6.2832f, p2 = (float)rnd.NextDouble() * 6.2832f, p3 = (float)rnd.NextDouble() * 6.2832f;
        var b = new MeshBuilder();

        for (int i = 0; i <= rings; i++)
        {
            float t = i / (float)rings;
            float r = Mathf.Lerp(e.hillsInnerRadius, e.hillsOuterRadius, t);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
            for (int j = 0; j < segments; j++)
            {
                float a = j / (float)segments * Mathf.PI * 2f;
                float n = 0.5f + 0.5f * (0.5f * Mathf.Sin(3f * a + p1 + 1.5f * t)
                                       + 0.3f * Mathf.Sin(7f * a + p2 + 3f * t)
                                       + 0.2f * Mathf.Sin(13f * a + p3 + 6f * t));
                float h = rise * Mathf.Lerp(e.hillsMinHeight, e.hillsMaxHeight, n);
                var p = new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
                b.Vertex(p, Vector3.up, new Vector2(p.x, p.z));
            }
        }
        for (int i = 1; i <= rings; i++)
        for (int j = 0; j < segments; j++)
        {
            int Ix(int ri, int sj) => ri * segments + (sj % segments);
            b.Quad(Ix(i - 1, j), Ix(i, j), Ix(i, j + 1), Ix(i - 1, j + 1));
        }

        var mesh = b.ToMesh("Cerros");
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        string path = Folder + "/Cerros.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            EnsureFolder(Folder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        return existing;
    }

    // ------------------------------------------------------------------ árboles con modelo

    /// <summary>Lo que el generador necesita de un modelo de árbol, medido sobre su propia malla.</summary>
    public sealed class TreeModelInfo
    {
        public Mesh mesh;
        public Material[] materials;
        /// <summary>Ancho medio de la copa (media de X y Z de las hojas), en las unidades del modelo.</summary>
        public float crownDiameter;
        /// <summary>Radio del tronco a la altura de una persona (entre 1 y 2 m), para su collider.</summary>
        public float trunkRadius;
    }

    /// <summary>
    /// Malla y materiales URP de un prefab de árbol. Sirve para prefabs normales (MeshFilter con su
    /// malla) y para los del Tree Creator, que no guardan la malla en el MeshFilter sino como
    /// sub-asset y usan shaders del pipeline antiguo, que en URP salen rosas. Se toma esa malla
    /// tal cual: submalla 0 = corteza, 1 = hojas, y se le ponen materiales URP Lit hechos con la
    /// misma textura (el atlas del Tree Creator), creados una vez en la carpeta Blockout.
    /// </summary>
    public static TreeModelInfo TreeModel(GameObject prefab, Color barkTint, Color leafTint, bool overwrite = false)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        var filter = prefab.GetComponentInChildren<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        var sourceMats = new List<Material>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (mesh == null && o is Mesh m) mesh = m;
            if (o is Material mat) sourceMats.Add(mat);
        }
        if (mesh == null) return null;

        // Textura de cada parte: la del material de corteza / hojas del modelo, o la primera que haya.
        Texture Pick(string word)
        {
            foreach (var mat in sourceMats)
                if ((mat.name + mat.shader.name).Contains(word) && mat.HasProperty("_MainTex") && mat.mainTexture != null)
                    return mat.mainTexture;
            foreach (var mat in sourceMats)
                if (mat.HasProperty("_MainTex") && mat.mainTexture != null) return mat.mainTexture;
            return null;
        }

        string folder = Slug(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)));
        var bark = Material($"Blockout_Arbol_{folder}_Corteza", barkTint, 0.1f, 0f, false, overwrite);
        var leaves = Material($"Blockout_Arbol_{folder}_Hojas", leafTint, 0.15f, 0f, false, overwrite);
        if (overwrite || bark.GetTexture("_BaseMap") == null)
        {
            bark.SetTexture("_BaseMap", Pick("Bark"));
            bark.enableInstancing = true;
            EditorUtility.SetDirty(bark);
        }
        if (overwrite || leaves.GetTexture("_BaseMap") == null)
        {
            // Hojas recortadas por la transparencia del atlas y visibles por las dos caras.
            leaves.SetTexture("_BaseMap", Pick("Leaf"));
            leaves.SetFloat("_AlphaClip", 1f);
            leaves.SetFloat("_Cutoff", 0.3f);
            leaves.EnableKeyword("_ALPHATEST_ON");
            leaves.SetFloat("_Cull", (float)CullMode.Off);
            leaves.SetOverrideTag("RenderType", "TransparentCutout");
            leaves.renderQueue = (int)RenderQueue.AlphaTest;
            leaves.doubleSidedGI = true;
            leaves.enableInstancing = true;
            EditorUtility.SetDirty(leaves);
        }

        var v = mesh.vertices;
        var leafIdx = mesh.subMeshCount > 1 ? mesh.GetIndices(1) : mesh.GetIndices(0);
        var barkIdx = mesh.GetIndices(0);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (int i in leafIdx) { var p = new Vector2(v[i].x, v[i].z); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        float trunk = 0f;
        foreach (int i in barkIdx)
            if (v[i].y > 1f && v[i].y < 2f) trunk = Mathf.Max(trunk, new Vector2(v[i].x, v[i].z).magnitude);

        return new TreeModelInfo
        {
            mesh = mesh,
            materials = mesh.subMeshCount > 1 ? new[] { bark, leaves } : new[] { leaves },
            crownDiameter = ((max - min).x + (max - min).y) / 2f,
            trunkRadius = trunk > 0f ? trunk : 0.5f,
        };
    }

    // ------------------------------------------------------------------ mallas generadas

    /// <summary>
    /// Polígono en planta extruido entre <paramref name="bottom"/> y <paramref name="top"/> (en Y):
    /// el terreno de la parcela con la forma de la cerca, o una capa de grama. Coordenadas de mundo,
    /// UV en metros (la repetición la pone el material).
    /// </summary>
    public static Mesh Extrusion(string name, IList<Vector2> polygon, float top, float bottom)
    {
        var b = new MeshBuilder();
        var tris = PlanGeometry.Triangulate(polygon);
        foreach (var (y, n) in new[] { (top, Vector3.up), (bottom, Vector3.down) })
        {
            int first = b.Count;
            foreach (var p in polygon) b.Vertex(new Vector3(p.x, y, p.y), n, p);
            for (int i = 0; i < tris.Count; i += 3)
                b.Triangle(first + tris[i], first + tris[i + 1], first + tris[i + 2]);
        }

        // Cantos: un quad por lado, con la normal hacia fuera del polígono.
        float sign = Mathf.Sign(PlanGeometry.SignedArea(polygon));
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i], c = polygon[(i + 1) % polygon.Count], d = c - a;
            var n = new Vector3(d.y * sign, 0f, -d.x * sign).normalized;
            int v = b.Vertex(new Vector3(a.x, top, a.y), n, new Vector2(0f, top));
            b.Vertex(new Vector3(c.x, top, c.y), n, new Vector2(d.magnitude, top));
            b.Vertex(new Vector3(c.x, bottom, c.y), n, new Vector2(d.magnitude, bottom));
            b.Vertex(new Vector3(a.x, bottom, a.y), n, new Vector2(0f, bottom));
            b.Quad(v, v + 1, v + 2, v + 3);
        }
        return SaveGenerated(b.ToMesh(name));
    }

    /// <summary>
    /// Todas las vías en una malla: la cara de arriba del asfalto a <paramref name="height"/> m. Cada
    /// tramo es un rectángulo del ancho de su vía y cada vértice un disco de ese diámetro, que
    /// redondea quiebres y extremos como el trazo del plano. Lo que se solapa es coplanar, del mismo
    /// material y con la misma normal, así que no se nota.
    /// </summary>
    public static Mesh Roads(string name, IEnumerable<RoadSpec> roads, float height)
    {
        const int discSegments = 20;
        var b = new MeshBuilder();
        Vector3 At(Vector2 p) => new Vector3(p.x, height, p.y);

        foreach (var road in roads)
        {
            float hw = road.width / 2f;
            for (int i = 1; i < road.path.Count; i++)
            {
                Vector2 a = road.path[i - 1], c = road.path[i], dir = (c - a).normalized;
                var side = new Vector2(-dir.y, dir.x) * hw;
                int v = b.Vertex(At(a + side), Vector3.up, a + side);
                b.Vertex(At(c + side), Vector3.up, c + side);
                b.Vertex(At(c - side), Vector3.up, c - side);
                b.Vertex(At(a - side), Vector3.up, a - side);
                b.Quad(v, v + 1, v + 2, v + 3);
            }
            foreach (var p in road.path)
            {
                int center = b.Vertex(At(p), Vector3.up, p);
                for (int j = 0; j <= discSegments; j++)
                {
                    float ang = j / (float)discSegments * Mathf.PI * 2f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * hw;
                    b.Vertex(At(q), Vector3.up, q);
                }
                for (int j = 0; j < discSegments; j++)
                    b.Triangle(center, center + 1 + j, center + 2 + j);
            }
        }
        return SaveGenerated(b.ToMesh(name));
    }

    /// <summary>
    /// Techo inclinado sobre una huella de <paramref name="size"/> m (X × Z local), con el origen en el
    /// centro de la cabeza de los muros. El caballete va a lo largo del lado mayor. Es un sólido:
    /// canto de <paramref name="fascia"/> m en el alero, faldones con <paramref name="pitch"/> grados y
    /// el plafón plano debajo, que desde dentro hace de cielo raso.
    ///
    /// Submallas: 0 = cubierta (teja o zinc), 1 = plafón, 2 = hastiales (van con el material del
    /// muro). Cuatro aguas: los cuatro faldones con la misma pendiente y alero en todo el contorno.
    /// Dos aguas: alero solo en los lados largos; los hastiales van a ras de los muros.
    /// </summary>
    public static Mesh Roof(string name, RoofType type, Vector2 size, float pitch, float overhang, float fascia)
    {
        bool ridgeAlongZ = size.y >= size.x;
        float span = ridgeAlongZ ? size.x : size.y, length = ridgeAlongZ ? size.y : size.x;
        float so = span / 2f + overhang;
        float lo = type == RoofType.CuatroAguas ? length / 2f + overhang : length / 2f;
        float rise = so * Mathf.Tan(pitch * Mathf.Deg2Rad);
        float ridge = type == RoofType.CuatroAguas ? Mathf.Max(0f, lo - so) : lo;
        float top = fascia + rise;

        // (u, y, v): u a través del caballete, v a lo largo. Se llevan al marco del edificio al final.
        Vector3 P(float u, float y, float v) => ridgeAlongZ ? new Vector3(u, y, v) : new Vector3(v, y, u);
        var b = new MeshBuilder();

        void Face(int sub, Vector3 normal, params Vector3[] pts)
        {
            b.SubMesh = sub;
            int first = b.Count;
            foreach (var p in pts) b.Vertex(p, normal, new Vector2(p.x, p.z));
            for (int i = 1; i < pts.Length - 1; i++) b.Triangle(first, first + i, first + i + 1);
        }

        float s = Mathf.Sin(pitch * Mathf.Deg2Rad), c = Mathf.Cos(pitch * Mathf.Deg2Rad);
        foreach (float side in new[] { 1f, -1f })
        {
            // Faldones largos.
            Face(0, P(side * s, c, 0f).normalized,
                 P(side * so, fascia, -lo), P(side * so, fascia, lo), P(0f, top, ridge), P(0f, top, -ridge));
            // Canto del alero en los lados largos.
            Face(0, P(side, 0f, 0f), P(side * so, 0f, -lo), P(side * so, 0f, lo), P(side * so, fascia, lo), P(side * so, fascia, -lo));

            if (type == RoofType.CuatroAguas)
            {
                // Faldones de los extremos (triángulos) y su canto.
                float run = lo - ridge;
                Face(0, P(0f, 1f, side * rise / Mathf.Max(run, 0.001f)).normalized,
                     P(-so, fascia, side * lo), P(so, fascia, side * lo), P(0f, top, side * ridge));
                Face(0, P(0f, 0f, side), P(-so, 0f, side * lo), P(so, 0f, side * lo), P(so, fascia, side * lo), P(-so, fascia, side * lo));
            }
            else
            {
                // Hastial: el pentágono del extremo, con el material del muro.
                Face(2, P(0f, 0f, side), P(-so, 0f, side * lo), P(so, 0f, side * lo), P(so, fascia, side * lo),
                     P(0f, top, side * lo), P(-so, fascia, side * lo));
            }
        }
        Face(1, Vector3.down, P(-so, 0f, -lo), P(so, 0f, -lo), P(so, 0f, lo), P(-so, 0f, lo));
        b.SubMesh = 2; // que exista aunque quede vacía (cuatro aguas): el renderer lleva tres materiales
        return SaveGenerated(b.ToMesh(name));
    }

    /// <summary>
    /// Muchas copias de una malla unidad en una sola malla (p. ej. los cientos de postes de la cerca):
    /// un objeto en la escena en vez de uno por pieza.
    /// </summary>
    public static Mesh Combine(string name, Mesh unit, IList<Matrix4x4> placements)
    {
        var parts = new CombineInstance[placements.Count];
        for (int i = 0; i < placements.Count; i++)
            parts[i] = new CombineInstance { mesh = unit, transform = placements[i] };
        var mesh = new Mesh { name = name };
        if (unit.vertexCount * placements.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.CombineMeshes(parts, true, true);
        mesh.RecalculateBounds();
        return SaveGenerated(mesh);
    }

    static Mesh BuildSphere()
    {
        const int rings = 12, segments = 20;
        var b = new MeshBuilder();
        for (int i = 0; i <= rings; i++)
        {
            float phi = Mathf.PI * i / rings; // 0 = abajo
            for (int j = 0; j <= segments; j++)
            {
                float theta = 2f * Mathf.PI * j / segments;
                var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                b.Vertex(n * 0.5f + Vector3.up * 0.5f, n, new Vector2(j / (float)segments, i / (float)rings));
            }
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < segments; j++)
            {
                int a = i * (segments + 1) + j;
                b.Quad(a, a + 1, a + segments + 2, a + segments + 1);
            }
        return b.ToMesh("EsferaUnidad");
    }

    static Mesh BuildUnitBlock()
    {
        var verts = new Vector3[24];
        var normals = new Vector3[24];
        var uvs = new Vector2[24];
        var tris = new int[36];
        Vector3[] faces = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        for (int f = 0; f < 6; f++)
        {
            Vector3 n = faces[f];
            // U × V = -n deja las esquinas en sentido horario vistas desde fuera (cara frontal en Unity).
            // En las caras laterales V = arriba, para que una textura quede derecha.
            Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.Cross(n, Vector3.up);
            Vector3 v = Vector3.Cross(u, n);
            Vector3 c = n * 0.5f + Vector3.up * 0.5f;

            int b = f * 4;
            verts[b + 0] = c + (-u - v) * 0.5f;
            verts[b + 1] = c + (-u + v) * 0.5f;
            verts[b + 2] = c + ( u + v) * 0.5f;
            verts[b + 3] = c + ( u - v) * 0.5f;
            uvs[b + 0] = new Vector2(0, 0);
            uvs[b + 1] = new Vector2(0, 1);
            uvs[b + 2] = new Vector2(1, 1);
            uvs[b + 3] = new Vector2(1, 0);
            for (int i = 0; i < 4; i++) normals[b + i] = n;

            int t = f * 6;
            tris[t + 0] = b; tris[t + 1] = b + 1; tris[t + 2] = b + 2;
            tris[t + 3] = b; tris[t + 4] = b + 2; tris[t + 5] = b + 3;
        }

        var mesh = new Mesh { name = "BloqueUnidad", vertices = verts, normals = normals, uv = uvs, triangles = tris };
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Mesh BuildFrustum(string name, float bottomRadius, float topRadius)
    {
        const int segments = 24;
        var b = new MeshBuilder();

        // Lateral con normales suaves; la pendiente inclina la normal si los radios difieren.
        float slope = bottomRadius - topRadius;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments, a = t * Mathf.PI * 2f;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            var n = new Vector3(cos, slope, sin).normalized;
            b.Vertex(new Vector3(cos * bottomRadius, 0f, sin * bottomRadius), n, new Vector2(t, 0f));
            b.Vertex(new Vector3(cos * topRadius, 1f, sin * topRadius), n, new Vector2(t, 1f));
        }
        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            b.Quad(v, v + 1, v + 3, v + 2);
        }

        // Tapas planas.
        foreach (var (y, r, n) in new[] { (0f, bottomRadius, Vector3.down), (1f, topRadius, Vector3.up) })
        {
            int center = b.Vertex(new Vector3(0f, y, 0f), n, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                b.Vertex(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), n,
                         new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < segments; i++)
                b.Triangle(center, center + 1 + i, center + 2 + i);
        }
        return b.ToMesh(name);
    }

    static Mesh BuildParaboloid(string name, float focalRatio)
    {
        const int rings = 16, segments = 64;
        const float rim = 0.5f;
        float f = focalRatio; // con D = 1, f = f/D
        var b = new MeshBuilder();

        // Dos pasadas: la cara cóncava (normal hacia el foco) y la convexa (la trasera).
        foreach (float side in new[] { 1f, -1f })
        {
            int center = b.Vertex(Vector3.zero, Vector3.forward * side, new Vector2(0.5f, 0.5f));
            int first = center + 1;
            for (int i = 1; i <= rings; i++)
            {
                float r = rim * i / rings;
                for (int j = 0; j < segments; j++)
                {
                    float a = j / (float)segments * Mathf.PI * 2f;
                    float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
                    // Normal de z = (x² + y²)/4f hacia el lado cóncavo: (−x/2f, −y/2f, 1).
                    var n = new Vector3(-x / (2f * f), -y / (2f * f), 1f).normalized * side;
                    b.Vertex(new Vector3(x, y, r * r / (4f * f)), n, new Vector2(0.5f + x, 0.5f + y));
                }
            }

            int Ring(int i, int j) => first + (i - 1) * segments + (j % segments);
            for (int j = 0; j < segments; j++)
                b.Triangle(center, Ring(1, j), Ring(1, j + 1));
            for (int i = 2; i <= rings; i++)
                for (int j = 0; j < segments; j++)
                    b.Quad(Ring(i - 1, j), Ring(i, j), Ring(i, j + 1), Ring(i - 1, j + 1));
        }
        return b.ToMesh(name);
    }

    /// <summary>
    /// Acumula vértices y triángulos. Cada triángulo se orienta solo según las normales de sus
    /// vértices, así que no hay que acertar a mano el sentido de giro (Unity: horario = cara frontal).
    /// Los triángulos van a la submalla <see cref="SubMesh"/> (una por material).
    /// </summary>
    sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<List<int>> subMeshes = new List<List<int>> { new List<int>() };
        int sub;

        public int Count => verts.Count;

        public int SubMesh
        {
            get => sub;
            set { sub = value; while (subMeshes.Count <= sub) subMeshes.Add(new List<int>()); }
        }

        public int Vertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            verts.Add(p); normals.Add(n); uvs.Add(uv);
            return verts.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            var facing = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (Vector3.Dot(facing, normals[a] + normals[b] + normals[c]) < 0f) (b, c) = (c, b);
            var tris = subMeshes[sub];
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subMeshes.Count;
            for (int i = 0; i < subMeshes.Count; i++) mesh.SetTriangles(subMeshes[i], i);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
