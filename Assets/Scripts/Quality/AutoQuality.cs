using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Elige la calidad al arrancar, sin preguntar: la plataforma tiene que correr en un portátil con
/// gráfica integrada (Intel UHD/Iris, AMD Vega).
///
///  1. Al arrancar: nivel "Baja" si la gráfica es integrada o tiene poca memoria; si no, el del
///     proyecto ("PC"). Se puede forzar lanzando el juego con <c>-calidad baja</c> o <c>-calidad alta</c>.
///  2. Si aun así una escena va por debajo de <see cref="MinFps"/> en sus primeros segundos, baja a
///     "Baja" (una vez; queda escrito en el Player.log).
///  3. En "Baja" apaga además el bloom de los volúmenes de la escena (en su copia, no en el asset).
///
/// En el Editor no cambia el nivel (manda el elegido en Project Settings > Quality) y solo avisa en
/// consola de lo que haría; el bloom sí lo apaga si el nivel elegido es "Baja", para poder probarlo.
/// Lo que cambia cada nivel está en Assets/Settings/Baja_RPAsset y en MODULO-3D.md §4.6.
/// </summary>
public static class AutoQuality
{
    public const string LowLevel = "Baja";
    const string LogTag = "[Calidad]";

    /// <summary>Por debajo de esto, medido tras cargar una escena, se pasa a "Baja".</summary>
    const float MinFps = 30f;

    public static bool IsLow => QualitySettings.names[QualitySettings.GetQualityLevel()] == LowLevel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (_, _) => ApplySceneTweaks();

        string forced = Argument("-calidad");
        bool low = forced != null ? forced.ToLowerInvariant().StartsWith("b")
                                  : IsLowEndGpu(SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize);
        string why = forced != null ? $"forzada con -calidad {forced}" : low ? "gráfica integrada o con poca memoria" : "gráfica dedicada";
        if (Application.isEditor)
            Debug.Log($"{LogTag} En un build arrancaría en {(low ? LowLevel : "la calidad del proyecto")} ({why}): " +
                      $"{SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB. En el Editor manda Project Settings > Quality.");
        else
        {
            if (low) SetLow();
            Debug.Log($"{LogTag} {QualitySettings.names[QualitySettings.GetQualityLevel()]} ({why}): " +
                      $"{SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB.");
        }

        if (forced == null)
        {
            var watch = new GameObject("Calidad (vigila los fps)") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(watch);
            watch.AddComponent<FpsWatch>();
        }
    }

    /// <summary>
    /// Gráficas para las que el nivel del proyecto es demasiado: integradas de Intel (no las Arc
    /// dedicadas), APU de AMD (las dedicadas llevan "RX" o "Pro W"), NVIDIA MX y GT, el render por
    /// software y cualquiera con menos de 2 GB. Pública para poder probarla desde el Editor.
    /// </summary>
    public static bool IsLowEndGpu(string device, int memoryMB)
    {
        string n = (device ?? "").ToLowerInvariant();
        if (n.Contains("basic render") || n.Contains("llvmpipe") || n.Contains("swiftshader")) return true; // sin driver: por software
        if (n.Contains("intel")) return !Regex.IsMatch(n, @"arc(\(tm\))? [ab]\d{3}");
        if (n.Contains("radeon") || n.Contains("amd")) return !Regex.IsMatch(n, @"\brx\b|pro w|radeon vii");
        if (n.Contains("nvidia") || n.Contains("geforce")) return Regex.IsMatch(n, @"\bmx ?\d{3}|\bgt \d{3,4}\b");
        return memoryMB > 0 && memoryMB < 2048;
    }

    static void SetLow()
    {
        int index = System.Array.IndexOf(QualitySettings.names, LowLevel);
        if (index < 0)
        {
            Debug.LogWarning($"{LogTag} No hay nivel '{LowLevel}' en Project Settings > Quality.");
            return;
        }
        QualitySettings.SetQualityLevel(index, true);
        ApplySceneTweaks();
    }

    /// <summary>Lo que el nivel no puede decidir solo: el bloom vive en el perfil de cada escena.</summary>
    static void ApplySceneTweaks()
    {
        if (!IsLow) return;
        foreach (var volume in Object.FindObjectsByType<Volume>())
            if (volume.profile != null && volume.profile.TryGet(out Bloom bloom))
                bloom.active = false;
    }

    static string Argument(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, System.StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) return args[i + 1];
            if (args[i].StartsWith(name + "=", System.StringComparison.OrdinalIgnoreCase)) return args[i].Substring(name.Length + 1);
        }
        return null;
    }

    /// <summary>
    /// Mide los fps de cada escena entre los segundos 2 y 6 tras cargarla (los primeros fotogramas
    /// compilan shaders y no cuentan) y baja a "Baja" si no llega a <see cref="MinFps"/>.
    /// </summary>
    sealed class FpsWatch : MonoBehaviour
    {
        const float Warmup = 2f, Window = 4f;
        float since, measured;
        int frames;
        bool done;

        void OnEnable() => SceneManager.sceneLoaded += Restart;
        void OnDisable() => SceneManager.sceneLoaded -= Restart;
        void Restart(Scene scene, LoadSceneMode mode) { since = 0f; measured = 0f; frames = 0; done = IsLow; }

        void Update()
        {
            if (done) return;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.5f) return; // la ventana perdió el foco o hubo una carga: no cuenta
            since += dt;
            if (since < Warmup) return;
            measured += dt;
            frames++;
            if (measured < Window) return;

            done = true;
            float fps = frames / measured;
            if (fps >= MinFps) return;
            if (Application.isEditor)
            {
                Debug.Log($"{LogTag} '{SceneManager.GetActiveScene().name}' va a {fps:0} fps: en un build pasaría a {LowLevel}.");
                return;
            }
            Debug.Log($"{LogTag} '{SceneManager.GetActiveScene().name}' va a {fps:0} fps: paso a {LowLevel}.");
            SetLow();
        }
    }
}
