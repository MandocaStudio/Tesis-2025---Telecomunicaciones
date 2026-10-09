using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Levanta el modelo de la estación a partir de un StationLayout, en la escena activa.
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
        public bool terrain, fence, building, roof, antennas, site, scatter, environment;

        const string Prefix = "PVI.Station3D.";

        public bool All => terrain && fence && building && roof && antennas && site && scatter && environment;

        public static Options Load() => new Options
        {
            terrain   = EditorPrefs.GetBool(Prefix + "terrain", true),
            fence     = EditorPrefs.GetBool(Prefix + "fence", true),
            building  = EditorPrefs.GetBool(Prefix + "building", true),
            roof      = EditorPrefs.GetBool(Prefix + "roof", true),
            antennas  = EditorPrefs.GetBool(Prefix + "antennas", true),
            site      = EditorPrefs.GetBool(Prefix + "site", true),
            scatter   = EditorPrefs.GetBool(Prefix + "scatter", true),
            environment = EditorPrefs.GetBool(Prefix + "environment", true),
        };

        public void Save()
        {
            EditorPrefs.SetBool(Prefix + "terrain", terrain);
            EditorPrefs.SetBool(Prefix + "fence", fence);
            EditorPrefs.SetBool(Prefix + "building", building);
            EditorPrefs.SetBool(Prefix + "roof", roof);
            EditorPrefs.SetBool(Prefix + "antennas", antennas);
            EditorPrefs.SetBool(Prefix + "site", site);
            EditorPrefs.SetBool(Prefix + "scatter", scatter);
            EditorPrefs.SetBool(Prefix + "environment", environment);
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

    /// <summary>
    /// Vuelve a poner en los materiales los colores y texturas de acabado del generador. Regenerar
    /// no los toca (para respetar retoques a mano); esto sí, y solo cuando se pide.
    /// </summary>
    [MenuItem("PVI/Estación 3D/Reaplicar colores del acabado")]
    static void ReapplyFinish()
    {
        var kit = new Kit(reapplyFinish: true);
        var layout = FindRoots().Select(r => r.layout).FirstOrDefault(l => l != null)
                     ?? AssetDatabase.LoadAssetAtPath<StationLayout>(DefaultLayoutPath);
        if (layout != null)
        {
            // Las superficies con textura real salen del layout, no de la paleta: se vuelven a poner.
            kit.ApplySurfaces(layout.surfaces);
            foreach (var model in layout.treeDesign.models.Where(m => m != null))
                BlockoutAssets.TreeModel(model, Kit.BarkTint, Kit.LeafTint, overwrite: true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"{LogTag} Colores del acabado reaplicados en {BlockoutAssets.Folder}.");
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

        BlockoutAssets.BeginGenerated();
        var kit = new Kit();
        kit.ApplySurfaces(layout.surfaces);
        var root = new GameObject(RootName);
        root.AddComponent<StationGeneratedRoot>().layout = layout;

        if (opts.terrain)   BuildTerrain(root.transform, layout, kit);
        if (opts.fence)     BuildFence(root.transform, layout, kit);
        if (opts.building)  BuildBuildings(root.transform, layout, kit, opts.roof);
        if (opts.antennas)  BuildAntennas(root.transform, layout, kit);
        if (opts.site)      BuildSite(root.transform, layout, kit);
        if (opts.scatter)   kit.Count += ScatterBuilder.Build(root.transform, layout);
        if (opts.environment) BuildEnvironment(root.transform, layout, kit);

        // Las mallas generadas que ya no salen del layout solo se pueden reconocer si se generó todo.
        if (opts.All) BlockoutAssets.EndGenerated();
        AssetDatabase.SaveAssets();

        // Se registra al final para que el Undo se lleve la jerarquía entera de una vez.
        Undo.RegisterCreatedObjectUndo(root, "Regenerar estación");
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log($"{LogTag} Estación generada desde '{layout.name}': {kit.Count} piezas en '{root.scene.name}'.", root);
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
        // Losa con la forma de la cerca; cara superior en Y = 0. UV en metros.
        var e = L.environment;
        k.Tile(k.Ground, Vector2.one, e.soilTile, e.grassDetailTile);
        var mesh = BlockoutAssets.Extrusion("Parcela", L.fence.outline, 0f, -L.groundThickness);
        k.MeshPiece(root, "Terreno", Vector3.zero, Quaternion.identity, mesh, new[] { k.Ground }, collider: true);
    }

    static void BuildFence(Transform root, StationLayout L, Kit k)
    {
        var f = L.fence;
        var outline = f.outline;
        var fence = Group(root, "Cerca perimetral");
        int gateEdge = PlanGeometry.NearestEdge(outline, f.gatePosition);
        float orientation = Mathf.Sign(PlanGeometry.SignedArea(outline));
        var posts = new List<Matrix4x4>();

        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 a = outline[i], b = outline[(i + 1) % outline.Count];
            float length = Vector2.Distance(a, b);
            if (length <= 0f) continue;
            Vector2 dir = (b - a) / length;
            var rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y)); // +Z local = a lo largo del lado
            string side = Compass(new Vector2(dir.y, -dir.x) * orientation);

            bool hasGate = i == gateEdge;
            float gc = Vector2.Dot(f.gatePosition - a, dir);
            float g0 = gc - f.gateWidth / 2f, g1 = gc + f.gateWidth / 2f;

            // Malla: un paño por lado, o dos si el portón lo corta.
            var spans = hasGate ? new[] { (0f, g0), (g1, length) } : new[] { (0f, length) };
            int n = 1;
            foreach (var (s0, s1) in spans)
            {
                if (s1 - s0 <= 0f) continue;
                string name = spans.Length > 1 ? $"Malla {side} · tramo {n++}" : $"Malla {side}";
                k.Piece(fence, name, Flat(a + dir * ((s0 + s1) / 2f)), rotation,
                        new Vector3(f.meshThickness, f.height, s1 - s0), k.Block, k.ChainLink, collider: true);
            }

            // Postes repartidos uniformes; el del final de cada lado lo pone el lado siguiente.
            int count = Mathf.Max(1, Mathf.CeilToInt(length / f.postSpacing));
            float step = length / count;
            for (int j = 0; j < count; j++)
            {
                float s = j * step;
                if (hasGate && s > g0 - f.gatePostSize && s < g1 + f.gatePostSize) continue;
                posts.Add(Matrix4x4.TRS(Flat(a + dir * s), rotation, new Vector3(f.postSize, f.height, f.postSize)));
            }

            if (!hasGate) continue;
            var gate = Group(fence, "Portón", Flat(a + dir * gc));
            gate.localRotation = rotation;
            var post = new Vector3(f.gatePostSize, f.height, f.gatePostSize);
            k.Box(gate, "Poste 1", Vector3.back * (f.gateWidth / 2f), post, k.Post);
            k.Box(gate, "Poste 2", Vector3.forward * (f.gateWidth / 2f), post, k.Post);
            k.Box(gate, "Hoja", Vector3.zero, new Vector3(f.gateLeafThickness, f.height, f.gateWidth - f.gatePostSize), k.Gate);
        }

        // Cientos de postes finos y sin collider (los tapa la malla): una sola malla.
        k.MeshPiece(fence, "Postes", Vector3.zero, Quaternion.identity,
                    BlockoutAssets.Combine("Postes", k.Block, posts), new[] { k.Post }, collider: false);
    }

    static void BuildBuildings(Transform root, StationLayout L, Kit k, bool roof)
    {
        var group = Group(root, "Edificios");
        foreach (var b in L.buildings)
            BuildBuilding(group, b, L, k, roof);
    }

    /// <summary>
    /// Un edificio en su marco local: origen en el centro de la huella a ras de suelo, girado
    /// <c>rotation</c> grados; x de −W/2 (oeste) a +W/2 (este), z de −D/2 (sur) a +D/2 (norte).
    /// Losa de piso, cuatro muros por dentro de la huella con sus puertas y ventanas, tabiques,
    /// franja azul y techo.
    /// </summary>
    static void BuildBuilding(Transform parent, BuildingSpec b, StationLayout L, Kit k, bool roof)
    {
        var d = L.buildingDesign;
        var bld = Group(parent, b.name, Flat(b.center));
        bld.localRotation = Quaternion.Euler(0f, b.rotation, 0f);

        float W = b.size.x, D = b.size.y;
        float t = d.exteriorWallThickness, tI = d.interiorWallThickness;
        float floorTop = d.floorThickness, wallH = b.wallHeight - floorTop;
        var wallMat = b.walls == WallFinish.Blanco ? k.ExteriorWall : k.ServiceWall;

        k.Box(bld, "Piso", Vector3.zero, new Vector3(W, floorTop, D), k.Floor);

        // Vanos de cada muro, medidos desde el arranque de ESE muro: los norte y sur arrancan en la
        // esquina oeste exterior; los este y oeste, un espesor de muro más al norte de la esquina sur.
        var openings = new Dictionary<Side, List<Opening>>();
        foreach (Side s in System.Enum.GetValues(typeof(Side))) openings[s] = new List<Opening>();
        float Start(Side s) => s == Side.Este || s == Side.Oeste ? t : 0f;

        foreach (var door in b.doors)
            openings[door.side].Add(new Opening(door.offset - Start(door.side), door.width, door.height));

        if (b.windows)
            foreach (Side s in openings.Keys.ToList())
                foreach (float c in WindowCenters(b, s, L))
                    openings[s].Add(new Opening(c - Start(s), d.windowWidth, d.windowHeight, d.windowSill, glazed: true));

        var walls = Group(bld, "Muros");
        Wall(k, walls, "Fachada norte", true, new Vector3(-W / 2f, floorTop, D / 2f - t / 2f), W, t, wallH, openings[Side.Norte], wallMat, d.glassThickness);
        Wall(k, walls, "Fachada sur", true, new Vector3(-W / 2f, floorTop, -D / 2f + t / 2f), W, t, wallH, openings[Side.Sur], wallMat, d.glassThickness);
        Wall(k, walls, "Fachada oeste", false, new Vector3(-W / 2f + t / 2f, floorTop, -D / 2f + t), D - 2f * t, t, wallH, openings[Side.Oeste], wallMat, d.glassThickness);
        Wall(k, walls, "Fachada este", false, new Vector3(W / 2f - t / 2f, floorTop, -D / 2f + t), D - 2f * t, t, wallH, openings[Side.Este], wallMat, d.glassThickness);

        // Los edificios que no se recorren llevan las puertas cerradas: una hoja en medio del vano.
        if (!b.enterable)
            foreach (var door in b.doors)
            {
                bool alongX = door.side == Side.Norte || door.side == Side.Sur;
                float inward = door.side == Side.Norte || door.side == Side.Este ? 1f : -1f;
                var at = alongX
                    ? new Vector3(-W / 2f + door.offset, floorTop, inward * (D / 2f - t / 2f))
                    : new Vector3(inward * (W / 2f - t / 2f), floorTop, -D / 2f + door.offset);
                k.Box(walls, $"Puerta {door.side}", at, Oriented(alongX, door.width, door.height, d.doorLeafThickness), k.Gate);
            }

        foreach (var p in b.partitions)
        {
            var door = p.doorOffset >= 0f
                ? new List<Opening> { new Opening(p.doorOffset - t, d.interiorDoorWidth, d.interiorDoorHeight) }
                : null;
            if (p.northSouth)
                Wall(k, walls, p.name, false, new Vector3(-W / 2f + p.position, floorTop, -D / 2f + t), D - 2f * t, tI, wallH, door, k.Partition);
            else
                Wall(k, walls, p.name, true, new Vector3(-W / 2f + t, floorTop, -D / 2f + p.position), W - 2f * t, tI, wallH, door, k.Partition);
        }

        // Franja azul: cuatro bandas pegadas por fuera de la huella. Las de norte y sur cubren las
        // esquinas; las de este y oeste van entre ellas, así ninguna cara se solapa con otra.
        if (b.stripe)
        {
            float sd = d.stripeDepth, sh = d.stripeHeight, sy = b.wallHeight - d.stripeBelowEaves - sh;
            var stripe = Group(bld, "Franja azul");
            k.Box(stripe, "Norte", new Vector3(0f, sy, D / 2f + sd / 2f), new Vector3(W + 2f * sd, sh, sd), k.Stripe, collider: false);
            k.Box(stripe, "Sur", new Vector3(0f, sy, -D / 2f - sd / 2f), new Vector3(W + 2f * sd, sh, sd), k.Stripe, collider: false);
            k.Box(stripe, "Oeste", new Vector3(-W / 2f - sd / 2f, sy, 0f), new Vector3(sd, sh, D), k.Stripe, collider: false);
            k.Box(stripe, "Este", new Vector3(W / 2f + sd / 2f, sy, 0f), new Vector3(sd, sh, D), k.Stripe, collider: false);
        }

        if (!roof) return;
        var cover = b.cover == RoofCover.Teja ? k.RoofTile : b.cover == RoofCover.Zinc ? k.Zinc : k.Roof;
        float o = d.roofOverhang;
        if (b.roof == RoofType.Losa)
        {
            k.Box(bld, "Losa de techo", new Vector3(0f, b.wallHeight, 0f), new Vector3(W + 2f * o, d.slabThickness, D + 2f * o), cover);
            return;
        }
        var mesh = BlockoutAssets.Roof("Techo_" + BlockoutAssets.Slug(b.name), b.roof, b.size, b.roofPitch, o, d.roofFascia);
        k.MeshPiece(bld, "Techo", new Vector3(0f, b.wallHeight, 0f), Quaternion.identity, mesh,
                    new[] { cover, k.Soffit, wallMat }, collider: false);
    }

    /// <summary>
    /// Centros de las ventanas de un muro, medidos como sus puertas (desde la esquina exterior oeste o
    /// sur). Se reparten centradas cada <c>windowSpacing</c> m y se saltan las que pisarían una puerta,
    /// el encuentro con un tabique o un tramo de muro pegado a otro edificio (darían a una pared).
    /// </summary>
    static IEnumerable<float> WindowCenters(BuildingSpec b, Side side, StationLayout L)
    {
        var d = L.buildingDesign;
        bool alongX = side == Side.Norte || side == Side.Sur;
        float length = b.WallLength(side), ww = d.windowWidth;
        float usable = length - 2f * d.windowCornerMargin;
        if (usable < ww) yield break;

        int count = Mathf.FloorToInt((usable - ww) / d.windowSpacing) + 1;
        float first = length / 2f - (count - 1) * d.windowSpacing / 2f;
        var print = b.Footprint;
        const float clearance = 0.4f;

        for (int i = 0; i < count; i++)
        {
            float c = first + i * d.windowSpacing;
            if (b.doors.Any(door => door.side == side && Mathf.Abs(c - door.offset) < (ww + door.width) / 2f + clearance))
                continue;
            if (b.partitions.Any(p => p.northSouth == alongX && Mathf.Abs(c - p.position) < (ww + d.interiorWallThickness) / 2f + clearance))
                continue;

            // Punto medio de la ventana, medio metro por fuera de la fachada.
            var local = side switch
            {
                Side.Norte => new Vector2(-b.size.x / 2f + c, b.size.y / 2f + 0.5f),
                Side.Sur   => new Vector2(-b.size.x / 2f + c, -b.size.y / 2f - 0.5f),
                Side.Este  => new Vector2(b.size.x / 2f + 0.5f, -b.size.y / 2f + c),
                _          => new Vector2(-b.size.x / 2f - 0.5f, -b.size.y / 2f + c),
            };
            var outside = print.ToWorld(local);
            if (L.buildings.Any(other => other != b && other.Footprint.Contains(outside)))
                continue;
            yield return c;
        }
    }

    static void BuildAntennas(Transform root, StationLayout L, Kit k)
    {
        var antennas = Group(root, "Antenas");
        foreach (var a in L.antennas)
        {
            // Ancla a ras de suelo en el eje del pedestal, que queda detrás del plato (ver AntennaGeometry).
            var anchor = Group(antennas, a.name, Flat(AntennaGeometry.PedestalPosition(a, L.antennaDesign)));
            if (a.HasPedestal)
                k.Box(anchor, "Pedestal", Vector3.zero,
                      new Vector3(a.pedestalSize.x, a.pedestalHeight, a.pedestalSize.y), k.Concrete);
            if (a.dishDiameter > 0f)
                BuildAntenna(anchor, a, L.antennaDesign, k);
        }
    }

    /// <summary>
    /// La antena tipo, parametrizada por su diámetro: montura azimut-elevación con soporte en Y,
    /// reflector paraboloide y subreflector en el foco (Cassegrain). Los dos pivotes son reales:
    /// girar "Montura (azimut)" en Y o "Elevación" en X apunta la antena como la montura de verdad.
    /// Las medidas salen de AntennaGeometry, la misma que usa la validación.
    /// </summary>
    static void BuildAntenna(Transform anchor, AntennaSpec a, AntennaDesign d, Kit k)
    {
        float D = a.dishDiameter;
        float armT = d.yokeThickness * D;
        float halfYoke = d.yokeWidth * D / 2f;
        float stemW = d.stemWidth * D;
        float turnH = d.turntableHeight * D;
        float turnD = a.HasPedestal ? Mathf.Min(d.yokeWidth * D, a.pedestalSize.x, a.pedestalSize.y) : d.yokeWidth * D;

        // Montura: gira en azimut sobre la cara superior del pedestal. +Z local = hacia donde apunta.
        var mount = Group(anchor, "Montura (azimut)", new Vector3(0f, a.PedestalTop, 0f));
        mount.localRotation = Quaternion.Euler(0f, a.azimuth, 0f);
        k.Piece(mount, "Plataforma", Vector3.zero, Quaternion.identity, new Vector3(turnD, turnH, turnD), k.Cylinder, k.Steel);

        // Soporte en Y: un tronco que se abre en dos brazos hasta los cojinetes del eje de elevación.
        float stemTop = turnH + (a.mountHeight - turnH) * d.stemFraction;
        var yoke = Group(mount, "Soporte en Y");
        k.Box(yoke, "Tronco", new Vector3(0f, turnH, 0f), new Vector3(stemW, stemTop - turnH, stemW), k.Steel);
        k.Strut(yoke, "Brazo izquierdo", new Vector3(-(stemW - armT) / 2f, stemTop, 0f),
                new Vector3(-halfYoke, a.mountHeight, 0f), armT, k.Block, k.Steel, collider: true);
        k.Strut(yoke, "Brazo derecho", new Vector3((stemW - armT) / 2f, stemTop, 0f),
                new Vector3(halfYoke, a.mountHeight, 0f), armT, k.Block, k.Steel, collider: true);

        // Elevación: gira sobre el eje X. Con −elevación, +Z local sube hacia el cielo.
        var el = Group(mount, "Elevación", new Vector3(0f, a.mountHeight, 0f));
        el.localRotation = Quaternion.Euler(-a.elevation, 0f, 0f);
        k.Strut(el, "Eje", new Vector3(-halfYoke - armT / 2f, 0f, 0f), new Vector3(halfYoke + armT / 2f, 0f, 0f),
                armT, k.Cylinder, k.Steel);

        float h = AntennaGeometry.VertexOffset(a, d);
        float f = AntennaGeometry.FocalLength(a, d);
        float hub = d.hubSize * D;
        k.Box(el, "Cubo", new Vector3(0f, -hub / 2f, h / 2f), new Vector3(hub, hub, h), k.Steel, collider: false);

        // Reflector: vértice delante del eje, abierto hacia +Z. Escala uniforme = mismo f/D.
        k.Piece(el, "Plato", new Vector3(0f, 0f, h), Quaternion.identity, Vector3.one * D,
                BlockoutAssets.Paraboloid(d.focalRatio), k.Antenna);

        // Estructura de respaldo: costillas del cubo a la trasera del plato. Como el paraboloide es
        // convexo y la costilla le llega por detrás con más pendiente que él, nunca lo atraviesa;
        // se quedan un grosor por detrás de la superficie para que la punta no asome por delante.
        var backup = Group(el, "Estructura de respaldo");
        float ribT = d.backupRibThickness * D, ribR = d.backupAttach * D / 2f;
        for (int i = 0; i < d.backupRibs; i++)
        {
            float ang = 360f / d.backupRibs * i * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            k.Strut(backup, $"Costilla {i + 1}", dir * (hub / 2f),
                    dir * ribR + Vector3.forward * (h + ribR * ribR / (4f * f) - ribT), ribT, k.Cylinder, k.Steel);
        }

        // Cassegrain: la bocina sale del vértice y el subreflector, convexo hacia el plato, va en el foco.
        // En acero y no en blanco: de frente, blanco sobre el plato blanco, el subreflector no se ve.
        k.Strut(el, "Bocina", new Vector3(0f, 0f, h), new Vector3(0f, 0f, h + d.feedLength * D),
                d.feedDiameter * D, k.Horn, k.Steel);
        float subD = d.subreflectorDiameter * D;
        k.Piece(el, "Subreflector", new Vector3(0f, 0f, h + f), Quaternion.identity, Vector3.one * subD,
                BlockoutAssets.Paraboloid(d.subreflectorFocalRatio), k.Steel);

        // Cuatro patas del plato al subreflector, a 45° de los ejes para no tapar la bocina.
        var legs = Group(el, "Patas del subreflector");
        float footR = d.strutAttach * D / 2f, headR = subD / 2f * d.strutAttach;
        for (int i = 0; i < 4; i++)
        {
            float ang = (45f + 90f * i) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            var foot = dir * footR + Vector3.forward * (h + footR * footR / (4f * f));
            var head = dir * headR + Vector3.forward * (h + f);
            k.Strut(legs, $"Pata {i + 1}", foot, head, d.strutThickness * D, k.Cylinder, k.Steel);
        }

        // La montura se mueve: fuera del batching estático, que la congelaría en Play.
        foreach (var t in mount.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
    }

    static void BuildSite(Transform root, StationLayout L, Kit k)
    {
        var s = L.site;
        var e = L.environment;
        var site = Group(root, "Vías, losas, tanques y árboles");

        // Todas las vías en una malla, sin collider: 5 cm de asfalto no frenan a nadie.
        k.MeshPiece(site, "Vías", Vector3.zero, Quaternion.identity,
                    BlockoutAssets.Roads("Vias", s.roads, s.pavingThickness), new[] { k.Asphalt }, collider: false);

        var pads = Group(site, "Losas y áreas");
        k.Tile(k.Grass, Vector2.one, e.grassTile, e.grassDetailTile);
        foreach (var p in s.pads)
        {
            if (p.kind == PadKind.Grama)
            {
                // Capa fina de grama con UV en metros, como el terreno.
                var mesh = BlockoutAssets.Extrusion("Grama_" + BlockoutAssets.Slug(p.name), p.Footprint.Corners(), p.thickness, 0f);
                k.MeshPiece(pads, p.name, Vector3.zero, Quaternion.identity, mesh, new[] { k.Grass }, collider: false);
            }
            else
            {
                var pad = k.Piece(pads, p.name, Flat(p.center), Quaternion.Euler(0f, p.rotation, 0f),
                                  Flat3(p.size, p.thickness), k.Block, p.kind == PadKind.Grava ? k.Gravel : k.Concrete, collider: true);
                if (p.items.x <= 0 || p.items.y <= 0) continue;

                // Vehículos: uno centrado en cada hueco de la rejilla, girados con la losa.
                var parked = Group(pads, p.name + " · vehículos", Flat(p.center));
                parked.localRotation = pad.transform.localRotation;
                Vector2 cell = new Vector2(p.size.x / p.items.x, p.size.y / p.items.y);
                for (int cx = 0; cx < p.items.x; cx++)
                for (int cz = 0; cz < p.items.y; cz++)
                    k.Box(parked, $"Vehículo {cx * p.items.y + cz + 1}",
                          new Vector3(-p.size.x / 2f + (cx + 0.5f) * cell.x, p.thickness, -p.size.y / 2f + (cz + 0.5f) * cell.y),
                          p.itemSize, k.Tank);
            }
        }

        BuildFacilities(site, L, k);
        BuildTrees(site, L, k);
    }

    static void BuildFacilities(Transform parent, StationLayout L, Kit k)
    {
        var group = Group(parent, "Tanques y equipos");
        foreach (var f in L.facilities)
        {
            // Ancla a ras de suelo en el centro de la huella; losa opcional y el cuerpo encima.
            var anchor = Group(group, f.name, Flat(f.position));
            if (f.padHeight > 0f)
                k.Box(anchor, "Losa", Vector3.zero, Flat3(f.size, f.padHeight), k.Concrete);

            var inner = f.size - Vector2.one * (2f * f.inset);
            var bodyBase = Vector3.up * f.padHeight;
            bool alongX = inner.x >= inner.y;
            var axis = alongX ? Vector3.right : Vector3.forward;
            switch (f.shape)
            {
                case FacilityShape.Equipo:
                    k.Box(anchor, "Equipo", bodyBase, Flat3(inner, f.height), k.Steel);
                    break;
                case FacilityShape.Cisterna:
                    // Abierta: cuatro muros de concreto y el agua dentro, un palmo por debajo del borde.
                    float wall = Mathf.Min(0.3f, Mathf.Min(inner.x, inner.y) / 4f);
                    var cistern = Group(anchor, "Cisterna", bodyBase);
                    k.Box(cistern, "Muro norte", new Vector3(0f, 0f, inner.y / 2f - wall / 2f), new Vector3(inner.x, f.height, wall), k.Concrete);
                    k.Box(cistern, "Muro sur", new Vector3(0f, 0f, -inner.y / 2f + wall / 2f), new Vector3(inner.x, f.height, wall), k.Concrete);
                    k.Box(cistern, "Muro oeste", new Vector3(-inner.x / 2f + wall / 2f, 0f, 0f), new Vector3(wall, f.height, inner.y - 2f * wall), k.Concrete);
                    k.Box(cistern, "Muro este", new Vector3(inner.x / 2f - wall / 2f, 0f, 0f), new Vector3(wall, f.height, inner.y - 2f * wall), k.Concrete);
                    k.Box(cistern, "Agua", Vector3.zero, new Vector3(inner.x - 2f * wall, f.height - 0.25f, inner.y - 2f * wall), k.Water, collider: false);
                    break;
                case FacilityShape.TanqueVertical:
                    // Repartidos a lo largo del lado mayor, cada uno en su hueco, con aire entre ellos.
                    int n = Mathf.Max(1, f.count);
                    float cell = (alongX ? inner.x : inner.y) / n;
                    float gap = n > 1 ? 0.5f : 0f;
                    float dia = Mathf.Min(cell - gap, alongX ? inner.y : inner.x);
                    for (int i = 0; i < n; i++)
                        k.Piece(anchor, n > 1 ? $"Tanque {i + 1}" : "Tanque",
                                bodyBase + axis * (-(n * cell) / 2f + (i + 0.5f) * cell), Quaternion.identity,
                                new Vector3(dia, f.height, dia), k.Cylinder, k.Tank, collider: true);
                    break;
                case FacilityShape.TanqueHorizontal:
                    // Tumbados a lo largo del lado mayor, uno al lado del otro, apoyados en la losa: el
                    // eje queda a medio diámetro.
                    int m = Mathf.Max(1, f.count);
                    float length = alongX ? inner.x : inner.y, across = alongX ? inner.y : inner.x;
                    var sideways = alongX ? Vector3.forward : Vector3.right;
                    for (int i = 0; i < m; i++)
                    {
                        var center = bodyBase + Vector3.up * (f.height / 2f) + sideways * (-across / 2f + (i + 0.5f) * across / m);
                        k.Strut(anchor, m > 1 ? $"Tanque {i + 1}" : "Tanque", center - axis * (length / 2f), center + axis * (length / 2f),
                                f.height, k.Cylinder, k.Tank, collider: true);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Cientos de árboles en tres mallas (troncos y dos tonos de copa) en vez de tres objetos por
    /// árbol. Los troncos llevan su malla como collider; las copas, ninguno.
    /// </summary>
    static void BuildTrees(Transform parent, StationLayout L, Kit k)
    {
        var td = L.treeDesign;
        var group = Group(parent, "Árboles");

        // Con modelos: cada árbol es el modelo escalado a su copa, con un collider en el tronco.
        var models = td.models.Where(m => m != null)
                              .Select(m => BlockoutAssets.TreeModel(m, Kit.BarkTint, Kit.LeafTint))
                              .Where(info => info != null).ToList();
        if (models.Count > 0)
        {
            for (int i = 0; i < L.trees.Count; i++)
            {
                var t = L.trees[i];
                var info = models[((t.model % models.Count) + models.Count) % models.Count];
                float scale = t.crownDiameter / info.crownDiameter;
                var tree = k.ModelPiece(group, $"Árbol {i + 1}", Flat(t.position), Quaternion.Euler(0f, t.yaw, 0f),
                                        scale, info.mesh, info.materials);
                var trunk = tree.AddComponent<CapsuleCollider>();
                trunk.radius = info.trunkRadius;
                trunk.height = 5f;
                trunk.center = new Vector3(0f, 2.5f, 0f);
            }
            return;
        }

        // Sin modelos: troncos y copas esféricas en tres mallas combinadas.
        var trunks = new List<Matrix4x4>();
        var crowns = new[] { new List<Matrix4x4>(), new List<Matrix4x4>() };
        for (int i = 0; i < L.trees.Count; i++)
        {
            var t = L.trees[i];
            var at = Flat(t.position);
            trunks.Add(Matrix4x4.TRS(at, Quaternion.identity, new Vector3(td.trunkDiameter, td.trunkHeight, td.trunkDiameter)));
            // Cada copa girada distinto para que la malla no se repita igual en todas.
            crowns[i % 2].Add(Matrix4x4.TRS(at + Vector3.up * (td.trunkHeight - td.crownOverlap), Quaternion.Euler(0f, i * 47f, 0f),
                                            new Vector3(t.crownDiameter, t.crownDiameter * td.crownHeightRatio, t.crownDiameter)));
        }
        if (L.trees.Count == 0) return;
        k.MeshPiece(group, "Troncos", Vector3.zero, Quaternion.identity,
                    BlockoutAssets.Combine("Arboles_Troncos", k.Cylinder, trunks), new[] { k.Trunk }, collider: true);
        k.MeshPiece(group, "Copas", Vector3.zero, Quaternion.identity,
                    BlockoutAssets.Combine("Arboles_Copas", k.Sphere, crowns[0]), new[] { k.Crown }, collider: false);
        if (crowns[1].Count > 0)
            k.MeshPiece(group, "Copas (otro tono)", Vector3.zero, Quaternion.identity,
                        BlockoutAssets.Combine("Arboles_Copas2", k.Sphere, crowns[1]), new[] { k.Crown2 }, collider: false);
    }

    /// <summary>
    /// Sabana alrededor de la parcela y anillo de cerros al fondo, centrados en la parcela. La sabana
    /// queda un poco por debajo de Y = 0 para no hacer z-fighting con el terreno de la parcela.
    /// </summary>
    static void BuildEnvironment(Transform root, StationLayout L, Kit k)
    {
        var e = L.environment;
        var env = Group(root, "Entorno");
        var center = Flat(L.Bounds.center);

        k.Tile(k.Savanna, Vector2.one * e.savannaSize, e.savannaTile, e.grassDetailTile);
        k.Box(env, "Sabana", center + Vector3.down * (e.savannaDrop + L.groundThickness),
              Flat3(Vector2.one * e.savannaSize, L.groundThickness), k.Savanna);
        k.Piece(env, "Cerros", center + Vector3.down * e.savannaDrop, Quaternion.identity, Vector3.one,
                BlockoutAssets.Hills(e), k.Hills);
    }

    /// <summary>Punto de la planta (X, Z) a ras de suelo.</summary>
    static Vector3 Flat(Vector2 p) => new Vector3(p.x, 0f, p.y);

    /// <summary>Tamaño de una pieza con huella X × Z y altura <paramref name="height"/>.</summary>
    static Vector3 Flat3(Vector2 footprint, float height) => new Vector3(footprint.x, height, footprint.y);

    /// <summary>Rumbo de una dirección en planta: "N", "NE", "E"… (O = oeste).</summary>
    static string Compass(Vector2 dir)
    {
        string[] names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
        float deg = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        return names[Mathf.RoundToInt(Mathf.Repeat(deg, 360f) / 45f) % 8];
    }

    // ------------------------------------------------------------------ muros

    /// <summary>Vano en un muro: puerta (antepecho 0) o ventana (con antepecho y vidrio).</summary>
    readonly struct Opening
    {
        public readonly float center, width, height, sill;
        public readonly bool glazed;

        public Opening(float center, float width, float height, float sill = 0f, bool glazed = false)
        {
            this.center = center; this.width = width; this.height = height; this.sill = sill; this.glazed = glazed;
        }
    }

    /// <summary>
    /// Muro recto de <paramref name="length"/> m a lo largo de X o de Z, desde <paramref name="start"/>
    /// (punto del eje del muro en su base). Cada vano parte el muro en tramos y deja un dintel encima;
    /// las ventanas llevan además antepecho debajo y un vidrio de <paramref name="glass"/> m en medio.
    /// </summary>
    static void Wall(Kit k, Transform parent, string name, bool alongX, Vector3 start,
                     float length, float thickness, float height, List<Opening> openings, Material mat,
                     float glass = 0f)
    {
        if (openings == null || openings.Count == 0)
        {
            k.Box(parent, name, start + Along(alongX, length / 2f), Oriented(alongX, length, height, thickness), mat);
            return;
        }

        var wall = Group(parent, name, start);
        float cursor = 0f;
        int piece = 1, n = 0;
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

            n++;
            var mid = Along(alongX, (a + b) / 2f);
            float head = o.sill + o.height;
            if (a > cursor)
                k.Box(wall, $"Tramo {piece++}", Along(alongX, (cursor + a) / 2f), Oriented(alongX, a - cursor, height, thickness), mat);
            if (o.sill > 0f)
                k.Box(wall, $"Antepecho {n}", mid, Oriented(alongX, b - a, o.sill, thickness), mat);
            if (height > head)
                k.Box(wall, $"Dintel {n}", mid + Vector3.up * head, Oriented(alongX, b - a, height - head, thickness), mat);
            if (o.glazed && glass > 0f)
                k.Box(wall, $"Vidrio {n}", mid + Vector3.up * o.sill, Oriented(alongX, b - a, o.height, glass), k.Glass);
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
        public readonly Material Ground, Savanna, Grass, Hills, ChainLink, Post, Gate, Floor, ExteriorWall,
                                 ServiceWall, Stripe, Glass, Partition, Roof, RoofTile, Zinc, Soffit, Concrete,
                                 Antenna, Steel, Asphalt, Gravel, Tank, Water, Trunk, Crown, Crown2;

        /// <summary>Tintes de los árboles con modelo: la corteza del atlas es muy colorida (eucalipto arcoíris).</summary>
        public static readonly Color BarkTint = new Color(0.80f, 0.74f, 0.66f);
        public static readonly Color LeafTint = new Color(0.82f, 0.88f, 0.74f);

        /// <summary>
        /// Paleta del acabado. Los materiales se crean con estos valores la primera vez; después solo
        /// se pisan con <paramref name="reapplyFinish"/>. La franja usa el azul de acento del
        /// aplicativo (--color-purple de Variables.uss, #1560D8); la teja, el rojo del plano.
        /// </summary>
        public Kit(bool reapplyFinish = false)
        {
            Material M(string name, Color c, float smoothness = 0.15f, float metallic = 0f, bool transparent = false) =>
                BlockoutAssets.Material(name, c, smoothness, metallic, transparent, reapplyFinish);

            Ground       = M("Blockout_Terreno",      Color.white, 0.05f);
            Savanna      = M("Blockout_Sabana",       Color.white, 0.05f);
            Grass        = M("Blockout_Grama",        Color.white, 0.05f);
            Hills        = M("Blockout_Cerros",       new Color(0.36f, 0.44f, 0.28f), 0.05f);
            ChainLink    = M("Blockout_Malla",        new Color(0.45f, 0.48f, 0.50f, 0.35f), 0.3f, 0.5f, transparent: true);
            Post         = M("Blockout_Poste",        new Color(0.50f, 0.52f, 0.55f), 0.4f, 0.6f);
            Gate         = M("Blockout_Porton",       new Color(0.20f, 0.24f, 0.30f), 0.35f, 0.5f);
            Floor        = M("Blockout_Piso",         new Color(0.74f, 0.74f, 0.72f), 0.45f);
            ExteriorWall = M("Blockout_MuroExterior", new Color(0.95f, 0.95f, 0.94f), 0.1f);
            ServiceWall  = M("Blockout_MuroServicio", new Color(0.80f, 0.79f, 0.76f), 0.1f);
            Stripe       = M("Blockout_Franja",       new Color(0.082f, 0.376f, 0.847f), 0.3f);
            Glass        = M("Blockout_Vidrio",       new Color(0.30f, 0.45f, 0.60f, 0.45f), 0.95f, transparent: true);
            Partition    = M("Blockout_Tabique",      new Color(0.90f, 0.91f, 0.92f), 0.1f);
            Roof         = M("Blockout_Techo",        new Color(0.80f, 0.80f, 0.78f), 0.1f);
            RoofTile     = M("Blockout_Teja",         new Color(0.66f, 0.29f, 0.17f), 0.2f);
            Zinc         = M("Blockout_Zinc",         new Color(0.64f, 0.66f, 0.68f), 0.45f, 0.5f);
            Soffit       = M("Blockout_Plafon",       new Color(0.93f, 0.92f, 0.90f), 0.1f);
            Concrete     = M("Blockout_Concreto",     new Color(0.68f, 0.66f, 0.62f), 0.1f);
            Antenna      = M("Blockout_Antena",       new Color(0.93f, 0.94f, 0.95f), 0.35f);
            Steel        = M("Blockout_Acero",        new Color(0.55f, 0.58f, 0.62f), 0.4f, 0.6f);
            Asphalt      = M("Blockout_Asfalto",      new Color(0.50f, 0.48f, 0.45f), 0.1f);
            Gravel       = M("Blockout_Grava",        new Color(0.60f, 0.54f, 0.45f), 0.05f);
            Tank         = M("Blockout_Tanque",       new Color(0.88f, 0.89f, 0.87f), 0.3f, 0.2f);
            Water        = M("Blockout_Agua",         new Color(0.07f, 0.12f, 0.11f), 0.9f);
            Trunk        = M("Blockout_Tronco",       new Color(0.36f, 0.27f, 0.19f), 0.1f);
            Crown        = M("Blockout_Copa",         new Color(0.20f, 0.33f, 0.13f), 0.1f);
            Crown2       = M("Blockout_Copa2",        new Color(0.27f, 0.38f, 0.16f), 0.1f);

            // Suelos: una textura grande por material (base) y el mismo grano fino (detail map de URP Lit).
            foreach (var (mat, texture) in new (Material, System.Func<Texture2D>)[]
                     {
                         (Ground, BlockoutAssets.SoilTexture), (Savanna, BlockoutAssets.SavannaTexture),
                         (Grass, BlockoutAssets.GrassTexture),
                     })
            {
                if (reapplyFinish || mat.GetTexture("_BaseMap") == null)
                    mat.SetTexture("_BaseMap", texture());
                if (reapplyFinish || mat.GetTexture("_DetailAlbedoMap") == null)
                {
                    mat.SetTexture("_DetailAlbedoMap", BlockoutAssets.GrassDetailTexture());
                    mat.SetFloat("_DetailAlbedoMapScale", 1f);
                    mat.EnableKeyword("_DETAIL_MULX2");
                }
                EditorUtility.SetDirty(mat);
            }
        }

        /// <summary>
        /// Pone las texturas reales del layout en los materiales que las tienen, con el shader
        /// "PVI/Superficie en metros" (UV en metros del mundo: no se estiran con la pieza). Las
        /// superficies sin textura se quedan como estaban: URP Lit de color liso. Va en cada
        /// generación porque sale de los datos, no de retoques a mano.
        /// </summary>
        public void ApplySurfaces(SurfacesSpec s)
        {
            Surface(Ground, s.parcel.main, s.parcel, topOnly: true);
            Surface(Grass, s.field.main, s.field, topOnly: true);
            Surface(Savanna, s.savanna.main, s.savanna, topOnly: true);
            Surface(Hills, s.hills.main, s.hills, topOnly: true);
            Surface(Asphalt, s.asphalt);
            Surface(Gravel, s.gravel);
            Surface(Concrete, s.concrete);
            Surface(Floor, s.floor);
            Surface(ExteriorWall, s.walls);
            Surface(Partition, s.walls);
            Surface(Soffit, s.walls);
            Surface(ServiceWall, s.serviceWalls);
            Surface(Stripe, s.stripe);
            Surface(RoofTile, s.roofTile);
            Surface(Zinc, s.zinc);
            Surface(Roof, s.flatRoof);
        }

        static void Surface(Material m, SurfaceLayer a, GroundSurface ground = null, bool topOnly = false)
        {
            if (a == null || a.color == null) return;
            var shader = Shader.Find(SurfaceShader);
            if (shader == null)
            {
                Debug.LogWarning($"{LogTag} No encuentro el shader '{SurfaceShader}' (Assets/Shaders): '{m.name}' se queda de color liso.");
                return;
            }
            if (m.shader != shader)
            {
                m.shader = shader;
                m.shaderKeywords = new string[0]; // las de URP Lit no significan nada aquí
            }
            m.SetTexture("_BaseMap", a.color);
            m.SetTexture("_BumpMap", a.normal);
            m.SetFloat("_Tile", a.tile);
            m.SetColor("_BaseColor", a.tint);
            m.SetFloat("_Smoothness", a.smoothness);
            m.SetFloat("_TopOnly", topOnly ? 1f : 0f);
            m.SetTexture("_PatchMap", BlockoutAssets.PatchNoise());
            m.SetFloat("_MacroScale", a.variationSize);
            m.SetFloat("_MacroStrength", a.variation);

            var p = ground?.patches;
            bool twoLayers = p != null && p.color != null && ground.cover > 0f;
            m.SetFloat("_Layer2On", twoLayers ? 1f : 0f);
            if (twoLayers)
            {
                m.SetTexture("_Layer2Map", p.color);
                m.SetTexture("_Layer2BumpMap", p.normal);
                m.SetFloat("_Layer2Tile", p.tile);
                m.SetColor("_Layer2Color", p.tint);
                m.SetFloat("_Layer2Smoothness", p.smoothness);
                m.SetFloat("_PatchScale", ground.patchSize);
                m.SetFloat("_PatchCover", ground.cover);
                m.SetFloat("_PatchSoftness", ground.softness);
            }
            EditorUtility.SetDirty(m);
        }

        const string SurfaceShader = "PVI/Superficie en metros";

        /// <summary>
        /// Repeticiones de un suelo sobre una pieza cuya UV va de 0 a <paramref name="size"/> (en
        /// metros, o 1 en las cajas), en sus dos escalas. Se deduce de las medidas en cada generación.
        /// Solo cuenta en los suelos sin textura real (los de "Superficie en metros" usan metros).
        /// </summary>
        public void Tile(Material mat, Vector2 size, float tile, float detailTile)
        {
            mat.SetTextureScale("_BaseMap", size / tile);
            mat.SetTextureScale("_DetailAlbedoMap", size / detailTile);
            EditorUtility.SetDirty(mat);
        }

        public readonly Mesh Block    = BlockoutAssets.UnitBlock();
        public readonly Mesh Cylinder = BlockoutAssets.Frustum("CilindroUnidad", 0.5f, 0.5f);
        public readonly Mesh Horn     = BlockoutAssets.Frustum("BocinaUnidad", 0.25f, 0.5f);
        public readonly Mesh Sphere   = BlockoutAssets.Sphere();
        public int Count;

        /// <summary>
        /// Caja apoyada en <paramref name="basePosition"/> (centro de su cara inferior) y con
        /// <paramref name="size"/> en metros. Las piezas nulas o negativas no se crean.
        /// </summary>
        public GameObject Box(Transform parent, string name, Vector3 basePosition, Vector3 size,
                              Material mat, bool collider = true) =>
            Piece(parent, name, basePosition, Quaternion.identity, size, Block, mat, collider);

        /// <summary>
        /// Barra de sección <paramref name="thickness"/> de <paramref name="from"/> a <paramref name="to"/>.
        /// Las mallas unidad crecen en +Y desde su base, así que basta con girar +Y hacia el destino.
        /// </summary>
        public GameObject Strut(Transform parent, string name, Vector3 from, Vector3 to, float thickness,
                                Mesh mesh, Material mat, bool collider = false)
        {
            var span = to - from;
            return Piece(parent, name, from, Quaternion.FromToRotation(Vector3.up, span),
                         new Vector3(thickness, span.magnitude, thickness), mesh, mat, collider);
        }

        /// <summary>
        /// Pieza genérica: una malla unidad con su base en <paramref name="basePosition"/>, girada y
        /// escalada a su medida real. El collider (opcional) es la caja que envuelve la malla unidad,
        /// salvo en los cilindros, que llevan el suyo (convexo) para no tener esquinas invisibles.
        /// </summary>
        public GameObject Piece(Transform parent, string name, Vector3 basePosition, Quaternion rotation,
                                Vector3 size, Mesh mesh, Material mat, bool collider = false)
        {
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f) return null;

            var go = NewPiece(parent, name, basePosition, rotation, mesh, new[] { mat });
            go.transform.localScale = size;
            if (collider && mesh == Cylinder)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                mc.convex = true;
            }
            else if (collider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.5f, 0f);
                box.size = Vector3.one;
            }
            return go;
        }

        /// <summary>
        /// Pieza con una malla ya a su medida (generada), sin escalar, con un material por submalla.
        /// Su collider, si lo lleva, es la propia malla.
        /// </summary>
        public GameObject MeshPiece(Transform parent, string name, Vector3 position, Quaternion rotation,
                                    Mesh mesh, Material[] mats, bool collider)
        {
            var go = NewPiece(parent, name, position, rotation, mesh, mats);
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>
        /// Modelo con su malla y materiales, a escala uniforme. Sin batching estático: los árboles son
        /// la misma malla muchas veces, y así se dibujan por instancias en vez de copiarse.
        /// </summary>
        public GameObject ModelPiece(Transform parent, string name, Vector3 position, Quaternion rotation,
                                     float scale, Mesh mesh, Material[] mats)
        {
            var go = NewPiece(parent, name, position, rotation, mesh, mats);
            go.transform.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            return go;
        }

        GameObject NewPiece(Transform parent, string name, Vector3 position, Quaternion rotation, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            Count++;
            return go;
        }
    }
}
