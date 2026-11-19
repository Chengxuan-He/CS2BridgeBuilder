using System.Numerics;
using BridgeBuilder.Bridges;

var palettes = new[] {
    new[] { Vector4.One, Vector4.One, Vector4.One },
    new[] { Vector4.One, new Vector4(.2529503f, .130393f, .104226f, .5f), Vector4.One },
    new[] { new Vector4(.2f,.4f,.6f,.8f), new Vector4(.8f,.3f,.1f,1), new Vector4(.1f,.7f,.4f,.25f) },
    new[] { Vector4.One, new Vector4(1,1,1,.5f), Vector4.One }
};
int cases = 0;
foreach (var colors in palettes)
foreach (float r in new[] { 0f, .125f, .5f, 1f })
foreach (float g in new[] { 0f, .125f, .5f, 1f })
foreach (float b in new[] { 0f, .125f, .5f, 1f })
{
    var control = new Vector3(r,g,b);
    var actual = NativePalette.Factor(control,colors[0],colors[1],colors[2]);
    // Independent scalar transcription of source567 GBuffer sample/mad sequence.
    for (int component=0;component<4;component++)
    {
        float rg=r+g, sum=b+rg, mix=colors[0][component];
        mix = g*rg*(colors[1][component]-mix)+mix;
        mix = b*sum*(colors[2][component]-mix)+mix;
        float expected=Math.Min(sum,1)*(mix-1)+1;
        if(Math.Abs(actual[component]-expected)>1e-6f)throw new Exception("Native color expression differs");
    }
    bool coat = r*(1-colors[0].W)!=0 || g*(1-colors[1].W)!=0 || b*(1-colors[2].W)!=0;
    if(NativePalette.RemovesCoating(control,colors[0],colors[1],colors[2])!=coat)throw new Exception("Native coating branch differs");
    if(control==Vector3.Zero && actual!=Vector4.One)throw new Exception("Unmasked pixels changed");
    cases++;
}
var alphaOnly=NativePalette.Factor(Vector3.UnitY,Vector4.One,new Vector4(1,1,1,.5f),Vector4.One);
if(alphaOnly!=new Vector4(1,1,1,.5f))throw new Exception("White RGB with nonwhite alpha was bypassed");
Console.WriteLine($"PASS {cases} native color-mask expressions; alpha-only smoothness/coating and unmasked preservation.");
