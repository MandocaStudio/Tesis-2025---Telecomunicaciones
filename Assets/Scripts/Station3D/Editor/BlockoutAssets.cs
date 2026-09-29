using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Malla y materiales que usa el blockout. Se crean la primera vez en
/// Assets/Data/Station3D/Blockout/ y a partir de ahí se REUTILIZAN: regenerar no los pisa, así que
/// el acabado visual puede editar esos materiales (o sustituirlos) sin que se pierda.
/// </summary>
public static class BlockoutAssets
{
    public const string Folder = "Assets/Data/Station3D/Blockout";

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

    public static Material Material(string name, Color color, bool transparent = false)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        EnsureFolder(Folder);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader) { name = name };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.15f);
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
        AssetDatabase.CreateAsset(mat, path);
        return mat;
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
    /// </summary>
    sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        public int Vertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            verts.Add(p); normals.Add(n); uvs.Add(uv);
            return verts.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            var facing = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (Vector3.Dot(facing, normals[a] + normals[b] + normals[c]) < 0f) (b, c) = (c, b);
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
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
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
