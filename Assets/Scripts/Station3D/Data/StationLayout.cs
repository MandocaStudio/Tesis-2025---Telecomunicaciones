using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Medidas de la Estación Terrena "Andrés Bello" sacadas del plano de conjunto
/// (Planos/plano_andres_bello_v2.svg, trazado sobre imagen satelital). Es la ÚNICA fuente de verdad
/// del modelo 3D: StationGenerator no lleva ninguna medida propia, todo lo lee de aquí.
///
/// Coordenadas: origen en la esquina SO de la caja que envuelve la cerca, X hacia el este, Z hacia
/// el norte, Y arriba. 1 unidad = 1 metro. Del SVG: 4 px = 1 m, X = (px − 110) / 4 y
/// Z = (1096,25 − py) / 4. Los giros van en grados en sentido horario visto desde arriba, que es el
/// mismo sentido del rotate() del SVG: el número del plano se copia tal cual.
///
/// Los valores iniciales de este script son los del plano y solo sirven para crear el asset
/// (Assets/Data/AndresBelloLayout.asset); a partir de ahí manda el asset. "Reset" en el Inspector
/// equivale a volver al plano.
///
/// Cada Tooltip dice de dónde sale el número: "Plano" = rotulado, "Medido" = medido sobre el dibujo
/// (el plano es una estimación: ±1 m), "Foto" = medido sobre la foto satelital de la que sale el
/// plano (MODULO-3D.md §2.8), "Supuesto" = ninguno de los dos lo da y es una decisión de construcción.
/// </summary>
[CreateAssetMenu(fileName = "StationLayout", menuName = "PVI/Station Layout")]
public class StationLayout : ScriptableObject
{
    [Header("Parcela")]
    [Tooltip("Supuesto. Espesor de la losa de terreno; su cara superior es Y = 0. La huella es la de la cerca.")]
    public float groundThickness = 0.5f;

    public FenceSpec fence = new FenceSpec();
    public SiteSpec site = new SiteSpec();

    [Tooltip("Construcción común a todos los edificios (supuestos).")]
    public BuildingDesign buildingDesign = new BuildingDesign();

    [Tooltip("Edificios del plano. Huellas y giros medidos; alturas, techos y puertas supuestos.")]
    public List<BuildingSpec> buildings = new List<BuildingSpec>
    {
        // El conjunto de techo de teja: tres cuerpos que se tocan, con pasos entre ellos.
        new BuildingSpec("Edificio principal", new Vector2(90.625f, 125f), new Vector2(18.75f, 46.875f), 0f, 4f,
                         RoofType.CuatroAguas, RoofCover.Teja, WallFinish.Blanco, true, true, true,
                         "Plano: sala de control y equipos RF (≈ 19 × 47 m; la franja sur de 15,6 m va dibujada aparte).")
            .WithDoor(Side.Oeste, 31.25f, 2f, 2.4f)
            .WithDoor(Side.Sur, 9.375f, 2f, 2.4f)
            .WithDoor(Side.Norte, 14.0625f, 2f, 2.4f)
            .WithPartition("Sala de control y equipos RF | Hall", false, 15.625f, 9.375f),
        new BuildingSpec("Ala norte (oficinas)", new Vector2(114.0625f, 155.46875f), new Vector2(46.875f, 14.0625f), 0f, 4f,
                         RoofType.CuatroAguas, RoofCover.Teja, WallFinish.Blanco, true, true, true, "Plano: ≈ 47 × 14 m.")
            .WithDoor(Side.Sur, 4.6875f, 2f, 2.4f)
            .WithDoor(Side.Sur, 37.5f, 2f, 2.4f)
            .WithDoor(Side.Este, 7.03125f, 2f, 2.4f),
        new BuildingSpec("Ala este", new Vector2(150f, 157.03125f), new Vector2(25f, 17.1875f), 0f, 4f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Blanco, true, true, true,
                         "Plano: ≈ 25 × 17 m, teja. Foto: techo plano claro (solo una esquina roja), así que va con losa.")
            .WithDoor(Side.Oeste, 7.03125f, 2f, 2.4f)
            .WithDoor(Side.Sur, 12.5f, 2f, 2.4f),
        new BuildingSpec("Oficinas / Administración", new Vector2(42.1875f, 158.4375f), new Vector2(34.375f, 16.25f), -8f, 4f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Blanco, true, true, true,
                         "Plano: ≈ 35 × 16 m, techo de teja. Foto: techo plano claro, así que va con losa.")
            .WithDoor(Side.Sur, 17.1875f, 2f, 2.4f)
            .WithDoor(Side.Norte, 25.9f, 2f, 2.4f),

        // Techo gris (el plano dice "zinc o losa"): la foto enseña losa clara en todos menos el galpón.
        new BuildingSpec("Galpón", new Vector2(159.375f, 203.4375f), new Vector2(18.75f, 22.5f), 10f, 6.5f,
                         RoofType.DosAguas, RoofCover.Zinc, WallFinish.Gris, false, false, false, "Plano: ≈ 19 × 22 m.")
            .WithDoor(Side.Oeste, 11.25f, 5f, 4.5f),
        new BuildingSpec("Sala de equipos", new Vector2(122.1875f, 193.75f), new Vector2(13.125f, 8.125f), 0f, 3.5f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Gris, false, true, false, "")
            .WithDoor(Side.Sur, 6.25f, 1.2f, 2.2f)
            .WithDoor(Side.Norte, 4.8f, 1.2f, 2.2f),
        new BuildingSpec("Planta eléctrica", new Vector2(28.125f, 84.375f), new Vector2(18.75f, 18.75f), 0f, 5f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Gris, false, false, false, "Foto: techo plano claro.")
            .WithDoor(Side.Este, 9.375f, 3f, 3f),
        new BuildingSpec("Depósito / taller", new Vector2(39.84375f, 63.6f), new Vector2(23.4375f, 18.75f), 8f, 5f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Gris, false, true, false,
                         "Medido en (39,8, 67,2); bajado 3,6 m al sur porque en el plano se mete 3,1 m en la planta eléctrica. Foto: techo plano claro.")
            .WithDoor(Side.Este, 9.375f, 4f, 4f),
        new BuildingSpec("Caseta", new Vector2(75.625f, 210.9375f), new Vector2(8.75f, 6.25f), 0f, 3f,
                         RoofType.Losa, RoofCover.Concreto, WallFinish.Gris, false, true, false, "")
            .WithDoor(Side.Sur, 4.375f, 1f, 2.1f),
    };

    [Tooltip("Tanques y equipos sueltos. Posiciones y huellas medidas; alturas supuestas.")]
    public List<FacilitySpec> facilities = new List<FacilitySpec>
    {
        // Foto: los "Tanques" son dos cilindros tumbados de este a oeste, y el tanque de agua, una
        // cisterna abierta (agua oscura dentro de un borde claro). La cisterna la movió el usuario
        // 6,9 m al este (2026-10-09; en la foto estaba en X = 170,75).
        //               nombre            forma                           centro (X, Z)                  huella (X × Z)          alto  losa  margen  cuántos
        new FacilitySpec("Tanques",        FacilityShape.TanqueHorizontal, new Vector2(27.5f, 194.4f),    new Vector2(12f, 9f),   3.2f, 0.3f, 0.6f,   2),
        new FacilitySpec("Tanque de agua", FacilityShape.Cisterna,         new Vector2(177.65f, 140.15f), new Vector2(9f, 13.5f), 1.2f, 0f,   0f,     1),
    };

    [Tooltip("Proporciones comunes a todas las antenas, como fracción del diámetro: una sola antena " +
             "parametrizada por su diámetro, no un modelo por antena.")]
    public AntennaDesign antennaDesign = new AntennaDesign();

    [Tooltip("Antenas en el orden del plano.")]
    public List<AntennaSpec> antennas = BuildAntennaList();

    public TreeDesign treeDesign = new TreeDesign();

    [Tooltip("Foto: 67 árboles grandes colocados sobre las manchas de follaje de la imagen satelital (el " +
             "plano solo dibuja 6). Pueden quedar fuera de la cerca: el bosque del este y del norte. Ver PhotoTrees.")]
    public List<TreeSpec> trees = PhotoTrees.Create();

