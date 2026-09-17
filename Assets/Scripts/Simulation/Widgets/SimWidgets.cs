using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ─────────────────────────────────────────────────────────────────────────────
// Instrumentos del simulador dibujados de forma NATIVA con Painter2D (UI Toolkit).
// Paleta clara azul (coherente con el resto de la app), con acentos ámbar/rojo
// para alertas. Nada de sprites externos.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Colores y utilidades de dibujo compartidas.</summary>
public static class SimPalette
{
    public static readonly Color Blue   = new Color(0.122f, 0.471f, 0.969f);
    public static readonly Color Navy   = new Color(0.071f, 0.161f, 0.290f);
    public static readonly Color Grid   = new Color(0.788f, 0.859f, 0.949f);
    public static readonly Color GridSoft = new Color(0.855f, 0.906f, 0.965f);
    public static readonly Color Amber  = new Color(0.941f, 0.627f, 0.125f);
    public static readonly Color Red    = new Color(0.898f, 0.282f, 0.302f);
    public static readonly Color Green  = new Color(0.086f, 0.749f, 0.537f);
    public static readonly Color Track  = new Color(0.820f, 0.878f, 0.953f);

    /// <summary>Dibuja un arco como polilínea (evita la API de Angle). Ángulos en grados, y hacia arriba.</summary>
    public static void ArcStrip(Painter2D p, float cx, float cy, float r, float aStart, float aEnd, int seg = 48)
    {
        p.BeginPath();
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Deg2Rad * Mathf.Lerp(aStart, aEnd, i / (float)seg);
            var pt = new Vector2(cx + r * Mathf.Cos(a), cy - r * Mathf.Sin(a));
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.Stroke();
    }

    /// <summary>Círculo relleno pequeño (para puntos de constelación).</summary>
    public static void FillDot(Painter2D p, float cx, float cy, float r, int seg = 10)
    {
        p.BeginPath();
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Deg2Rad * (360f * i / seg);
            var pt = new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a));
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.ClosePath();
        p.Fill();
    }

    public static void Line(Painter2D p, float x1, float y1, float x2, float y2)
    {
        p.BeginPath(); p.MoveTo(new Vector2(x1, y1)); p.LineTo(new Vector2(x2, y2)); p.Stroke();
    }
}

// ═══════════════════════════ GAUGE CIRCULAR ═══════════════════════════
public class GaugeElement : VisualElement
{
    private float _min = 0f, _max = 1f, _value = 0f;
    private Color _color = SimPalette.Blue;
    private readonly Label _valLabel = new Label("—");
    private readonly Label _capLabel = new Label("");

    private const float A_START = 225f;   // grados
    private const float A_SWEEP = 270f;

    public GaugeElement(string caption)
    {
        AddToClassList("sim-gauge");
        _valLabel.AddToClassList("sim-gauge-value");
        _valLabel.style.position = Position.Absolute;
        _valLabel.style.left = 0; _valLabel.style.right = 0;
        _valLabel.style.top = Length.Percent(34);
        _valLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        _capLabel.text = caption;
        _capLabel.AddToClassList("sim-gauge-cap");
        _capLabel.style.position = Position.Absolute;
        _capLabel.style.left = 0; _capLabel.style.right = 0;
        _capLabel.style.bottom = 4;
        _capLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        Add(_valLabel);
        Add(_capLabel);
        generateVisualContent += OnPaint;
    }

    public void Configure(float min, float max) { _min = min; _max = max; }

    public void SetValue(float value, string display, Color color)
    {
        _value = value; _color = color;
        _valLabel.text = display;
        MarkDirtyRepaint();
    }

    private void OnPaint(MeshGenerationContext ctx)
    {
        float w = contentRect.width, h = contentRect.height;
        if (w < 4 || h < 4) return;
        var p = ctx.painter2D;
        p.lineCap = LineCap.Round;
        p.lineJoin = LineJoin.Round;

        float cx = w * 0.5f;
        float cy = h * 0.60f;
        float r  = Mathf.Min(w * 0.5f, h * 0.60f) - 10f;
        if (r < 4) return;

        float f = Mathf.Clamp01((_value - _min) / Mathf.Max(_max - _min, 1e-6f));
        float stroke = Mathf.Clamp(r * 0.17f, 8f, 22f);   // el trazo acompaña al tamaño del gauge
        int seg = r > 70f ? 96 : 48;
        float aEnd   = A_START - A_SWEEP;
        float aValue = A_START - A_SWEEP * f;

        // La pista y el valor se dibujan como tramos CONTIGUOS, no superpuestos: al apilar dos
        // arcos gruesos del mismo radio, las costuras de la teselación del de abajo (color casi
        // blanco) se transparentaban a través del de arriba y salpicaban el arco de puntos claros.
        p.lineWidth = stroke;
        if (f < 0.999f)
        {
            p.strokeColor = SimPalette.Track;
            SimPalette.ArcStrip(p, cx, cy, r, aValue, aEnd, Mathf.Max(8, Mathf.RoundToInt(seg * (1f - f))));
        }
        if (f > 0.001f)
        {
            p.strokeColor = _color;
            SimPalette.ArcStrip(p, cx, cy, r, A_START, aValue, Mathf.Max(8, Mathf.RoundToInt(seg * f)));
        }
    }
}

// ═══════════════════════════ ANALIZADOR DE ESPECTRO ═══════════════════════════
public class SpectrumElement : VisualElement
{
    private float _noise = 0.15f;    // 0..1 piso de ruido
    private float _carrier = 0.8f;   // 0..1 altura de portadora
    private Color _trace = SimPalette.Blue;

