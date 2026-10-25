Shader "RootAccess/BackdropBlur"
{
 Properties { _MainTex("Texture",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass { CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _Direction;
   fixed4 frag(v2f_img i):SV_Target {
    float2 step=_Direction.xy;
    fixed4 c=tex2D(_MainTex,i.uv)*.227027;
    c+=(tex2D(_MainTex,i.uv+step)+tex2D(_MainTex,i.uv-step))*.1945946;
    c+=(tex2D(_MainTex,i.uv+step*2)+tex2D(_MainTex,i.uv-step*2))*.1216216;
    c+=(tex2D(_MainTex,i.uv+step*3)+tex2D(_MainTex,i.uv-step*3))*.054054;
    c+=(tex2D(_MainTex,i.uv+step*4)+tex2D(_MainTex,i.uv-step*4))*.016216;
    return c;
   }
   ENDCG }
 }
}
