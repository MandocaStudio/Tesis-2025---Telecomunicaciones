using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Cielo de la escena a partir de un HDRI (Skybox/Cubemap con el .hdr en _Tex): lo gira para que su
/// sol caiga donde está la luz direccional, saca del propio HDRI el color de la niebla (su horizonte)
/// y de la luz ambiente, y genera un cubemap de reflejos pequeño, ya girado y con el sol limitado.
///
/// Son ajustes de ESCENA, fuera del root generado: el generador no los toca y esto no toca la
/// estación. Se vuelve a pulsar si cambia el sol o el HDRI. MODULO-3D.md §4.5.
/// </summary>
public static class SkySetup
{
    const string LogTag = "[Cielo]";
    const string ReflectionPath = "Assets/Settings/Modulo3DReflejos.asset";

    /// <summary>Lado de cada cara del cubemap de reflejos. Basta para vidrio, agua y acero.</summary>
    const int ReflectionSize = 128;
    /// <summary>
    /// Tope de cada píxel del HDRI al hacer los reflejos. El sol vale ~65 000 y, sin tope, hasta las
    /// superficies ásperas (los cerros) lo reflejaban como manchas blancas.
    /// </summary>
    const float ReflectionClamp = 40f;

    /// <summary>
    /// Luminancia de la luz ambiente desde arriba y desde el horizonte. Se toman del cielo de antes
    /// (el procedural de Unity) para que el cambio no aclare ni oscurezca la escena; del HDRI sale el
    /// tono. Cuadra con el sol de 1,3: el HDRI da ~5,5 veces más luz de sol que de cielo.
    /// </summary>
    const float AmbientSkyLuminance = 0.24f, AmbientEquatorLuminance = 0.26f;
    /// <summary>Lo que rebota del suelo: tierra ocre al sol (el HDRI "pure sky" no tiene suelo).</summary>
    static readonly Color AmbientGroundLinear = new Color(0.17f, 0.145f, 0.11f);

