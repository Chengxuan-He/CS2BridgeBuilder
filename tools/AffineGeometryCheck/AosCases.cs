using BridgeBuilder.Bridges;
using System.Security.Cryptography;

// Exercise the actual offline lowering and runtime together, including patterns
// absent from today's measured tables. No independent replacement SIMD kernel.
internal static class AosCases
{
    internal static void Run()
    {
        int cases=0;
        foreach(bool dependent in new[]{false,true})foreach(bool preload in new[]{false,true})foreach(bool alias in new[]{false,true})foreach(int count in Enumerable.Range(0,17))
        {
            var code=new List<int[]>();int register=0;
            int Op(int op,int a,int b=0,int c=0){int r=register++;code.Add(new[]{op,r,a,b,c,0,0,0});return r;}
            void Store(int r,int axis)=>code.Add(new[]{10,r,1,axis,-1,0,0,0});
            int x=Op(2,0,0,-1),y=Op(2,0,1,-1),z=Op(2,0,2,-1),p=Op(1,0),q=Op(1,1);
            Store(Op(4,x,Op(6,y,p)),0);
            int later=dependent?Op(2,1,0,-1):z;
            Store(Op(5,later,q),1);Store(preload?Op(4,x,Op(0,0)):Op(0,0),2);
            byte[] packed=AosPacking.Convert(Bbn(code,register,new[]{-0f}));
            var data=HardcodedNumbers.Decode(packed,"AoS test");
            var source=Enumerable.Range(0,24).Select(i=>new Point(i*1.25f-6,i%3-1,i*.125f)).ToArray();
            var output=alias?source:(Point[])source.Clone();var expectedSource=(Point[])source.Clone();var expected=alias?expectedSource:(Point[])output.Clone();
            float[] parameters={.25f,-3.5f};int first=3;
            Interpret(code,register,new[]{expectedSource,expected},parameters,new[]{-0f},first,count);
            AffineGeometry.Execute(data,0,new[]{source,output},Array.Empty<float[]>(),parameters,first,count);
            for(int i=0;i<output.Length;i++)Equal(expected[i],output[i]);cases++;
        }
        foreach(int count in new[]{511,512,513,1025})foreach(bool alias in new[]{false,true})
        {
            var code=new List<int[]>{new[]{2,0,0,0,-1,0,0,0},new[]{1,1,0,0,0,0,0,0},new[]{4,2,0,1,0,0,0,0},new[]{10,2,1,0,-1,0,0,0}};
            var data=HardcodedNumbers.Decode(AosPacking.Convert(Bbn(code,3,Array.Empty<float>(),count)),"chunk proof");
            if(data.I32[data.I32[3]+4]!=512)throw new Exception("Independent rows not scheduled in blocks.");
            var source=Enumerable.Range(0,count).Select(i=>new Point(i*.25f,-i,i)).ToArray();var output=alias?source:(Point[])source.Clone();
            var expected=(Point[])source.Clone();for(int i=0;i<count;i++)expected[i].X+=.125f;
            AffineGeometry.Execute(data,0,new[]{source,output},Array.Empty<float[]>(),new[]{.125f});
            for(int i=0;i<count;i++)Equal(expected[i],output[i]);cases++;
        }
        // Untouched target axes must retain the exact NaN payload and signed zero.
        foreach(int count in Enumerable.Range(0,17))
        {
            var source=Enumerable.Range(0,24).Select(i=>new Point(i,BitConverter.Int32BitsToSingle(unchecked((int)0x7fc01234)),BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)))).ToArray();
            var output=(Point[])source.Clone();var weights=Enumerable.Repeat(.25f,24).ToArray();
            AffineGeometry.Execute(StructurePrograms.Data,StructurePrograms.DenseDelta,new[]{source,output},new[]{weights},new[]{8f},3,count);
            for(int i=0;i<24;i++)Equal(new Point(source[i].X+(i>=3 && i<3+count?2f:0f),source[i].Y,source[i].Z),output[i]);cases++;
        }
        Console.WriteLine($"PASS {cases} offline-to-runtime AoS cases: mixed XYZ, cross-axis write/read, aliased snapshots, first offset, tails, unchanged NaN payload and signed zero.");
    }
    static byte[] Bbn(List<int[]> code,int registers,float[] values,int count=-1)
    {
        var words=new List<int>{0x31465041,1,1,4,count,registers,code.Count};foreach(var c in code)words.AddRange(c);
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
        {writer.Write(0x314e4242);writer.Write(1);foreach(int length in new[]{words.Count,0,0,0,values.Length,0})writer.Write(length);foreach(int w in words)writer.Write(w);foreach(float v in values)writer.Write(v);}
        byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));return stream.ToArray();
    }
    static void Interpret(List<int[]> code,int registers,Point[][] coordinates,float[] parameters,float[] values,int first,int count)
    {
        var data=new float[registers][];for(int i=0;i<registers;i++)data[i]=new float[count];
        foreach(var c in code)for(int row=0;row<count;row++)
        {
            if(c[0]==10){ref var point=ref coordinates[c[2]][first+row];if(c[3]==0)point.X=data[c[1]][row];else if(c[3]==1)point.Y=data[c[1]][row];else point.Z=data[c[1]][row];continue;}
            var source=c[0]==2?coordinates[c[2]][first+row]:default;
            data[c[1]][row]=c[0] switch{0=>values[c[2]],1=>parameters[c[2]],2=>c[3]==0?source.X:c[3]==1?source.Y:source.Z,4=>data[c[2]][row]+data[c[3]][row],5=>data[c[2]][row]-data[c[3]][row],6=>data[c[2]][row]*data[c[3]][row],_=>throw new Exception("Oracle opcode")};
        }
    }
    static void Equal(Point a,Point b)
    {if(BitConverter.SingleToInt32Bits(a.X)!=BitConverter.SingleToInt32Bits(b.X)||BitConverter.SingleToInt32Bits(a.Y)!=BitConverter.SingleToInt32Bits(b.Y)||BitConverter.SingleToInt32Bits(a.Z)!=BitConverter.SingleToInt32Bits(b.Z))throw new Exception($"AoS mismatch {a.X},{a.Y},{a.Z} / {b.X},{b.Y},{b.Z}");}
}
