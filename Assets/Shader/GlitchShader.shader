Shader "Custom/GlitchEffectRandomTimingURP_Irregular"
{
    Properties
    {
        _MainTex           ("Texture",          2D)    = "white" {}
        _BlockSize         ("Block Size",       Range(1,100))  = 20
        _GlitchAmount      ("Glitch Amount",    Range(0,1))    = 0.1
        _GlitchFrequency   ("Glitch Frequency", Range(0.1,10)) = 1
        _GlitchChance      ("Glitch Chance",    Range(0,1))    = 0.02
        _ColorSeparation   ("Color Separation", Range(0,0.1))  = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "GlitchPass"
            ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _BlockSize;
            float _GlitchAmount;
            float _GlitchFrequency;
            float _GlitchChance;
            float _ColorSeparation;

            // �V���v���� 2D �n�b�V���֐�
            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float time = _Time.y;

                // �s�K���O���b�`����FFrequency �ŃT���v�����O���������� Chance �ȉ��Ȃ�A�N�e�B�u
                float sample = rand(float2(time * _GlitchFrequency, time * 0.37));
                float isActive = step(sample, _GlitchChance);

                // �u���b�N�P�� UV
                float2 block = floor(uv * _BlockSize) / _BlockSize;

                // ��{�I�t�Z�b�g
                float baseRand = rand(float2(block.y, time * _GlitchFrequency + 1.23));
                float offset = (baseRand * 2.0 - 1.0) * _GlitchAmount * isActive;

                // �e�`�����l���p�I�t�Z�b�g�i�F������傫�߂Ɂj
                float sep = _ColorSeparation * isActive * 2.0; // �{�� 2.0 �ŋ���

                float3 col;
                col.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset + sep, offset)).r;
                col.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset,        offset)).g;
                col.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset - sep, offset)).b;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
