using System.Reflection;
using BridgeBuilder.Bridges;

var constructor=typeof(MovableUvDomains).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
MovableUvDomains Read(string path)=>(MovableUvDomains)constructor.Invoke(new object[]{HardcodedNumbers.Decode(File.ReadAllBytes(path),path)});
long domains=0,faces=0;int resources=0;
foreach(var original in Directory.GetFiles(args[0],"UvDomain_*.bin",SearchOption.AllDirectories))
{
    var candidate=Path.Combine(args[1],Path.GetRelativePath(args[0],original));
    var a=Read(original).Items;var b=Read(candidate).Items;
    if(a.Length!=b.Length)throw new Exception("Domain count changed: "+original);
    for(int i=0;i<a.Length;i++)
    {
        if(a[i].A!=b[i].A || a[i].B!=b[i].B || a[i].C!=b[i].C || a[i].Triangle!=b[i].Triangle || !a[i].Faces.SequenceEqual(b[i].Faces))
            throw new Exception($"Domain topology mismatch: {original}/{i}");
        for(int axis=0;axis<4;axis++)if(BitConverter.SingleToInt32Bits(a[i].Bounds[axis])!=BitConverter.SingleToInt32Bits(b[i].Bounds[axis]))
            throw new Exception($"Domain Float32 bit mismatch: {original}/{i}/{axis}");
        faces+=a[i].Faces.Length;
    }
    domains+=a.Length;resources++;
}
Console.WriteLine($"PASS actual runtime decoding: {resources} resources, {domains} domains, {faces} ordered faces, exact anchor/flag/Float32 bits.");

namespace BridgeBuilder.Bridges
{
    internal static class MovableUvCatalogue
    {
        internal static HardcodedNumbers? Load(string key)=>throw new Exception("Recipe check must not load catalogues or unrelated bridge data: "+key);
    }
}
