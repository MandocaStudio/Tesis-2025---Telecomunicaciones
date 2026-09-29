using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Medidas de la Estación Terrena "Andrés Bello" sacadas del plano de conjunto
/// (Planos/estacion-andres-bello-plano-conjunto.png). Es la ÚNICA fuente de verdad del modelo 3D:
/// StationGenerator no lleva ninguna medida propia, todo lo lee de aquí.
///
/// Coordenadas: origen en la esquina SO de la parcela, X hacia el este, Z hacia el norte, Y arriba.
/// 1 unidad = 1 metro.
///
/// Los valores iniciales de este script son los del plano y solo sirven para crear el asset
/// (Assets/Data/AndresBelloLayout.asset); a partir de ahí manda el asset. "Reset" en el Inspector
/// equivale a volver al plano.
///
/// Cada Tooltip dice de dónde sale el número: "Plano" = rotulado (exacto), "Medido" = medido sobre
/// el dibujo (±0,5 m), "Supuesto" = el plano no lo da y es una decisión de construcción.
/// </summary>
[CreateAssetMenu(fileName = "StationLayout", menuName = "PVI/Station Layout")]
public class StationLayout : ScriptableObject
{
    [Header("Parcela")]
    [Tooltip("Plano: 200 × 140 m. X (este-oeste) × Z (norte-sur).")]
    public Vector2 plotSize = new Vector2(200f, 140f);
    [Tooltip("Supuesto. Espesor de la losa de terreno; su cara superior es Y = 0.")]
    public float groundThickness = 0.5f;

    public FenceSpec fence = new FenceSpec();
    public BuildingSpec building = new BuildingSpec();
    public SiteSpec site = new SiteSpec();
    public EnvironmentSpec environment = new EnvironmentSpec();

    [Tooltip("Servicios y edificios auxiliares (§2.4 del plano y la caseta). Posiciones y huellas medidas; alturas supuestas.")]
    public List<FacilitySpec> facilities = new List<FacilitySpec>
    {
        //               nombre                           forma                            centro (X, Z)              huella (X × Z)          alto   losa   margen
        new FacilitySpec("Planta eléctrica (generadores)", FacilityShape.Edificio,         new Vector2(141f, 55.5f),  new Vector2(14f, 9f),   4.5f,  0f,    0f),
        new FacilitySpec("Tanque diésel",                  FacilityShape.TanqueHorizontal, new Vector2(153f, 55.5f),  new Vector2(6f, 9f),    3f,    0.3f,  1f),
        new FacilitySpec("Transformador",                  FacilityShape.Equipo,           new Vector2(161f, 57f),    new Vector2(6f, 6f),    2.5f,  0.3f,  1.5f),
        new FacilitySpec("Chillers A/A",                   FacilityShape.Equipo,           new Vector2(139f, 46f),    new Vector2(10f, 6f),   2.4f,  0.3f,  1f),
        new FacilitySpec("Tanque de agua",                 FacilityShape.TanqueVertical,   new Vector2(20f, 35f),     new Vector2(8f, 8f),    6f,    0f,    0f),
        new FacilitySpec("Caseta de vigilancia",           FacilityShape.Edificio,         new Vector2(109f, 9.5f),   new Vector2(6f, 5f),    3f,    0f,    0f),
    };

    [Tooltip("Proporciones comunes a todas las antenas, como fracción del diámetro: una sola antena " +
             "parametrizada por su diámetro, no un modelo por antena.")]
    public AntennaDesign antennaDesign = new AntennaDesign();

    [Tooltip("Antenas en el orden del plano.")]
    public List<AntennaSpec> antennas = new List<AntennaSpec>
    {
        //               nombre                posición (X, Z)          Ø     alto  pedestal                    h ped.  montura
        new AntennaSpec("Camatagua 1 (1970)", new Vector2(50f, 85f),   32f,  30f, new Vector2(9f, 9f),       9.05f,  6.4f),
        new AntennaSpec("Camatagua 2 (1980)", new Vector2(120f, 85f),  30f,  28f, new Vector2(8.5f, 8.5f),   8.36f,  6f),
        new AntennaSpec("Antena 3",           new Vector2(168f, 100f), 11f,  0f,  new Vector2(3f, 3f),       1.5f,   2.2f),
        new AntennaSpec("Antena 4",           new Vector2(170f, 62f),  7f,   0f,  new Vector2(1.75f, 1.75f), 1f,     1.4f),
        new AntennaSpec("VSAT 1",             new Vector2(178f, 44f),  3.6f, 0f,  new Vector2(1.2f, 1.2f),   0.3f,   1.5f),
        new AntennaSpec("VSAT 2",             new Vector2(186f, 44f),  3.6f, 0f,  new Vector2(1.2f, 1.2f),   0.3f,   1.5f),
        new AntennaSpec("VSAT 3",             new Vector2(194f, 44f),  3.6f, 0f,  new Vector2(1.2f, 1.2f),   0.3f,   1.5f),
    };

