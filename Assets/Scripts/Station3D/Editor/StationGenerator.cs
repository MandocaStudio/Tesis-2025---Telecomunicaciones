using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Levanta el blockout de la estación a partir de un StationLayout, en la escena activa.
///
/// Reglas (MODULO-3D.md §4):
///  1. Todo cuelga de un único root marcado con StationGeneratedRoot. Regenerar = borrar ESE root
///     y reconstruirlo; nada fuera de él se toca.
///  2. Ninguna medida vive aquí: todas salen del StationLayout.
///  3. 1 unidad = 1 metro. Cada caja es BloqueUnidad escalado a su medida real.
///  4. El layout se valida antes de construir y cada problema sale como warning en consola.
///  5. Pivotes a ras de suelo: cada grupo se apoya en Y = 0 (los muros, en la losa de piso).
/// </summary>
public static class StationGenerator
{
    public const string RootName = "--- GENERADO: Estación ---";
    public const string DefaultLayoutPath = "Assets/Data/AndresBelloLayout.asset";
    const string LogTag = "[Estación]";

    /// <summary>Qué partes construir. Se recuerdan entre sesiones del Editor.</summary>
    public struct Options
    {
        public bool terrain, fence, building, roof, pedestals;

        const string Prefix = "PVI.Station3D.";

        public static Options Load() => new Options
        {
            terrain   = EditorPrefs.GetBool(Prefix + "terrain", true),
            fence     = EditorPrefs.GetBool(Prefix + "fence", true),
            building  = EditorPrefs.GetBool(Prefix + "building", true),
            roof      = EditorPrefs.GetBool(Prefix + "roof", true),
            pedestals = EditorPrefs.GetBool(Prefix + "pedestals", true),
        };

        public void Save()
        {
            EditorPrefs.SetBool(Prefix + "terrain", terrain);
            EditorPrefs.SetBool(Prefix + "fence", fence);
            EditorPrefs.SetBool(Prefix + "building", building);
            EditorPrefs.SetBool(Prefix + "roof", roof);
            EditorPrefs.SetBool(Prefix + "pedestals", pedestals);
        }
    }

    [MenuItem("PVI/Estación 3D/Regenerar blockout")]
    static void RegenerateFromMenu()
    {
        var layout = FindRoots().Select(r => r.layout).FirstOrDefault(l => l != null)
                     ?? AssetDatabase.LoadAssetAtPath<StationLayout>(DefaultLayoutPath);
        if (layout == null)
        {
            Debug.LogWarning($"{LogTag} No hay layout: crea {DefaultLayoutPath} (Create > PVI > Station Layout).");
            return;
        }
        Generate(layout, Options.Load());
    }

    /// <summary>Roots generados en la escena activa.</summary>
    public static List<StationGeneratedRoot> FindRoots()
    {
        var scene = EditorSceneManager.GetActiveScene();
        return Object.FindObjectsByType<StationGeneratedRoot>(FindObjectsInactive.Include)
                     .Where(r => r.gameObject.scene == scene)
                     .ToList();
    }