    public EnvironmentSpec environment = new EnvironmentSpec();

    [Tooltip("Texturas reales de cada superficie (Poly Haven, CC0: Assets/PolyHaven). Sin textura, la superficie " +
             "queda del color liso del acabado.")]
    public SurfacesSpec surfaces = new SurfacesSpec();

    [Tooltip("Arbustos, grama alta y piedras repartidos por el terreno (dibujados por instancias). Los modelos " +
             "son los arbustos Yughues y los de Poly Haven (Assets/PolyHaven).")]
    public ScatterSpec scatter = new ScatterSpec();

    static List<AntennaSpec> BuildAntennaList()
    {
        // Posición = centro del plato visto desde arriba (plano o foto). Ø y altura, de la foto: Ø por
        // el ancho del plato, altura del centro del plato por lo que se aleja su sombra (sol a ≈ 45°,
        // así que el alejamiento ES la altura). El pedestal sale de ahí: eje = centro − lo que el plato
        // sube sobre el eje, y el pedestal es el eje menos la montura (0,2·D). Ver MODULO-3D.md §2.8.
        // No hay "Antena 2": en el plano era el plato de la Antena 1, y su "base circular", la sombra.
        var list = new List<AntennaSpec>
        {
            //              nombre                             posición (X, Z)                   Ø      alto  pedestal                   h ped.  montura
            new AntennaSpec("Antena 1 · Camatagua 1 (1970)",   new Vector2(100.625f, 171.875f), 28f,    0f,  new Vector2(8f, 8f),     8.48f,  5.6f),
            new AntennaSpec("Antena 3 · Camatagua 2 (1980)",   new Vector2(112.5f, 114.0625f),  30f,    28f, new Vector2(8.5f, 8.5f), 8.36f,  6f),
            new AntennaSpec("Antena 4",                        new Vector2(121.875f, 231.25f),  12f,    0f,  new Vector2(3.4f, 3.4f), 3.68f,  2.4f),
            // "Platos Ø 11–14 m sobre pedestales": la foto enseña cinco, no ocho (varios círculos del
            // plano caían en sombras). Posiciones, Ø y alturas, de la foto; el 2 no deja ver su sombra.
            new AntennaSpec("Plato 1",                         new Vector2(125.93f, 73.53f),    12f,    0f,  new Vector2(3.2f, 3.2f), 2.38f,  2.4f),
            new AntennaSpec("Plato 2",                         new Vector2(129.95f, 65.81f),    6f,     0f,  new Vector2(1.6f, 1.6f), 1f,     1.2f),
            new AntennaSpec("Plato 3",                         new Vector2(120.81f, 56.03f),    10f,    0f,  new Vector2(2.7f, 2.7f), 1.87f,  2f),
            new AntennaSpec("Plato 4",                         new Vector2(98.53f, 41.44f),     8.5f,   0f,  new Vector2(2.3f, 2.3f), 2.43f,  1.7f),
            new AntennaSpec("Plato 5",                         new Vector2(137.42f, 17.3f),     16.5f,  0f,  new Vector2(4.5f, 4.5f), 6.68f,  3.3f),
        };

        // "Losa de concreto · 12 platos Ø 7 m (2 × 6)". En la foto son dos columnas de cinco,
        // inclinadas unos 20° y más al este que en el plano, y dos platos pequeños (Ø ≈ 3,6 m) al
        // final de la columna este. El pedestal nace en el terreno y atraviesa la capa de grava.
        Vector2[] columns =
        {
            new Vector2(147.54f, 76.77f), new Vector2(150.21f, 68.25f), new Vector2(153.31f, 59.52f),
            new Vector2(157.8f, 50.27f),  new Vector2(160.94f, 41.76f),
            new Vector2(160.26f, 75.41f), new Vector2(162.29f, 66.23f), new Vector2(166.54f, 58.13f),
            new Vector2(170.84f, 49.2f),  new Vector2(173.41f, 40.54f),
        };
        int n = 1;
        foreach (var p in columns)
            list.Add(new AntennaSpec($"Losa · plato {n++}", p, 7f, 0f, new Vector2(1.75f, 1.75f), 1.2f, 1.4f));
        foreach (var p in new[] { new Vector2(176.14f, 34.55f), new Vector2(177.82f, 31.14f) })
            list.Add(new AntennaSpec($"Losa · plato {n++}", p, 3.6f, 0f, new Vector2(1.2f, 1.2f), 0.5f, 1.5f));
        return list;
    }

    /// <summary>Margen admitido entre la altura calculada de una antena y la "≈" del plano.</summary>
    public const float HeightTolerance = 0.5f;

    /// <summary>Caja que envuelve la cerca: la parcela.</summary>
    public Rect Bounds => PlanGeometry.Bounds(fence.outline);

    /// <summary>
    /// Comprueba que el layout es coherente consigo mismo y con el plano: que todo quede dentro de la
    /// cerca, que nada se pise, que puertas, ventanas y tabiques quepan en su muro. Devuelve un
    /// mensaje por problema; lista vacía = todo cuadra.
    /// </summary>
    public List<string> Validate()
    {
        const float tol = 0.01f;
        var issues = new List<string>();
        var outline = fence.outline;
        var d = buildingDesign;

        if (outline.Count < 3)
        {
            issues.Add("La cerca necesita al menos tres vértices.");
            return issues;
        }

        // Portón: sobre un lado de la cerca y sin llegar a sus esquinas.
        int gateEdge = PlanGeometry.NearestEdge(outline, fence.gatePosition);
        Vector2 ga = outline[gateEdge], gb = outline[(gateEdge + 1) % outline.Count];
        if (PlanGeometry.DistanceToSegment(fence.gatePosition, ga, gb) > 0.5f)
            issues.Add($"El portón ({fence.gatePosition.x:0.#}, {fence.gatePosition.y:0.#}) no está sobre la cerca.");
        else if (Vector2.Distance(fence.gatePosition, ga) < fence.gateWidth / 2f || Vector2.Distance(fence.gatePosition, gb) < fence.gateWidth / 2f)
            issues.Add("El portón no cabe en su lado de la cerca: se come una esquina.");

        bool Inside(Vector2 p) => PlanGeometry.Contains(outline, p);
        bool InsideAll(IEnumerable<Vector2> ps) => ps.All(Inside);

        // Edificios.
        var prints = buildings.Select(b => b.Footprint).ToList();
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (!InsideAll(prints[i].Corners()))
                issues.Add($"'{b.name}' se sale de la cerca.");
            for (int j = 0; j < i; j++)
                if (prints[i].Overlaps(prints[j]))
                    issues.Add($"'{b.name}' se pisa con '{buildings[j].name}'.");

            float wallClear = b.wallHeight - d.floorThickness;
            if (wallClear <= 0f)
                issues.Add($"'{b.name}': con {b.wallHeight:0.##} m de muro no queda altura sobre la losa de piso.");

            float openingTop = d.floorThickness + (b.windows ? d.windowSill + d.windowHeight : 0f);
            foreach (var door in b.doors)
            {
                float wall = b.WallLength(door.side);
                if (door.offset - door.width / 2f < 0f || door.offset + door.width / 2f > wall)
                    issues.Add($"'{b.name}': la puerta {door.side} (a {door.offset:0.##} m, {door.width:0.##} de ancho) se sale de su muro de {wall:0.##} m.");
                if (door.height >= wallClear)
                    issues.Add($"'{b.name}': la puerta {door.side} mide {door.height:0.##} m y el muro solo {wallClear:0.##} m.");
                openingTop = Mathf.Max(openingTop, d.floorThickness + door.height);
            }
            if (b.windows && d.windowSill + d.windowHeight >= wallClear)
                issues.Add($"'{b.name}': las ventanas no caben en el muro de {wallClear:0.##} m.");

            foreach (var p in b.partitions)
            {
                float across = p.northSouth ? b.size.x : b.size.y;
                if (p.position <= d.exteriorWallThickness || p.position >= across - d.exteriorWallThickness)
                    issues.Add($"'{b.name}': el tabique '{p.name}' cae fuera del edificio.");
                float along = p.northSouth ? b.size.y : b.size.x, t = d.exteriorWallThickness;
                if (p.doorOffset >= 0f && (p.doorOffset - d.interiorDoorWidth / 2f < t || p.doorOffset + d.interiorDoorWidth / 2f > along - t))
                    issues.Add($"'{b.name}': la puerta del tabique '{p.name}' se sale de él.");
            }

            if (b.stripe)
            {
                float stripeBottom = b.wallHeight - d.stripeBelowEaves - d.stripeHeight;
                if (stripeBottom < openingTop)
                    issues.Add($"'{b.name}': la franja azul empieza a {stripeBottom:0.##} m y hay vanos hasta {openingTop:0.##} m: los taparía.");
            }
            if (b.roof != RoofType.Losa && (b.roofPitch <= 0f || b.roofPitch >= 60f))
                issues.Add($"'{b.name}': la pendiente del techo ({b.roofPitch:0.#}°) tiene que estar entre 0 y 60°.");
        }