    /// <summary>Margen admitido entre la altura calculada de una antena y la "≈" del plano.</summary>
    public const float HeightTolerance = 0.5f;

    /// <summary>
    /// Comprueba que el layout es coherente consigo mismo y con la parcela: que los locales de cada
    /// fila sumen el ancho del edificio, que las filas sumen su fondo, que las puertas quepan en su
    /// local y den a donde dicen. Devuelve un mensaje por problema; lista vacía = todo cuadra.
    /// </summary>
    public List<string> Validate()
    {
        const float tol = 0.01f;
        var issues = new List<string>();
        var b = building;

        float depthSum = b.rows.Sum(r => r.depth);
        if (Mathf.Abs(depthSum - b.size.y) > tol)
            issues.Add($"Las filas suman {depthSum:0.##} m de fondo y el edificio mide {b.size.y:0.##} m.");

        for (int r = 0; r < b.rows.Count; r++)
        {
            var row = b.rows[r];
            float widthSum = row.rooms.Sum(x => x.width);
            if (Mathf.Abs(widthSum - b.size.x) > tol)
                issues.Add($"Fila '{row.name}': los locales suman {widthSum:0.##} m y el edificio mide {b.size.x:0.##} m.");

            foreach (var room in row.rooms)
            foreach (var door in room.doors)
            {
                if (door.offset - door.width / 2f < 0f || door.offset + door.width / 2f > room.width)
                    issues.Add($"La puerta ({door.side}) de '{room.name}' se sale del local: centro a {door.offset:0.##} m, ancho {door.width:0.##} m, local de {room.width:0.##} m.");
                if (door.height >= b.WallHeight)
                    issues.Add($"La puerta ({door.side}) de '{room.name}' mide {door.height:0.##} m y el muro solo {b.WallHeight:0.##} m.");
                if (door.side == DoorSide.Pasillo && b.CorridorNeighbor(r) == 0)
                    issues.Add($"'{room.name}' tiene puerta al pasillo pero su fila no colinda con ninguna fila marcada como pasillo.");
                if (door.side == DoorSide.Fachada && r != 0 && r != b.rows.Count - 1)
                    issues.Add($"'{room.name}' tiene puerta a fachada pero su fila no es ni la primera (norte) ni la última (sur).");
            }
        }

        if (b.WallHeight <= 0f)
            issues.Add($"Las losas de piso y techo ({b.floorThickness + b.roofThickness:0.##} m) no dejan altura para los muros ({b.height:0.##} m).");

        // Fachada: ventanas dentro de su local y sin pisar la puerta; franja por encima de todos los vanos.
        float openingTop = b.floorThickness + b.windowSill + b.windowHeight;
        if (b.windowSill + b.windowHeight >= b.WallHeight)
            issues.Add($"Las ventanas (antepecho {b.windowSill:0.##} + {b.windowHeight:0.##} m) no caben en el muro de {b.WallHeight:0.##} m.");
        for (int r = 0; r < b.rows.Count; r++)
        foreach (var room in b.rows[r].rooms)
        {
            if (room.windows.Count > 0 && r != 0 && r != b.rows.Count - 1)
                issues.Add($"'{room.name}' tiene ventanas pero su fila no da a ninguna fachada.");
            foreach (var w in room.windows)
            {
                if (w.offset - w.width / 2f < 0f || w.offset + w.width / 2f > room.width)
                    issues.Add($"La ventana de '{room.name}' se sale del local.");
                foreach (var door in room.doors)
                    if (door.side == DoorSide.Fachada && Mathf.Abs(w.offset - door.offset) < (w.width + door.width) / 2f)
                        issues.Add($"La ventana de '{room.name}' se pisa con su puerta de fachada.");
            }
            foreach (var door in room.doors)
                if (door.side == DoorSide.Fachada)
                    openingTop = Mathf.Max(openingTop, b.floorThickness + door.height);
        }
        if (b.stripeBottom < openingTop)
            issues.Add($"La franja azul empieza a {b.stripeBottom:0.##} m y hay vanos de fachada hasta {openingTop:0.##} m: los taparía.");
        if (b.stripeBottom + b.stripeHeight > b.height - b.roofThickness)
            issues.Add("La franja azul se mete en la losa de techo.");
        if (b.roofUnits * b.roofUnitSize.x > b.size.x)
            issues.Add($"Los {b.roofUnits} equipos de A/A no caben a lo largo de la losa.");

        var min = b.center - b.size / 2f;
        var max = b.center + b.size / 2f;
        if (min.x < 0f || min.y < 0f || max.x > plotSize.x || max.y > plotSize.y)
            issues.Add($"El edificio ({min.x:0.#}–{max.x:0.#}, {min.y:0.#}–{max.y:0.#}) se sale de la parcela.");

        foreach (var a in antennas)
        {
            if (a.position.x < 0f || a.position.y < 0f || a.position.x > plotSize.x || a.position.y > plotSize.y)
                issues.Add($"La antena '{a.name}' está fuera de la parcela.");
            if (a.HasPedestal
                && Mathf.Abs(a.position.x - b.center.x) < (a.pedestalSize.x + b.size.x) / 2f
                && Mathf.Abs(a.position.y - b.center.y) < (a.pedestalSize.y + b.size.y) / 2f)
                issues.Add($"El pedestal de '{a.name}' se mete en el edificio.");

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

        float sideLength = fence.gateSide == PlotSide.Sur || fence.gateSide == PlotSide.Norte ? plotSize.x : plotSize.y;
        if (fence.gateCenter - fence.gateWidth / 2f < 0f || fence.gateCenter + fence.gateWidth / 2f > sideLength)
            issues.Add($"El portón se sale del lado {fence.gateSide} de la cerca.");

        foreach (var p in site.paving)
            if (!InsidePlot(p.min, p.max))
                issues.Add($"El pavimento '{p.name}' se sale de la parcela.");

        var pk = site.parking;
        if (!InsidePlot(pk.min, pk.max))
            issues.Add("El estacionamiento se sale de la parcela.");
        if (pk.stalls * pk.stallWidth > pk.max.x - pk.min.x + tol)
            issues.Add($"Los {pk.stalls} puestos ({pk.stalls * pk.stallWidth:0.##} m) no caben en los {pk.max.x - pk.min.x:0.##} m del estacionamiento.");
        if (pk.stallSetback + pk.stallDepth > pk.max.y - pk.min.y + tol)
            issues.Add("Los puestos son más profundos que el estacionamiento.");

        foreach (var d in site.ducts)
            if (d.path.Count < 2)
                issues.Add($"El ducto '{d.name}' necesita al menos dos puntos.");

        var buildingRect = (min, max);
        for (int i = 0; i < facilities.Count; i++)
        {
            var f = facilities[i];
            var (fMin, fMax) = f.Footprint;
            if (!InsidePlot(fMin, fMax))
                issues.Add($"'{f.name}' se sale de la parcela.");
            if (Overlaps((fMin, fMax), buildingRect))
                issues.Add($"'{f.name}' se mete en el edificio.");
            for (int j = 0; j < i; j++)
                if (Overlaps(f.Footprint, facilities[j].Footprint))
                    issues.Add($"'{f.name}' se pisa con '{facilities[j].name}'.");

            var inner = f.size - Vector2.one * (2f * f.inset);
            if (inner.x <= 0f || inner.y <= 0f)
                issues.Add($"El margen de '{f.name}' ({f.inset:0.##} m) se come toda su huella.");
            else if (f.shape == FacilityShape.TanqueHorizontal && f.height > Mathf.Min(inner.x, inner.y) + tol)
                issues.Add($"El tanque '{f.name}' (Ø {f.height:0.##} m) no cabe en su losa.");
        }

        var env = environment;
        float plotHalfDiagonal = plotSize.magnitude / 2f;
        if (env.hillsInnerRadius <= plotHalfDiagonal)
            issues.Add($"Los cerros empiezan a {env.hillsInnerRadius:0} m del centro y la parcela llega a {plotHalfDiagonal:0} m: se meterían en ella.");
        if (env.hillsOuterRadius <= env.hillsInnerRadius)
            issues.Add("El radio exterior de los cerros tiene que ser mayor que el interior.");
        if (env.savannaSize / 2f < env.hillsOuterRadius)
            issues.Add("La sabana no llega hasta el borde exterior de los cerros: se vería el vacío por debajo.");
        if (env.hillsMinHeight > env.hillsMaxHeight)
            issues.Add("La altura mínima de los cerros supera a la máxima.");

        return issues;
    }

    bool InsidePlot(Vector2 min, Vector2 max) =>
        min.x >= -0.01f && min.y >= -0.01f && max.x <= plotSize.x + 0.01f && max.y <= plotSize.y + 0.01f;

    /// <summary>Solape estricto de dos rectángulos en planta (tocarse por un borde no cuenta).</summary>
    static bool Overlaps((Vector2 min, Vector2 max) a, (Vector2 min, Vector2 max) b) =>
        a.min.x < b.max.x - 0.01f && b.min.x < a.max.x - 0.01f &&
        a.min.y < b.max.y - 0.01f && b.min.y < a.max.y - 0.01f;
}

public enum PlotSide { Sur, Norte, Este, Oeste }

/// <summary>Pasillo = en el tabique que da a la fila pasillo. Fachada = en el muro exterior de la fila.</summary>
public enum DoorSide { Pasillo, Fachada }

[Serializable]
public class FenceSpec
{
    [Header("Cerca perimetral (malla ciclón)")]
    [Tooltip("Plano: h = 2,5 m.")]
    public float height = 2.5f;
    [Tooltip("Supuesto. Separación máxima entre postes; se reparte uniforme en cada lado.")]
    public float postSpacing = 3f;
    [Tooltip("Supuesto. Lado de la sección de los postes.")]
    public float postSize = 0.08f;
    [Tooltip("Supuesto. Espesor de la malla en el blockout.")]
    public float meshThickness = 0.04f;

