Shader "Hidden/BB129/Missing" { Properties {  _Smoothness("Smooth",Float)=.5 _EmissiveIntensity("Emission",Float)=0 _Metallic("Metal",Float)=0 _NormalScale("Normal",Float)=1 _AlphaToMask("Alpha",Float)=0 _BaseColor("Color",Color)=(1,1,1,1) } SubShader { Pass { Name "Forward" CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma shader_feature_local _TEST_APPEARANCE
#pragma shader_feature_local _TANGENTSPACE_OCTO
#include "UnityCG.cginc"
float4 vert(float4 v:POSITION):SV_POSITION{return UnityObjectToClipPos(v);} float4 frag():SV_Target{return 1;}
ENDCG } } }