        foreach (var p in site.pads)
        {
            if (!InsideAll(p.Footprint.Corners()))
                issues.Add($"La losa '{p.name}' se sale de la cerca.");
            if (p.kind != PadKind.Grama)
                foreach (var b in buildings)
                    if (p.Footprint.Overlaps(b.Footprint))
                        issues.Add($"La losa '{p.name}' se mete en '{b.name}'.");
        }

        foreach (var r in site.roads)
            if (r.path.Count < 2)
                issues.Add($"La vía '{r.name}' necesita al menos dos puntos.");

        for (int i = 0; i < facilities.Count; i++)
        {
            var f = facilities[i];
            if (!InsideAll(f.Footprint.Corners()))
                issues.Add($"'{f.name}' se sale de la cerca.");
            foreach (var b in buildings)
                if (f.Footprint.Overlaps(b.Footprint))
                    issues.Add($"'{f.name}' se mete en '{b.name}'.");
            for (int j = 0; j < i; j++)
                if (f.Footprint.Overlaps(facilities[j].Footprint))
                    issues.Add($"'{f.name}' se pisa con '{facilities[j].name}'.");

            var inner = f.size - Vector2.one * (2f * f.inset);
            if (inner.x <= 0f || inner.y <= 0f)
                issues.Add($"El margen de '{f.name}' ({f.inset:0.##} m) se come toda su huella.");
            else if (f.shape == FacilityShape.TanqueHorizontal && f.height * Mathf.Max(1, f.count) > Mathf.Min(inner.x, inner.y) + tol)
                issues.Add($"Los {Mathf.Max(1, f.count)} tanques de '{f.name}' (Ø {f.height:0.##} m) no caben uno al lado del otro en su losa.");
            if (f.count < 1)
                issues.Add($"'{f.name}' tiene que tener al menos una unidad.");
        }

        for (int i = 0; i < antennas.Count; i++)
        {
            var a = antennas[i];
            float reach = a.PedestalReach;
            var pedestal = AntennaGeometry.PedestalPosition(a, antennaDesign);
            if (!Inside(pedestal) || PlanGeometry.DistanceToOutline(outline, pedestal) < reach)
                issues.Add($"La antena '{a.name}' se sale de la cerca.");
            if (a.HasPedestal)
            {
                foreach (var b in buildings)
                    if (b.Footprint.Distance(pedestal) < reach)
                        issues.Add($"El pedestal de '{a.name}' se mete en '{b.name}'.");
                for (int j = 0; j < i; j++)
                    if (antennas[j].HasPedestal &&
                        Vector2.Distance(pedestal, AntennaGeometry.PedestalPosition(antennas[j], antennaDesign)) < reach + antennas[j].PedestalReach)
                        issues.Add($"El pedestal de '{a.name}' se pisa con el de '{antennas[j].name}'.");
            }

            if (a.dishDiameter <= 0f) continue;
            if (a.elevation <= 0f || a.elevation > 90f)
                issues.Add($"La elevación de '{a.name}' ({a.elevation:0.#}°) tiene que estar entre 0 y 90°.");
            else if (AntennaGeometry.AxisHeight(a) + AntennaGeometry.LowestAboveAxis(a, antennaDesign) < 0f)
                issues.Add($"Con {a.elevation:0.#}° de elevación el plato de '{a.name}' se mete en el suelo: sube la montura o el pedestal.");

            if (a.overallHeight > 0f)
            {
                float h = AntennaGeometry.OverallHeight(a, antennaDesign);
                if (Mathf.Abs(h - a.overallHeight) > HeightTolerance)
                    issues.Add($"'{a.name}' mide {h:0.##} m de alto y el plano dice ≈ {a.overallHeight:0.#} m. " +
                               "Usa 'Ajustar pedestales' en el Constructor.");
            }
        }

        // Los árboles pueden quedar fuera de la cerca (el bosque de alrededor), pero no nacer en ella
        // ni dentro de un edificio, un tanque o una losa de concreto.
        var solid = buildings.Select(b => (b.name, b.Footprint))
            .Concat(facilities.Select(f => (f.name, f.Footprint)))
            .Concat(site.pads.Where(p => p.kind != PadKind.Grama).Select(p => (p.name, p.Footprint)))
            .ToList();
        foreach (var t in trees)
        {
            string at = $"({t.position.x:0.#}, {t.position.y:0.#})";
            if (PlanGeometry.DistanceToOutline(outline, t.position) < treeDesign.trunkDiameter + fence.postSize)
                issues.Add($"El árbol de {at} nace sobre la cerca.");
            foreach (var (name, print) in solid)
                if (print.Distance(t.position) < treeDesign.trunkDiameter)
                    issues.Add($"El árbol de {at} nace dentro de '{name}'.");
        }

        var env = environment;
        float plotHalfDiagonal = Bounds.size.magnitude / 2f;
        if (env.hillsInnerRadius <= plotHalfDiagonal)
            issues.Add($"Los cerros empiezan a {env.hillsInnerRadius:0} m del centro y la parcela llega a {plotHalfDiagonal:0} m: se meterían en ella.");
        if (env.hillsOuterRadius <= env.hillsInnerRadius)
            issues.Add("El radio exterior de los cerros tiene que ser mayor que el interior.");
        if (env.savannaSize / 2f < env.hillsOuterRadius)
            issues.Add("La sabana no llega hasta el borde exterior de los cerros: se vería el vacío por debajo.");
        if (env.hillsMinHeight > env.hillsMaxHeight)
            issues.Add("La altura mínima de los cerros supera a la máxima.");

        foreach (var (name, layer) in scatter.All())
        {
            bool used = layer.alongFence + layer.perTree + layer.savanna + layer.parcelPatches + layer.field +
                        layer.alongRoads + layer.alongPads + layer.parcel > 0f;
            if (used && !layer.models.Any(m => m.model != null))
                issues.Add($"La capa '{name}' tiene dónde salir pero ningún modelo.");
            if (layer.height.x <= 0f || layer.height.y < layer.height.x)
                issues.Add($"El alto de '{name}' tiene que ir de un número positivo a otro igual o mayor.");
            if (layer.radius <= 0f)
                issues.Add($"El radio de '{name}' tiene que ser mayor que 0.");
            if (layer.distances == null || layer.distances.Length == 0)
                issues.Add($"'{name}' no tiene distancias de dibujo.");
            else
                for (int i = 0; i < layer.distances.Length; i++)
                    if (layer.distances[i] <= (i > 0 ? layer.distances[i - 1] : 0f))
                        issues.Add($"Las distancias de dibujo de '{name}' tienen que ir de menor a mayor.");
            if (layer.fenceBand.x < layer.radius)
                issues.Add($"'{name}' puede caer sobre la cerca: la franja junto a ella empieza a {layer.fenceBand.x:0.##} m y mide {layer.radius:0.##} m de radio.");
        }

        foreach (var (name, layer) in surfaces.All())
        {
            if (layer.color == null) continue;
            if (layer.tile <= 0f)
                issues.Add($"La textura de '{name}' tiene que repetirse cada más de 0 m.");
            if (layer.normal == null)
                issues.Add($"La textura de '{name}' no tiene relieve (normal map): se verá plana.");
        }

        return issues;
    }
}

