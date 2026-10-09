using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

/// <summary>
/// "Vegetación y piedras": reparte por el terreno las capas de StationLayout.scatter (arbustos, grama
/// alta, piedras) y monta un InstancedScatter por capa. Las copias se guardan empaquetadas en
/// Generado/Disperso_*.asset (ScatterData): miles de copias sin un GameObject cada una.
///
/// Dónde sale cada capa lo dicen sus densidades por zona (ScatterLayer); que no caiga en vías, edificios,
/// tanques, losas, bajo un plato ni sobre la cerca lo decide SiteClearance, lo mismo que con los árboles.
/// Con la misma semilla, el reparto sale idéntico.
/// </summary>
public static class ScatterBuilder
{
    const string LogTag = "[Estación]";

    /// <summary>Más copias que esto en una capa ya pesan en una gráfica integrada: se avisa.</summary>
    const int InstanceBudget = 15000;

    sealed class VariantSource
    {
        public string name;
        public Material[] materials;
        public InstancedScatter.Band[] bands;
        public Matrix4x4 pivot;
        public float height;
        public float weight;
    }

    public static int Build(Transform parent, StationLayout L)
    {
        var group = new GameObject("Vegetación y piedras").transform;
        group.SetParent(parent, false);
        var clearance = new SiteClearance(L);
        var patches = PatchMask(L.surfaces.parcel);
        int pieces = 0;
        foreach (var (name, layer) in L.scatter.All())
            if (BuildLayer(group, name, layer, L, clearance, patches)) pieces++;
        return pieces;
    }

    static bool BuildLayer(Transform parent, string name, ScatterLayer layer, StationLayout L, SiteClearance clearance,
                           Func<Vector2, float> patches)
    {
        int bandCount = layer.distances?.Length ?? 0;
        var variants = new List<VariantSource>();
        foreach (var m in layer.models.Where(m => m.model != null))
            variants.AddRange(Variants(m, layer, bandCount));
        if (variants.Count == 0 || bandCount == 0) return false;

        var placed = Place(layer, L, clearance, patches, variants);
        int total = placed.Sum(p => p.Count);

        // Copias empaquetadas en su asset (conserva el GUID al regenerar).
        string path = $"{BlockoutAssets.GeneratedFolder}/Disperso_{BlockoutAssets.Slug(name)}.asset";
        var data = AssetDatabase.LoadAssetAtPath<ScatterData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ScatterData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.sets = placed.Select(p => new ScatterData.Set { packed = ScatterData.Pack(p.Select(x => x.Item1).ToList(), p.Select(x => x.Item2).ToList()) }).ToList();
        EditorUtility.SetDirty(data);

        var go = new GameObject(char.ToUpper(name[0]) + name.Substring(1));
        go.transform.SetParent(parent, false);
        var scatter = go.AddComponent<InstancedScatter>();
        scatter.data = data;
        scatter.distances = layer.distances.ToArray();
        scatter.lowDistanceScale = layer.lowDistanceScale;
        scatter.lowDensity = layer.lowDensity;
        scatter.variants = variants.Select(v => new InstancedScatter.Variant
        {
            name = v.name, materials = v.materials, bands = v.bands, pivot = v.pivot,
        }).ToList();
        scatter.Build();

        Debug.Log($"{LogTag} {go.name}: {total} copias de {variants.Count} variantes.", go);
        if (total > InstanceBudget)
            Debug.LogWarning($"{LogTag} '{name}' suma {total} copias (más de {InstanceBudget}): baja sus densidades, que pesa en una gráfica integrada.", L);
        return true;
    }

    // ------------------------------------------------------------------ modelos → variantes