    [Header("Portón")]
    [Tooltip("Plano: en la cerca sur.")]
    public PlotSide gateSide = PlotSide.Sur;
    [Tooltip("Medido: X ≈ 100 m. Centro a lo largo del lado, desde la esquina oeste (lados N/S) o sur (lados E/O).")]
    public float gateCenter = 100f;
    [Tooltip("Medido: 8 m, el mismo ancho que la vía de acceso.")]
    public float gateWidth = 8f;
    [Tooltip("Supuesto. Sección de los dos postes del portón.")]
    public float gatePostSize = 0.2f;
    [Tooltip("Supuesto. Espesor de la hoja del portón.")]
    public float gateLeafThickness = 0.08f;
}

[Serializable]
public class BuildingSpec
{
    [Header("Edificio de Operaciones")]
    public string name = "Edificio de Operaciones";
    [Tooltip("Medido: centro de la huella en (95, 51). Fachada principal al sur.")]
    public Vector2 center = new Vector2(95f, 51f);
    [Tooltip("Plano: huella exterior de 66 (X) × 22 (Z) m.")]
    public Vector2 size = new Vector2(66f, 22f);
    [Tooltip("Plano: h = 4,5 m, del suelo a la cara superior de la losa de techo.")]
    public float height = 4.5f;