/// <summary>Lados de un edificio en SU marco local (norte = +Z local, que gira con el edificio).</summary>
public enum Side { Norte, Sur, Este, Oeste }

[Serializable]
public class FenceSpec
{
    [Header("Cerca perimetral (malla ciclón)")]
    [Tooltip("Medido: vértices de la cerca en planta (X, Z), en orden. El plano la dibuja como un " +
             "hexágono; el terreno de la parcela toma esta misma forma.")]
    public List<Vector2> outline = new List<Vector2>
    {
        new Vector2(0f, 210.9375f),
        new Vector2(81.25f, 245.3125f),
        new Vector2(281.25f, 248.4375f),
        new Vector2(281.25f, 6.25f),
        new Vector2(78.125f, 0f),
        new Vector2(0f, 48.4375f),
    };
    [Tooltip("Plano: h ≈ 2,5 m.")]
    public float height = 2.5f;
    [Tooltip("Supuesto. Separación máxima entre postes; se reparte uniforme en cada lado.")]
    public float postSpacing = 3f;
    [Tooltip("Supuesto. Lado de la sección de los postes.")]
    public float postSize = 0.08f;
    [Tooltip("Supuesto. Espesor de la malla en el blockout.")]
    public float meshThickness = 0.04f;

    [Header("Portón")]
    [Tooltip("Medido: donde la vía de acceso cruza la cerca oeste. Tiene que caer sobre un lado de la cerca.")]
    public Vector2 gatePosition = new Vector2(0f, 107.8125f);
    [Tooltip("Supuesto: algo más ancho que la vía de acceso (3,5 m).")]
    public float gateWidth = 6f;
    [Tooltip("Supuesto. Sección de los dos postes del portón.")]
    public float gatePostSize = 0.2f;
    [Tooltip("Supuesto. Espesor de la hoja del portón.")]
    public float gateLeafThickness = 0.08f;
}

/// <summary>Construcción común a todos los edificios. Todo son supuestos: el plano no la da.</summary>
[Serializable]
public class BuildingDesign
{
    [Header("Estructura")]
    [Tooltip("Losa de piso, apoyada sobre el terreno.")]
    public float floorThickness = 0.15f;
    [Tooltip("Muro de bloque. Va por dentro de la huella: su cara exterior es el borde del plano.")]
    public float exteriorWallThickness = 0.2f;
    [Tooltip("Tabique interior, centrado en su eje.")]
    public float interiorWallThickness = 0.15f;

    [Header("Techo")]
    [Tooltip("Vuelo del techo más allá de los muros (aleros). En los techos a dos aguas, solo en los lados largos: el hastial va a ras.")]
    public float roofOverhang = 0.6f;
    [Tooltip("Canto del techo en el alero.")]
    public float roofFascia = 0.18f;
    [Tooltip("Espesor de las losas de techo planas.")]
    public float slabThickness = 0.25f;

    [Header("Franja azul (plano anterior: \"bloque blanco con franja azul\")")]
    [Tooltip("Distancia del borde superior de la franja al alero.")]
    public float stripeBelowEaves = 0.15f;
    public float stripeHeight = 0.45f;
    [Tooltip("Lo que sobresale la franja del muro (así no hace z-fighting con él).")]
    public float stripeDepth = 0.03f;

    [Header("Ventanas (plano anterior: \"ventanas horizontales\")")]
    [Tooltip("Antepecho, desde el piso terminado.")]
    public float windowSill = 1.1f;
    public float windowHeight = 1f;
    public float windowWidth = 2.4f;
    [Tooltip("Distancia entre centros de ventana. Se reparten centradas en cada muro, saltando puertas, " +
             "tabiques y los tramos de muro que dan a otro edificio.")]
    public float windowSpacing = 4.5f;
    [Tooltip("Distancia mínima de una ventana a la esquina del edificio.")]
    public float windowCornerMargin = 1.2f;
    [Tooltip("Espesor del vidrio, centrado en el muro.")]
    public float glassThickness = 0.02f;

    [Header("Puertas interiores")]
    public float interiorDoorWidth = 1.2f;
    public float interiorDoorHeight = 2.1f;
    [Tooltip("Espesor de la hoja que cierra las puertas de los edificios que no se recorren.")]
    public float doorLeafThickness = 0.06f;
}

public enum RoofType { CuatroAguas, DosAguas, Losa }
public enum RoofCover { Teja, Zinc, Concreto }
public enum WallFinish { Blanco, Gris }

[Serializable]
public class BuildingSpec
{
    public string name;
    [Tooltip("Medido: centro de la huella en planta (X, Z).")]
    public Vector2 center;
    [Tooltip("Medido / plano: huella exterior en SU marco: X local (\"este-oeste\") × Z local (\"norte-sur\").")]
    public Vector2 size;
    [Tooltip("Medido: giro del edificio en grados, horario visto desde arriba (el rotate() del SVG).")]
    public float rotation;
    [Tooltip("Supuesto: del suelo a la cabeza de los muros, donde arranca el techo.")]
    public float wallHeight = 4f;

    [Header("Acabado")]
    [Tooltip("Plano: teja roja en el conjunto principal y en oficinas; gris (\"zinc o losa\") en el resto. " +
             "El caballete de los techos inclinados va a lo largo del lado mayor.")]
    public RoofType roof;
    public RoofCover cover;
    [Tooltip("Supuesto. Pendiente de los faldones, en grados.")]
    public float roofPitch = 20f;
    public WallFinish walls;
    [Tooltip("Franja azul bajo el alero.")]
    public bool stripe;
    [Tooltip("Ventanas horizontales repartidas por los muros (ver BuildingDesign).")]
    public bool windows;
    [Tooltip("Si se puede entrar: las puertas quedan abiertas. Si no, llevan hoja.")]
    public bool enterable;

    [Tooltip("Supuesto (el plano no dibuja puertas). En los muros norte y sur, la posición se mide desde " +
             "el extremo oeste; en los muros este y oeste, desde el extremo sur. Todo en el marco del edificio.")]
    public List<BuildingDoor> doors = new List<BuildingDoor>();
    [Tooltip("Tabiques interiores.")]
    public List<Partition> partitions = new List<Partition>();
    [Tooltip("De dónde sale lo que no es obvio.")]
    [TextArea] public string notes;

    public Footprint Footprint => new Footprint(center, size, rotation);

    /// <summary>
    /// Largo del muro de un lado, medido como sus puertas: de esquina exterior a esquina exterior.
    /// </summary>
    public float WallLength(Side side) => side == Side.Norte || side == Side.Sur ? size.x : size.y;

    public BuildingSpec() { }

    public BuildingSpec(string name, Vector2 center, Vector2 size, float rotation, float wallHeight,
                        RoofType roof, RoofCover cover, WallFinish walls, bool stripe, bool windows, bool enterable,
                        string notes)
    {
        this.name = name; this.center = center; this.size = size; this.rotation = rotation;
        this.wallHeight = wallHeight; this.roof = roof; this.cover = cover; this.walls = walls;
        this.stripe = stripe; this.windows = windows; this.enterable = enterable; this.notes = notes;
        roofPitch = cover == RoofCover.Zinc ? 12f : 20f;
    }

    public BuildingSpec WithDoor(Side side, float offset, float width, float height)
    {
        doors.Add(new BuildingDoor { side = side, offset = offset, width = width, height = height });
        return this;
    }

    public BuildingSpec WithPartition(string name, bool northSouth, float position, float doorOffset)
    {
        partitions.Add(new Partition { name = name, northSouth = northSouth, position = position, doorOffset = doorOffset });
        return this;
    }
}

[Serializable]
public class BuildingDoor
{
    public Side side;
    [Tooltip("Centro del vano: desde el extremo oeste (muros N y S) o sur (muros E y O).")]
    public float offset;
    public float width;
    public float height;
}

[Serializable]
public class Partition
{
    public string name;
    [Tooltip("Falso: corre de este a oeste, a 'position' m de la cara exterior sur. Verdadero: corre de " +
             "norte a sur, a 'position' m de la cara exterior oeste.")]
    public bool northSouth;
    public float position;
    [Tooltip("Centro de su puerta, medido como las de fachada: desde la cara exterior oeste (tabique " +
             "este-oeste) o sur (tabique norte-sur). −1 = sin puerta.")]
    public float doorOffset = -1f;
}

