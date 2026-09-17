using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador del Módulo 2D (simulador de enlace satelital).
/// Arma la UI (sliders/dropdowns/instrumentos), recalcula el Link Budget en cada cambio
/// y actualiza en vivo: escena animada, gauges, espectro, constelación, fórmula y notas.
/// Lógica de cálculo separada en LinkBudgetModel (C# puro, testeable).
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(100)]
public class SimulatorController : MonoBehaviour
{
    [SerializeField] private SatelliteParameters satParams;
    [SerializeField] private string backScene = "Menu Inicial";

    // Controles
    private Slider slPt, slGt, slLa;
    private Label  valPt, valGt, valLa;
    private DropdownField dBanda, dClima, dPol, dXpi, dMod;

    // Salidas
    private Label lblFormula, lblNote, lblStatus;
    private GaugeElement gPr, gCN, gBer, gTp;
    private SpectrumElement spectrum;
    private ConstellationElement constellation;
    private LinkSceneElement scene;

    private void OnEnable()
    {
        if (satParams == null)
        {
            Debug.LogWarning("[SimulatorController] Sin SatelliteParameters asignado; usando valores por defecto.");
            satParams = ScriptableObject.CreateInstance<SatelliteParameters>();
        }

        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) { Debug.LogWarning("[SimulatorController] root no listo."); return; }

        // ── Controles ──────────────────────────────────────────────
        slPt = root.Q<Slider>("SliderPt"); valPt = root.Q<Label>("ValPt");
        slGt = root.Q<Slider>("SliderGt"); valGt = root.Q<Label>("ValGt");
        slLa = root.Q<Slider>("SliderLa"); valLa = root.Q<Label>("ValLa");

        Configure(slPt, satParams.rangePtdBW, satParams.defaultPtdBW);
        Configure(slGt, satParams.rangeGtdBi, satParams.defaultGtdBi);
        Configure(slLa, satParams.rangeLadB,  satParams.defaultLadB);

        dBanda = root.Q<DropdownField>("DropBanda");
        dClima = root.Q<DropdownField>("DropClima");
        dPol   = root.Q<DropdownField>("DropPol");
        dXpi   = root.Q<DropdownField>("DropXpi");
        dMod   = root.Q<DropdownField>("DropMod");

        SetChoices(dBanda, new List<string> {
            $"C ({satParams.freqC_GHz:0.#} GHz)",
            $"Ku ({satParams.freqKu_GHz:0.#} GHz)",
            $"Ka ({satParams.freqKa_GHz:0.#} GHz)" }, 0);
        SetChoices(dClima, new List<string> { "Despejado", "Lluvia Moderada", "Lluvia Intensa" }, 0);
        SetChoices(dPol,   new List<string> { "Lineal", "Circular" }, 0);
        SetChoices(dXpi,   new List<string> { "Alto (buen aislamiento)", "Medio", "Bajo (falla)" }, 0);
        SetChoices(dMod,   new List<string> { "BPSK", "QPSK", "16-QAM", "64-QAM" }, 1);

        // ── Salidas ────────────────────────────────────────────────
        lblFormula = root.Q<Label>("LblFormula");
        lblNote    = root.Q<Label>("LblNote");
        lblStatus  = root.Q<Label>("LblStatus");

        gPr = MakeGauge(root, "GaugeHost_Pr", "Pr (dBW)", -160f, -40f, big: true);
        gCN = MakeGauge(root, "GaugeHost_CN", "C/N (dB)", 0f, 30f);
        gBer = MakeGauge(root, "GaugeHost_BER", "BER (10⁻ˣ)", 0f, 12f);
        gTp = MakeGauge(root, "GaugeHost_TP", "Mbps", 0f, Mathf.Max(1f, satParams.defaultSymbolRateMBaud * 6f));

        spectrum = new SpectrumElement();
        root.Q<VisualElement>("SpectrumHost")?.Add(spectrum);
        constellation = new ConstellationElement();
        root.Q<VisualElement>("ConstellationHost")?.Add(constellation);
        scene = new LinkSceneElement();
        root.Q<VisualElement>("SceneHost")?.Add(scene);