    [Header("Construcción (supuestos: el plano no los da)")]
    [Tooltip("Losa de piso, apoyada sobre el terreno.")]
    public float floorThickness = 0.15f;
    public float roofThickness = 0.25f;
    [Tooltip("Muro de bloque. Va por dentro de la huella: su cara exterior es el borde de los 66 × 22.")]
    public float exteriorWallThickness = 0.2f;
    [Tooltip("Tabique interior, centrado en el eje entre locales.")]
    public float interiorWallThickness = 0.15f;

    [Header("Acabado de fachada (plano: bloque blanco con franja azul, ventanas horizontales)")]
    [Tooltip("Supuesto. Altura del borde inferior de la franja azul, desde el suelo.")]
    public float stripeBottom = 3.4f;
    [Tooltip("Supuesto. Ancho de la franja.")]
    public float stripeHeight = 0.45f;
    [Tooltip("Supuesto. Lo que sobresale la franja del muro (así no hace z-fighting con él).")]
    public float stripeDepth = 0.03f;
    [Tooltip("Supuesto. Antepecho de las ventanas, desde el piso terminado.")]
    public float windowSill = 1.1f;
    [Tooltip("Supuesto. Alto de las ventanas: con 1,1 + 1 m su dintel queda a la altura del de las puertas (2,1 m).")]
    public float windowHeight = 1f;
    [Tooltip("Supuesto. Espesor del vidrio, centrado en el muro.")]
    public float glassThickness = 0.02f;

