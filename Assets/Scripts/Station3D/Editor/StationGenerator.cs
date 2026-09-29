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
        public bool terrain, fence, building, roof, antennas, site, environment;

        const string Prefix = "PVI.Station3D.";

        public static Options Load() => new Options
        {
            terrain   = EditorPrefs.GetBool(Prefix + "terrain", true),
            fence     = EditorPrefs.GetBool(Prefix + "fence", true),
            building  = EditorPrefs.GetBool(Prefix + "building", true),
            roof      = EditorPrefs.GetBool(Prefix + "roof", true),
            antennas  = EditorPrefs.GetBool(Prefix + "antennas", true),
            site      = EditorPrefs.GetBool(Prefix + "site", true),
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
    /// Vuelve a poner en los materiales los colores de acabado del generador. Regenerar no los toca
    /// (para respetar retoques a mano); esto sí, y solo cuando se pide.
    /// </summary>
    [MenuItem("PVI/Estación 3D/Reaplicar colores del acabado")]
    static void ReapplyFinish()
    {
        new Kit(reapplyFinish: true);
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

        var kit = new Kit();
        var root = new GameObject(RootName);
        root.AddComponent<StationGeneratedRoot>().layout = layout;

        if (opts.terrain)   BuildTerrain(root.transform, layout, kit);
        if (opts.fence)     BuildFence(root.transform, layout, kit);
        if (opts.building)  BuildBuilding(root.transform, layout.building, kit, opts.roof);
        if (opts.antennas)  BuildAntennas(root.transform, layout, kit);
        if (opts.site)      { BuildSite(root.transform, layout, kit); BuildFacilities(root.transform, layout, kit); }
        if (opts.environment) BuildEnvironment(root.transform, layout, kit);

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
        // Grama a escala real, en sus dos escalas (ver EnvironmentSpec).
        k.Tile(k.Ground, L.plotSize, L.environment);
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

                foreach (var w in room.windows)
                {
                    var window = new Opening(x0 + w.offset + W / 2f, w.width, b.windowHeight, b.windowSill, glazed: true);
                    if (r == 0) northOpenings.Add(window);
                    else if (r == last) southOpenings.Add(window);
                }

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
        Wall(k, exterior, "Fachada norte", true, new Vector3(-W / 2f, floorTop, D / 2f - tE / 2f), W, tE, wallH, northOpenings, k.ExteriorWall, b.glassThickness);
        Wall(k, exterior, "Fachada sur", true, new Vector3(-W / 2f, floorTop, -D / 2f + tE / 2f), W, tE, wallH, southOpenings, k.ExteriorWall, b.glassThickness);

        // Franja azul: cuatro bandas pegadas por fuera de la huella. Las de norte y sur cubren las
        // esquinas; las de este y oeste van entre ellas, así ninguna cara se solapa con otra.
        float sd = b.stripeDepth;
        var stripe = Group(exterior, "Franja azul");
        k.Box(stripe, "Norte", new Vector3(0f, b.stripeBottom, D / 2f + sd / 2f), new Vector3(W + 2f * sd, b.stripeHeight, sd), k.Stripe, collider: false);
        k.Box(stripe, "Sur", new Vector3(0f, b.stripeBottom, -D / 2f - sd / 2f), new Vector3(W + 2f * sd, b.stripeHeight, sd), k.Stripe, collider: false);
        k.Box(stripe, "Oeste", new Vector3(-W / 2f - sd / 2f, b.stripeBottom, 0f), new Vector3(sd, b.stripeHeight, D), k.Stripe, collider: false);
        k.Box(stripe, "Este", new Vector3(W / 2f + sd / 2f, b.stripeBottom, 0f), new Vector3(sd, b.stripeHeight, D), k.Stripe, collider: false);
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

        if (!roof) return;
        k.Box(bld, "Losa de techo", new Vector3(0f, b.height - b.roofThickness, 0f),
              new Vector3(W, b.roofThickness, D), k.Roof);

        // Equipos de A/A repartidos en fila a lo largo del eje de la losa.
        var units = Group(bld, "Equipos de A/A");
        for (int i = 0; i < b.roofUnits; i++)
            k.Box(units, $"Equipo A/A {i + 1}", new Vector3(-W / 2f + (i + 0.5f) * W / b.roofUnits, b.height, 0f),
                  b.roofUnitSize, k.Steel);
    }

    static void BuildAntennas(Transform root, StationLayout L, Kit k)
    {
        var antennas = Group(root, "Antenas");
        foreach (var a in L.antennas)
        {
            // Ancla a ras de suelo en el eje de la antena.
            var anchor = Group(antennas, a.name, new Vector3(a.position.x, 0f, a.position.y));
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
        var mount = Group(anchor, "Montura (azimut)", new Vector3(0f, a.HasPedestal ? a.pedestalHeight : 0f, 0f));
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
        var site = Group(root, "Vías, estacionamiento y ductos");

        foreach (var p in s.paving)
            k.Box(site, p.name, Flat((p.min + p.max) / 2f), Flat3(p.max - p.min, s.pavingThickness), k.Asphalt);

        // Estacionamiento: losa de asfalto y, encima, las líneas que separan los puestos.
        var pk = s.parking;
        var parking = Group(site, "Estacionamiento");
        k.Box(parking, "Losa", Flat((pk.min + pk.max) / 2f), Flat3(pk.max - pk.min, s.pavingThickness), k.Asphalt);
        float rowStart = (pk.min.x + pk.max.x) / 2f - pk.stalls * pk.stallWidth / 2f;
        float stallNorth = pk.max.y - pk.stallSetback;
        for (int i = 0; i <= pk.stalls; i++)
        {
            var line = new Vector2(rowStart + i * pk.stallWidth, stallNorth - pk.stallDepth / 2f);
            k.Box(parking, $"Línea {i + 1}", Flat(line) + Vector3.up * s.pavingThickness,
                  new Vector3(pk.lineWidth, s.pavingThickness / 5f, pk.stallDepth), k.Paint, collider: false);
        }

        // Ductos a ras de suelo, un tramo recto por segmento de la polilínea.
        var ducts = Group(site, "Guías de onda y ductos");
        foreach (var d in s.ducts)
        {
            var duct = Group(ducts, d.name);
            for (int i = 1; i < d.path.Count; i++)
            {
                Vector3 a = Flat(d.path[i - 1]), b = Flat(d.path[i]);
                k.Piece(duct, $"Tramo {i}", (a + b) / 2f, Quaternion.LookRotation(b - a),
                        new Vector3(s.ductWidth, s.ductHeight, Vector3.Distance(a, b)), k.Block, k.Concrete, collider: true);
            }
        }
    }

    static void BuildFacilities(Transform root, StationLayout L, Kit k)
    {
        var group = Group(root, "Servicios");
        foreach (var f in L.facilities)
        {
            // Ancla a ras de suelo en el centro de la huella; losa opcional y el cuerpo encima.
            var anchor = Group(group, f.name, Flat(f.position));
            if (f.padHeight > 0f)
                k.Box(anchor, "Losa", Vector3.zero, Flat3(f.size, f.padHeight), k.Concrete);

            var inner = f.size - Vector2.one * (2f * f.inset);
            var bodyBase = Vector3.up * f.padHeight;
            switch (f.shape)
            {
                case FacilityShape.Edificio:
                    k.Box(anchor, "Cuerpo", bodyBase, Flat3(inner, f.height), k.ExteriorWall);
                    break;
                case FacilityShape.Equipo:
                    k.Box(anchor, "Equipo", bodyBase, Flat3(inner, f.height), k.Steel);
                    break;
                case FacilityShape.TanqueVertical:
                    float dia = Mathf.Min(inner.x, inner.y);
                    k.Piece(anchor, "Tanque", bodyBase, Quaternion.identity, new Vector3(dia, f.height, dia),
                            k.Cylinder, k.Tank, collider: true);
                    break;
                case FacilityShape.TanqueHorizontal:
                    // Tumbado a lo largo del lado mayor, apoyado en la losa: el eje queda a medio diámetro.
                    bool alongX = inner.x >= inner.y;
                    float length = alongX ? inner.x : inner.y;
                    var axis = alongX ? Vector3.right : Vector3.forward;
                    var center = bodyBase + Vector3.up * (f.height / 2f);
                    k.Strut(anchor, "Tanque", center - axis * (length / 2f), center + axis * (length / 2f),
                            f.height, k.Cylinder, k.Tank, collider: true);
                    break;
            }
        }
    }

    /// <summary>
    /// Sabana alrededor de la parcela y anillo de cerros al fondo, centrados en la parcela. La sabana
    /// queda un poco por debajo de Y = 0 para no hacer z-fighting con el terreno de la parcela.
    /// </summary>
    static void BuildEnvironment(Transform root, StationLayout L, Kit k)
    {
        var e = L.environment;
        var env = Group(root, "Entorno");
        var center = Flat(L.plotSize / 2f);

        k.Tile(k.Savanna, Vector2.one * e.savannaSize, e);
        k.Box(env, "Sabana", center + Vector3.down * (e.savannaDrop + L.groundThickness),
              Flat3(Vector2.one * e.savannaSize, L.groundThickness), k.Savanna);
        k.Piece(env, "Cerros", center + Vector3.down * e.savannaDrop, Quaternion.identity, Vector3.one,
                BlockoutAssets.Hills(e), k.Hills);
    }

    /// <summary>Punto de la planta (X, Z) a ras de suelo.</summary>
    static Vector3 Flat(Vector2 p) => new Vector3(p.x, 0f, p.y);

    /// <summary>Tamaño de una pieza con huella X × Z y altura <paramref name="height"/>.</summary>
    static Vector3 Flat3(Vector2 footprint, float height) => new Vector3(footprint.x, height, footprint.y);

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
        public readonly Material Ground, Savanna, Hills, ChainLink, Post, Gate, Floor, ExteriorWall, Stripe,
                                 Glass, Partition, Roof, Concrete, Antenna, Steel, Asphalt, Paint, Tank;

        /// <summary>
        /// Paleta del acabado (Sesión D). Los materiales se crean con estos valores la primera vez;
        /// después solo se pisan con <paramref name="reapplyFinish"/>. La franja usa el azul de
        /// acento del aplicativo (--color-purple de Variables.uss, #1560D8).
        /// </summary>
        public Kit(bool reapplyFinish = false)
        {
            Material M(string name, Color c, float smoothness = 0.15f, float metallic = 0f, bool transparent = false) =>
                BlockoutAssets.Material(name, c, smoothness, metallic, transparent, reapplyFinish);

            Ground       = M("Blockout_Terreno",      Color.white, 0.05f);
            Savanna      = M("Blockout_Sabana",       Color.white, 0.05f);
            Hills        = M("Blockout_Cerros",       new Color(0.36f, 0.44f, 0.28f), 0.05f);
            ChainLink    = M("Blockout_Malla",        new Color(0.45f, 0.48f, 0.50f, 0.35f), 0.3f, 0.5f, transparent: true);
            Post         = M("Blockout_Poste",        new Color(0.50f, 0.52f, 0.55f), 0.4f, 0.6f);
            Gate         = M("Blockout_Porton",       new Color(0.20f, 0.24f, 0.30f), 0.35f, 0.5f);
            Floor        = M("Blockout_Piso",         new Color(0.74f, 0.74f, 0.72f), 0.45f);
            ExteriorWall = M("Blockout_MuroExterior", new Color(0.95f, 0.95f, 0.94f), 0.1f);
            Stripe       = M("Blockout_Franja",       new Color(0.082f, 0.376f, 0.847f), 0.3f);
            Glass        = M("Blockout_Vidrio",       new Color(0.30f, 0.45f, 0.60f, 0.45f), 0.95f, transparent: true);
            Partition    = M("Blockout_Tabique",      new Color(0.90f, 0.91f, 0.92f), 0.1f);
            Roof         = M("Blockout_Techo",        new Color(0.62f, 0.62f, 0.60f), 0.1f);
            Concrete     = M("Blockout_Concreto",     new Color(0.68f, 0.66f, 0.62f), 0.1f);
            Antenna      = M("Blockout_Antena",       new Color(0.93f, 0.94f, 0.95f), 0.35f);
            Steel        = M("Blockout_Acero",        new Color(0.55f, 0.58f, 0.62f), 0.4f, 0.6f);
            Asphalt      = M("Blockout_Asfalto",      new Color(0.29f, 0.29f, 0.30f), 0.1f);
            Paint        = M("Blockout_Pintura",      new Color(0.95f, 0.95f, 0.92f), 0.2f);
            Tank         = M("Blockout_Tanque",       new Color(0.88f, 0.89f, 0.87f), 0.3f, 0.2f);

            // Grama en dos escalas: manchas grandes (base) y grano fino (detail map de URP Lit).
            foreach (var grass in new[] { Ground, Savanna })
            {
                if (reapplyFinish || grass.GetTexture("_BaseMap") == null)
                    grass.SetTexture("_BaseMap", BlockoutAssets.GrassTexture());
                if (reapplyFinish || grass.GetTexture("_DetailAlbedoMap") == null)
                {
                    grass.SetTexture("_DetailAlbedoMap", BlockoutAssets.GrassDetailTexture());
                    grass.SetFloat("_DetailAlbedoMapScale", 1f);
                    grass.EnableKeyword("_DETAIL_MULX2");
                }
                EditorUtility.SetDirty(grass);
            }
        }

        /// <summary>
        /// Repeticiones de la grama sobre una pieza de <paramref name="size"/> metros, en sus dos
        /// escalas. Se deduce de las medidas en cada generación.
        /// </summary>
        public void Tile(Material mat, Vector2 size, EnvironmentSpec e)
        {
            mat.SetTextureScale("_BaseMap", size / e.grassTile);
            mat.SetTextureScale("_DetailAlbedoMap", size / e.grassDetailTile);
            EditorUtility.SetDirty(mat);
        }

        public readonly Mesh Block    = BlockoutAssets.UnitBlock();
        public readonly Mesh Cylinder = BlockoutAssets.Frustum("CilindroUnidad", 0.5f, 0.5f);
        public readonly Mesh Horn     = BlockoutAssets.Frustum("BocinaUnidad", 0.25f, 0.5f);
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
        /// escalada a su medida real. El collider (opcional) es la caja que envuelve la malla unidad.
        /// </summary>
        public GameObject Piece(Transform parent, string name, Vector3 basePosition, Quaternion rotation,
                                Vector3 size, Mesh mesh, Material mat, bool collider = false)
        {
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePosition;
            go.transform.localRotation = rotation;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
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
