using System;
namespace RootAccess.UI
{
    [Serializable] public sealed class ScreenIndex { public string[] screens; }
    [Serializable] public sealed class FigmaScreen { public string name, figmaId; public FigmaNode[] nodes; }
    [Serializable] public sealed class FigmaNode
    {
        public string id, name, kind, text, asset, mask, imageMode, font, fontStyle, align, valign, letterUnit, strokeAlign;
        public int parent, assetWidth, assetHeight;
        public float x,y,w,h,opacity=1,rotation,strokeWidth,fontSize,lineHeight,letterSpacing;
        public float[] radii;
        public bool clip;
        public FigmaPaint[] fills,strokes;
        public FigmaEffect[] effects;
        public FigmaArc arc;
    }
    [Serializable] public sealed class FigmaArc { public float startingAngle,endingAngle,innerRadius; }
    [Serializable] public sealed class FigmaPaint { public string type; public float[] color,matrix; public float opacity=1; public FigmaStop[] stops; }
    [Serializable] public sealed class FigmaStop { public float position; public float[] color; }
    [Serializable] public sealed class FigmaEffect { public string type; public float radius,spread,offsetX,offsetY;public float[] color; }
}