    [Header("Equipos de A/A sobre la losa (plano: losa plana con equipos de A/A)")]
    [Tooltip("Supuesto. Cuántos, repartidos en fila a lo largo del eje de la losa.")]
    public int roofUnits = 6;
    [Tooltip("Supuesto. Tamaño de cada equipo: X × alto × Z.")]
    public Vector3 roofUnitSize = new Vector3(1.4f, 1.1f, 0.9f);

    [Header("Distribución")]
    [Tooltip("Filas de NORTE a SUR. Sus fondos deben sumar el fondo del edificio y los anchos de cada fila, su ancho. " +
             "Las cotas de los locales son a ejes de tabique.")]
    public List<RoomRow> rows = new List<RoomRow>
    {
        new RoomRow("Fila norte", 12f, false,
            new Room("Sala de Equipos RF", 26f, "Racks, HPA, LNA", Door.Corridor(12.7f)).WithWindow(13f, 16f),
            new Room("Sala de Control",    20f, "Consolas",        Door.Corridor(9.7f)).WithWindow(10f, 12f),
            new Room("Energía / UPS",      20f, "",                Door.Corridor(9.7f)).WithWindow(10f, 10f)),
        new RoomRow("Pasillo", 3f, true,
            new Room("Pasillo", 66f, "Recorre todo el edificio")),
        new RoomRow("Fila sur", 7f, false,
            new Room("Recepción",         16f, "Entrada principal", Door.Facade(6.1f), Door.Corridor(6.7f)).WithWindow(11.5f, 6f),
            new Room("Oficinas",          18f, "",                  Door.Corridor(8.6f)).WithWindow(9f, 12f),
            new Room("Baños / Cocina",    12f, "",                  Door.Corridor(5.6f)).WithWindow(6f, 6f),
            new Room("Taller / Depósito", 20f, "Acceso taller",     Door.Facade(13f), Door.Corridor(9.7f)).WithWindow(6f, 8f)),
    };

    /// <summary>Altura libre de los muros, entre la losa de piso y la de techo.</summary>
    public float WallHeight => height - floorThickness - roofThickness;

    /// <summary>
    /// Dónde está el pasillo respecto a la fila <paramref name="row"/>: -1 = la fila anterior
    /// (al norte), +1 = la siguiente (al sur), 0 = no colinda con ningún pasillo.
    /// </summary>
    public int CorridorNeighbor(int row)
    {
        if (row > 0 && rows[row - 1].isCorridor) return -1;
        if (row < rows.Count - 1 && rows[row + 1].isCorridor) return +1;
        return 0;
    }
}

[Serializable]
public class RoomRow
{
    public string name;
    [Tooltip("Fondo de la fila (N-S) en metros.")]
    public float depth;
    [Tooltip("Fila que hace de pasillo: las puertas 'Pasillo' de las filas vecinas se abren en el tabique que comparten con ella.")]
    public bool isCorridor;
    [Tooltip("Locales de OESTE a ESTE.")]
    public List<Room> rooms = new List<Room>();

    public RoomRow() { }

    public RoomRow(string name, float depth, bool isCorridor, params Room[] rooms)
    {
        this.name = name;
        this.depth = depth;
        this.isCorridor = isCorridor;
        this.rooms = rooms.ToList();
    }
}

[Serializable]
public class Room
{
    public string name;
    [Tooltip("Plano: ancho (E-O) del local, a ejes de tabique.")]
    public float width;
    [Tooltip("Uso del local según el plano.")]
    public string notes;
    public List<Door> doors = new List<Door>();
    [Tooltip("Supuesto (el plano no dibuja ventanas). Ventanas en el muro de fachada del local: la fila norte da a la fachada norte, la sur a la sur.")]
    public List<Window> windows = new List<Window>();

    public Room() { }