[Serializable]
public class AntennaSpec
{
    public string name;
    [Tooltip("Plano / foto: centro del PLATO visto desde arriba (X, Z), que es lo que dibuja el plano y se ve " +
             "en la foto. El pedestal va detrás: el plato inclinado se adelanta hacia donde apunta " +
             "(ver AntennaGeometry.PedestalPosition).")]
    public Vector2 position;
    [Tooltip("Foto: ancho del plato (Camatagua 2: 30 m, del plano). 0 = sin plato.")]
    public float dishDiameter;
    [Tooltip("Plano: altura total aproximada (0 = no rotulada). La de Camatagua 2 viene del plano anterior.")]
    public float overallHeight;

    [Header("Pedestal")]
    [Tooltip("Supuesto: huella X × Z, ≈ 0,27–0,28 del diámetro. (0, 0) = sin pedestal.")]
    public Vector2 pedestalSize;
    [Tooltip("Foto: sale de la altura del plato medida por su sombra (MODULO-3D.md §2.8). En Camatagua 2 está " +
             "AJUSTADA para que la antena dé la altura del plano ('Ajustar pedestales'); en los platos de la losa, supuesta.")]
    public float pedestalHeight;
    [Tooltip("Supuesto (0,2·D). Altura del eje de elevación sobre la cara superior del pedestal: plataforma + soporte en Y.")]
    public float mountHeight;

    [Header("Apuntamiento")]
    [Tooltip("Plano: las flechas rojas apuntan al SUR (arco geoestacionario). Grados desde el norte, en sentido horario.")]
    public float azimuth = 180f;
    [Tooltip("Plano: ≈ 65°. Grados sobre el horizonte.")]
    public float elevation = 65f;

    public bool HasPedestal => pedestalSize.x > 0f && pedestalSize.y > 0f && pedestalHeight > 0f;

    /// <summary>Cara superior del pedestal, donde se apoya la montura.</summary>
    public float PedestalTop => HasPedestal ? pedestalHeight : 0f;

    /// <summary>Radio en planta que ocupa el pedestal, medido desde su eje (hasta la esquina).</summary>
    public float PedestalReach => HasPedestal ? pedestalSize.magnitude / 2f : 0f;

    public AntennaSpec() { }

    public AntennaSpec(string name, Vector2 position, float dishDiameter, float overallHeight,
                       Vector2 pedestalSize, float pedestalHeight, float mountHeight)
    {
        this.name = name;
        this.position = position;
        this.dishDiameter = dishDiameter;
        this.overallHeight = overallHeight;
        this.pedestalSize = pedestalSize;
        this.pedestalHeight = pedestalHeight;
        this.mountHeight = mountHeight;
    }
}

/// <summary>
/// Proporciones de la antena tipo: montura azimut-elevación con soporte en Y, reflector
/// paraboloide y subreflector en el foco (Cassegrain). Todo en fracciones del diámetro del plato,
/// así que la misma antena sirve para los 30 m de Camatagua 2 y para los 7 m de los platos de la losa.
/// Todos son SUPUESTOS: el plano solo da el diámetro y la anatomía.
/// </summary>
[Serializable]
public class AntennaDesign
{
    [Header("Reflector")]
    [Tooltip("f/D del reflector principal. Las Cassegrain usan 0,3–0,4. Fija la profundidad: R² / (4f).")]
    public float focalRatio = 0.35f;
    [Tooltip("Distancia del eje de elevación al vértice del plato (el cubo que los une).")]
    public float vertexOffset = 0.09f;
    [Tooltip("Lado del cubo trasero, entre el eje y el plato.")]
    public float hubSize = 0.12f;
    [Tooltip("Costillas de la estructura de respaldo, del cubo a la trasera del plato (número, no fracción).")]
    public int backupRibs = 8;
    [Tooltip("Dónde se apoyan esas costillas en el plato, como fracción del RADIO.")]
    public float backupAttach = 0.7f;
    [Tooltip("Grosor de las costillas.")]
    public float backupRibThickness = 0.012f;

    [Header("Alimentación (Cassegrain)")]
    [Tooltip("Diámetro del subreflector. Va en el foco del plato, convexo hacia él.")]
    public float subreflectorDiameter = 0.1f;
    [Tooltip("f/D con el que se dibuja el casquete del subreflector (solo le da la curvatura).")]
    public float subreflectorFocalRatio = 0.6f;
    [Tooltip("Largo de la bocina que sale del vértice hacia el subreflector.")]
    public float feedLength = 0.08f;
    [Tooltip("Diámetro de la boca de la bocina.")]
    public float feedDiameter = 0.035f;
    [Tooltip("Dónde se apoyan las cuatro patas que sostienen el subreflector, como fracción del RADIO.")]
    public float strutAttach = 0.8f;
    [Tooltip("Grosor de esas patas.")]
    public float strutThickness = 0.008f;

    [Header("Montura (soporte en Y)")]
    [Tooltip("Separación entre los dos cojinetes del eje de elevación.")]
    public float yokeWidth = 0.3f;
    [Tooltip("Grosor de los brazos del soporte y del eje.")]
    public float yokeThickness = 0.035f;
    [Tooltip("Ancho del tronco del soporte, antes de abrirse en Y.")]
    public float stemWidth = 0.12f;
    [Tooltip("Qué parte de la altura de la montura es tronco (el resto son los brazos). Fracción de la montura, no de D.")]
    public float stemFraction = 0.45f;
    [Tooltip("Espesor de la plataforma giratoria de azimut.")]
    public float turntableHeight = 0.025f;
}

/// <summary>Lo que va a ras de suelo: vías y losas.</summary>
[Serializable]
public class SiteSpec
{
    [Header("Vías (asfalto)")]
    [Tooltip("Supuesto. Espesor del asfalto sobre el terreno.")]
    public float pavingThickness = 0.05f;
    [Tooltip("Medido: los trazos grises del plano, con su grosor como ancho. Los extremos y los quiebres " +
             "van redondeados, como el trazo. La vía de acceso empieza fuera de la cerca.")]
    public List<RoadSpec> roads = new List<RoadSpec>
    {
        new RoadSpec("Vía de acceso", 3.5f,
            new Vector2(-12.5f, 104.6875f), new Vector2(25f, 114.0625f), new Vector2(50f, 120.3125f), new Vector2(62.5f, 125f)),
        new RoadSpec("Anillo vial · tramo norte", 3f,
            new Vector2(62.5f, 125f), new Vector2(60.9375f, 145.3125f), new Vector2(68.75f, 160.9375f),
            new Vector2(81.25f, 167.1875f), new Vector2(93.75f, 168.75f)),
        new RoadSpec("Anillo vial · tramo sur", 3f,
            new Vector2(62.5f, 125f), new Vector2(65.625f, 104.6875f), new Vector2(78.125f, 92.1875f),
            new Vector2(96.875f, 89.0625f), new Vector2(118.75f, 90.625f), new Vector2(134.375f, 98.4375f),
            new Vector2(137.5f, 110.9375f), new Vector2(135.9375f, 126.5625f), new Vector2(134.375f, 142.1875f),
            new Vector2(128.125f, 148.4375f)),
        new RoadSpec("Ramal norte (sala de equipos y Antena 4)", 2.5f,
            new Vector2(93.75f, 168.75f), new Vector2(112.5f, 176.5625f), new Vector2(121.875f, 189.0625f),
            new Vector2(118.75f, 207.8125f), new Vector2(121.875f, 223.4375f)),
        new RoadSpec("Lazo oeste (oficinas y tanques)", 2.25f,
            new Vector2(62.5f, 125f), new Vector2(43.75f, 132.8125f), new Vector2(25f, 148.4375f),
            new Vector2(12.5f, 167.1875f), new Vector2(9.375f, 189.0625f), new Vector2(18.75f, 204.6875f),
            new Vector2(37.5f, 210.9375f), new Vector2(56.25f, 204.6875f), new Vector2(62.5f, 189.0625f),
            new Vector2(59.375f, 173.4375f), new Vector2(50f, 165.625f)),
        new RoadSpec("Ramal al tanque de agua", 2f,
            new Vector2(135.9375f, 126.5625f), new Vector2(162.5f, 132.8125f), new Vector2(181.25f, 139.0625f)),
        new RoadSpec("Ramal a la losa de antenas", 2f,
            new Vector2(134.375f, 98.4375f), new Vector2(143.75f, 82.8125f), new Vector2(162.5f, 81.25f)),
    };

