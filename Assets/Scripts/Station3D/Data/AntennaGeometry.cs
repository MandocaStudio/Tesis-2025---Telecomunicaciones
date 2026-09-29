using UnityEngine;

/// <summary>
/// Geometría de la antena tipo (montura azimut-elevación + paraboloide Cassegrain) en función de
/// su diámetro. C# puro, como LinkBudgetModel: la usan StationGenerator para construir y
/// StationLayout.Validate para medir, así que los dos calculan exactamente igual.
///
/// Marco de elevación: origen en el eje de elevación, +Z = dirección de apuntamiento,
/// X = el propio eje. Un punto (x, y, z) de ese marco queda a y·cos(el) + z·sin(el) por encima
/// del eje cuando la antena mira a <c>el</c> grados sobre el horizonte.
/// </summary>
public static class AntennaGeometry
{
    /// <summary>Distancia focal f = (f/D)·D.</summary>
    public static float FocalLength(AntennaSpec a, AntennaDesign d) => d.focalRatio * a.dishDiameter;

    /// <summary>
    /// Profundidad del plato, R² / (4f). Es la misma relación del módulo 2D al revés:
    /// f = R² / (4·profundidad), y ahí va el subreflector.
    /// </summary>
    public static float Depth(AntennaSpec a, AntennaDesign d)
    {
        float r = a.dishDiameter / 2f;
        return r * r / (4f * FocalLength(a, d));
    }

    /// <summary>Distancia del eje de elevación al vértice del plato.</summary>
    public static float VertexOffset(AntennaSpec a, AntennaDesign d) => d.vertexOffset * a.dishDiameter;

    /// <summary>Altura del eje de elevación sobre el suelo.</summary>
    public static float AxisHeight(AntennaSpec a) => (a.HasPedestal ? a.pedestalHeight : 0f) + a.mountHeight;

    /// <summary>Punto más alto de la antena sobre su eje: el borde superior del plato o el del subreflector.</summary>
    public static float TopAboveAxis(AntennaSpec a, AntennaDesign d)
    {
        float s = Mathf.Sin(a.elevation * Mathf.Deg2Rad), c = Mathf.Cos(a.elevation * Mathf.Deg2Rad);
        float h = VertexOffset(a, d), f = FocalLength(a, d);

        float rimTop = a.dishDiameter / 2f * c + (h + Depth(a, d)) * s;

        float subR = d.subreflectorDiameter * a.dishDiameter / 2f;
        float subDepth = subR * subR / (4f * d.subreflectorFocalRatio * d.subreflectorDiameter * a.dishDiameter);
        float subTop = subR * c + (h + f + subDepth) * s;

        return Mathf.Max(rimTop, subTop);
    }

    /// <summary>
    /// Punto más bajo del plato respecto a su eje (negativo = por debajo). Sobre el meridiano
    /// inferior del paraboloide la altura es −r·cos(el) + (h + r²/4f)·sin(el), con mínimo en
    /// r = 2f / tan(el) — que puede caer dentro del plato, no necesariamente en el borde.
    /// </summary>
    public static float LowestAboveAxis(AntennaSpec a, AntennaDesign d)
    {
        float s = Mathf.Sin(a.elevation * Mathf.Deg2Rad), c = Mathf.Cos(a.elevation * Mathf.Deg2Rad);
        float h = VertexOffset(a, d), f = FocalLength(a, d), rim = a.dishDiameter / 2f;
        float Height(float r) => -r * c + (h + r * r / (4f * f)) * s;

        float rMin = s > 0f ? Mathf.Clamp(2f * f * c / s, 0f, rim) : rim;
        return Mathf.Min(Height(rMin), Height(rim));
    }

    /// <summary>Altura total de la antena, del suelo a su punto más alto.</summary>
    public static float OverallHeight(AntennaSpec a, AntennaDesign d) => AxisHeight(a) + TopAboveAxis(a, d);

    /// <summary>Altura de pedestal con la que la antena mide <paramref name="overall"/> metros.</summary>
    public static float PedestalHeightFor(AntennaSpec a, AntennaDesign d, float overall) =>
        overall - a.mountHeight - TopAboveAxis(a, d);
}
