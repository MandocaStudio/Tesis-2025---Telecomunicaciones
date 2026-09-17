using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ─────────────────────────────────────────────────────────────────────────────
// Escena 2D del enlace (apartado superior), estilo ESPACIO EXTERIOR:
// fondo estrellado, el planeta Tierra como GLOBO centrado abajo, el satélite GEO sobre él
// y una ANTENA PARABÓLICA en cada extremo del enlace, apoyadas sobre el limbo del planeta
// y apuntando al satélite. Haces sinusoidales brillantes (uplink cian, downlink verde),
// nube de lluvia según el clima y etiquetas flotantes Eb/N0 · BER · Lp.
//
// GEOMETRÍA: el globo se dimensiona a partir del ALTO del visor (no del ancho), porque el
// visor es mucho más ancho que alto; con el radio atado al ancho el planeta se salía de
// cuadro y las antenas caían fuera de pantalla. El polo visible queda siempre al 34% del
// alto y las estaciones sobre el limbo a ±65°, de modo que todo entra sea cual sea el tamaño.
//
// ANTENAS: se dibujan en ESCORZO (elipse de apertura + cuenco detrás), no como perfil
// parabólico de canto: el perfil exacto es geométricamente correcto pero a este tamaño se
// lee como una astilla, mientras que la elipse es la silueta que se reconoce como parabólica.
// ─────────────────────────────────────────────────────────────────────────────
public class LinkSceneElement : VisualElement
{
    // Paleta del espacio
    private static readonly Color Space   = new Color(0.027f, 0.047f, 0.110f);
    private static readonly Color Ocean   = new Color(0.086f, 0.278f, 0.525f);
    private static readonly Color OceanDk = new Color(0.031f, 0.114f, 0.251f);
    private static readonly Color Land    = new Color(0.239f, 0.510f, 0.318f);
    private static readonly Color LandDk  = new Color(0.169f, 0.400f, 0.251f);
    private static readonly Color Atmo    = new Color(0.498f, 0.784f, 1.000f);
    private static readonly Color Metal   = new Color(0.878f, 0.914f, 0.969f);
    private static readonly Color MetalMd = new Color(0.694f, 0.745f, 0.827f);
    private static readonly Color MetalDk = new Color(0.416f, 0.478f, 0.588f);
    private static readonly Color Panel   = new Color(0.208f, 0.510f, 0.898f);
    private static readonly Color PanelDk = new Color(0.075f, 0.220f, 0.478f);
    private static readonly Color Cyan    = new Color(0.271f, 0.780f, 0.941f);
    private static readonly Color GreenB  = new Color(0.373f, 0.878f, 0.616f);
    private static readonly Color Lost    = new Color(0.933f, 0.353f, 0.365f);

    // Layout normalizado — lo comparten el pintado y las etiquetas flotantes.
    private const float SatY        = 0.15f;   // altura del satélite (0..1 del alto)
    private const float StationDeg  = 65f;     // estaciones sobre el limbo, a ±65° del polo
    private const float EarthTopY   = 0.34f;   // el polo visible del globo, al 34% del alto
    private const float EarthRadH   = 0.95f;   // radio = alto · K …
    private const float EarthRadW   = 0.30f;   // … acotado por ancho · K (visores muy anchos)
    private const float DishFlatten = 0.34f;   // achatamiento de la elipse de apertura

    private float _phase;
    private int   _weather;
    private float _intensity = 1f;
    private bool  _lost;

    private readonly List<Vector3> _stars = new List<Vector3>(); // x,y (norm), tamaño
    private readonly Label _lblEbN0 = new Label();
    private readonly Label _lblBer  = new Label();
    private readonly Label _lblLp   = new Label();
    private readonly Label _capSat, _capStation, _capVsat;