    [Header("Losas y áreas")]
    [Tooltip("Medido: rectángulos (girados) del plano. Grama = una capa fina de grama corta sobre el terreno.")]
    public List<PadSpec> pads = new List<PadSpec>
    {
        new PadSpec("Patio de vehículos", PadKind.Concreto, new Vector2(189.0625f, 230.4f), new Vector2(21.875f, 29.6875f), -8f, 0.15f)
        {
            // Foto: filas de vehículos o contenedores blancos.
            items = new Vector2Int(3, 9), itemSize = new Vector3(5f, 2.3f, 2.4f),
        },
        // El plano dice "losa de concreto"; la foto enseña un recinto de tierra y grava con su camino alrededor.
        new PadSpec("Recinto de los platos pequeños", PadKind.Grava, new Vector2(157.8125f, 53.75f), new Vector2(56.25f, 53.125f), 0f, 0.05f),
        new PadSpec("Campo abierto (grama corta)", PadKind.Grama, new Vector2(232.8125f, 89.0625f), new Vector2(90.625f, 112.5f), 0f, 0.02f),
    };
}

[Serializable]
public class RoadSpec
{
    public string name;
    [Tooltip("Medido: el grosor del trazo del plano.")]
    public float width;
    [Tooltip("Medido: polilínea del eje en planta (X, Z).")]
    public List<Vector2> path = new List<Vector2>();

    public RoadSpec() { }
    public RoadSpec(string name, float width, params Vector2[] path) { this.name = name; this.width = width; this.path = path.ToList(); }
}

/// <summary>Concreto y Grava se pisan (llevan collider); Grama es una capa fina de grama corta.</summary>
public enum PadKind { Concreto, Grava, Grama }

[Serializable]
public class PadSpec
{
    public string name;
    public PadKind kind;
    [Tooltip("Medido: centro (X, Z). El patio de vehículos está bajado 1,6 m: en el plano su esquina NE se sale de la cerca.")]
    public Vector2 center;
    public Vector2 size;
    [Tooltip("Medido: giro en grados, horario visto desde arriba.")]
    public float rotation;
    [Tooltip("Supuesto.")]
    public float thickness;
    [Tooltip("Foto: vehículos o contenedores encima, en una rejilla de columnas (X local) × filas (Z local), " +
             "uno centrado en cada hueco. (0, 0) = ninguno.")]
    public Vector2Int items;
    [Tooltip("Supuesto: tamaño de cada uno (X local × alto × Z local).")]
    public Vector3 itemSize;

    public Footprint Footprint => new Footprint(center, size, rotation);

    public PadSpec() { }
    public PadSpec(string name, PadKind kind, Vector2 center, Vector2 size, float rotation, float thickness)
    {
        this.name = name; this.kind = kind; this.center = center; this.size = size; this.rotation = rotation; this.thickness = thickness;
    }
}

/// <summary>
/// Equipo = caja de acero. TanqueVertical = cilindros repartidos a lo largo del lado mayor, con
/// Ø = lo que dé cada hueco. TanqueHorizontal = cilindros tumbados a lo largo del lado mayor, uno al
/// lado del otro, con Ø = la altura. Cisterna = depósito abierto: borde de concreto y agua dentro
/// (el rectángulo azul del plano; en la foto, agua oscura con un borde claro).
/// </summary>
public enum FacilityShape { Equipo, TanqueVertical, TanqueHorizontal, Cisterna }

[Serializable]
public class FacilitySpec
{
    public string name;
    public FacilityShape shape;
    [Tooltip("Medido: centro de la huella en planta (X, Z).")]
    public Vector2 position;
    [Tooltip("Medido: huella X × Z.")]
    public Vector2 size;
    [Tooltip("Supuesto: altura del cuerpo sobre la losa (en un tanque horizontal, su diámetro).")]
    public float height;
    [Tooltip("Supuesto: losa de concreto bajo el equipo, de toda la huella. 0 = sin losa.")]
    public float padHeight;
    [Tooltip("Supuesto: margen entre el borde de la huella y el equipo.")]
    public float inset;
    [Tooltip("Supuesto: cuántas unidades (el plano dice \"Tanques\", en plural).")]
    public int count = 1;

    public Footprint Footprint => new Footprint(position, size, 0f);

    public FacilitySpec() { }

    public FacilitySpec(string name, FacilityShape shape, Vector2 position, Vector2 size,
                        float height, float padHeight, float inset, int count)
    {
        this.name = name; this.shape = shape; this.position = position; this.size = size;
        this.height = height; this.padHeight = padHeight; this.inset = inset; this.count = count;
    }
}

/// <summary>El árbol: un modelo (prefab) escalado a su copa o, sin modelos, uno simple de tronco y copa esférica.</summary>
[Serializable]
public class TreeDesign
{
    [Tooltip("Modelos de árbol (prefabs). Se usa su malla con materiales URP del generador, así que valen " +
             "también los del Tree Creator, cuyos shaders no funcionan en URP. Cada árbol se escala para que " +
             "su copa mida lo que pide. Vacío = árbol simple (tronco + copa esférica).")]
    public List<GameObject> models = new List<GameObject>();

    [Header("Árbol simple (solo si no hay modelos)")]
    public float trunkHeight = 2.4f;
    [Tooltip("También es la distancia mínima del tronco a un edificio o a la cerca en la validación.")]
    public float trunkDiameter = 0.35f;
    [Tooltip("Alto de la copa como fracción de su diámetro (copa algo achatada).")]
    public float crownHeightRatio = 0.75f;
    [Tooltip("Cuánto se mete el tronco dentro de la copa.")]
    public float crownOverlap = 0.4f;
}

[Serializable]
public class TreeSpec
{
    [Tooltip("Foto: posición del TRONCO (X, Z), elegida para que la copa del modelo caiga sobre la mancha de " +
             "follaje de la foto (en algunos modelos la copa va descentrada).")]
    public Vector2 position;
    [Tooltip("Foto: diámetro de la copa. El modelo se escala para darlo.")]
    public float crownDiameter;
    [Tooltip("Cuál de TreeDesign.models (se toma en módulo).")]
    public int model;
    [Tooltip("Giro en grados, horario visto desde arriba.")]
    public float yaw;

    public TreeSpec() { }
    public TreeSpec(Vector2 position, float crownDiameter) { this.position = position; this.crownDiameter = crownDiameter; }
}

/// <summary>
/// El suelo y lo que rodea la parcela. El plano dice "terreno de sabana seca (tierra ocre + manchas de
/// grama)"; los cerros del fondo son del plano anterior. Todo son supuestos sin precisión métrica, y
/// el centro es el de la parcela.
/// </summary>
[Serializable]
public class EnvironmentSpec
{
    [Header("Suelo (supuestos)")]
    [Tooltip("Metros que cubre cada repetición de la textura de tierra con manchas de grama, dentro de la cerca.")]
    public float soilTile = 55f;
    [Tooltip("Metros que cubre cada repetición de la grama del campo abierto. Grande a propósito: con 6 m la " +
             "repetición se veía en cuadrícula desde el aire.")]
    public float grassTile = 50f;
    [Tooltip("Metros que cubre cada repetición de la sabana de fuera de la cerca. Solo se ve de lejos, y con " +
             "50 m sus manchas formaban una cuadrícula a la vista.")]
    public float savannaTile = 160f;
    [Tooltip("Metros que cubre cada repetición del grano fino (detail map). Da el detalle de cerca.")]
    public float grassDetailTile = 2.5f;

    [Header("Sabana (supuestos)")]
    [Tooltip("Lado del suelo exterior, centrado en la parcela.")]
    public float savannaSize = 3400f;
    [Tooltip("Cuánto queda por debajo de la parcela (Y = 0), para que no haya z-fighting con ella.")]
    public float savannaDrop = 0.02f;