        // ── Eventos ────────────────────────────────────────────────
        slPt.RegisterValueChangedCallback(_ => Recalc());
        slGt.RegisterValueChangedCallback(_ => Recalc());
        slLa.RegisterValueChangedCallback(_ => Recalc());
        dBanda.RegisterValueChangedCallback(_ => Recalc());
        dClima.RegisterValueChangedCallback(_ => Recalc());
        dPol.RegisterValueChangedCallback(_ => Recalc());
        dXpi.RegisterValueChangedCallback(_ => Recalc());
        dMod.RegisterValueChangedCallback(_ => Recalc());

        var back = root.Q<Button>("BtnBack");
        if (back != null && !string.IsNullOrEmpty(backScene))
            back.clicked += () => SceneManager.LoadScene(backScene);

        Recalc();
    }

    private static void Configure(Slider s, Vector2 range, float def)
    {
        if (s == null) return;
        s.lowValue = range.x; s.highValue = range.y; s.value = def;
    }

    private static void SetChoices(DropdownField d, List<string> choices, int index)
    {
        if (d == null) return;
        d.choices = choices;
        d.index = Mathf.Clamp(index, 0, choices.Count - 1);
    }

    private GaugeElement MakeGauge(VisualElement root, string host, string cap, float min, float max, bool big = false)
    {
        var g = new GaugeElement(cap);
        g.Configure(min, max);
        if (big) g.AddToClassList("sim-gauge--big");
        root.Q<VisualElement>(host)?.Add(g);
        return g;
    }

    // ─────────────────────────── RECÁLCULO EN VIVO ───────────────────────────
    private void Recalc()
    {
        int band  = dBanda != null ? dBanda.index : 0;
        int clima = dClima != null ? dClima.index : 0;
        int pol   = dPol   != null ? dPol.index   : 0;
        int xpiI  = dXpi   != null ? dXpi.index   : 0;
        int modI  = dMod   != null ? dMod.index   : 1;

        // Atenuación por clima (la banda Ka sufre más)
        float rain = clima == 1 ? satParams.rainModeradaDb : (clima == 2 ? satParams.rainIntensaDb : 0f);
        if (band == 2) rain *= satParams.kaRainFactor;

        // Aislamiento XPI; la polarización circular aporta algo de margen extra
        float xpi = xpiI == 0 ? satParams.xpiAltoDb : (xpiI == 1 ? satParams.xpiMedioDb : satParams.xpiBajoDb);
        if (pol == 1) xpi += 3f;

        var mod = ModFromIndex(modI);

        var inp = new LinkInputs
        {
            PtdBW = slPt.value,
            GtdBi = slGt.value,
            GrdBi = satParams.rxGainDbi,
            freqGHz = satParams.FreqForBand(band),
            distanceKm = satParams.distanceKm,
            extraLoss_dB = slLa.value + rain,
            bandwidthHz = satParams.bandwidthMHz * 1e6,
            sysNoiseTempK = satParams.sysNoiseTempK,
            symbolRate = satParams.defaultSymbolRateMBaud * 1e6,
            mod = mod,
            codeRate = satParams.codeRate,
            applyXpi = true,
            xpiDb = xpi,
            latencyMs = satParams.latencyMs
        };

        var res = LinkBudgetModel.Compute(inp);

        // Valores de los sliders
        if (valPt != null) valPt.text = $"{slPt.value:0.0} dBW";
        if (valGt != null) valGt.text = $"{slGt.value:0.0} dBi";
        if (valLa != null) valLa.text = $"{slLa.value:0.0} dB (+{rain:0.0} clima)";

        // Fórmula del Link Budget en vivo
        if (lblFormula != null)
            lblFormula.text =
                "Pr = Pt + Gt + Gr − Lp − La\n" +
                $"Pr = {inp.PtdBW:0.0} + {inp.GtdBi:0.0} + {inp.GrdBi:0.0} − {res.LpDb:0.0} − {inp.extraLoss_dB:0.0}\n" +
                $"Pr = {res.PrDbW:0.0} dBW      (Lp = {res.LpDb:0.0} dB · N = {res.noiseDbW:0.0} dBW)";

        Color q = QualityColor(res);

        // Gauges
        gPr?.SetValue((float)res.PrDbW, $"{res.PrDbW:0.0}", SimPalette.Blue);
        gCN?.SetValue((float)res.cnEffDb, $"{res.cnEffDb:0.0}", q);
        float berAxis = res.ber > 0 ? Mathf.Clamp((float)(-Math.Log10(res.ber)), 0f, 12f) : 12f;
        gBer?.SetValue(berAxis, FormatBer(res.ber), BerColor(res));
        gTp?.SetValue((float)(res.throughputBps / 1e6), $"{res.throughputBps / 1e6:0.0}", res.phaseLock ? SimPalette.Green : SimPalette.Red);

        // Instrumentos
        float cnNorm = Mathf.Clamp01((float)res.cnEffDb / 22f);
        spectrum?.SetLevels(1f - cnNorm * 0.9f, Mathf.Clamp01(cnNorm + 0.1f), q);
        float dispersion = Mathf.Clamp01((22f - (float)res.cnEffDb) / 22f) * 0.6f + 0.05f;
        constellation?.SetState(mod, dispersion, !res.phaseLock);

        // Escena animada
        scene?.SetState(q, cnNorm, !res.phaseLock, clima,
            $"Eb/N0: {res.ebN0Db:0.0} dB",
            $"BER: {FormatBer(res.ber)} ({res.berRating})",
            $"Lp: {res.LpDb:0.0} dB");

        // Barra de estado
        if (lblStatus != null)
        {
            string jitter = !res.phaseLock ? "Alto" : (res.ber > 1e-6 ? "Medio" : "Bajo");
            lblStatus.text =
                $"Modulación: {ModulationInfo.Name(mod)}   ·   Rs: {satParams.defaultSymbolRateMBaud:0.0} MBaud   ·   " +
                $"Rb: {res.rbBps / 1e6:0.0} Mbps   ·   Latencia GEO: {satParams.latencyMs:0} ms   ·   Jitter: {jitter}";
        }

        // Nota explicativa dinámica (editable desde el ScriptableObject)
        if (lblNote != null)
            lblNote.text = PickNote(band, clima, xpiI, res);
    }

    private string PickNote(int band, int clima, int xpiI, LinkResults res)
    {
        if (!res.phaseLock)
            return $"⚠ Pérdida de enganche de fase: C/N={res.cnEffDb:0.0} dB es insuficiente y el BER se disparó a {FormatBer(res.ber)}. La constelación colapsa y el throughput cae a 0.";
        if (band == 2 && clima > 0)
            return satParams.noteRainKa;
        if (xpiI == 2)
            return satParams.noteXpi;
        return $"{satParams.noteModulation}  (Estado actual: BER {FormatBer(res.ber)} — {res.berRating}).";
    }

    // ─────────────────────────── helpers ───────────────────────────
    private static Modulation ModFromIndex(int i)
    {
        switch (i)
        {
            case 0:  return Modulation.BPSK;
            case 1:  return Modulation.QPSK;
            case 2:  return Modulation.QAM16;
            default: return Modulation.QAM64;
        }
    }

    private static Color QualityColor(LinkResults r)
    {
        if (!r.phaseLock) return SimPalette.Red;
        if (r.ber > 1e-6) return SimPalette.Amber;
        return SimPalette.Blue;
    }

    private static Color BerColor(LinkResults r)
    {
        if (!r.phaseLock) return SimPalette.Red;
        if (r.ber <= 1e-6) return SimPalette.Green;
        if (r.ber <= 1e-3) return SimPalette.Amber;
        return SimPalette.Red;
    }

    private static readonly string[] Sups = { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };

    private static string FormatBer(double ber)
    {
        if (ber < 1e-12) return "< 10⁻¹²";
        int e = (int)Math.Floor(Math.Log10(ber));
        double m = ber / Math.Pow(10, e);
        return $"{m:0.0}·10{Sup(e)}";
    }

    private static string Sup(int n)
    {
        string s = Math.Abs(n).ToString();
        string outp = n < 0 ? "⁻" : "";
        foreach (char c in s) outp += Sups[c - '0'];
        return outp;
    }
}