    public LinkSceneElement()
    {
        AddToClassList("sim-scene-canvas");
        style.backgroundColor = Space;

        BuildStars(150);

        var info = new VisualElement();
        info.AddToClassList("sim-scene-info");
        info.style.position = Position.Absolute;
        info.style.top = 14;
        info.style.right = 14;
        _lblEbN0.AddToClassList("sim-scene-metric");
        _lblBer.AddToClassList("sim-scene-metric");
        _lblLp.AddToClassList("sim-scene-metric");
        info.Add(_lblEbN0); info.Add(_lblBer); info.Add(_lblLp);
        Add(info);

        _capSat     = Caption("SATÉLITE GEO · Transpondedor (VENESAT-1)");
        _capStation = Caption("ESTACIÓN TERRENA · Bárbula (Uplink)");
        _capVsat    = Caption("RECEPTOR VSAT (Downlink)");

        generateVisualContent += OnPaint;
        RegisterCallback<GeometryChangedEvent>(_ => PlaceCaptions());
        schedule.Execute(() => { _phase += 0.28f; MarkDirtyRepaint(); }).Every(40);
    }

    private void BuildStars(int n)
    {
        var prev = Random.state;
        Random.InitState(20240620);
        for (int i = 0; i < n; i++)
            _stars.Add(new Vector3(Random.value, Random.value, Random.Range(0.6f, 1.8f)));
        Random.state = prev;
    }