    [Header("Cerros al fondo (supuestos)")]
    [Tooltip("Distancia del centro de la parcela a la que empiezan a subir.")]
    public float hillsInnerRadius = 650f;
    [Tooltip("Borde exterior del anillo de cerros.")]
    public float hillsOuterRadius = 1600f;
    public float hillsMinHeight = 60f;
    public float hillsMaxHeight = 240f;
    [Tooltip("Semilla del perfil de los cerros: el mismo número da siempre los mismos cerros.")]
    public int hillsSeed = 1970;
}

/// <summary>
/// Una textura real aplicada a una superficie, a su tamaño de verdad. La pinta el shader
/// "PVI/Superficie en metros", que la repite cada <see cref="tile"/> metros sea cual sea la pieza.
/// </summary>
[Serializable]
public class SurfaceLayer
{
    [Tooltip("Color (Poly Haven, 1k). Vacío = la superficie queda del color liso del acabado.")]
    public Texture2D color;
    [Tooltip("Relieve (normal map, convención OpenGL: la \"nor_gl\" de Poly Haven).")]
    public Texture2D normal;
    [Tooltip("Poly Haven: lo que mide de verdad una repetición de la textura, en metros.")]
    public float tile = 3f;
    [Tooltip("Supuesto: tinte que multiplica la textura (blanco = la textura tal cual).")]
    public Color tint = Color.white;
    [Tooltip("Supuesto: lisura (0 = mate). Las texturas no traen la suya: se usa este número.")]
    [Range(0f, 1f)] public float smoothness = 0.1f;
    [Tooltip("Supuesto: variación de tono a gran escala, para que no se vea la repetición (0 = ninguna).")]
    [Range(0f, 1f)] public float variation = 0.15f;
    [Tooltip("Supuesto: tamaño de esa variación de tono, en metros.")]
    public float variationSize = 25f;

    public SurfaceLayer() { }
    public SurfaceLayer(float tile, Color tint, float smoothness = 0.1f, float variation = 0.15f, float variationSize = 25f)
    {
        this.tile = tile; this.tint = tint; this.smoothness = smoothness; this.variation = variation; this.variationSize = variationSize;
    }
}

/// <summary>Un suelo de dos texturas: la principal y otra por manchas encima (grama sobre tierra).</summary>
[Serializable]
public class GroundSurface
{
    public SurfaceLayer main = new SurfaceLayer();
    [Tooltip("La que sale por manchas encima de la principal.")]
    public SurfaceLayer patches = new SurfaceLayer();
    [Tooltip("Foto / supuesto: cuánto suelo cubren las manchas (0 = nada, 1 = todo).")]
    [Range(0f, 1f)] public float cover = 0.35f;
    [Tooltip("Supuesto: tamaño típico de una mancha, en metros.")]
    public float patchSize = 22f;
    [Tooltip("Supuesto: qué tan difuminado es el borde de las manchas.")]
    [Range(0.01f, 0.5f)] public float softness = 0.12f;
}

/// <summary>
/// Qué textura lleva cada superficie de la estación. Las texturas son de Poly Haven (CC0) y están en
/// Assets/PolyHaven (con su LICENCIA.txt); el tamaño de cada repetición es el que da Poly Haven.
/// </summary>
[Serializable]
public class SurfacesSpec
{
    // Tintes: multiplican la textura en espacio lineal (Unity pasa el color a lineal, así que 1,3 en el
    // inspector es ×1,78). Están calculados con el color medio de cada textura para llegar al tono del
    // acabado anterior (MODULO-3D.md §4.4): concreto claro, muros blancos, teja roja, vías gris claro.

    [Header("Suelos (dos texturas mezcladas por manchas)")]
    [Tooltip("Plano: \"tierra ocre + manchas de grama\" dentro de la cerca.")]
    public GroundSurface parcel = new GroundSurface
    {
        main = new SurfaceLayer(4f, new Color(1.2f, 1.32f, 1.46f), 0.08f, 0.3f, 60f),
        patches = new SurfaceLayer(2f, new Color(1.5f, 1.6f, 1.6f), 0.12f),
        cover = 0.35f, patchSize = 10f, softness = 0.15f,
    };
    [Tooltip("Campo abierto: grama corta con algo de tierra.")]
    public GroundSurface field = new GroundSurface
    {
        main = new SurfaceLayer(2f, new Color(1f, 1.05f, 0.95f), 0.12f, 0.3f, 60f),
        patches = new SurfaceLayer(4f, new Color(1.2f, 1.32f, 1.46f), 0.08f),
        cover = 0.1f, patchSize = 8f, softness = 0.15f,
    };
    [Tooltip("Sabana seca de fuera de la cerca: grama pajiza con tierra.")]
    public GroundSurface savanna = new GroundSurface
    {
        main = new SurfaceLayer(2f, new Color(0.93f, 1.03f, 1.02f), 0.1f, 0.4f, 200f),
        patches = new SurfaceLayer(4f, new Color(1.2f, 1.32f, 1.46f), 0.08f),
        cover = 0.2f, patchSize = 30f, softness = 0.2f,
    };
    [Tooltip("Cerros del fondo: se ven a más de 650 m, así que cuenta la variación grande, no el detalle. " +
             "Las manchas son monte más oscuro.")]
    public GroundSurface hills = new GroundSurface
    {
        main = new SurfaceLayer(10f, new Color(0.85f, 0.95f, 0.8f), 0.05f, 0.45f, 500f),
        patches = new SurfaceLayer(10f, new Color(1.3f, 1.4f, 1.4f), 0.05f),
        cover = 0.35f, patchSize = 80f, softness = 0.3f,
    };

    [Header("Pavimentos")]
    public SurfaceLayer asphalt = new SurfaceLayer(4f, new Color(1.05f, 1.05f, 1f), 0.12f, 0.2f, 30f);
    [Tooltip("Recinto de los platos pequeños (foto: tierra y grava).")]
    public SurfaceLayer gravel = new SurfaceLayer(2.5f, new Color(1.04f, 1.2f, 1.4f), 0.05f, 0.25f, 20f);
    [Tooltip("Losas, pedestales, cisterna y bases de los tanques.")]
    public SurfaceLayer concrete = new SurfaceLayer(3f, new Color(1.95f, 1.93f, 1.9f), 0.12f, 0.2f, 15f);
    [Tooltip("Piso de los edificios.")]
    public SurfaceLayer floor = new SurfaceLayer(3f, new Color(2.1f, 2.1f, 2.05f), 0.3f, 0.1f, 15f);

    [Header("Edificios")]
    [Tooltip("Muros de bloque pintado de blanco (plano anterior).")]
    public SurfaceLayer walls = new SurfaceLayer(2f, new Color(1.26f, 1.32f, 1.35f), 0.08f, 0.12f, 12f);
    [Tooltip("Muros de los edificios de servicio (gris claro).")]
    public SurfaceLayer serviceWalls = new SurfaceLayer(2f, new Color(1.18f, 1.23f, 1.22f), 0.08f, 0.15f, 12f);
    [Tooltip("Franja azul bajo el alero: la misma pintura, en el azul de acento del aplicativo (#1560D8).")]
    public SurfaceLayer stripe = new SurfaceLayer(2f, new Color(0.16f, 0.59f, 1.35f), 0.2f, 0.08f, 12f);
    [Tooltip("Techos de teja (edificio principal y ala norte).")]
    public SurfaceLayer roofTile = new SurfaceLayer(3.5f, new Color(1.23f, 0.77f, 0.72f), 0.15f, 0.2f, 15f);
    [Tooltip("Techo de zinc del galpón.")]
    public SurfaceLayer zinc = new SurfaceLayer(1.12f, new Color(0.95f, 0.93f, 1f), 0.4f, 0.25f, 10f);
    [Tooltip("Techos planos de losa (foto: claros).")]
    public SurfaceLayer flatRoof = new SurfaceLayer(3f, new Color(2.25f, 2.25f, 2.2f), 0.1f, 0.25f, 12f);