    public static GameObject Generate(StationLayout layout, Options opts)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning($"{LogTag} Sal de Play para generar: lo construido en Play se pierde al pararlo.");
            return null;
        }

        foreach (string issue in layout.Validate())
            Debug.LogWarning($"{LogTag} {issue}", layout);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Regenerar estación");

        foreach (var old in FindRoots())
            Undo.DestroyObjectImmediate(old.gameObject);

        var kit = new Kit();
        var root = new GameObject(RootName);
        root.AddComponent<StationGeneratedRoot>().layout = layout;

        if (opts.terrain)   BuildTerrain(root.transform, layout, kit);
        if (opts.fence)     BuildFence(root.transform, layout, kit);
        if (opts.building)  BuildBuilding(root.transform, layout.building, kit, opts.roof);
        if (opts.pedestals) BuildPedestals(root.transform, layout, kit);

        // Se registra al final para que el Undo se lleve la jerarquía entera de una vez.
        Undo.RegisterCreatedObjectUndo(root, "Regenerar estación");
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log($"{LogTag} Blockout generado desde '{layout.name}': {kit.Count} piezas en '{root.scene.name}'.", root);
        return root;
    }

    public static void Clear()
    {
        var roots = FindRoots();
        if (roots.Count == 0) return;
        Undo.SetCurrentGroupName("Borrar estación generada");
        foreach (var r in roots)
        {
            var scene = r.gameObject.scene;
            Undo.DestroyObjectImmediate(r.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    // ------------------------------------------------------------------ partes

    static void BuildTerrain(Transform root, StationLayout L, Kit k)
    {
        // Cara superior en Y = 0: la losa crece hacia abajo.
        k.Box(root, "Terreno",
              new Vector3(L.plotSize.x / 2f, -L.groundThickness, L.plotSize.y / 2f),
              new Vector3(L.plotSize.x, L.groundThickness, L.plotSize.y), k.Ground);
    }

    static void BuildFence(Transform root, StationLayout L, Kit k)
    {
        var f = L.fence;
        var fence = Group(root, "Cerca perimetral");
        var posts = Group(fence, "Postes");
        float W = L.plotSize.x, H = L.plotSize.y;

        // Los lados N y S ponen los postes de esquina; E y O no, para no duplicarlos.
        FenceSide(PlotSide.Sur,   new Vector3(0, 0, 0), true,  W, true);
        FenceSide(PlotSide.Norte, new Vector3(0, 0, H), true,  W, true);
        FenceSide(PlotSide.Oeste, new Vector3(0, 0, 0), false, H, false);
        FenceSide(PlotSide.Este,  new Vector3(W, 0, 0), false, H, false);

        void FenceSide(PlotSide side, Vector3 start, bool alongX, float length, bool cornerPosts)
        {
            bool hasGate = side == f.gateSide;
            float g0 = f.gateCenter - f.gateWidth / 2f, g1 = f.gateCenter + f.gateWidth / 2f;
            Vector3 dir = alongX ? Vector3.right : Vector3.forward;

            // Malla: un tramo por lado, o dos si el portón lo corta.
            var spans = hasGate ? new[] { (0f, g0), (g1, length) } : new[] { (0f, length) };
            int n = 1;
            foreach (var (a, b) in spans)
            {
                if (b - a <= 0f) continue;
                string name = spans.Length > 1 ? $"Malla {side} · tramo {n++}" : $"Malla {side}";
                k.Box(fence, name, start + dir * ((a + b) / 2f),
                      Oriented(alongX, b - a, f.height, f.meshThickness), k.ChainLink);
            }

            int count = Mathf.Max(1, Mathf.CeilToInt(length / f.postSpacing));
            float step = length / count;
            for (int i = 0; i <= count; i++)
            {
                if (!cornerPosts && (i == 0 || i == count)) continue;
                float s = i * step;
                if (hasGate && s > g0 - f.gatePostSize && s < g1 + f.gatePostSize) continue;
                k.Box(posts, $"Poste {side} {i}", start + dir * s,
                      new Vector3(f.postSize, f.height, f.postSize), k.Post, collider: false);
            }

            if (!hasGate) return;
            var gate = Group(fence, "Portón", start + dir * f.gateCenter);
            k.Box(gate, "Poste oeste", dir * -(f.gateWidth / 2f),
                  new Vector3(f.gatePostSize, f.height, f.gatePostSize), k.Post);
            k.Box(gate, "Poste este", dir * (f.gateWidth / 2f),
                  new Vector3(f.gatePostSize, f.height, f.gatePostSize), k.Post);
            k.Box(gate, "Hoja", Vector3.zero,
                  Oriented(alongX, f.gateWidth - f.gatePostSize, f.height, f.gateLeafThickness), k.Gate);
        }
    }

    static void BuildBuilding(Transform root, BuildingSpec b, Kit k, bool roof)
    {
        // Coordenadas locales del edificio: origen en el centro de la huella, a ras de suelo;
        // x de -W/2 (oeste) a +W/2 (este), z de +D/2 (norte) a -D/2 (sur).
        var bld = Group(root, b.name, new Vector3(b.center.x, 0f, b.center.y));
        var rooms = Group(bld, "Locales");
        var exterior = Group(bld, "Muros exteriores");
        var partitions = Group(bld, "Tabiquería");

        float W = b.size.x, D = b.size.y;
        float tE = b.exteriorWallThickness, tI = b.interiorWallThickness;
        float floorTop = b.floorThickness, wallH = b.WallHeight;
        int last = b.rows.Count - 1;

        // Vanos que va a llevar cada muro, medidos desde su extremo oeste.
        var northOpenings = new List<Opening>();
        var southOpenings = new List<Opening>();
        var rowBoundaryOpenings = Enumerable.Range(0, Mathf.Max(0, last)).Select(_ => new List<Opening>()).ToArray();

        float zTop = D / 2f;
        for (int r = 0; r <= last; r++)
        {
            var row = b.rows[r];
            float zBot = zTop - row.depth;
            float x0 = -W / 2f;

            for (int j = 0; j < row.rooms.Count; j++)
            {
                var room = row.rooms[j];
                float x1 = x0 + room.width;

                var roomGo = Group(rooms, room.name, new Vector3((x0 + x1) / 2f, 0f, (zTop + zBot) / 2f));
                k.Box(roomGo, "Piso", Vector3.zero, new Vector3(room.width, b.floorThickness, row.depth), k.Floor);

                foreach (var door in room.doors)
                {
                    float fromWest = x0 + door.offset + W / 2f;
                    var opening = new Opening(fromWest, door.width, door.height);
                    if (door.side == DoorSide.Fachada)
                    {
                        if (r == 0) northOpenings.Add(opening);
                        else if (r == last) southOpenings.Add(opening);
                    }
                    else
                    {
                        // Los tabiques longitudinales empiezan en la cara interior del muro oeste.
                        var inPartition = new Opening(fromWest - tE, door.width, door.height);
                        int side = b.CorridorNeighbor(r);
                        if (side == -1) rowBoundaryOpenings[r - 1].Add(inPartition);
                        else if (side == +1) rowBoundaryOpenings[r].Add(inPartition);
                    }
                }

                // Tabique transversal al este del local, de cara interior a cara interior de la fila.
                if (j < row.rooms.Count - 1)
                {
                    float zN = zTop - (r == 0 ? tE : tI / 2f);
                    float zS = zBot + (r == last ? tE : tI / 2f);
                    Wall(k, partitions, $"Tabique {room.name} | {row.rooms[j + 1].name}", false,
                         new Vector3(x1, floorTop, zS), zN - zS, tI, wallH, null, k.Partition);
                }
                x0 = x1;
            }
            zTop = zBot;
        }

        // Muros exteriores por dentro de la huella. Norte y sur de punta a punta; este y oeste
        // entre ellos, para que las esquinas no se solapen.
        Wall(k, exterior, "Fachada norte", true, new Vector3(-W / 2f, floorTop, D / 2f - tE / 2f), W, tE, wallH, northOpenings, k.ExteriorWall);
        Wall(k, exterior, "Fachada sur", true, new Vector3(-W / 2f, floorTop, -D / 2f + tE / 2f), W, tE, wallH, southOpenings, k.ExteriorWall);
        Wall(k, exterior, "Fachada oeste", false, new Vector3(-W / 2f + tE / 2f, floorTop, -D / 2f + tE), D - 2f * tE, tE, wallH, null, k.ExteriorWall);
        Wall(k, exterior, "Fachada este", false, new Vector3(W / 2f - tE / 2f, floorTop, -D / 2f + tE), D - 2f * tE, tE, wallH, null, k.ExteriorWall);

        // Tabiques longitudinales entre filas, de muro oeste a muro este.
        zTop = D / 2f;
        for (int r = 0; r < last; r++)
        {
            zTop -= b.rows[r].depth;
            Wall(k, partitions, $"Tabique {b.rows[r].name} | {b.rows[r + 1].name}", true,
                 new Vector3(-W / 2f + tE, floorTop, zTop), W - 2f * tE, tI, wallH, rowBoundaryOpenings[r], k.Partition);
        }

        if (roof)
            k.Box(bld, "Losa de techo", new Vector3(0f, b.height - b.roofThickness, 0f),
                  new Vector3(W, b.roofThickness, D), k.Roof);
    }

    static void BuildPedestals(Transform root, StationLayout L, Kit k)
    {
        var antennas = Group(root, "Antenas");
        foreach (var a in L.antennas)
        {
            // Ancla a ras de suelo en el eje de la antena; ahí colgará el modelo de la antena.
            var anchor = Group(antennas, a.name, new Vector3(a.position.x, 0f, a.position.y));
            if (a.HasPedestal)
                k.Box(anchor, "Pedestal", Vector3.zero,
                      new Vector3(a.pedestalSize.x, a.pedestalHeight, a.pedestalSize.y), k.Concrete);
        }
    }

    // ------------------------------------------------------------------ muros

    readonly struct Opening
    {
        public readonly float center, width, height;
        public Opening(float center, float width, float height) { this.center = center; this.width = width; this.height = height; }
    }

    /// <summary>
    /// Muro recto de <paramref name="length"/> m a lo largo de X o de Z, desde <paramref name="start"/>
    /// (punto del eje del muro en su base). Cada vano parte el muro en tramos y deja un dintel encima.
    /// </summary>
    static void Wall(Kit k, Transform parent, string name, bool alongX, Vector3 start,
                     float length, float thickness, float height, List<Opening> openings, Material mat)
    {
        if (openings == null || openings.Count == 0)
        {
            k.Box(parent, name, start + Along(alongX, length / 2f), Oriented(alongX, length, height, thickness), mat);
            return;
        }

        var wall = Group(parent, name, start);
        float cursor = 0f;
        int piece = 1, lintel = 1;
        foreach (var o in openings.OrderBy(o => o.center))
        {
            float a = Mathf.Clamp(o.center - o.width / 2f, 0f, length);
            float b = Mathf.Clamp(o.center + o.width / 2f, 0f, length);
            if (a < cursor)
            {
                Debug.LogWarning($"{LogTag} Dos vanos se pisan en '{name}' (a {o.center:0.##} m). Se recorta el segundo.");
                a = cursor;
            }
            if (b <= a) continue;

            if (a > cursor)
                k.Box(wall, $"Tramo {piece++}", Along(alongX, (cursor + a) / 2f), Oriented(alongX, a - cursor, height, thickness), mat);
            if (height > o.height)
                k.Box(wall, $"Dintel {lintel++}", Along(alongX, (a + b) / 2f) + Vector3.up * o.height,
                      Oriented(alongX, b - a, height - o.height, thickness), mat);
            cursor = b;
        }
        if (cursor < length)
            k.Box(wall, $"Tramo {piece}", Along(alongX, (cursor + length) / 2f), Oriented(alongX, length - cursor, height, thickness), mat);
    }

    static Vector3 Along(bool alongX, float s) => alongX ? new Vector3(s, 0f, 0f) : new Vector3(0f, 0f, s);

    /// <summary>Tamaño de una pieza de <paramref name="length"/> m a lo largo de X o de Z.</summary>
    static Vector3 Oriented(bool alongX, float length, float height, float thickness) =>
        alongX ? new Vector3(length, height, thickness) : new Vector3(thickness, height, length);

    static Transform Group(Transform parent, string name, Vector3 localPosition = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    // ------------------------------------------------------------------ piezas

    /// <summary>Malla, materiales y contador de piezas de una generación.</summary>
    sealed class Kit
    {
        public readonly Material Ground       = BlockoutAssets.Material("Blockout_Terreno",      new Color(0.56f, 0.64f, 0.42f));
        public readonly Material ChainLink    = BlockoutAssets.Material("Blockout_Malla",        new Color(0.30f, 0.33f, 0.36f, 0.35f), transparent: true);
        public readonly Material Post         = BlockoutAssets.Material("Blockout_Poste",        new Color(0.28f, 0.30f, 0.33f));
        public readonly Material Gate         = BlockoutAssets.Material("Blockout_Porton",       new Color(0.20f, 0.22f, 0.26f));
        public readonly Material Floor        = BlockoutAssets.Material("Blockout_Piso",         new Color(0.62f, 0.63f, 0.66f));
        public readonly Material ExteriorWall = BlockoutAssets.Material("Blockout_MuroExterior", new Color(0.93f, 0.94f, 0.96f));
        public readonly Material Partition    = BlockoutAssets.Material("Blockout_Tabique",      new Color(0.80f, 0.83f, 0.88f));
        public readonly Material Roof         = BlockoutAssets.Material("Blockout_Techo",        new Color(0.70f, 0.72f, 0.76f));
        public readonly Material Concrete     = BlockoutAssets.Material("Blockout_Concreto",     new Color(0.66f, 0.65f, 0.62f));

        readonly Mesh block = BlockoutAssets.UnitBlock();
        public int Count;

        /// <summary>
        /// Caja apoyada en <paramref name="basePosition"/> (centro de su cara inferior) y con
        /// <paramref name="size"/> en metros. Las piezas nulas o negativas no se crean.
        /// </summary>
        public GameObject Box(Transform parent, string name, Vector3 basePosition, Vector3 size,
                              Material mat, bool collider = true)
        {
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePosition;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = block;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.5f, 0f);
                box.size = Vector3.one;
            }
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            Count++;
            return go;
        }
    }
}