    public SpectrumElement()
    {
        AddToClassList("sim-screen");
        generateVisualContent += OnPaint;
        schedule.Execute(MarkDirtyRepaint).Every(90); // animación del ruido
    }

    public void SetLevels(float noise01, float carrier01, Color trace)
    {
        _noise = Mathf.Clamp01(noise01);
        _carrier = Mathf.Clamp01(carrier01);
        _trace = trace;
    }

    private void OnPaint(MeshGenerationContext ctx)
    {
        float w = contentRect.width, h = contentRect.height;
        if (w < 4 || h < 4) return;
        var p = ctx.painter2D;

        // rejilla tipo osciloscopio
        p.lineWidth = 1f; p.strokeColor = SimPalette.GridSoft;
        for (int i = 1; i < 5; i++) SimPalette.Line(p, 0, h * i / 5f, w, h * i / 5f);
        for (int i = 1; i < 8; i++) SimPalette.Line(p, w * i / 8f, 0, w * i / 8f, h);

        float baseY = h - 6f;
        float usable = h - 12f;
        float cxN = w * 0.5f;         // portadora al centro
        float sigma = w * 0.05f;

        p.lineWidth = 2f; p.strokeColor = _trace;
        p.BeginPath();
        int N = Mathf.Max(24, (int)(w / 4f));
        for (int i = 0; i <= N; i++)
        {
            float x = w * i / N;
            float noise = _noise * 0.28f * usable * Random.value;
            float d = (x - cxN) / sigma;
            float peak = _carrier * usable * Mathf.Exp(-0.5f * d * d);
            float y = baseY - noise - peak;
            var pt = new Vector2(x, y);
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.Stroke();
    }
}

// ═══════════════════════════ CONSTELACIÓN QPSK/QAM ═══════════════════════════
public class ConstellationElement : VisualElement
{
    private Modulation _mod = Modulation.QPSK;
    private float _dispersion = 0.1f;   // sigma relativa
    private bool  _lost = false;
    private List<Vector2> _ideal = new List<Vector2>();

    public ConstellationElement()
    {
        AddToClassList("sim-screen");
        Rebuild(Modulation.QPSK);
        generateVisualContent += OnPaint;
        schedule.Execute(MarkDirtyRepaint).Every(120); // re-dispersión
    }

    public void SetState(Modulation mod, float dispersion, bool lost)
    {
        if (mod != _mod) Rebuild(mod);
        _dispersion = Mathf.Max(0f, dispersion);
        _lost = lost;
    }

    private void Rebuild(Modulation mod)
    {
        _mod = mod;
        _ideal = IdealPoints(mod);
    }

    private static List<Vector2> IdealPoints(Modulation mod)
    {
        var pts = new List<Vector2>();
        if (mod == Modulation.BPSK) { pts.Add(new Vector2(-1, 0)); pts.Add(new Vector2(1, 0)); return pts; }
        if (mod == Modulation.QPSK)
        {
            pts.Add(new Vector2(-1, -1)); pts.Add(new Vector2(-1, 1));
            pts.Add(new Vector2(1, -1));  pts.Add(new Vector2(1, 1));
            return pts;
        }
        int side = mod == Modulation.QAM16 ? 4 : 8;     // 16-QAM=4x4, 64-QAM=8x8
        float maxLevel = side - 1f;                      // p.ej. 3 o 7
        for (int r = 0; r < side; r++)
            for (int c = 0; c < side; c++)
            {
                float x = (2 * c - (side - 1)) / maxLevel;
                float y = (2 * r - (side - 1)) / maxLevel;
                pts.Add(new Vector2(x, y));
            }
        return pts;
    }

    private void OnPaint(MeshGenerationContext ctx)
    {
        float w = contentRect.width, h = contentRect.height;
        if (w < 4 || h < 4) return;
        var p = ctx.painter2D;

        float cx = w * 0.5f, cy = h * 0.5f;
        float halfW = w * 0.42f, halfH = h * 0.42f;

        // ejes I/Q
        p.lineWidth = 1f; p.strokeColor = SimPalette.Grid;
        SimPalette.Line(p, cx, 6, cx, h - 6);
        SimPalette.Line(p, 6, cy, w - 6, cy);

        Color dot = _lost ? SimPalette.Red : SimPalette.Blue;
        p.fillColor = dot;

        float sigmaPx = (_lost ? 0.7f : _dispersion * 0.22f) * Mathf.Min(halfW, halfH);
        int perCluster = _lost ? 26 : 34;
        float rDot = _mod == Modulation.QAM64 ? 1.6f : 2.1f;

        foreach (var ip in _ideal)
        {
            float bx = cx + ip.x * halfW;
            float by = cy - ip.y * halfH;
            for (int k = 0; k < perCluster; k++)
            {
                // ruido gaussiano (Box-Muller)
                float u1 = Mathf.Max(1e-4f, Random.value), u2 = Random.value;
                float mag = Mathf.Sqrt(-2f * Mathf.Log(u1));
                float gx = mag * Mathf.Cos(2f * Mathf.PI * u2);
                float gy = mag * Mathf.Sin(2f * Mathf.PI * u2);
                float x = bx + gx * sigmaPx;
                float y = by + gy * sigmaPx;
                if (x < 2 || x > w - 2 || y < 2 || y > h - 2) continue;
                SimPalette.FillDot(p, x, y, rDot);
            }
        }
    }
}
