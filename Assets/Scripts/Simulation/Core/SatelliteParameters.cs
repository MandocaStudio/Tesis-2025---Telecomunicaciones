using UnityEngine;

/// <summary>
/// Parámetros del satélite y del enlace, editables desde el Inspector (VENESAT-1).
/// Al ser ScriptableObject, los valores reales de tu Capítulo II se ajustan SIN tocar código.
/// El asset vive en Assets/Data/VenesatParameters.asset.
/// </summary>
[CreateAssetMenu(fileName = "VenesatParameters", menuName = "PVI/Satellite Parameters")]
public class SatelliteParameters : ScriptableObject
{
    [Header("Identidad")]
    public string satelliteName = "VENESAT-1 (Simón Bolívar)";
    [Tooltip("Longitud orbital en grados. VENESAT-1 ≈ 78° Oeste (negativo = Oeste).")]
    public float orbitalLongitudeDeg = -78f;

    [Header("Geometría del enlace")]
    [Tooltip("Distancia estación–satélite (slant range) en km. GEO nadir ≈ 35786 km.")]
    public float distanceKm = 38000f;

    [Header("Recepción / ruido")]
    public float rxGainDbi     = 40f;   // Gr
    public float sysNoiseTempK = 150f;  // Tsys
    public float bandwidthMHz  = 36f;   // ancho del transpondedor

    [Header("Señal por defecto")]
    public float defaultSymbolRateMBaud = 30f;   // Rs
    [Range(0f, 1f)] public float codeRate = 0.8f; // FEC para throughput útil
    public float latencyMs = 250f;                // latencia GEO

    [Header("Bandas soportadas (frecuencia GHz)")]
    public float freqC_GHz  = 4.0f;
    public float freqKu_GHz = 12.0f;
    public float freqKa_GHz = 20.0f;

    [Header("Rangos de los sliders")]
    public Vector2 rangePtdBW  = new Vector2(-10f, 30f);
    public float   defaultPtdBW = 15f;
    public Vector2 rangeGtdBi  = new Vector2(20f, 60f);
    public float   defaultGtdBi = 42f;
    public Vector2 rangeLadB   = new Vector2(0f, 20f);
    public float   defaultLadB  = 3.1f;

    [Header("Interferencia XPI (C/XPI en dB; alto = mejor aislamiento)")]
    public float xpiAltoDb  = 30f;
    public float xpiMedioDb = 20f;
    public float xpiBajoDb  = 12f;

    [Header("Atenuación extra por clima (dB, se suma a La)")]
    public float rainModeradaDb = 3f;
    public float rainIntensaDb  = 10f;
    [Tooltip("Factor multiplicador de lluvia en banda Ka (sufre mucho más que C/Ku).")]
    public float kaRainFactor = 2.0f;

    [Header("Notas explicativas (editables, NO hardcodeadas en la UI)")]
    [TextArea] public string noteIntro =
        "Ajusta los parámetros y observa cómo cambian C/N, BER y throughput en tiempo real.";
    [TextArea] public string noteXpi =
        "Con reutilización de polarización, un aislamiento bajo deja que la otra polarización se filtre como interferencia, degradando el BER.";
    [TextArea] public string noteRainKa =
        "En banda Ka la lluvia atenúa mucho más la señal; si C/N cae bajo el umbral, el receptor pierde el enganche de fase.";
    [TextArea] public string noteModulation =
        "Modulaciones de mayor orden (16/64-QAM) llevan más bits por símbolo (más throughput) pero exigen mejor C/N para el mismo BER.";

    // Frecuencia de la banda por índice (0=C, 1=Ku, 2=Ka)
    public float FreqForBand(int bandIndex)
    {
        switch (bandIndex)
        {
            case 0:  return freqC_GHz;
            case 1:  return freqKu_GHz;
            default: return freqKa_GHz;
        }
    }
}
