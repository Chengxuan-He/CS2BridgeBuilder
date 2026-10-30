Shader "Hidden/Lift04PreparedBoundary" {
Properties { _MainTex("Source",2D)="white"{} _Prepared("Prepared",2D)="white"{} }
SubShader { Pass { ZTest Always Cull Off ZWrite Off
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma target 3.5
#include "UnityCG.cginc"
sampler2D _MainTex,_Prepared;float4 _Bounds,_Content;float _Mip;int _Mode;
float4 frag(v2f_img i):SV_Target {
 int edge=(int)floor(i.uv.y*4);float2 q=float2(i.uv.x,i.uv.x);
 if(edge<2)q.x=edge;else q.y=edge-2;
 float2 a=lerp(_Bounds.xy,_Bounds.zw,q),b=lerp(_Content.xy,_Content.zw,q);
 return _Mode==0?tex2Dlod(_MainTex,float4(a,0,_Mip)):tex2Dlod(_Prepared,float4(b,0,_Mip));
}
ENDCG
} } }
