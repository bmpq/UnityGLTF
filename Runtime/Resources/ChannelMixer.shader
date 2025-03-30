Shader "Hidden/ChannelMixer"
{
    Properties
    {
        _TexFirst ("Texture First (Source)", 2D) = "white" {}
        _TexSecond ("Texture Second (Source)", 2D) = "white" {}

        [Enum(ChannelMapping.ChannelSource)] _SourceR ("Output R Source", Float) = 0 // Default: TexFirst Red
        [Enum(ChannelMapping.ChannelSource)] _SourceG ("Output G Source", Float) = 1 // Default: TexFirst Green
        [Enum(ChannelMapping.ChannelSource)] _SourceB ("Output B Source", Float) = 2 // Default: TexFirst Blue
        [Enum(ChannelMapping.ChannelSource)] _SourceA ("Output A Source", Float) = 3 // Default: TexFirst Alpha
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _TexFirst;
            sampler2D _TexSecond;
            float _SourceR; // Enum values arrive as floats
            float _SourceG;
            float _SourceB;
            float _SourceA;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float GetChannel(float4 colFirst, float4 colSecond, float sourceIndex)
            {
                int idx = (int)(sourceIndex);

                if      (idx == 0) return colFirst.r; // TexA Red
                else if (idx == 1) return colFirst.g; // TexA Green
                else if (idx == 2) return colFirst.b; // TexA Blue
                else if (idx == 3) return colFirst.a; // TexA Alpha
                else if (idx == 4) return colSecond.r; // TexB Red
                else if (idx == 5) return colSecond.g; // TexB Green
                else if (idx == 6) return colSecond.b; // TexB Blue
                else if (idx == 7) return colSecond.a; // TexB Alpha

                return 0.0;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 colFirst = tex2D(_TexFirst, i.uv);
                float4 colSecond = tex2D(_TexSecond, i.uv);

                float outR = GetChannel(colFirst, colSecond, _SourceR);
                float outG = GetChannel(colFirst, colSecond, _SourceG);
                float outB = GetChannel(colFirst, colSecond, _SourceB);
                float outA = GetChannel(colFirst, colSecond, _SourceA);

                return float4(outR, outG, outB, outA);
            }
            ENDCG
        }
    }
}