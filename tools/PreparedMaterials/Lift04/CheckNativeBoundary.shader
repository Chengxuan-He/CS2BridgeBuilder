Shader "Hidden/Lift04NativeBoundary"
{
    Properties { _MainTex("Source",2D)="white"{} }
    SubShader { Pass { ZTest Always Cull Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
        #pragma target 3.5
        #include "UnityCG.cginc"
        sampler2D _MainTex; float4 _MainTex_TexelSize; float4 _Bounds; int _OutputMode;
        float4 sampleSource(float2 uv) { return tex2Dlod(_MainTex,float4(uv,0,0)); }
        float4 frag(v2f_img i):SV_Target
        {
            int edge=(int)floor(i.uv.y*4);
            float2 halfPixel=_MainTex_TexelSize.xy*.5;
            float2 uv=lerp(_Bounds.xy,_Bounds.zw,float2(i.uv.x,i.uv.x));
            float2 first=uv,last=uv;
            if(edge<2) { uv.x=edge==0?_Bounds.x:_Bounds.z;first.x=_Bounds.x+halfPixel.x;last.x=_Bounds.z-halfPixel.x; }
            else { uv.y=edge==2?_Bounds.y:_Bounds.w;first.y=_Bounds.y+halfPixel.y;last.y=_Bounds.w-halfPixel.y; }
            float4 original=sampleSource(uv), repeated=(sampleSource(first)+sampleSource(last))*.5;
            return _OutputMode==1?original:(_OutputMode==2?repeated:abs(original-repeated));
        }
        ENDCG
    } }
}