    static IEnumerable<VariantSource> Variants(ScatterModel m, ScatterLayer layer, int bandCount)
    {
        var prefab = m.model;
        var toRoot = prefab.transform.worldToLocalMatrix;
        var lodGroup = prefab.GetComponentInChildren<LODGroup>();

        if (lodGroup != null)
        {
            // Una variante: cada nivel del LODGroup, combinado en una malla en metros.
            var lods = lodGroup.GetLODs();
            var meshes = new Mesh[lods.Length];
            Material source = null;
            for (int l = 0; l < lods.Length; l++)
            {
                var parts = lods[l].renderers.Where(r => r != null && r.GetComponent<MeshFilter>() != null).ToList();
                source ??= parts.Select(r => r.sharedMaterial).FirstOrDefault(x => x != null);
                var combine = parts.Select(r => new CombineInstance
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    transform = toRoot * r.transform.localToWorldMatrix,
                }).ToArray();
                var mesh = new Mesh { name = $"Disperso_{BlockoutAssets.Slug(prefab.name)}_LOD{l}" };
                mesh.CombineMeshes(combine, true, true);
                mesh.RecalculateBounds();
                meshes[l] = BlockoutAssets.SaveGenerated(mesh);
            }
            var b = meshes[0].bounds;
            var mat = ScatterMaterial(prefab.name, m, source);
            yield return new VariantSource
            {
                name = prefab.name,
                materials = new[] { mat },
                bands = Enumerable.Range(0, bandCount).Select(i => new InstancedScatter.Band
                {
                    mesh = meshes[Mathf.Min(i, meshes.Length - 1)], meshLod = -1, castShadows = i < layer.shadowBands,
                }).ToArray(),
                pivot = Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y - layer.sink * b.size.y, -b.center.z)),
                height = b.size.y,
                weight = m.weight,
            };
            yield break;
        }

        // Sin LODGroup (los FBX de Poly Haven): cada malla hija es una variante y los niveles salen de su
        // Mesh LOD, eligiendo el primero que quepa en el presupuesto de triángulos.
        var filters = prefab.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToList();
        var material = ScatterMaterial(prefab.name + (m.color != null ? "_" + m.color.name : ""), m,
                                       filters.Select(f => f.GetComponent<MeshRenderer>()?.sharedMaterial).FirstOrDefault(x => x != null));
        foreach (var f in filters)
        {
            var mesh = f.sharedMesh;
            var M = toRoot * f.transform.localToWorldMatrix;
            M.SetColumn(3, new Vector4(0f, 0f, 0f, 1f)); // en el FBX van en fila: cada una a su origen
            var b = TransformBounds(M, mesh.bounds);
            int near = 0;
            while (near < mesh.lodCount - 1 && mesh.GetLod(0, near).indexCount / 3 > layer.nearTriangles) near++;
            yield return new VariantSource
            {
                name = f.name + (m.color != null ? " · " + m.color.name : ""),
                materials = new[] { material },
                bands = Enumerable.Range(0, bandCount).Select(i => new InstancedScatter.Band
                {
                    mesh = mesh,
                    meshLod = mesh.lodCount > 1 ? Mathf.Min(near + 2 * i, mesh.lodCount - 1) : -1,
                    castShadows = i < layer.shadowBands,
                }).ToArray(),
                pivot = Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y - layer.sink * b.size.y, -b.center.z)) * M,
                height = b.size.y,
                weight = m.weight / filters.Count,
            };
        }
    }

    /// <summary>
    /// Material URP Lit de un modelo repartido. La textura sale de los datos o, si no, del material del
    /// modelo (los arbustos Yughues traen el Standard del pipeline antiguo, que en URP sale rosa: se toma
    /// su textura y su recorte). Hojas: recortadas por la transparencia y por las dos caras.
    /// </summary>
    static Material ScatterMaterial(string name, ScatterModel m, Material source)
    {
        var mat = BlockoutAssets.Material("Blockout_Disperso_" + BlockoutAssets.Slug(name), m.tint, m.foliage ? 0.15f : 0.1f, 0f, false, overwrite: true);
        var color = m.color != null ? m.color : source != null && source.HasProperty("_MainTex") ? source.mainTexture : null;
        var normal = m.normal != null ? m.normal : source != null && source.HasProperty("_BumpMap") ? source.GetTexture("_BumpMap") : null;
        mat.SetTexture("_BaseMap", color);
        mat.SetTexture("_BumpMap", normal);
        if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
        if (m.foliage)
        {
            float cutoff = source != null && source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f;
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", cutoff);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            mat.doubleSidedGI = true;
        }
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------ reparto

    static List<(Vector4, float)>[] Place(ScatterLayer layer, StationLayout L, SiteClearance clearance,
                                         Func<Vector2, float> patches, List<VariantSource> variants)
    {
        var rnd = new Random(layer.seed);
        var result = variants.Select(_ => new List<(Vector4, float)>()).ToArray();
        var taken = new Dictionary<Vector2Int, List<Vector2>>();
        var outline = L.fence.outline;
        float totalWeight = variants.Sum(v => v.weight);
        float R() => (float)rnd.NextDouble();
        Vector2 Disc() { float a = R() * Mathf.PI * 2f, r = Mathf.Sqrt(R()); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }

        bool Crowded(Vector2 p)
        {
            float min = layer.radius * 0.9f;
            var c = new Vector2Int(Mathf.FloorToInt(p.x / min), Mathf.FloorToInt(p.y / min));
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (taken.TryGetValue(c + new Vector2Int(x, y), out var list) && list.Any(q => (q - p).sqrMagnitude < min * min))
                    return true;
            if (!taken.TryGetValue(c, out var own)) taken[c] = own = new List<Vector2>();
            own.Add(p);
            return false;
        }

        void Put(Vector2 p)
        {
            bool inside = PlanGeometry.Contains(outline, p);
            if (!clearance.IsClear(p, layer.radius) || Crowded(p)) return;
            float pick = R() * totalWeight;
            int v = 0;
            while (v < variants.Count - 1 && (pick -= variants[v].weight) > 0f) v++;
            float height = Mathf.Lerp(layer.height.x, layer.height.y, R());
            float y = inside ? 0f : -L.environment.savannaDrop;
            result[v].Add((new Vector4(p.x, y, p.y, R() * Mathf.PI * 2f), height / variants[v].height));
        }

        void Clump(Vector2 center, Func<Vector2, bool> where)
        {
            int n = rnd.Next(layer.clump.x, layer.clump.y + 1);
            for (int i = 0; i < n; i++)
            {
                var p = i == 0 ? center : center + Disc() * layer.clumpRadius;
                if (where(p)) Put(p);
            }
        }

        int Count(float perUnit, float units) { float n = perUnit * units; int k = (int)n; return k + (R() < n - k ? 1 : 0); }

        // Un punto al azar sobre la cerca, con la normal hacia fuera de la parcela.
        float orientation = Mathf.Sign(PlanGeometry.SignedArea(outline));
        float fenceLength = 0f;
        for (int i = 0; i < outline.Count; i++) fenceLength += Vector2.Distance(outline[i], outline[(i + 1) % outline.Count]);
        (Vector2 point, Vector2 outward) OnFence()
        {
            float s = R() * fenceLength;
            for (int i = 0; ; i++)
            {
                Vector2 a = outline[i % outline.Count], b = outline[(i + 1) % outline.Count];
                float len = Vector2.Distance(a, b);
                if (s <= len || i >= outline.Count - 1)
                {
                    var dir = (b - a) / Mathf.Max(len, 0.001f);
                    return (a + dir * Mathf.Min(s, len), new Vector2(dir.y, -dir.x) * orientation);
                }
                s -= len;
            }
        }

        bool Inside(Vector2 p) => PlanGeometry.Contains(outline, p);
        bool Outside(Vector2 p) => !Inside(p);
        bool Anywhere(Vector2 p) => true;

        // Junto a la cerca, por los dos lados.
        for (int i = Count(layer.alongFence, fenceLength / 100f); i > 0; i--)
        {
            var (p, n) = OnFence();
            float side = R() < 0.5f ? 1f : -1f;
            Clump(p + n * side * Mathf.Lerp(layer.fenceBand.x, layer.fenceBand.y, R()),
                  side > 0f ? Outside : Inside);
        }

        // Al pie de los árboles, bajo la copa.
        foreach (var t in L.trees)
            for (int i = Count(layer.perTree, 1f); i > 0; i--)
                Clump(t.position + Disc().normalized * Mathf.Lerp(1.2f, Mathf.Max(1.5f, t.crownDiameter * 0.35f), R()), Anywhere);

        // Sabana de fuera, hasta savannaWidth de la cerca.
        for (int i = Count(layer.savanna, fenceLength * layer.savannaWidth / 1000f); i > 0; i--)
        {
            var (p, n) = OnFence();
            Clump(p + n * Mathf.Lerp(layer.fenceBand.y, layer.savannaWidth, R()), Outside);
        }

        // Sobre las manchas de grama del suelo de la parcela (el mismo ruido que el shader).
        var box = PlanGeometry.Bounds(outline);
        float parcelArea = Mathf.Abs(PlanGeometry.SignedArea(outline));
        if (layer.parcelPatches > 0f)
        {
            int want = Count(layer.parcelPatches, parcelArea * L.surfaces.parcel.cover / 100f);
            for (int tries = want * 20; want > 0 && tries > 0; tries--)
            {
                var p = new Vector2(Mathf.Lerp(box.xMin, box.xMax, R()), Mathf.Lerp(box.yMin, box.yMax, R()));
                if (!Inside(p) || patches(p) < 0.6f) continue;
                Clump(p, q => Inside(q) && patches(q) > 0.4f);
                want--;
            }
        }

        // Campo abierto (las losas de grama).
        foreach (var pad in L.site.pads.Where(x => x.kind == PadKind.Grama))
            for (int i = Count(layer.field, pad.size.x * pad.size.y / 100f); i > 0; i--)
            {
                var print = pad.Footprint;
                var p = print.ToWorld(new Vector2((R() - 0.5f) * pad.size.x, (R() - 0.5f) * pad.size.y));
                Clump(p, q => print.Contains(q));
            }

        // Al borde de las vías, por los dos lados.
        foreach (var road in L.site.roads)
        {
            float length = 0f;
            for (int i = 1; i < road.path.Count; i++) length += Vector2.Distance(road.path[i - 1], road.path[i]);
            for (int k = Count(layer.alongRoads, 2f * length / 100f); k > 0; k--)
            {
                float s = R() * length;
                for (int i = 1; i < road.path.Count; i++)
                {
                    Vector2 a = road.path[i - 1], b = road.path[i];
                    float len = Vector2.Distance(a, b);
                    if (s > len && i < road.path.Count - 1) { s -= len; continue; }
                    var dir = (b - a) / Mathf.Max(len, 0.001f);
                    var normal = new Vector2(-dir.y, dir.x) * (R() < 0.5f ? 1f : -1f);
                    Clump(a + dir * Mathf.Min(s, len) + normal * (road.width / 2f + Mathf.Lerp(layer.edgeBand.x, layer.edgeBand.y, R())), Anywhere);
                    break;
                }
            }
        }

        // Al borde de las losas (concreto y grava).
        foreach (var pad in L.site.pads.Where(x => x.kind != PadKind.Grama))
        {
            var print = pad.Footprint;
            float perimeter = 2f * (pad.size.x + pad.size.y);
            for (int k = Count(layer.alongPads, perimeter / 100f); k > 0; k--)
            {
                float s = R() * perimeter, hx = pad.size.x / 2f, hz = pad.size.y / 2f;
                float off = Mathf.Lerp(layer.edgeBand.x, layer.edgeBand.y, R());
                Vector2 local = s < pad.size.x ? new Vector2(-hx + s, -hz - off)
                              : (s -= pad.size.x) < pad.size.y ? new Vector2(hx + off, -hz + s)
                              : (s -= pad.size.y) < pad.size.x ? new Vector2(hx - s, hz + off)
                              : new Vector2(-hx - off, hz - (s - pad.size.x));
                Clump(print.ToWorld(local), Anywhere);
            }
        }

        // Sueltas por la parcela.
        for (int i = Count(layer.parcel, parcelArea / 1000f), tries = i * 20; i > 0 && tries > 0; tries--)
        {
            var p = new Vector2(Mathf.Lerp(box.xMin, box.xMax, R()), Mathf.Lerp(box.yMin, box.yMax, R()));
            if (!Inside(p)) continue;
            Clump(p, Inside);
            i--;
        }

        return result;
    }

    /// <summary>
    /// Dónde están las manchas de grama del suelo de la parcela: la misma cuenta que hace el shader
    /// "PVI/Superficie en metros" con Manchas.png (sin el deshilachado del borde). 0 = tierra, 1 = grama.
    /// </summary>
    static Func<Vector2, float> PatchMask(GroundSurface g)
    {
        var img = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.HideAndDontSave };
        img.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(BlockoutAssets.PatchNoise())));
        img.wrapMode = TextureWrapMode.Repeat;
        img.filterMode = FilterMode.Bilinear;
        float scale = g.patchSize * 4f, a = 1f - g.cover - g.softness, b = 1f - g.cover + g.softness;
        return p =>
        {
            var u = p / scale;
            float r = img.GetPixelBilinear(u.x, u.y).r * 0.6f + img.GetPixelBilinear(u.x * 0.37f + 0.21f, u.y * 0.37f + 0.21f).r * 0.4f;
            r = Mathf.Clamp01((r - 0.5f) * 1.5f + 0.5f);
            float t = Mathf.Clamp01((r - a) / (b - a));
            return t * t * (3f - 2f * t);
        };
    }

    static Bounds TransformBounds(Matrix4x4 m, Bounds b)
    {
        var result = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            result.Encapsulate(m.MultiplyPoint3x4(corner));
        }
        return result;
    }
}