    /// <summary>Todas las capas con su nombre, para validarlas.</summary>
    public IEnumerable<(string, SurfaceLayer)> All()
    {
        foreach (var (n, g) in new[] { ("parcela", parcel), ("campo abierto", field), ("sabana", savanna), ("cerros", hills) })
        {
            yield return (n, g.main);
            yield return (n + " (manchas)", g.patches);
        }
        yield return ("asfalto", asphalt); yield return ("grava", gravel); yield return ("concreto", concrete);
        yield return ("piso", floor); yield return ("muros", walls); yield return ("muros de servicio", serviceWalls);
        yield return ("franja", stripe); yield return ("teja", roofTile); yield return ("zinc", zinc); yield return ("losa de techo", flatRoof);
    }
}

/// <summary>Un modelo de una capa repartida (un arbusto, el juego de matas, el juego de piedras).</summary>
[Serializable]
public class ScatterModel
{
    [Tooltip("Prefab o FBX. Con LODGroup (los arbustos Yughues) se usan sus niveles de detalle; si no, cada " +
             "malla hija es una variante y los niveles salen de su Mesh LOD (actívalo en el importador).")]
    public GameObject model;
    [Tooltip("Color (con transparencia si recorta). Vacío = el del material del modelo.")]
    public Texture2D color;
    [Tooltip("Relieve (normal map). Vacío = el del material del modelo, si tiene.")]
    public Texture2D normal;
    [Tooltip("Supuesto: tinte.")]
    public Color tint = Color.white;
    [Tooltip("Supuesto: cuántas veces sale frente a los demás modelos de la capa.")]
    public float weight = 1f;
    [Tooltip("Hojas y briznas: se recortan por la transparencia y se ven por las dos caras.")]
    public bool foliage = true;
}

/// <summary>
/// Una capa repartida por el terreno. Dónde sale, en grupos de <see cref="clump"/> copias: junto a la
/// cerca (por los dos lados), al pie de los árboles, en la sabana de fuera, sobre las manchas de grama
/// de la parcela (las mismas del suelo), en el campo abierto, al borde de las vías y las losas, y
/// suelto por la parcela. Nunca en vías, edificios, tanques, losas, bajo un plato ni sobre la cerca
/// (SiteClearance). Todo son supuestos: ni el plano ni la foto los dan.
/// </summary>
[Serializable]
public class ScatterLayer
{
    public List<ScatterModel> models = new List<ScatterModel>();
    [Tooltip("Supuesto: alto de cada copia en metros, al azar entre los dos.")]
    public Vector2 height = new Vector2(0.6f, 1.2f);
    [Tooltip("Supuesto: radio que ocupa en planta. Es el margen con vías, edificios, losas, platos y cerca.")]
    public float radius = 0.5f;
    [Tooltip("Supuesto: copias por grupo, al azar entre los dos (la vegetación crece en matas).")]
    public Vector2Int clump = new Vector2Int(1, 1);
    [Tooltip("Supuesto: radio del grupo, en metros.")]
    public float clumpRadius = 1f;
    [Tooltip("Supuesto: cuánto se hunde en el suelo, como fracción de su alto (las piedras van medio enterradas).")]
    [Range(0f, 0.5f)] public float sink = 0.03f;

    [Header("Dónde (grupos)")]
    [Tooltip("Por cada 100 m de cerca, contando los dos lados.")]
    public float alongFence;
    [Tooltip("Franja junto a la cerca: de qué distancia a qué distancia de ella (m).")]
    public Vector2 fenceBand = new Vector2(0.8f, 3f);
    [Tooltip("Por árbol, al pie (bajo la copa).")]
    public float perTree;
    [Tooltip("Por cada 1000 m² de sabana, fuera de la cerca.")]
    public float savanna;
    [Tooltip("Hasta qué distancia de la cerca se reparte por la sabana (m).")]
    public float savannaWidth = 80f;
    [Tooltip("Por cada 100 m² de mancha de grama dentro de la cerca (las manchas del suelo).")]
    public float parcelPatches;
    [Tooltip("Por cada 100 m² del campo abierto.")]
    public float field;
    [Tooltip("Por cada 100 m de borde de vía, contando los dos lados.")]
    public float alongRoads;
    [Tooltip("Por cada 100 m de borde de losa (concreto y grava).")]
    public float alongPads;
    [Tooltip("Franja junto a las vías y las losas: de qué distancia a qué distancia de su borde (m).")]
    public Vector2 edgeBand = new Vector2(0.1f, 1f);
    [Tooltip("Por cada 1000 m² de la parcela, sueltos donde quepan.")]
    public float parcel;

    [Header("Dibujo")]
    [Tooltip("Hasta dónde se usa cada nivel de detalle (m); la última es hasta dónde se ve. Más cerca = más barato.")]
    public float[] distances = { 10f, 25f, 50f };
    [Tooltip("Triángulos como mucho de una copia en el nivel cercano (con Mesh LOD; cada nivel siguiente, ~¼).")]
    public int nearTriangles = 1500;
    [Tooltip("Cuántos niveles, empezando por el cercano, dan sombra (0 = ninguno: lo barato).")]
    public int shadowBands;
    [Tooltip("En calidad Baja: qué parte de las copias se dibuja.")]
    [Range(0f, 1f)] public float lowDensity = 0.5f;
    [Tooltip("En calidad Baja: las distancias de dibujo × esto.")]
    [Range(0.1f, 1f)] public float lowDistanceScale = 0.6f;
    [Tooltip("Semilla: el mismo número reparte siempre igual.")]
    public int seed = 1;
}

[Serializable]
public class ScatterSpec
{
    [Tooltip("Arbustos (pack Yughues): junto a la cerca, al pie de los árboles y sueltos por la sabana.")]
    public ScatterLayer bushes = new ScatterLayer
    {
        height = new Vector2(0.8f, 1.8f), radius = 0.7f, clump = new Vector2Int(1, 3), clumpRadius = 1.6f, sink = 0.05f,
        alongFence = 6f, fenceBand = new Vector2(0.9f, 3.5f), perTree = 1.2f, savanna = 0.8f, savannaWidth = 120f, parcel = 0.15f,
        distances = new[] { 15f, 35f, 70f, 110f }, shadowBands = 2, lowDensity = 0.7f, lowDistanceScale = 0.6f, seed = 11,
    };
    [Tooltip("Grama alta (Poly Haven grass_medium_02, verde y seca): junto a la cerca, al pie de los árboles, en la " +
             "sabana, sobre las manchas de grama y en el campo abierto.")]
    public ScatterLayer grass = new ScatterLayer
    {
        // El modelo son briznas sueltas de 20–40 cm: estiradas a 1 m se veían cuatro palos. Mejor poco
        // estiradas y en macollas de 6 a 12.
        height = new Vector2(0.3f, 0.65f), radius = 0.25f, clump = new Vector2Int(6, 12), clumpRadius = 0.9f, sink = 0.03f,
        alongFence = 50f, fenceBand = new Vector2(0.3f, 2.5f), perTree = 4f, savanna = 8f, savannaWidth = 70f,
        parcelPatches = 2f, field = 1.2f,
        distances = new[] { 12f, 28f, 55f }, nearTriangles = 2600, shadowBands = 0, lowDensity = 0.45f, lowDistanceScale = 0.55f, seed = 23,
    };
    [Tooltip("Piedras sueltas (Poly Haven namaqualand_stones_01 y _rocks_01): al borde de las vías y las losas, y " +
             "algunas sueltas.")]
    public ScatterLayer rocks = new ScatterLayer
    {
        height = new Vector2(0.06f, 0.3f), radius = 0.3f, clump = new Vector2Int(1, 3), clumpRadius = 0.6f, sink = 0.25f,
        alongRoads = 25f, alongPads = 25f, edgeBand = new Vector2(0.15f, 1f), perTree = 0.5f, parcel = 0.6f,
        savanna = 0.3f, savannaWidth = 40f, fenceBand = new Vector2(0.5f, 2f),
        distances = new[] { 6f, 15f, 35f }, nearTriangles = 900, shadowBands = 0, lowDensity = 0.6f, lowDistanceScale = 0.6f, seed = 37,
    };

    public IEnumerable<(string, ScatterLayer)> All()
    {
        yield return ("arbustos", bushes);
        yield return ("grama alta", grass);
        yield return ("piedras", rocks);
    }
}