    private Label Caption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("sim-scene-caption");
        l.style.position = Position.Absolute;
        Add(l);
        return l;
    }

    private void PlaceCaptions()
    {
        float w = contentRect.width, h = contentRect.height;
        if (w < 8 || h < 8) return;
        float s = IconScale(h);

        _capSat.style.left = w * 0.5f;
        _capSat.style.top = h * SatY - 52f * s;
        _capSat.style.translate = new Translate(Length.Percent(-50), 0);

        // Los rótulos de las estaciones se anclan HACIA AFUERA del globo. Centrados sobre la
        // antena, su mitad interior caía sobre el azul del planeta: la antena se apoya en el
        // limbo, y hacia el centro el limbo sube, así que ese lado queda sobre la Tierra.
        var st = SurfaceAt(w, h, -StationDeg);
        var vs = SurfaceAt(w, h, StationDeg);

        _capStation.style.left = st.x - 18f * s;
        _capStation.style.top = st.y - 66f * s;
        _capStation.style.translate = new Translate(Length.Percent(-100), 0);

        _capVsat.style.left = vs.x + 18f * s;
        _capVsat.style.top = vs.y - 66f * s;
        _capVsat.style.translate = new Translate(0, 0);
    }

    public void SetState(Color quality, float intensity, bool lost, int weather,
                         string ebn0, string ber, string lp)
    {
        _intensity = Mathf.Clamp01(intensity);
        _lost = lost; _weather = weather;
        _lblEbN0.text = ebn0; _lblBer.text = ber; _lblLp.text = lp;
        MarkDirtyRepaint();
    }

    // ───────────────────────── geometría del planeta ─────────────────────────
    private static float IconScale(float h) => Mathf.Clamp(h / 380f, 0.85f, 2f);

    private static float EarthRadius(float w, float h) => Mathf.Min(h * EarthRadH, w * EarthRadW);

    private static Vector2 EarthCenter(float w, float h)
        => new Vector2(w * 0.5f, h * EarthTopY + EarthRadius(w, h));

    /// <summary>Dirección radial (normal a la superficie) para un ángulo medido desde el polo.</summary>
    private static Vector2 Radial(float angleDeg)
    {
        float a = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
    }

    /// <summary>Punto de la superficie a un ángulo dado desde el polo visible.</summary>
    private static Vector2 SurfaceAt(float w, float h, float angleDeg)
        => EarthCenter(w, h) + Radial(angleDeg) * EarthRadius(w, h);

    // ───────────────────────── pintado ─────────────────────────
    private void OnPaint(MeshGenerationContext ctx)
    {
        float w = contentRect.width, h = contentRect.height;
        if (w < 8 || h < 8) return;
        var p = ctx.painter2D;
        p.lineJoin = LineJoin.Round;
        p.lineCap = LineCap.Round;

        float s = IconScale(h);
        var sat      = new Vector2(w * 0.5f, h * SatY);
        var stBase   = SurfaceAt(w, h, -StationDeg);
        var vsatBase = SurfaceAt(w, h, StationDeg);

        var stDish   = MakeDish(stBase, Radial(-StationDeg), sat, s);
        var vsatDish = MakeDish(vsatBase, Radial(StationDeg), sat, s);

        DrawStars(p, w, h);
        DrawEarth(p, w, h);
        DrawGlow(p, sat, s);

        DrawBeam(p, stDish.focus, sat, +1, Cyan, s);
        DrawBeam(p, sat, vsatDish.focus, -1, GreenB, s);

        DrawDish(p, stBase, stDish, s);
        DrawDish(p, vsatBase, vsatDish, s);
        DrawSatellite(p, sat, s);

        if (_weather > 0) DrawRain(p, Vector2.Lerp(sat, vsatDish.focus, 0.5f), s);
    }

    private void DrawStars(Painter2D p, float w, float h)
    {
        for (int i = 0; i < _stars.Count; i++)
        {
            var st = _stars[i];
            float tw = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(_phase * 0.05f + i * 0.7f));
            p.fillColor = new Color(1f, 1f, 1f, tw * 0.85f);
            SimPalette.FillDot(p, st.x * w, st.y * h, st.z, 6);
        }
    }

    private void DrawGlow(Painter2D p, Vector2 c, float s)
    {
        for (int g = 4; g >= 1; g--)
        {
            p.fillColor = new Color(0.45f, 0.68f, 1f, 0.04f * g);
            SimPalette.FillDot(p, c.x, c.y, 20f * g * s, 28);
        }
    }

    private void DrawEarth(Painter2D p, float w, float h)
    {
        float R = EarthRadius(w, h);
        var c = EarthCenter(w, h);

        // halo de atmósfera (capas de fuera hacia dentro)
        for (int i = 5; i >= 1; i--)
        {
            p.lineWidth = 4f * i;
            p.strokeColor = new Color(Atmo.r, Atmo.g, Atmo.b, 0.05f);
            SimPalette.ArcStrip(p, c.x, c.y, R + 2.5f * i, 0f, 360f, 120);
        }

        p.fillColor = Ocean;
        SimPalette.FillDot(p, c.x, c.y, R, 160);

        // Continentes: manchas IRREGULARES (no círculos) en coordenadas polares sobre el globo.
        // Posiciones verificadas contra TRES invariantes — si se tocan cualquiera de ellas al
        // moverlas, vuelven los defectos que ya aparecieron:
        //   1. VISIBLE   dist·cos(ángulo) > 0.29  → cae dentro del casquete que se ve.
        //   2. CONTENIDA dist + radio·1.45 < 1    → no se sale del planeta (1.45 = pico del
        //                                            contorno modulado de DrawLandmass).
        //   3. SEPARADA  distancia entre centros > suma de radios·1.45 → no se funden en una
        //                                            única masa verde.
        // Tierra COSTERA bajo cada antena: las estaciones se apoyan justo en el limbo (dist=1),
        // donde una mancha normal nunca puede llegar sin salirse del planeta. Esta va limitada
        // por fuera por el propio limbo, así que toca el pie de la antena y por construcción no
        // puede desbordar el globo. Centradas en ±65° = el ángulo exacto de las antenas.
        DrawCoastalLand(p, c, R, -StationDeg, 22f, 0.24f,  7, Land);
        DrawCoastalLand(p, c, R,  StationDeg, 22f, 0.24f, 13, Land);

        DrawLandmass(p, c, R,   0f, 0.52f, 0.180f,  5, LandDk);
        DrawLandmass(p, c, R, -30f, 0.82f, 0.100f, 11, Land);
        DrawLandmass(p, c, R,  30f, 0.82f, 0.100f, 17, LandDk);

        // Volumen de la esfera: luz arriba-izquierda, sombra abajo-derecha.
        p.fillColor = new Color(1f, 1f, 1f, 0.06f);
        SimPalette.FillDot(p, c.x - R * 0.22f, c.y - R * 0.30f, R * 0.50f, 60);
        p.fillColor = new Color(OceanDk.r, OceanDk.g, OceanDk.b, 0.30f);
        SimPalette.FillDot(p, c.x + R * 0.34f, c.y + R * 0.34f, R * 0.48f, 60);

        // limbo brillante
        p.lineWidth = 2.5f;
        p.strokeColor = new Color(Atmo.r, Atmo.g, Atmo.b, 0.9f);
        SimPalette.ArcStrip(p, c.x, c.y, R, 0f, 360f, 140);
    }

    // Franja de tierra pegada al limbo: por fuera la limita el propio borde del planeta y por
    // dentro una costa irregular. Es la única forma de tener verde EN el limbo (donde se apoyan
    // las antenas) sin riesgo de desbordar el globo: el radio nunca supera R por construcción.
    private static void DrawCoastalLand(Painter2D p, Vector2 globe, float R, float centreDeg,
                                        float halfSpanDeg, float depth, int seed, Color col)
    {
        const int N = 30;
        float a0 = centreDeg - halfSpanDeg, a1 = centreDeg + halfSpanDeg;

        p.fillColor = col;
        p.BeginPath();
        for (int i = 0; i <= N; i++)                       // borde exterior = limbo exacto
        {
            var pt = globe + Radial(Mathf.Lerp(a0, a1, i / (float)N)) * R;
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        for (int i = N; i >= 0; i--)                       // vuelta por la costa, hacia dentro
        {
            float t = i / (float)N;
            // El seno se anula en los extremos, así que la costa cierra sobre el limbo y la
            // franja se funde con el borde en vez de cortarse en seco.
            float taper = Mathf.Sin(Mathf.PI * t);
            float wob = 1f + 0.28f * Mathf.Sin(t * 9f + seed) + 0.16f * Mathf.Sin(t * 17f + seed * 1.7f);
            p.LineTo(globe + Radial(Mathf.Lerp(a0, a1, t)) * (R * (1f - depth * taper * wob)));
        }
        p.ClosePath();
        p.Fill();
    }

    // Mancha de tierra con contorno irregular (radio modulado por tres armónicas).
    private static void DrawLandmass(Painter2D p, Vector2 globe, float R, float angleDeg,
                                     float dist, float rad, int seed, Color col)
    {
        var centre = globe + Radial(angleDeg) * (R * dist);
        float r = R * rad;
        p.fillColor = col;
        p.BeginPath();
        const int N = 30;
        for (int i = 0; i <= N; i++)
        {
            float a = i / (float)N * Mathf.PI * 2f;
            float k = 1f
                    + 0.22f * Mathf.Sin(a * 3f + seed)
                    + 0.14f * Mathf.Sin(a * 5f + seed * 2.3f)
                    + 0.08f * Mathf.Sin(a * 8f + seed * 0.7f);
            var pt = new Vector2(centre.x + Mathf.Cos(a) * r * k,
                                 centre.y + Mathf.Sin(a) * r * k * 0.72f);
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.ClosePath();
        p.Fill();
    }

    // ───────────────────────── antena parabólica ─────────────────────────
    private struct DishGeom
    {
        public Vector2 centre;  // centro de la apertura
        public Vector2 dir;     // hacia dónde apunta
        public Vector2 focus;   // foco: alimentador y origen del haz
        public float radius, depth;
    }

    private static DishGeom MakeDish(Vector2 basePt, Vector2 up, Vector2 aim, float s)
    {
        var centre = basePt + up * (32f * s);
        var dir = (aim - centre).normalized;
        float radius = 30f * s, depth = 12f * s;
        return new DishGeom
        {
            centre = centre,
            dir = dir,
            focus = centre + dir * (radius * radius / (4f * depth)),
            radius = radius,
            depth = depth
        };
    }

    /// <summary>Traza una elipse de semiejes (ru sobre u, rv sobre v) centrada en c.</summary>
    private static void EllipsePath(Painter2D p, Vector2 c, Vector2 u, Vector2 v,
                                    float ru, float rv, int n = 40)
    {
        p.BeginPath();
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n * Mathf.PI * 2f;
            var pt = c + u * (ru * Mathf.Cos(t)) + v * (rv * Mathf.Sin(t));
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.ClosePath();
    }

    private void DrawDish(Painter2D p, Vector2 basePt, DishGeom g, float s)
    {
        var d = g.dir;
        var u = new Vector2(-d.y, d.x);
        float R = g.radius, rv = R * DishFlatten;

        // Pedestal: mástil grueso hasta la parte trasera del plato + peana.
        var backCentre = g.centre - d * (g.depth * 0.55f);
        p.lineWidth = 6f * s; p.strokeColor = MetalDk;
        SimPalette.Line(p, basePt.x, basePt.y, backCentre.x, backCentre.y);
        p.fillColor = MetalDk;
        SimPalette.FillDot(p, basePt.x, basePt.y, 7f * s, 14);

        // Cuenco trasero: misma elipse desplazada hacia atrás → asoma como media luna oscura
        // por detrás de la apertura y da sensación de profundidad.
        p.fillColor = MetalDk;
        EllipsePath(p, backCentre, u, d, R, rv);
        p.Fill();

        // Cara interior (apertura) y su borde.
        p.fillColor = Metal;
        EllipsePath(p, g.centre, u, d, R, rv);
        p.Fill();
        p.lineWidth = 2f * s; p.strokeColor = MetalMd;
        EllipsePath(p, g.centre, u, d, R, rv);
        p.Stroke();

        // Sombreado interior del plato (media luna clara hacia el borde de atrás).
        p.fillColor = new Color(MetalMd.r, MetalMd.g, MetalMd.b, 0.55f);
        EllipsePath(p, g.centre - d * (rv * 0.28f), u, d, R * 0.82f, rv * 0.62f);
        p.Fill();

        // Alimentador en el foco, sostenido por dos brazos desde el borde.
        p.lineWidth = 1.8f * s; p.strokeColor = MetalMd;
        var rimA = g.centre - u * R;
        var rimB = g.centre + u * R;
        SimPalette.Line(p, rimA.x, rimA.y, g.focus.x, g.focus.y);
        SimPalette.Line(p, rimB.x, rimB.y, g.focus.x, g.focus.y);
        p.fillColor = MetalDk;
        SimPalette.FillDot(p, g.focus.x, g.focus.y, 4.2f * s, 12);
        p.fillColor = _lost ? Lost : Cyan;
        SimPalette.FillDot(p, g.focus.x, g.focus.y, 2.4f * s, 10);
    }

    // Haz sinusoidal brillante A→B (glow + núcleo). Rojo si se perdió el enlace.
    private void DrawBeam(Painter2D p, Vector2 a, Vector2 b, float sign, Color baseColor, float s)
    {
        Color col = _lost ? Lost : baseColor;
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float amplitude = 7f * s * Mathf.Lerp(0.4f, 1f, _intensity);

        for (int pass = 0; pass < 2; pass++)
        {
            p.lineWidth = (pass == 0 ? 8f : 2.6f) * s;
            float a2 = pass == 0 ? 0.16f * _intensity : Mathf.Lerp(0.5f, 1f, _intensity);
            p.strokeColor = new Color(col.r, col.g, col.b, a2);
            p.BeginPath();
            const int N = 72;
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N;
                Vector2 basePt = Vector2.Lerp(a, b, t);
                float env = Mathf.Sin(Mathf.PI * t);
                float off = Mathf.Sin(t * 5f * Mathf.PI + sign * _phase) * amplitude * env;
                Vector2 pt = basePt + perp * off;
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.Stroke();
        }
    }

    // ───────────────────────── satélite ─────────────────────────
    private void DrawSatellite(Painter2D p, Vector2 c, float s)
    {
        float bw = 26f * s, bh = 34f * s;      // bus
        float pw = 40f * s, ph = 18f * s;      // panel solar
        float boom = 11f * s;

        p.lineWidth = 2.4f * s; p.strokeColor = MetalDk;
        SimPalette.Line(p, c.x, c.y - bh * 0.5f, c.x, c.y - bh * 0.5f - 13f * s);
        p.fillColor = Metal;
        SimPalette.FillDot(p, c.x, c.y - bh * 0.5f - 14f * s, 3f * s, 10);

        SimPalette.Line(p, c.x - bw * 0.5f, c.y, c.x - bw * 0.5f - boom, c.y);
        SimPalette.Line(p, c.x + bw * 0.5f, c.y, c.x + bw * 0.5f + boom, c.y);

        DrawSolarPanel(p, c.x - bw * 0.5f - boom - pw, c.y - ph * 0.5f, pw, ph, s);
        DrawSolarPanel(p, c.x + bw * 0.5f + boom, c.y - ph * 0.5f, pw, ph, s);

        p.fillColor = Metal;   FillRect(p, c.x - bw * 0.5f, c.y - bh * 0.5f, bw, bh);
        p.fillColor = MetalDk; FillRect(p, c.x - bw * 0.5f, c.y + bh * 0.18f, bw, bh * 0.20f);
        p.fillColor = new Color(Panel.r, Panel.g, Panel.b, 0.9f);
        FillRect(p, c.x - bw * 0.30f, c.y - bh * 0.30f, bw * 0.60f, bh * 0.24f);

        // Antena de comunicaciones apuntando a la Tierra, mismo escorzo que las de tierra.
        var down = new Vector2(0f, 1f);
        var u = new Vector2(-1f, 0f);
        float R = 17f * s, rv = R * DishFlatten, depth = 7f * s;
        var centre = new Vector2(c.x, c.y + bh * 0.5f + depth);

        p.fillColor = MetalDk;
        EllipsePath(p, centre - down * (depth * 0.55f), u, down, R, rv, 30);
        p.Fill();
        p.fillColor = Metal;
        EllipsePath(p, centre, u, down, R, rv, 30);
        p.Fill();
        p.lineWidth = 1.6f * s; p.strokeColor = MetalMd;
        EllipsePath(p, centre, u, down, R, rv, 30);
        p.Stroke();

        var focus = centre + down * (R * R / (4f * depth));
        p.lineWidth = 1.4f * s; p.strokeColor = MetalMd;
        SimPalette.Line(p, centre.x - R, centre.y, focus.x, focus.y);
        SimPalette.Line(p, centre.x + R, centre.y, focus.x, focus.y);
        p.fillColor = _lost ? Lost : Cyan;
        SimPalette.FillDot(p, focus.x, focus.y, 2.8f * s, 10);
    }

    private void DrawSolarPanel(Painter2D p, float x, float y, float w, float h, float s)
    {
        p.fillColor = Panel;  FillRect(p, x, y, w, h);
        p.lineWidth = 1.2f * s; p.strokeColor = PanelDk;
        for (int i = 1; i < 4; i++) SimPalette.Line(p, x + w * i / 4f, y, x + w * i / 4f, y + h);
        SimPalette.Line(p, x, y + h * 0.5f, x + w, y + h * 0.5f);
        p.lineWidth = 1.6f * s; p.strokeColor = MetalDk;
        p.BeginPath();
        p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + w, y));
        p.LineTo(new Vector2(x + w, y + h)); p.LineTo(new Vector2(x, y + h));
        p.ClosePath(); p.Stroke();
    }

    private void DrawRain(Painter2D p, Vector2 c, float s)
    {
        float k = (_weather == 2 ? 1.45f : 1f) * s;
        p.fillColor = new Color(0.62f, 0.70f, 0.82f, 0.95f);
        SimPalette.FillDot(p, c.x - 15f * k, c.y, 12f * k, 20);
        SimPalette.FillDot(p, c.x, c.y - 6f * k, 16f * k, 22);
        SimPalette.FillDot(p, c.x + 15f * k, c.y, 12f * k, 20);
        p.lineWidth = 2f * s; p.strokeColor = new Color(0.40f, 0.68f, 0.95f, 0.9f);
        int drops = _weather == 2 ? 7 : 4;
        for (int i = 0; i < drops; i++)
        {
            float dx = c.x - 17f * k + i * (34f * k / drops);
            float off = (Mathf.Sin(_phase * 0.6f + i) * 0.5f + 0.5f) * 10f * s;
            SimPalette.Line(p, dx, c.y + 13f * k + off, dx - 2f * s, c.y + 24f * k + off);
        }
    }

    private static void FillRect(Painter2D p, float x, float y, float w, float h)
    {
        p.BeginPath();
        p.MoveTo(new Vector2(x, y));
        p.LineTo(new Vector2(x + w, y));
        p.LineTo(new Vector2(x + w, y + h));
        p.LineTo(new Vector2(x, y + h));
        p.ClosePath();
        p.Fill();
    }
}
