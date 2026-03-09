Shader "Hidden/MinimapOverlayMarker" {
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        ZTest Always // <--- Mấu chốt để Marker luôn đè lên ngọn đồi
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}
