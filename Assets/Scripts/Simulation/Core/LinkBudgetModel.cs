using System;

// ─────────────────────────────────────────────────────────────────────────────
// Módulo de cálculo PURO del enlace satelital (Marco Teórico, Cap. II).
// No depende de UnityEngine → se puede testear con NUnit/Unity Test Framework.
// Fórmulas estándar (editables): FSPL, ruido térmico, C/N, C/(N+I) por XPI,
// Eb/N0, BER por función Q (erfc), Rb = Rs·log2(M).
// Sustituye estas fórmulas por las EXACTAS de tu Capítulo II si difieren.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Esquemas de modulación (el valor entero = M, puntos de la constelación).</summary>
public enum Modulation { BPSK = 2, QPSK = 4, QAM16 = 16, QAM64 = 64 }

public static class ModulationInfo
{
    public static int BitsPerSymbol(Modulation m) =>
        (int)Math.Round(Math.Log((int)m) / Math.Log(2.0));

    public static string Name(Modulation m)
    {
        switch (m)
        {
            case Modulation.BPSK:  return "BPSK";
            case Modulation.QPSK:  return "QPSK";
            case Modulation.QAM16: return "16-QAM";
            case Modulation.QAM64: return "64-QAM";
            default: return m.ToString();
        }
    }
}

/// <summary>Entradas del presupuesto de enlace (todas en unidades explícitas).</summary>
public struct LinkInputs
{
    // Transmisión
    public double PtdBW;        // Potencia Tx (dBW)
    public double GtdBi;        // Ganancia Tx (dBi)
    public double GrdBi;        // Ganancia Rx (dBi)
    // Trayectoria
    public double freqGHz;      // Frecuencia de la banda (GHz)
    public double distanceKm;   // Distancia estación–satélite (km)
    public double extraLoss_dB; // La = atmósfera + lluvia + gases (dB)
    // Ruido
    public double bandwidthHz;  // Ancho de banda de ruido (Hz)
    public double sysNoiseTempK;// Temperatura de ruido del sistema (K)
    // Señal digital
    public double symbolRate;   // Rs (símbolos/seg)
    public Modulation mod;      // Esquema de modulación
    public double codeRate;     // Tasa FEC (0..1) para throughput útil
    // Interferencia
    public bool   applyXpi;     // Reutilización de polarización activa
    public double xpiDb;        // C/XPI (dB): alto = buen aislamiento
    // Otros
    public double latencyMs;    // Latencia GEO (~250 ms)
}

/// <summary>Resultados calculados del enlace.</summary>
public struct LinkResults
{
    public double LpDb;          // Pérdida en espacio libre
    public double PrDbW;         // Potencia recibida
    public double noiseDbW;      // Potencia de ruido N = kTB
    public double cnDb;          // C/N (sin interferencia)
    public double cnEffDb;       // C/(N+I) efectivo
    public double ebN0Db;        // Eb/N0
    public double ber;           // Tasa de error de bit
    public double rbBps;         // Tasa de transmisión bruta (bit/s)
    public double throughputBps; // Tasa útil (0 si no hay enganche)
    public bool   phaseLock;     // ¿Hay enganche de fase?
    public string berRating;     // Calificación textual
}

public static class LinkBudgetModel
{
    // 10·log10(k) con k = 1.380649e-23 J/K  → -228.6 dBW/(Hz·K)
    private const double K_DBW = -228.6;

