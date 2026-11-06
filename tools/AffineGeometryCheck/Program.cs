using BridgeBuilder.Bridges;

var random = new Random(12031);
var source = new Point[60000];
for (int i=0;i<source.Length;i++) source[i]=new Point((float)(random.NextDouble()*200-100),i,-i);
var rows=Enumerable.Range(0,120000).Select(i=>i%source.Length).ToArray();
var weights=Enumerable.Range(0,rows.Length).Select(_=>(float)(random.NextDouble()*4-2)).ToArray();
int checks=0;
int decoded=0;
foreach(string name in typeof(HardcodedNumbers).Assembly.GetManifestResourceNames().Where(n=>n.StartsWith("BridgeBuilder.Numbers.") && n.EndsWith(".bin")))
{
    var data=HardcodedNumbers.Load(name.Substring("BridgeBuilder.Numbers.".Length,name.Length-"BridgeBuilder.Numbers.".Length-4));
    if(data.I32.Length==0)continue;
    if(data.I32[0]==0x31465041)throw new Exception("Legacy program shipped.");
    if(data.I32[0]!=0x32465041)continue;
    typeof(StructureCoordinateProgram).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Single().Invoke(new object[]{data});decoded++;
}
if(decoded==0)throw new Exception("No packed resources decoded.");
Console.WriteLine($"Decoded {decoded} packed resources.");
AosCases.Run();
foreach(float width in new[]{-12f,-.125f,-0f,0f,.125f,3.5f,16f})
{
    var expected=(Point[])source.Clone();
    foreach(int i in Enumerable.Range(0,rows.Length))expected[rows[i]].X=source[rows[i]].X+weights[i]*width;
    var actual=(Point[])source.Clone();
    AffineGeometry.Execute(StructurePrograms.Data,StructurePrograms.RowDelta,new[]{source,actual},new[]{weights},new[]{width},count:rows.Length,indices:new[]{rows});
    Compare(expected,actual);
}
var repeated=new[]{2,1,2,2,1,0,2};var coefficients=new[]{.25f,-.5f,1f};
var sequential=new[]{new Point(-0f,1,2),new Point(3,4,5),new Point(6,7,8)};var reference=(Point[])sequential.Clone();
foreach(int i in repeated)reference[i].X=reference[i].X+coefficients[i]*8.5f;
foreach(var stage in StructureRows.Stages(repeated))
    AffineGeometry.Execute(StructurePrograms.Data,StructurePrograms.DeltaTable,new[]{sequential,sequential},new[]{coefficients},new[]{8.5f},count:stage.Length,indices:new[]{stage});
Compare(reference,sequential);
for(int count=0;count<16;count++)
{
    var values=Enumerable.Range(0,count).Select(i=>new Point(i%2==0?-0f:float.Epsilon,i,-i)).ToArray();
    var result=(Point[])values.Clone();var expected=(Point[])values.Clone();
    var tailWeights=Enumerable.Range(0,count).Select(i=>i%2==0?-0f:-1f).ToArray();
    for(int i=0;i<count;i++)expected[i].X=values[i].X+tailWeights[i]*.25f;
    AffineGeometry.Execute(StructurePrograms.Data,StructurePrograms.DenseDelta,new[]{values,result},new[]{tailWeights},new[]{.25f},count:count);
    Compare(expected,result);
}
Console.WriteLine($"PASS {checks} coordinate checks: common binary programs, last-write scatter, sequential duplicates, tails and signed zero. Actual game Mono tests: AffineGeometryNativeCheck.");
void Compare(Point[] expected,Point[] actual)
{
    for(int i=0;i<actual.Length;i++)
    {
        if(BitConverter.SingleToInt32Bits(expected[i].X)!=BitConverter.SingleToInt32Bits(actual[i].X)||
            BitConverter.SingleToInt32Bits(expected[i].Y)!=BitConverter.SingleToInt32Bits(actual[i].Y)||
            BitConverter.SingleToInt32Bits(expected[i].Z)!=BitConverter.SingleToInt32Bits(actual[i].Z))throw new Exception("Coordinate bits differ at "+i);
        checks++;
    }
}
struct Point
{
    public float X,Y,Z;
    public Point(float x,float y,float z){X=x;Y=y;Z=z;}
}
