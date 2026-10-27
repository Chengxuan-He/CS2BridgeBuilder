Shader "Hidden/Lift05/ControlMipRead"
{
    Properties { _MainTex ("Source", 2D) = "black" {} _Mip ("Mip", Float) = 0 }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Mip;
            float4 frag(v2f_img i) : SV_Target { return tex2Dlod(_MainTex, float4(i.uv, 0, _Mip)); }
            ENDCG
        }
    }
}