    public static LinkResults Compute(LinkInputs i)
    {
        var r = new LinkResults();

        // 1) Pérdida en espacio libre (FSPL), f en GHz y d en km:
        //    Lp = 92.45 + 20·log10(f) + 20·log10(d)
        r.LpDb = 92.45
               + 20.0 * Math.Log10(Math.Max(i.freqGHz, 1e-6))
               + 20.0 * Math.Log10(Math.Max(i.distanceKm, 1e-6));

        // 2) Potencia recibida: Pr = Pt + Gt + Gr − Lp − La
        r.PrDbW = i.PtdBW + i.GtdBi + i.GrdBi - r.LpDb - i.extraLoss_dB;

        // 3) Ruido térmico: N = k·T·B  → dBW
        r.noiseDbW = K_DBW
                   + 10.0 * Math.Log10(Math.Max(i.sysNoiseTempK, 1e-6))
                   + 10.0 * Math.Log10(Math.Max(i.bandwidthHz, 1e-6));

        // 4) Relación portadora a ruido
        r.cnDb = r.PrDbW - r.noiseDbW;

        // 5) C/(N+I): la interferencia de polarización cruzada (XPI) se suma como ruido
        if (i.applyXpi)
        {
            double cnLin  = Db2Lin(r.cnDb);
            double xpiLin = Db2Lin(i.xpiDb);
            double eff = 1.0 / (1.0 / cnLin + 1.0 / xpiLin);
            r.cnEffDb = Lin2Db(eff);
        }
        else r.cnEffDb = r.cnDb;

        // 6) Eb/N0 = C/(N+I) + 10·log10(B / Rb),  Rb = Rs · log2(M)
        int bits = ModulationInfo.BitsPerSymbol(i.mod);
        r.rbBps = i.symbolRate * bits;
        double bOverRb = r.rbBps > 0 ? i.bandwidthHz / r.rbBps : 1.0;
        r.ebN0Db = r.cnEffDb + 10.0 * Math.Log10(Math.Max(bOverRb, 1e-9));

        // 7) BER según la modulación
        r.ber = Ber(r.ebN0Db, i.mod);

        // 8) Enganche de fase y throughput útil
        r.phaseLock = r.ber < 1e-2 && r.cnEffDb > 0.0;
        r.throughputBps = r.phaseLock ? r.rbBps * Clamp01(i.codeRate) : 0.0;

        // 9) Calificación textual
        r.berRating = Rating(r.ber, r.phaseLock);
        return r;
    }

    // ───────────────────────── BER ─────────────────────────
    private static double Ber(double ebN0Db, Modulation m)
    {
        double eb = Db2Lin(ebN0Db); // Eb/N0 lineal
        double M  = (int)m;

        // BPSK / QPSK (misma BER por bit con codificación Gray): BER = Q(√(2·Eb/N0))
        if (m == Modulation.BPSK || m == Modulation.QPSK)
            return Clamp(Q(Math.Sqrt(2.0 * eb)), 0.0, 0.5);

        // M-QAM cuadrada (aprox. Gray): BER ≈ (4/k)(1−1/√M)·Q(√(3k/(M−1)·Eb/N0))
        double k = Math.Log(M) / Math.Log(2.0);
        double q = Q(Math.Sqrt((3.0 * k / (M - 1.0)) * eb));
        double ber = (4.0 / k) * (1.0 - 1.0 / Math.Sqrt(M)) * q;
        return Clamp(ber, 0.0, 0.5);
    }

    /// <summary>Función Q gaussiana: Q(x) = 0.5·erfc(x/√2).</summary>
    private static double Q(double x) => 0.5 * Erfc(x / Math.Sqrt(2.0));

    /// <summary>erfc aproximada (Numerical Recipes), precisión ~1e-7 (suficiente para BER).</summary>
    private static double Erfc(double x)
    {
        double z = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.5 * z);
        double ans = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 +
            t * (0.09678418 + t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 +
            t * (1.48851587 + t * (-0.82215223 + t * 0.17087277)))))))));
        return x >= 0.0 ? ans : 2.0 - ans;
    }

    private static string Rating(double ber, bool phaseLock)
    {
        if (!phaseLock)     return "Enlace perdido";
        if (ber <= 1e-9)    return "Excelente";
        if (ber <= 1e-6)    return "Aceptable";
        if (ber <= 1e-3)    return "Marginal";
        return "Degradado";
    }

    // ───────────────────────── helpers ─────────────────────────
    public  static double Db2Lin(double db) => Math.Pow(10.0, db / 10.0);
    public  static double Lin2Db(double x)  => 10.0 * Math.Log10(Math.Max(x, 1e-300));
    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    private static double Clamp(double v, double a, double b) => v < a ? a : (v > b ? b : v);
}
