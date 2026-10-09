using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Árboles de la estación, sacados de la foto satelital de Google Maps (la misma del plano v2).
/// Lista generada por script, no a mano:
///  1. Mapa de follaje: en la foto, los píxeles verde oscuro (verde ≥ rojo + 10, verde ≥ azul + 15,
///     verde 35–100, rojo ≤ 75) se muestrearon en una rejilla de 5 m, llevados al modelo con la
///     semejanza foto → modelo (2,81 px/m, giro +1,1°). Salieron 346 puntos de copa, que se pintan
///     en un mapa de 1 m.
///  2. Árboles grandes: se recorre el mapa de la zona más densa a la menos y se pone un árbol de
///     copa 18, 15 o 12 m (el mayor que quepa) donde la copa caiga sobre follaje (≥ 45 %) y al
///     menos la mitad de ella (≥ 55 %) sea follaje que aún no tiene árbol. Así quedan pocos árboles
///     grandes en vez de muchos pequeños.
///  3. La copa no se mete en los techos (deja un fleco del 20 %) ni en los platos; el tronco queda a
///     1,5 m de edificios y tanques, a 1 m de las losas, a 1,5 m del borde de las vías y a 2 m de
///     la cerca.
///  4. Modelo y giro, al azar (semilla 1970). El tronco se coloca de modo que la copa del modelo,
///     que en algunos va descentrada, caiga sobre la mancha de la foto.
/// Salen 67: 50 dentro de la cerca y 17 fuera. Cada entrada es (X, Z del tronco, Ø de copa, modelo,
/// giro en grados).
/// </summary>
public static class PhotoTrees
{
    public static List<TreeSpec> Create() => new List<TreeSpec>
    {
            T(300.3f, 228.3f, 18f, 1, 232f), T(302.5f, 217.2f, 18f, 2, 40f), T(294.4f, 112.8f, 18f, 1, 348f),
            T(265.5f, 210.5f, 18f, 0, 28f), T(193.0f, 106.9f, 18f, 0, 216f), T(194.6f, 100.3f, 12f, 3, 127f),
            T(143.9f, 137.8f, 18f, 3, 55f), T(88.2f, 82.4f, 18f, 1, 253f), T(45.9f, 138.6f, 18f, 0, 239f),
            T(10.8f, 128.6f, 18f, 1, 67f), T(286.4f, 256.7f, 18f, 1, 328f), T(275.2f, 239.1f, 18f, 3, 300f),
            T(56.2f, 100.5f, 18f, 1, 279f), T(18.5f, 123.7f, 15f, 3, 164f), T(294.2f, 203.8f, 18f, 0, 152f),
            T(304.3f, 204.1f, 12f, 3, 325f), T(279.6f, 261.0f, 12f, 3, 6f), T(270.7f, 116.3f, 18f, 1, 142f),
            T(174.7f, 114.1f, 18f, 0, 145f), T(146.4f, 116.3f, 18f, 1, 212f), T(22.6f, 64.3f, 12f, 1, 152f),
            T(266.6f, 230.7f, 15f, 1, 16f), T(183.8f, 140.8f, 18f, 0, 241f), T(186.5f, 113.8f, 12f, 0, 351f),
            T(83.7f, 70.7f, 12f, 0, 228f), T(45.6f, 96.6f, 15f, 3, 199f), T(240.5f, 212.5f, 18f, 3, 178f),
            T(14.8f, 96.8f, 15f, 3, 304f), T(294.9f, 100.9f, 15f, 3, 87f), T(251.3f, 219.1f, 15f, 2, 82f),
            T(260.7f, 116.5f, 12f, 1, 103f), T(304.4f, 116.4f, 12f, 1, 235f), T(295.9f, 251.3f, 15f, 0, 211f),
            T(290.4f, 96.6f, 12f, 1, 307f), T(158.0f, 142.8f, 15f, 3, 209f), T(34.7f, 254.3f, 18f, 1, 139f),
            T(252.2f, 128.6f, 18f, 1, 293f), T(144.0f, 100.9f, 15f, 2, 35f), T(186.3f, 132.5f, 12f, 1, 274f),
            T(307.4f, 80.4f, 15f, 3, 18f), T(283.9f, 101.7f, 12f, 3, 270f), T(274.5f, 100.8f, 12f, 2, 4f),
            T(183.3f, 171.9f, 15f, 2, 106f), T(47.2f, 110.9f, 12f, 0, 268f), T(3.7f, 160.2f, 15f, 2, 267f),
            T(289.7f, 231.3f, 12f, 0, 185f), T(253.9f, 242.2f, 15f, 0, 115f), T(10.4f, 83.9f, 12f, 0, 211f),
            T(152.5f, 173.4f, 12f, 2, 123f), T(36.9f, 143.5f, 12f, 3, 100f), T(132.3f, -7.4f, 12f, 1, 290f),
            T(122.1f, 96.0f, 15f, 2, 305f), T(166.2f, 219.0f, 12f, 2, 135f), T(82.8f, 157.7f, 12f, 3, 107f),
            T(192.4f, 186.7f, 12f, 1, 332f), T(107.6f, 72.5f, 12f, 2, 31f), T(41.3f, 15.9f, 12f, 3, 180f),
            T(181.3f, 159.1f, 12f, 3, 84f), T(220.5f, 230.7f, 12f, 1, 351f), T(6.4f, 61.8f, 12f, 0, 350f),
            T(27.7f, 101.9f, 12f, 3, 261f), T(166.5f, 153.3f, 12f, 2, 243f), T(121.6f, 86.0f, 12f, 3, 258f),
            T(2.2f, 203.2f, 12f, 3, 290f), T(131.2f, 124.7f, 12f, 0, 264f), T(22.0f, 50.7f, 12f, 3, 56f),
            T(250.4f, 198.4f, 12f, 1, 220f),
    };

    static TreeSpec T(float x, float z, float crown, int model, float yaw) =>
        new TreeSpec(new Vector2(x, z), crown) { model = model, yaw = yaw };
}
