using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ¿Cabe algo aquí? Lo que se reparte por el terreno (matas, arbustos, piedras) no puede caer en una
/// vía, un edificio, un tanque, una losa, bajo un plato ni sobre la cerca, ni nacer dentro de un
/// tronco. Son las mismas reglas con las que se colocaron los árboles (PhotoTrees). C# puro: lo usan el
/// generador y StationLayout.Validate, así que miden igual.
/// </summary>
public sealed class SiteClearance
{
    readonly StationLayout layout;
    readonly List<(string name, Footprint print)> solids = new List<(string, Footprint)>();
    readonly List<(Vector2 center, float radius, string name)> discs = new List<(Vector2, float, string)>();

    public SiteClearance(StationLayout layout)
    {
        this.layout = layout;
        foreach (var b in layout.buildings) solids.Add((b.name, b.Footprint));
        foreach (var f in layout.facilities) solids.Add((f.name, f.Footprint));
        foreach (var p in layout.site.pads)
            if (p.kind != PadKind.Grama) solids.Add((p.name, p.Footprint));

        // Platos: su sombra en planta (el centro del plato y su radio) y el pedestal, que va detrás.
        foreach (var a in layout.antennas)
        {
            if (a.dishDiameter > 0f) discs.Add((a.position, a.dishDiameter / 2f, a.name));
            if (a.HasPedestal) discs.Add((AntennaGeometry.PedestalPosition(a, layout.antennaDesign), a.PedestalReach, a.name + " (pedestal)"));
        }
        // Troncos: medio metro de radio basta para los modelos de árbol (0,3–0,5 m).
        foreach (var t in layout.trees) discs.Add((t.position, 0.5f, "un tronco"));
    }

    /// <summary>
    /// Qué estorba a algo de radio <paramref name="radius"/> en <paramref name="p"/>, o null si cabe.
    /// <paramref name="onRoads"/> deja caer sobre las vías (no se usa: nada se reparte en ellas).
    /// </summary>
    public string Blocker(Vector2 p, float radius, bool onRoads = false)
    {
        var fence = layout.fence;
        if (PlanGeometry.DistanceToOutline(fence.outline, p) < radius + fence.postSize)
            return "la cerca";
        foreach (var (name, print) in solids)
            if (print.Distance(p) < radius) return name;
        foreach (var (center, r, name) in discs)
            if ((p - center).sqrMagnitude < (r + radius) * (r + radius)) return name;
        if (!onRoads)
            foreach (var road in layout.site.roads)
                if (DistanceToRoad(road, p) < road.width / 2f + radius) return road.name;
        return null;
    }

    public bool IsClear(Vector2 p, float radius) => Blocker(p, radius) == null;

    public static float DistanceToRoad(RoadSpec road, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 1; i < road.path.Count; i++)
            best = Mathf.Min(best, PlanGeometry.DistanceToSegment(p, road.path[i - 1], road.path[i]));
        return road.path.Count == 1 ? Vector2.Distance(p, road.path[0]) : best;
    }
}
