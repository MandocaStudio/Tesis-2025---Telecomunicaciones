using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Geometría en planta (X, Z) del plano de conjunto. C# puro, como AntennaGeometry: la usan
/// StationGenerator para construir y StationLayout.Validate para comprobar, así que los dos miden
/// exactamente igual.
///
/// Giros: grados en sentido HORARIO visto desde arriba, que es a la vez el sentido del
/// <c>rotate()</c> del SVG del plano y el de la Y de Unity. Así el número del plano va tal cual.
/// </summary>
public static class PlanGeometry
{
    /// <summary>Punto dentro de un polígono simple (regla par-impar). El borde cuenta como dentro.</summary>
    public static bool Contains(IList<Vector2> polygon, Vector2 p, float tolerance = 0.01f)
    {
        if (DistanceToOutline(polygon, p) <= tolerance) return true;
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>Distancia de un punto al contorno cerrado del polígono.</summary>
    public static float DistanceToOutline(IList<Vector2> polygon, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < polygon.Count; i++)
            best = Mathf.Min(best, DistanceToSegment(p, polygon[i], polygon[(i + 1) % polygon.Count]));
        return best;
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>Lado del polígono (de polygon[i] a polygon[i+1]) más cercano al punto.</summary>
    public static int NearestEdge(IList<Vector2> polygon, Vector2 p)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < polygon.Count; i++)
        {
            float d = DistanceToSegment(p, polygon[i], polygon[(i + 1) % polygon.Count]);
            if (d < bestDistance) { bestDistance = d; best = i; }
        }
        return best;
    }

    /// <summary>Área con signo: positiva si los vértices van en sentido antihorario visto desde arriba.</summary>
    public static float SignedArea(IList<Vector2> polygon)
    {
        float a = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p = polygon[i], q = polygon[(i + 1) % polygon.Count];
            a += p.x * q.y - q.x * p.y;
        }
        return a / 2f;
    }

    public static Rect Bounds(IList<Vector2> points)
    {
        if (points.Count == 0) return default;
        Vector2 min = points[0], max = points[0];
        foreach (var p in points) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>
    /// Triangula un polígono simple (convexo o no) por recorte de orejas. Devuelve índices de tres en
    /// tres, en el orden en que vienen los vértices; quien construye la malla decide la cara.
    /// </summary>
    public static List<int> Triangulate(IList<Vector2> polygon)
    {
        var result = new List<int>();
        var idx = new List<int>();
        for (int i = 0; i < polygon.Count; i++) idx.Add(i);
        float sign = Mathf.Sign(SignedArea(polygon));

        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool clipped = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                Vector2 a = polygon[ia], b = polygon[ib], c = polygon[ic];
                if (Cross(b - a, c - b) * sign <= 0f) continue; // vértice reflejo: no es oreja

                bool empty = true;
                foreach (int j in idx)
                    if (j != ia && j != ib && j != ic && InTriangle(polygon[j], a, b, c)) { empty = false; break; }
                if (!empty) continue;

                result.Add(ia); result.Add(ib); result.Add(ic);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break; // polígono degenerado: lo que quede se deja sin cubrir
        }
        if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
        return result;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }
}

/// <summary>
/// Rectángulo en planta, girado sobre su centro: la huella de un edificio o de una losa.
/// Ejes locales: +X = "este" del edificio y +Z = "norte" del edificio, girados <see cref="rotation"/>
/// grados en sentido horario.
/// </summary>
public readonly struct Footprint
{
    public readonly Vector2 center, size;
    public readonly float rotation;

    public Footprint(Vector2 center, Vector2 size, float rotation)
    {
        this.center = center; this.size = size; this.rotation = rotation;
    }

    /// <summary>Hacia dónde apunta el +X local en el mundo (la misma cuenta que Quaternion.Euler(0, rot, 0)).</summary>
    public Vector2 AxisX
    {
        get { float r = rotation * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), -Mathf.Sin(r)); }
    }

    public Vector2 AxisZ
    {
        get { float r = rotation * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(r), Mathf.Cos(r)); }
    }

    public Vector2 ToWorld(Vector2 local) => center + AxisX * local.x + AxisZ * local.y;

    public Vector2 ToLocal(Vector2 world)
    {
        var d = world - center;
        return new Vector2(Vector2.Dot(d, AxisX), Vector2.Dot(d, AxisZ));
    }

    public Vector2[] Corners()
    {
        Vector2 h = size / 2f;
        return new[]
        {
            ToWorld(new Vector2(-h.x, -h.y)), ToWorld(new Vector2(h.x, -h.y)),
            ToWorld(new Vector2(h.x, h.y)), ToWorld(new Vector2(-h.x, h.y)),
        };
    }

    public bool Contains(Vector2 world, float tolerance = 0f)
    {
        var l = ToLocal(world);
        return Mathf.Abs(l.x) <= size.x / 2f + tolerance && Mathf.Abs(l.y) <= size.y / 2f + tolerance;
    }

    /// <summary>Distancia de un punto a la huella (0 si está dentro).</summary>
    public float Distance(Vector2 world)
    {
        var l = ToLocal(world);
        float dx = Mathf.Max(0f, Mathf.Abs(l.x) - size.x / 2f), dz = Mathf.Max(0f, Mathf.Abs(l.y) - size.y / 2f);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// Solape estricto entre dos huellas (ejes separadores). Tocarse por un borde, o meterse menos
    /// de <paramref name="tolerance"/>, no cuenta: dos alas del mismo edificio se tocan sin solaparse.
    /// </summary>
    public bool Overlaps(Footprint other, float tolerance = 0.05f)
    {
        var a = Corners();
        var b = other.Corners();
        foreach (var axis in new[] { AxisX, AxisZ, other.AxisX, other.AxisZ })
        {
            float aMin = float.MaxValue, aMax = float.MinValue, bMin = float.MaxValue, bMax = float.MinValue;
            foreach (var p in a) { float d = Vector2.Dot(p, axis); aMin = Mathf.Min(aMin, d); aMax = Mathf.Max(aMax, d); }
            foreach (var p in b) { float d = Vector2.Dot(p, axis); bMin = Mathf.Min(bMin, d); bMax = Mathf.Max(bMax, d); }
            if (aMax <= bMin + tolerance || bMax <= aMin + tolerance) return false;
        }
        return true;
    }
}