    public Room(string name, float width, string notes, params Door[] doors)
    {
        this.name = name;
        this.width = width;
        this.notes = notes;
        this.doors = doors.ToList();
    }

    public Room WithWindow(float offset, float width)
    {
        windows.Add(new Window { offset = offset, width = width });
        return this;
    }
}

[Serializable]
public class Window
{
    [Tooltip("Centro de la ventana, en metros desde el extremo oeste del local.")]
    public float offset;
    public float width;
}

[Serializable]
public class Door
{
    public DoorSide side;
    [Tooltip("Medido: centro del vano, en metros desde el extremo oeste del local.")]
    public float offset;
    [Tooltip("Plano: 2 m en las dos de fachada. Medido: ≈ 1,2 m en las interiores (radio del arco dibujado).")]
    public float width;
    [Tooltip("Supuesto.")]
    public float height;

    public Door() { }

    public static Door Corridor(float offset) =>
        new Door { side = DoorSide.Pasillo, offset = offset, width = 1.2f, height = 2.1f };

    public static Door Facade(float offset) =>
        new Door { side = DoorSide.Fachada, offset = offset, width = 2f, height = 2.4f };
}

[Serializable]
public class AntennaSpec
{
    public string name;
    [Tooltip("Medido: centro del plato en planta (X, Z).")]
    public Vector2 position;
    [Tooltip("Plano: diámetro del plato.")]
    public float dishDiameter;
    [Tooltip("Plano: altura total aproximada (0 = no rotulada). La usa la sesión de antenas, no el blockout.")]
    public float overallHeight;
    [Tooltip("Plano en las antenas 1 y 2; medido en la 3 y la 4; supuesto en las VSAT (una losa). " +
             "Huella del pedestal de concreto. (0, 0) = sin pedestal.")]
    public Vector2 pedestalSize;
    [Tooltip("El plano no la rotula. En las antenas 1 y 2 está AJUSTADA para que la antena completa dé la " +
             "altura del plano (botón 'Ajustar pedestales'); en las demás es un supuesto.")]
    public float pedestalHeight;
    [Tooltip("Supuesto. Altura del eje de elevación sobre la cara superior del pedestal: plataforma + soporte en Y.")]
    public float mountHeight;

    [Header("Apuntamiento")]
    [Tooltip("Plano: todas apuntan al SUR (arco geoestacionario). Grados desde el norte, en sentido horario.")]
    public float azimuth = 180f;
    [Tooltip("Plano: ≈ 60–70°. Grados sobre el horizonte.")]
    public float elevation = 65f;

    public bool HasPedestal => pedestalSize.x > 0f && pedestalSize.y > 0f && pedestalHeight > 0f;

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
/// así que la misma antena sirve para los 32 m de Camatagua 1 y para los 3,6 m de una VSAT.
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

/// <summary>Lo que está a ras de suelo: pavimentos, estacionamiento y ductos.</summary>
[Serializable]
public class SiteSpec
{
    [Header("Pavimentos")]
    [Tooltip("Supuesto. Espesor del asfalto sobre el terreno.")]
    public float pavingThickness = 0.05f;
    [Tooltip("Medido. Rectángulos en planta, de la esquina SO (mín.) a la NE (máx.).")]
    public List<PavedArea> paving = new List<PavedArea>
    {
        new PavedArea("Vía interna de acceso", new Vector2(0f, 14f),  new Vector2(200f, 22f)),
        new PavedArea("Acceso del portón",     new Vector2(96f, 0f),  new Vector2(104f, 14f)),
    };

    public ParkingSpec parking = new ParkingSpec();

    [Header("Guías de onda y ductos de cables")]
    [Tooltip("Supuesto. Sección del ducto, que va a ras de suelo.")]
    public float ductWidth = 0.6f;
    public float ductHeight = 0.4f;
    [Tooltip("Medido: las líneas discontinuas verde oliva del plano. Las dos primeras salen del eje de su antena, " +
             "pasan por donde el plano empieza a dibujarlas y llegan a la fachada norte (Z = 62).")]
    public List<DuctSpec> ducts = new List<DuctSpec>
    {
        new DuctSpec("Guía de onda · Camatagua 1", new Vector2(50f, 85f),  new Vector2(60f, 69f),  new Vector2(74.7f, 62f)),
        new DuctSpec("Guía de onda · Camatagua 2", new Vector2(120f, 85f), new Vector2(110f, 70f), new Vector2(110f, 62f)),
        new DuctSpec("Ducto del transformador",    new Vector2(158f, 60f), new Vector2(158f, 64f)),
    };
}

[Serializable]
public class PavedArea
{
    public string name;
    public Vector2 min, max;

