#ifndef PVI_SUPERFICIE_EN_METROS_INPUT_INCLUDED
#define PVI_SUPERFICIE_EN_METROS_INPUT_INCLUDED

// Propiedades de "PVI/Superficie en metros". Un solo CBUFFER para todas las pasadas (lo pide el
// SRP Batcher). _BaseMap, _BumpMap y sus samplers los declara SurfaceInput.hlsl de URP; las pasadas
// de sombra y de profundidad de URP se incluyen tal cual y solo usan _BaseMap_ST, _BaseColor y _Cutoff.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _Layer2Color;
    half _Cutoff;
    half _BumpScale;
    half _Smoothness;
    half _Metallic;
    float _Tile;
    half _TopOnly;
    half _Layer2On;
    float _Layer2Tile;
    half _Layer2Smoothness;
    float _PatchScale;
    half _PatchCover;
    half _PatchSoftness;
    float _MacroScale;
    half _MacroStrength;
CBUFFER_END

TEXTURE2D(_Layer2Map);
TEXTURE2D(_Layer2BumpMap);
TEXTURE2D(_PatchMap);       SAMPLER(sampler_PatchMap);

// Proyección plana según la cara: la textura se apoya en el plano al que más mira la normal, con las
// UV en metros del mundo. Así una textura real queda a su tamaño en un cubo escalado (los muros, las
// losas) y en el suelo, con UNA lectura por textura (un triplanar haría tres). Las tangentes salen
// de la propia proyección para que el relieve (normal map) apunte bien en las seis caras.
// En las caras inclinadas (faldones) la textura se gira con la pendiente: "arriba" en la textura es
// "hacia el caballete", así las hileras de teja van paralelas al alero y las ondas del zinc bajan.
void PlanarFrame(float3 positionWS, float3 normalWS, out float2 uvMeters, out float3 tangentWS, out float3 bitangentWS)
{
    float3 a = abs(normalWS);
    if (_TopOnly < 0.5 && a.y >= a.x && a.y >= a.z && dot(normalWS.xz, normalWS.xz) > 0.0025)
    {
        float2 d = normalize(normalWS.xz) * (normalWS.y >= 0.0 ? 1.0 : -1.0); // cuesta abajo, en planta
        uvMeters = float2(dot(positionWS.xz, float2(-d.y, d.x)), -dot(positionWS.xz, d));
        tangentWS = float3(-d.y, 0.0, d.x);
        bitangentWS = float3(-d.x, 0.0, -d.y);
    }
    else if (_TopOnly > 0.5 || (a.y >= a.x && a.y >= a.z))
    {
        float s = normalWS.y >= 0.0 ? 1.0 : -1.0;
        uvMeters = float2(positionWS.x * s, positionWS.z);
        tangentWS = float3(s, 0.0, 0.0);
        bitangentWS = float3(0.0, 0.0, 1.0);
    }
    else if (a.x >= a.z)
    {
        float s = normalWS.x >= 0.0 ? 1.0 : -1.0;
        uvMeters = float2(positionWS.z * s, positionWS.y);
        tangentWS = float3(0.0, 0.0, s);
        bitangentWS = float3(0.0, 1.0, 0.0);
    }
    else
    {
        float s = normalWS.z >= 0.0 ? 1.0 : -1.0;
        uvMeters = float2(-positionWS.x * s, positionWS.y);
        tangentWS = float3(-s, 0.0, 0.0);
        bitangentWS = float3(0.0, 1.0, 0.0);
    }
}

#endif
