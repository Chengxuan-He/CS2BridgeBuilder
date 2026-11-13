using System.Text;
using BridgeBuilder.UI;
var count = 0;
void Check(byte[] bytes, float expected, string label)
{
    using var stream = new MemoryStream(bytes);
    if (!BridgeIconDimensions.TryRead(stream, out var ratio) || Math.Abs(ratio-expected)>1e-6f)
        throw new Exception(label+": "+ratio);
    count++; Console.WriteLine("PASS "+label+" ratio="+ratio);
}
void Svg(string text, float expected, string label) => Check(Encoding.UTF8.GetBytes(text),expected,label);
Svg("<svg width='600' height='300'/>",2,"explicit landscape SVG");
Svg("<svg width='100%' height='100%' viewBox='0 0 300 600'/>",.5f,"percentage SVG uses viewBox");
Svg("<svg viewBox='-10,-20,150,150'/>",1,"square viewBox with nonzero origin");
Svg("<svg width='1in' height='48px' viewBox='0 0 1 1'/>",2,"CSS length units and viewport precedence");
Svg("<svg width='10' height='5'/>",2,"unitless intrinsic lengths");
var dtd = "<!DOCTYPE svg PUBLIC '-//W3C//DTD SVG 1.1//EN' 'http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd'>";
Svg("<?xml version='1.0' encoding='utf-8' standalone='no'?>"+dtd+"<svg width='100%' height='100%' viewBox='0 0 219.502 219.502'/>",1,"Road Builder SVG 1.1 DOCTYPE");
Check(new byte[]{239,187,191}.Concat(Encoding.UTF8.GetBytes("<?xml version='1.0' encoding='utf-8'?>"+dtd+"<svg width='100%' height='100%' viewBox='0 0 400 200'/>" )).ToArray(),2,"BOM and DOCTYPE landscape");
Svg("<!DOCTYPE svg SYSTEM 'file:///missing-unreadable-external.dtd'><svg width='100' height='200'/>",.5f,"external DTD is never required");
Svg("<!DOCTYPE svg [<!ENTITY extent '400'>]><svg width='50' height='100'/>",.5f,"internal DTD is ignored");
try {
    Svg("<!DOCTYPE svg [<!ENTITY extent '400'>]><svg width='&extent;' height='100'/>",4,"must not expand entity");
    throw new Exception("Declared entity was expanded");
} catch(System.Xml.XmlException) { count++; Console.WriteLine("PASS declared entities are not expanded"); }
var gif=Encoding.ASCII.GetBytes("GIF89a\0\0\0\0");gif[6]=100;gif[8]=200;Check(gif,.5f,"GIF canvas");
Check(new byte[]{255,216,255,192,0,8,8,0,100,0,200,0},2,"JPEG frame dimensions");
var png = new byte[24];
new byte[]{137,80,78,71,13,10,26,10}.CopyTo(png,0);
png[18]=2; png[19]=88; png[22]=1; png[23]=44;
Check(png,2,"PNG landscape canvas");
foreach(var path in args)
    Check(File.ReadAllBytes(path),1,"actual original "+Path.GetFileName(path));
using(var empty = new MemoryStream(new byte[]{255,216,255,217}))
    if(BridgeIconDimensions.TryRead(empty,out _)) throw new Exception("Missing JPEG frame accepted");
Console.WriteLine($"PASS {count} dimension cases plus malformed frame; no game or browser used.");
