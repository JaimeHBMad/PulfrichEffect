// Desenfoque de un solo ojo para VR (HDRP, Custom Post Process).
// Lo usa el componente de Volume "OneEyeBlur" (OneEyeBlur.cs).
// Requiere XR Render Mode "Single Pass Instanced".
Shader "Hidden/Shader/OneEyeBlur"
{
    Properties
    {
        // Necesario para que HDRP enlace la textura de origen
        _MainTex ("Main Texture", 2DArray) = "grey" {}
    }

    HLSLINCLUDE

    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    // Número de muestras del desenfoque. Más = más suave pero más caro.
    #define SAMPLE_COUNT 24
    #define GOLDEN_ANGLE 2.39996323

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    TEXTURE2D_X(_MainTex);
    float _BlurRadius;
    float _BlurLeft;
    float _BlurRight;

    uint GetEyeIndex(float2 uv)
    {
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        // Single Pass Instanced: 0 = ojo izquierdo, 1 = ojo derecho
        return unity_StereoEyeIndex;
    #else
        // Sin casco (Game view normal): vista previa a pantalla partida.
        // Mitad izquierda = ojo izquierdo, mitad derecha = ojo derecho.
        return uv.x < 0.5 ? 0u : 1u;
    #endif
    }

    // uv en [0,1] de pantalla; se ajusta a la escala de los RTHandles de HDRP.
    float4 SampleSource(float2 uv)
    {
        return SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler,
                                  ClampAndScaleUVForBilinearPostProcessTexture(uv));
    }

    // Desenfoque en disco con distribución en espiral (ángulo áureo) y pesos gaussianos.
    float3 DiskBlur(float2 uv, float radius)
    {
        float3 sum = SampleSource(uv).rgb;
        float weightSum = 1.0;

        UNITY_UNROLL
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float r = sqrt((i + 0.5) / SAMPLE_COUNT);
            float a = i * GOLDEN_ANGLE;
            float2 offset = float2(cos(a), sin(a)) * (r * radius);
            float w = exp(-2.0 * r * r);
            sum += SampleSource(uv + offset).rgb * w;
            weightSum += w;
        }
        return sum / weightSum;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;
        float4 color = SampleSource(uv);

        uint eye = GetEyeIndex(uv);
        float amount = (eye == 0u) ? _BlurLeft : _BlurRight;
        float radius = _BlurRadius * amount;

        // Rama uniforme por ojo: el ojo nítido no paga el coste del desenfoque.
        UNITY_BRANCH
        if (radius <= 0.0001)
            return color;

        color.rgb = DiskBlur(uv, radius);
        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "OneEyeBlur"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
