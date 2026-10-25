Shader "RootAccess/FigmaShape"
{
 Properties {
  [PerRendererData] _MainTex("Texture",2D)="white"{} _Color("Color",Color)=(1,1,1,1)
  _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 shape:TEXCOORD1;float4 arc:TEXCOORD2;float4 radii:TEXCOORD3;};
   struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;float4 shape:TEXCOORD2;float4 arc:TEXCOORD3;float4 radii:TEXCOORD4;};
   sampler2D _MainTex,_MaskTex; float4 _Color,_ClipRect;
   float4 _Solid,_Stroke,_G0,_G1;
   float4 _Stops[16],_StopPositions[16];
   float4 _EffectGeometry[4],_EffectColors[4],_EffectType;
   float _Kind,_PaintType,_StopCount,_StrokeWidth,_Asset,_HasMask,_EffectCount;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;o.shape=v.shape;o.arc=v.arc;o.radii=v.radii;return o;}
   float distShape(float2 uv,float2 size,float4 arc,float4 radii){
    if(_Kind>0.5){float2 p=(uv-.5)*2;float d=(length(p)-1)*min(size.x,size.y)*.5;
     if(arc.z>0)d=max(d,(arc.z-length(p))*min(size.x,size.y)*.5);
     if(arc.y-arc.x<6.282){float angle=atan2(p.y,p.x);if(angle<0)angle+=6.2831853;float start=fmod(arc.x+12.566371,6.2831853);float delta=fmod(angle-start+6.2831853,6.2831853);if(delta>arc.y-arc.x)d=max(d,1);}
     return d;}
    float2 p=(uv-.5)*size;float r=uv.y<.5?(uv.x<.5?radii.x:radii.y):(uv.x<.5?radii.w:radii.z);
    r=min(r,min(size.x,size.y)*.5);float2 q=abs(p)-size*.5+r;return length(max(q,0))+min(max(q.x,q.y),0)-r;
   }
   float4 paint(float2 uv){if(_PaintType<1.5)return _Solid;float2 g=float2(dot(_G0.xyz,float3(uv,1)),dot(_G1.xyz,float3(uv,1)));float t=_PaintType<2.5?g.x:length((g-.5)*2);float4 c=_Stops[0];
    for(int j=1;j<16;j++){if(j>=_StopCount)break;float f=saturate((t-_StopPositions[j-1].x)/max(.00001,_StopPositions[j].x-_StopPositions[j-1].x));c=lerp(c,_Stops[j],f);}return c;}
   float4 over(float4 back,float4 fore){float a=fore.a+back.a*(1-fore.a);return float4((fore.rgb*fore.a+back.rgb*back.a*(1-fore.a))/max(a,.00001),a);}
   float4 frag(v2f i):SV_Target {
    float2 uv=float2(i.uv.x,1-i.uv.y),size=i.shape.xy;float d=distShape(uv,size,i.arc,i.radii),aa=max(fwidth(d),.65),inside=1-smoothstep(-aa*.5,aa*.5,d);
    float4 result=0;
    for(int e=0;e<4;e++){if(e>=_EffectCount)break;if(_EffectType[e]<1.5){float4 g=_EffectGeometry[e];g.y-=i.shape.w;float sd=distShape(uv-g.xy/size,size,i.arc,i.radii)-g.w;float alpha=1-smoothstep(-g.z*.5-aa,g.z*.5+aa,sd);float4 c=_EffectColors[e];c.a*=alpha;result=over(result,c);}}
    float4 fill=_Asset>.5?tex2D(_MainTex,i.uv):paint(uv);fill.a*=inside;
    if(_HasMask>.5)fill.a*=tex2D(_MaskTex,i.uv).a;
    result=over(result,fill);
    for(int e=0;e<4;e++){if(e>=_EffectCount)break;if(_EffectType[e]>1.5){float4 g=_EffectGeometry[e];float sd=distShape(uv-g.xy/size,size,i.arc,i.radii)+g.w;float alpha=smoothstep(-g.z-aa,aa,sd)*inside;float4 c=_EffectColors[e];c.a*=alpha;result=over(result,c);}}
    if(_StrokeWidth>0){float4 stroke=_Stroke;stroke.a*=inside*smoothstep(-_StrokeWidth-aa,-_StrokeWidth+aa,d);result=over(result,stroke);}
    result*=i.color;
    result.rgb*=i.shape.z;
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(result.a-.001);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
