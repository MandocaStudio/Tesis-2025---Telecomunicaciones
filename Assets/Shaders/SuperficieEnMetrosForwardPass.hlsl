#ifndef PVI_SUPERFICIE_EN_METROS_FORWARD_INCLUDED
#define PVI_SUPERFICIE_EN_METROS_FORWARD_INCLUDED

// Pasada principal de "PVI/Superficie en metros": la de URP Lit (LitForwardPass.hlsl) con las UV
// calculadas en el mundo (PlanarFrame), una segunda capa opcional mezclada por manchas y una
// variación de tono a gran escala que rompe la repetición. Sin mapas de lightmap: la escena no hornea.

#include "SuperficieEnMetrosInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uvMeters    : TEXCOORD0;
    float3 positionWS  : TEXCOORD1;
    half3 normalWS     : TEXCOORD2;
    half3 tangentWS    : TEXCOORD3;
    half3 bitangentWS  : TEXCOORD4;
    half fogFactor     : TEXCOORD5;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD6;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD8;
#endif
    float4 positionCS  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings SuperficieVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float3 tangentWS, bitangentWS;
    PlanarFrame(vertexInput.positionWS, normalWS, output.uvMeters, tangentWS, bitangentWS);
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalWS;
    output.tangentWS = tangentWS;
    output.bitangentWS = bitangentWS;

    output.fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    output.positionCS = vertexInput.positionCS;
    return output;
}

// Anti-repetición de los suelos (_TopOnly): una textura de 2–4 m repetida en un campo de kilómetros
// forma rayas rectas a ras de suelo. Se lee otra vez girada 37° y a otra escala, y se mezclan las dos
// por manchitas de ~3 m (_PatchMap.b): los bordes de la repetición ya no quedan en línea.
half3 SampleColor(TEXTURE2D_PARAM(map, samplerMap), float2 uv, half antiTile)
{
    half3 c = SAMPLE_TEXTURE2D(map, samplerMap, uv).rgb;
    if (antiTile > 0.0)
    {
        float2 r = float2(uv.x * 0.799 - uv.y * 0.602, uv.x * 0.602 + uv.y * 0.799) * 0.83 + 0.37;
        c = lerp(c, SAMPLE_TEXTURE2D(map, samplerMap, r).rgb, antiTile);
    }
    return c;
}

// Color, relieve y lisura de la superficie en este punto.
void SampleSurface(float2 uvMeters, out half3 albedo, out half3 normalTS, out half smoothness)
{
    half antiTile = 0.0;
    if (_TopOnly > 0.5)
        antiTile = smoothstep(0.3, 0.7, SAMPLE_TEXTURE2D(_PatchMap, sampler_PatchMap, uvMeters / 24.0).b);

    float2 uv = uvMeters / _Tile;
    albedo = SampleColor(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), uv, antiTile) * _BaseColor.rgb;
    normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    smoothness = _Smoothness;

    if (_Layer2On > 0.5)
    {
        // Segunda capa (la grama sobre la tierra) por manchas: el ruido de _PatchMap.r decide dónde, y
        // la diferencia de brillo de las dos texturas deshilacha el borde para que no sea una curva lisa.
        float2 uv2 = uvMeters / _Layer2Tile;
        half3 albedo2 = SampleColor(TEXTURE2D_ARGS(_Layer2Map, sampler_BaseMap), uv2, antiTile) * _Layer2Color.rgb;
        half3 normal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_Layer2BumpMap, sampler_BumpMap, uv2), _BumpScale);
        // El ruido trae unas cuatro manchas por repetición: se repite cada 4 × _PatchScale. Dos lecturas
        // a escalas que no encajan (× 0,37) rompen la cuadrícula que se vería en la sabana desde lejos.
        float2 pu = uvMeters / (_PatchScale * 4.0);
        half patch = SAMPLE_TEXTURE2D(_PatchMap, sampler_PatchMap, pu).r * 0.6
                   + SAMPLE_TEXTURE2D(_PatchMap, sampler_PatchMap, pu * 0.37 + 0.21).r * 0.4;
        patch = saturate((patch - 0.5) * 1.5 + 0.5);
        half edge = dot(albedo2 - albedo, half3(0.333, 0.333, 0.333));
        half k = smoothstep(1.0 - _PatchCover - _PatchSoftness, 1.0 - _PatchCover + _PatchSoftness, patch + edge * 0.35);
        albedo = lerp(albedo, albedo2, k);
        normalTS = normalize(lerp(normalTS, normal2, k));
        smoothness = lerp(smoothness, _Layer2Smoothness, k);
    }

    // Variación de tono a gran escala (_PatchMap.g, gris 0,5 = neutro): sin ella, la repetición de
    // una textura de pocos metros se ve en cuadrícula desde lejos.
    float2 mu = uvMeters / _MacroScale;
    half macro = SAMPLE_TEXTURE2D(_PatchMap, sampler_PatchMap, mu).g * 0.6
               + SAMPLE_TEXTURE2D(_PatchMap, sampler_PatchMap, mu * 0.29 + 0.53).g * 0.4;
    albedo *= lerp(1.0, macro * 2.0, _MacroStrength);
}

void SuperficieFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half3 albedo, normalTS;
    half smoothness;
    SampleSurface(input.uvMeters, albedo, normalTS, smoothness);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.alpha = 1.0;
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = normalTS;
    surfaceData.occlusion = 1.0;

    // Base tangente de la proyección, ajustada a la normal real (techos inclinados, edificios girados).
    // En los cantos de los suelos (_TopOnly proyecta desde arriba también las caras verticales) una de
    // las dos tangentes queda paralela a la normal y se anula: normalizarla daba NaN, y el bloom lo
    // convertía en un destello enorme. Se rehace con la otra, en float para que no se pierda precisión.
    float3 n = normalize(input.normalWS);
    float3 t = input.tangentWS - n * dot(n, input.tangentWS);
    float3 b = input.bitangentWS - n * dot(n, input.bitangentWS);
    if (dot(t, t) < 1e-4) t = cross(n, b);
    if (dot(b, b) < 1e-4) b = cross(t, n);
    t = normalize(t);
    b = normalize(b - t * dot(t, b));
    half3x3 tangentToWorld = half3x3(t, b, n);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
                                  inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = 1.0;
    outColor = color;
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
