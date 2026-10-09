using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Las copias de una capa repartida por el terreno (matas, arbustos, piedras), empaquetadas: por cada
/// variante, cinco floats por copia (X, Y, Z, giro en radianes, escala). Lo escribe el generador en
/// Assets/Data/Station3D/Generado/ y lo lee <see cref="InstancedScatter"/>. Va en un asset y no en la
/// escena porque son miles de copias: así el .unity no crece y Unity lo guarda en una sola línea.
/// </summary>
public class ScatterData : ScriptableObject
{
    public const int FloatsPerInstance = 5;

    [Serializable]
    public class Set
    {
        public byte[] packed = Array.Empty<byte>();
        public int Count => packed.Length / (FloatsPerInstance * sizeof(float));
    }

    public List<Set> sets = new List<Set>();

    public static byte[] Pack(IList<Vector4> positionsAndYaw, IList<float> scales)
    {
        var floats = new float[positionsAndYaw.Count * FloatsPerInstance];
        for (int i = 0; i < positionsAndYaw.Count; i++)
        {
            var p = positionsAndYaw[i];
            floats[i * 5] = p.x; floats[i * 5 + 1] = p.y; floats[i * 5 + 2] = p.z; floats[i * 5 + 3] = p.w; floats[i * 5 + 4] = scales[i];
        }
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>Matrices de las copias de un set, con el pivote de su variante (centrado y a ras de suelo).</summary>
    public static Matrix4x4[] Unpack(Set set, Matrix4x4 pivot)
    {
        var floats = new float[set.packed.Length / sizeof(float)];
        Buffer.BlockCopy(set.packed, 0, floats, 0, set.packed.Length);
        var result = new Matrix4x4[floats.Length / FloatsPerInstance];
        for (int i = 0; i < result.Length; i++)
        {
            int k = i * FloatsPerInstance;
            var pos = new Vector3(floats[k], floats[k + 1], floats[k + 2]);
            var rot = Quaternion.Euler(0f, floats[k + 3] * Mathf.Rad2Deg, 0f);
            result[i] = Matrix4x4.TRS(pos, rot, Vector3.one * floats[k + 4]) * pivot;
        }
        return result;
    }
}
