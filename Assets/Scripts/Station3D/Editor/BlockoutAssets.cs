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
    public static Mesh UnitBlock()
    {
        string path = Folder + "/BloqueUnidad.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;

        EnsureFolder(Folder);
        mesh = BuildUnitBlock();
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

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