    [MenuItem("PVI/Estación 3D/Ajustar cielo, luz ambiente y niebla")]
    public static void Apply()
    {
        var sky = RenderSettings.skybox;
        var cube = sky != null && sky.HasProperty("_Tex") ? sky.GetTexture("_Tex") : null;
        string hdrPath = cube != null ? AssetDatabase.GetAssetPath(cube) : null;
        if (hdrPath == null || !hdrPath.EndsWith(".hdr", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"{LogTag} El cielo de la escena tiene que ser un Skybox/Cubemap con un .hdr en Tex.");
            return;
        }
        var sun = RenderSettings.sun;
        if (sun == null)
        {
            Debug.LogWarning($"{LogTag} La escena no tiene sol (Lighting > Environment > Sun Source).");
            return;
        }

        var hdr = Equirect.Load(hdrPath, ReflectionClamp);

        // Giro: Unity pone el centro del HDRI (u = 0,5) al norte (+Z) y avanza hacia el este (+X) con u;
        // el _Rotation del Skybox/Cubemap gira el cielo hacia el oeste. Medido en la escena.
        Vector3 toSun = -sun.transform.forward;
        float sunAzimuth = Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg;
        float sunElevation = Mathf.Asin(toSun.y) * Mathf.Rad2Deg;
        float rotation = Mathf.Repeat(hdr.SunAzimuth - sunAzimuth, 360f);
        sky.SetFloat("_Rotation", rotation);
        EditorUtility.SetDirty(sky);
        if (Mathf.Abs(hdr.SunElevation - sunElevation) > 3f)
            Debug.LogWarning($"{LogTag} El sol del HDRI está a {hdr.SunElevation:0.#}° y la luz a {sunElevation:0.#}°: " +
                             "las sombras no van a cuadrar con el cielo. Cambia la altura de la luz o el HDRI.");

        float exposure = sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
        Color horizon = hdr.Band(0f, 3f) * exposure;
        Color skyAverage = hdr.Hemisphere() * exposure;

        // Niebla del color del horizonte: los cerros se funden con el cielo de verdad.
        RenderSettings.fogColor = Gamma(horizon);

        // Luz ambiente en tres tonos (cielo, horizonte, suelo). Va así y no "Skybox" porque el sol del
        // HDRI se colaría en ella y la luz direccional ya es ese sol.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Gamma(WithLuminance(skyAverage, AmbientSkyLuminance));
        RenderSettings.ambientEquatorColor = Gamma(WithLuminance(hdr.Band(0f, 10f), AmbientEquatorLuminance));
        RenderSettings.ambientGroundColor = Gamma(AmbientGroundLinear);

        // Reflejos: Unity no gira los reflejos con el cielo, así que se hornea el giro en el cubemap.
        var reflections = BuildReflections(hdr, rotation, exposure);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = reflections;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"{LogTag} '{Path.GetFileName(hdrPath)}': sol del HDRI a {hdr.SunAzimuth:0.#}° / {hdr.SunElevation:0.#}°, " +
                  $"cielo girado {rotation:0.#}° para la luz ({sunAzimuth:0.#}° / {sunElevation:0.#}°). Niebla {RenderSettings.fogColor}.");
    }

    static Cubemap BuildReflections(Equirect hdr, float rotation, float exposure)
    {
        var cube = new Cubemap(ReflectionSize, TextureFormat.RGBAHalf, true) { name = "Modulo3DReflejos" };
        var pixels = new Color[ReflectionSize * ReflectionSize];
        for (int f = 0; f < 6; f++)
        {
            var face = (CubemapFace)f;
            for (int y = 0; y < ReflectionSize; y++)
            for (int x = 0; x < ReflectionSize; x++)
            {
                float u = 2f * (x + 0.5f) / ReflectionSize - 1f, v = 2f * (y + 0.5f) / ReflectionSize - 1f;
                pixels[y * ReflectionSize + x] = hdr.Sample(FaceDirection(face, u, v), rotation) * exposure;
            }
            cube.SetPixels(pixels, face);
        }
        cube.Apply(true);

        var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(cube, ReflectionPath);
            return cube;
        }
        EditorUtility.CopySerialized(cube, existing);
        Object.DestroyImmediate(cube);
        return existing;
    }

    /// <summary>Dirección de un texel de una cara del cubemap (u, v en −1…1; v hacia abajo, como guarda Unity las caras).</summary>
    static Vector3 FaceDirection(CubemapFace face, float u, float v) => (face switch
    {
        CubemapFace.PositiveX => new Vector3(1f, -v, -u),
        CubemapFace.NegativeX => new Vector3(-1f, -v, u),
        CubemapFace.PositiveY => new Vector3(u, 1f, v),
        CubemapFace.NegativeY => new Vector3(u, -1f, -v),
        CubemapFace.PositiveZ => new Vector3(u, -v, 1f),
        _                     => new Vector3(-u, -v, -1f),
    }).normalized;

    static Color Gamma(Color linear) =>
        new Color(Mathf.LinearToGammaSpace(linear.r), Mathf.LinearToGammaSpace(linear.g), Mathf.LinearToGammaSpace(linear.b));

    static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    static Color WithLuminance(Color c, float luminance)
    {
        float l = Luminance(c);
        return l > 0f ? c * (luminance / l) : new Color(luminance, luminance, luminance);
    }

    /// <summary>
    /// HDRI equirectangular (Radiance .hdr) leído a mano y reducido a una rejilla pequeña: basta para
    /// colores medios y reflejos. Guarda dónde está el sol (el píxel más brillante del original).
    /// </summary>
    sealed class Equirect
    {
        const int Width = 512, Height = 256;
        readonly Color[] grid = new Color[Width * Height]; // fila 0 = arriba (cénit)

        public float SunAzimuth, SunElevation;

        public static Equirect Load(string path, float clamp)
        {
            byte[] data = File.ReadAllBytes(path);
            int i = 0;
            string Line()
            {
                int start = i;
                while (data[i] != (byte)'\n') i++;
                return System.Text.Encoding.ASCII.GetString(data, start, i++ - start);
            }
            if (!Line().StartsWith("#?")) throw new IOException($"{path} no es un Radiance .hdr");
            while (Line().Length > 0) { }
            string[] res = Line().Split(' ');
            if (res.Length != 4 || res[0] != "-Y" || res[2] != "+X") throw new IOException($"Orientación no soportada: {string.Join(" ", res)}");
            int H = int.Parse(res[1]), W = int.Parse(res[3]);

            var e = new Equirect();
            var sums = new double[Width * Height * 3];
            var counts = new int[Width * Height];
            var line = new byte[W * 4];
            float best = -1f;
            int bestX = 0, bestY = 0;
            for (int y = 0; y < H; y++)
            {
                if (data[i] == 2 && data[i + 1] == 2 && (data[i + 2] << 8 | data[i + 3]) == W)
                {
                    i += 4;
                    for (int c = 0; c < 4; c++)
                        for (int x = 0; x < W;)
                        {
                            int n = data[i++];
                            if (n > 128) { n -= 128; byte b = data[i++]; while (n-- > 0) line[(x++) * 4 + c] = b; }
                            else while (n-- > 0) line[(x++) * 4 + c] = data[i++];
                        }
                }
                else
                {
                    System.Buffer.BlockCopy(data, i, line, 0, W * 4);
                    i += W * 4;
                }

                int gy = y * Height / H;
                for (int x = 0; x < W; x++)
                {
                    int ex = line[x * 4 + 3];
                    if (ex == 0) continue;
                    float s = Mathf.Pow(2f, ex - 136);
                    float r = line[x * 4] * s, g = line[x * 4 + 1] * s, b = line[x * 4 + 2] * s;
                    float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                    if (lum > best) { best = lum; bestX = x; bestY = y; }
                    int k = gy * Width + x * Width / W;
                    sums[k * 3] += Mathf.Min(r, clamp);
                    sums[k * 3 + 1] += Mathf.Min(g, clamp);
                    sums[k * 3 + 2] += Mathf.Min(b, clamp);
                    counts[k]++;
                }
            }
            for (int k = 0; k < counts.Length; k++)
                if (counts[k] > 0)
                    e.grid[k] = new Color((float)(sums[k * 3] / counts[k]), (float)(sums[k * 3 + 1] / counts[k]), (float)(sums[k * 3 + 2] / counts[k]));

            e.SunAzimuth = Mathf.Repeat(((bestX + 0.5f) / W - 0.5f) * 360f, 360f);
            e.SunElevation = 90f - (bestY + 0.5f) / H * 180f;
            return e;
        }

        /// <summary>Color en una dirección del mundo, con el cielo girado <paramref name="rotation"/> grados (bilineal).</summary>
        public Color Sample(Vector3 dir, float rotation)
        {
            float az = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + rotation;
            float el = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float fx = Mathf.Repeat(az / 360f + 0.5f, 1f) * Width - 0.5f;
            float fy = Mathf.Clamp((90f - el) / 180f * Height - 0.5f, 0f, Height - 1f);
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            Color At(int x, int y) => grid[Mathf.Min(y, Height - 1) * Width + ((x % Width) + Width) % Width];
            return Color.Lerp(Color.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Color.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
        }

        /// <summary>Media de la franja entre dos alturas sobre el horizonte (grados).</summary>
        public Color Band(float fromElevation, float toElevation)
        {
            int y0 = Mathf.Clamp((int)((90f - toElevation) / 180f * Height), 0, Height - 1);
            int y1 = Mathf.Clamp((int)((90f - fromElevation) / 180f * Height), y0 + 1, Height);
            Color sum = Color.clear;
            for (int y = y0; y < y1; y++)
                for (int x = 0; x < Width; x++) sum += grid[y * Width + x];
            return sum / ((y1 - y0) * Width);
        }

        /// <summary>Media del cielo ponderada por el coseno (lo que recibe una cara mirando arriba).</summary>
        public Color Hemisphere()
        {
            Color sum = Color.clear;
            float weight = 0f;
            for (int y = 0; y < Height / 2; y++)
            {
                float el = 90f - (y + 0.5f) / Height * 180f;
                float w = Mathf.Sin(el * Mathf.Deg2Rad) * Mathf.Cos(el * Mathf.Deg2Rad); // coseno × área de la fila
                for (int x = 0; x < Width; x++) sum += grid[y * Width + x] * w;
                weight += w * Width;
            }
            return sum / weight;
        }
    }
}
