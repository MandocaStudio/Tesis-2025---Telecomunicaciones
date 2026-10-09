// "PVI/Superficie en metros": URP Lit con las UV sacadas de la posición en el mundo, en metros.
//
// Para qué: en el Módulo 3D casi todo es un cubo de 1 m escalado a su medida, y una textura normal se
// estira con la pieza. Aquí la textura se apoya en el plano al que mira cada cara y se repite cada
// "Metros por repetición", sea cual sea el tamaño de la pieza. Una lectura por textura (más barato
// que un triplanar), pensado para gráficas integradas.
//
// Extra para el suelo: una segunda capa (grama sobre tierra) mezclada por manchas grandes, y una
// variación de tono que rompe la repetición. Ver MODULO-3D.md §4.5.
//
// Solo para piezas QUIETAS: la textura no se mueve con la pieza (las antenas giran: no lo usan).
Shader "PVI/Superficie en metros"
{
    Properties
    {
        _BaseMap ("Color (textura)", 2D) = "white" {}
        _BaseColor ("Tinte", Color) = (1, 1, 1, 1)
        [Normal][NoScaleOffset] _BumpMap ("Relieve (normal map)", 2D) = "bump" {}
        _BumpScale ("Fuerza del relieve", Float) = 1
        _Smoothness ("Lisura", Range(0, 1)) = 0.1
        _Metallic ("Metálico", Range(0, 1)) = 0
        _Tile ("Metros por repetición", Float) = 3
        [Toggle] _TopOnly ("Proyectar siempre desde arriba (terreno con pendiente)", Float) = 0

        [Header(Segunda capa por manchas)]
        [Toggle] _Layer2On ("Activar", Float) = 0
        [NoScaleOffset] _Layer2Map ("Color (textura)", 2D) = "white" {}
        _Layer2Color ("Tinte", Color) = (1, 1, 1, 1)
        [Normal][NoScaleOffset] _Layer2BumpMap ("Relieve (normal map)", 2D) = "bump" {}
        _Layer2Tile ("Metros por repetición", Float) = 3
        _Layer2Smoothness ("Lisura", Range(0, 1)) = 0.1
        _PatchScale ("Tamaño de las manchas (m)", Float) = 25
        _PatchCover ("Cuánta segunda capa", Range(0, 1)) = 0.4
        _PatchSoftness ("Borde de las manchas", Range(0.01, 0.5)) = 0.12

        [Header(Manchas y variacion de tono)]
        [NoScaleOffset] _PatchMap ("Ruido (R: manchas, G: tono)", 2D) = "gray" {}
        _MacroScale ("Tamaño de la variación de tono (m)", Float) = 60
        _MacroStrength ("Fuerza de la variación de tono", Range(0, 1)) = 0.25

        [HideInInspector] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SuperficieVertex
            #pragma fragment SuperficieFragment

            // Las mismas variantes de luz que URP Lit, sin lightmaps (la escena no hornea).
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "SuperficieEnMetrosForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "SuperficieEnMetrosInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "SuperficieEnMetrosInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Normales de la geometría (sin el relieve) para el SSAO del nivel alto.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing

            #include "SuperficieEnMetrosInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