    public PavedArea() { }
    public PavedArea(string name, Vector2 min, Vector2 max) { this.name = name; this.min = min; this.max = max; }
}

[Serializable]
public class ParkingSpec
{
    [Header("Estacionamiento")]
    [Tooltip("Medido: X 70–102, Z 24–36 (32 × 12 m; el plano no lo rotula).")]
    public Vector2 min = new Vector2(70f, 24f);
    public Vector2 max = new Vector2(102f, 36f);
    [Tooltip("Plano: 8 puestos, en fila a lo largo del borde norte, centrados.")]
    public int stalls = 8;
    [Tooltip("Medido: 3,75 m entre ejes de puesto.")]
    public float stallWidth = 3.75f;
    [Tooltip("Medido: 6 m de fondo.")]
    public float stallDepth = 6f;
    [Tooltip("Medido: los puestos empiezan 1 m por dentro del borde norte.")]
    public float stallSetback = 1f;
    [Tooltip("Supuesto. Ancho de la línea pintada entre puestos.")]
    public float lineWidth = 0.12f;
}

[Serializable]
public class DuctSpec
{
    public string name;
    [Tooltip("Polilínea en planta (X, Z).")]
    public List<Vector2> path = new List<Vector2>();

    public DuctSpec() { }
    public DuctSpec(string name, params Vector2[] path) { this.name = name; this.path = path.ToList(); }
}

/// <summary>
/// Edificio = caja con el material de fachada. Equipo = caja de acero. TanqueVertical = cilindro
/// con Ø = lado menor de la huella. TanqueHorizontal = cilindro tumbado a lo largo del lado mayor,
/// con Ø = la altura.
/// </summary>
public enum FacilityShape { Edificio, Equipo, TanqueVertical, TanqueHorizontal }

[Serializable]
public class FacilitySpec
{
    public string name;
    public FacilityShape shape;
    [Tooltip("Medido: centro de la huella en planta (X, Z).")]
    public Vector2 position;
    [Tooltip("Plano / medido: huella X × Z.")]
    public Vector2 size;
    [Tooltip("Supuesto: altura del cuerpo sobre la losa (en un tanque horizontal, su diámetro).")]
    public float height;
    [Tooltip("Supuesto: losa de concreto bajo el equipo, de toda la huella. 0 = sin losa.")]
    public float padHeight;
    [Tooltip("Supuesto: margen entre el borde de la huella y el equipo. La huella del plano incluye el espacio alrededor.")]
    public float inset;

    public (Vector2 min, Vector2 max) Footprint => (position - size / 2f, position + size / 2f);

    public FacilitySpec() { }

    public FacilitySpec(string name, FacilityShape shape, Vector2 position, Vector2 size,
                        float height, float padHeight, float inset)
    {
        this.name = name;
        this.shape = shape;
        this.position = position;
        this.size = size;
        this.height = height;
        this.padHeight = padHeight;
        this.inset = inset;
    }
}

/// <summary>
/// Lo que rodea la parcela: la sabana y el anillo de cerros del fondo. Nada de esto viene en el
/// plano (solo dice "sabana plana con grama, cerros al fondo"): todo son supuestos, sin precisión
/// métrica, y el centro es el de la parcela.
/// </summary>
[Serializable]
public class EnvironmentSpec
{
    [Header("Sabana (supuestos)")]
    [Tooltip("Lado del suelo exterior, centrado en la parcela.")]
    public float savannaSize = 3400f;
    [Tooltip("Cuánto queda por debajo de la parcela (Y = 0), para que no haya z-fighting con ella.")]
    public float savannaDrop = 0.02f;
    [Tooltip("Metros que cubre cada repetición de la textura de grama (las manchas de verde y paja). " +
             "Grande a propósito: con 6 m la repetición se veía en cuadrícula desde el aire.")]
    public float grassTile = 50f;
    [Tooltip("Metros que cubre cada repetición del grano fino (detail map). Da el detalle de cerca.")]
    public float grassDetailTile = 2.5f;

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
