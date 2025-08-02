Shader "HikanyanLaboratory/FadeMask"
{
    Properties
    {
        _Color ("Main Color", Color) = (.5,.5,.5,1)
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _CutOff("Cut off", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;

            float4 _Color;
            float _CutOff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // メインテクスチャは添えるだけ
                fixed4 col = _Color * tex2D(_MainTex, i.texcoord);

                fixed maskValue = tex2D(_MaskTex, i.texcoord).a;

                // マスクのアルファ値がカットオフ値以下なら描画をスキップ（ピクセルを破棄）
                if (maskValue < _CutOff) discard;

                // カットオフを適用したカラーを返す
                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}