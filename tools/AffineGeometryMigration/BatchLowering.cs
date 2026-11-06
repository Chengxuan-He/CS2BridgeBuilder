using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

// Collapse immutable, disjoint part ranges before shipping. Runtime receives one
// bulk row table per LOD, not thousands of tiny program bindings and allocations.
internal static class BatchLowering
{
    internal static SyntaxNode Rewrite(SyntaxNode root,string path,string repository,bool apply,List<object> converted,Assembly assembly)
    {
        string style=Path.GetFileName(Path.GetDirectoryName(path))!;
        foreach(string name in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c=>c.Identifier.Text).ToArray())
        {
            var owners=root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text==name).ToArray();if(owners.Length!=1)continue;
            var owner=owners[0];
            var replacements=new Dictionary<SyntaxNode,SyntaxNode>();var fields=new List<MemberDeclarationSyntax>();int number=0;
            foreach(var loop in owner.DescendantNodes().OfType<ForEachStatementSyntax>())
            {
                if(loop.Statement is not ExpressionStatementSyntax statement || statement.Expression is not InvocationExpressionSyntax call
                    || call.Expression.ToString()!="AffineGeometry.Execute")continue;
                var args=call.ArgumentList.Arguments;
                if(args.Count!=7 || args[0].ToString()!="RangePrograms.Data" || !int.TryParse(args[1].ToString(),out int entry)
                    || args[5].ToString()!=loop.Identifier.Text+".First" || !args[6].ToString().Contains(loop.Identifier.Text+".Last"))continue;
                string input=Path.Combine(repository,"assets/HardcodedNumbers",style,$"Types_{style}_{name}_RangePrograms.bin");
                if(!File.Exists(input))continue;
                var parameters=args[4].Expression switch
                {
                    ArrayCreationExpressionSyntax array=>array.Initializer?.Expressions.ToArray(),
                    ImplicitArrayCreationExpressionSyntax array=>array.Initializer.Expressions.ToArray(),
                    _=>null
                };
                if(parameters==null)continue;
                var variants=Variants(loop);if(variants.Count==0)continue;
                var type=assembly.GetType("BridgeBuilder.Bridges."+name);if(type==null)continue;
                var bindings=new List<string>();var tableSlots=new Dictionary<int,string>();bool supported=true;
                for(int i=0;i<parameters.Length;i++)
                {
                    if(parameters[i] is MemberAccessExpressionSyntax member && member.Expression.ToString()==loop.Identifier.Text)
                        tableSlots[i]=member.Name.Identifier.Text;
                    else if(parameters[i].DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(n=>n.Identifier.Text==loop.Identifier.Text)){supported=false;break;}
                    else bindings.Add(parameters[i].ToString());
                }
                if(!supported)continue;
                var rows=new List<List<(int Index,Dictionary<int,float> Values)>>();
                foreach(var variant in variants)
                {
                    var field=type.GetField(variant.Field,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
                    var labels=field?.GetCustomAttribute<TupleElementNamesAttribute>()?.TransformNames;
                    if(field==null || labels==null){supported=false;break;}
                    var table=(Array)field.GetValue(null)!;var batch=new List<(int,Dictionary<int,float>)>();var seen=new HashSet<int>();
                    foreach(ITuple part in table)
                    {
                        int first=(int)part[labels.IndexOf("First")]!,last=(int)part[labels.IndexOf("Last")]!;
                        var values=new Dictionary<int,float>();
                        foreach(var slot in tableSlots)values[slot.Key]=Convert.ToSingle(part[labels.IndexOf(slot.Value)]);
                        for(int i=first;i<=last;i++){if(!seen.Add(i)){supported=false;break;}batch.Add((i,values));}
                        if(!supported)break;
                    }
                    rows.Add(batch);if(!supported)break;
                }
                if(!supported)continue;
                var (words,floats)=Read(input);int offset=words[3+entry],registers=words[offset+1],instructions=words[offset+2];
                var code=new List<int[]>();for(int i=0;i<instructions;i++)code.Add(words.Skip(offset+3+i*8).Take(8).ToArray());
                if(code.Any(c=>c[0] is 2 or 10 ? c[4]!=-1 : c[0] is 3 or 11 ? c[3]!=-1 : false))continue;
                while(owner.Members.OfType<ClassDeclarationSyntax>().Any(c=>c.Identifier.Text=="BatchProgram"+number))number++;
                string key=$"Types_{style}_{name}_BatchProgram{number}";string holder=$"BatchProgram{number++}";
                byte[] bytes=Write(rows,registers,code,floats,tableSlots);
                string resource=Path.Combine(repository,"assets/HardcodedNumbers",style,key+".bin");
                if(apply)File.WriteAllBytes(resource,bytes);
                string select=variants.Count==1 ? "0" : VariantExpression(loop);
                if(select.Length==0)throw new InvalidOperationException("Missing immutable batch selector.");
                string parameterText=bindings.Count==0?"Array.Empty<float>()":"new float[] { "+string.Join(", ",bindings)+" }";
                replacements.Add(loop,SyntaxFactory.ParseStatement($"AffineGeometry.Execute({holder}.Data, {select}, {args[2]}, {args[3]}, {parameterText});").WithTriviaFrom(loop));
                fields.Add(SyntaxFactory.ParseMemberDeclaration($"private static class {holder} {{ static {holder}() {{ }} internal static readonly HardcodedNumbers Data = HardcodedNumbers.Load(\"{key}\"); }}")!);
                converted.Add(new{File=Path.GetRelativePath(repository,path),Program=key,Entries=rows.Count,Rows=rows.Sum(r=>r.Count),Batch=true,Sha256=Convert.ToHexString(SHA256.HashData(bytes))});
            }
            if(replacements.Count!=0)root=root.ReplaceNode(owner,owner.ReplaceNodes(replacements.Keys,(old,_)=>replacements[old]).AddMembers(fields.ToArray()));
        }
        return root;
    }
    private static VariableDeclaratorSyntax? Declaration(ForEachStatementSyntax loop)
    {
        if(loop.Expression is not MemberAccessExpressionSyntax member)return null;
        return loop.Ancestors().OfType<MethodDeclarationSyntax>().First().DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault(v=>v.Identifier.Text==member.Expression.ToString());
    }
    private static string VariantExpression(ForEachStatementSyntax loop)=>Declaration(loop)?.Initializer?.Value is SwitchExpressionSyntax select?select.GoverningExpression.ToString():"";
    private static List<(int Entry,string Field)> Variants(ForEachStatementSyntax loop)
    {
        var result=new List<(int,string)>();
        if(loop.Expression is IdentifierNameSyntax direct){result.Add((0,direct.Identifier.Text));return result;}
        if(loop.Expression is not MemberAccessExpressionSyntax member)return result;
        string? Field(ExpressionSyntax expression)=>expression is TupleExpressionSyntax tuple?tuple.Arguments.FirstOrDefault(a=>a.NameColon?.Name.ToString()==member.Name.ToString())?.Expression.ToString():null;
        var value=Declaration(loop)?.Initializer?.Value;
        if(value is TupleExpressionSyntax tuple && Field(tuple) is string single)result.Add((0,single));
        else if(value is SwitchExpressionSyntax select)
            foreach(var arm in select.Arms)
                if(arm.Pattern is ConstantPatternSyntax pattern && int.TryParse(pattern.Expression.ToString(),out int entry) && Field(arm.Expression) is string field)result.Add((entry,field));
        result.Sort((a,b)=>a.Item1.CompareTo(b.Item1));
        if(result.Where((v,i)=>v.Item1!=i).Any())result.Clear();
        return result;
    }
    private static (int[],float[]) Read(string path)
    {
        using var reader=new BinaryReader(File.OpenRead(path));reader.ReadInt32();reader.ReadInt32();int n=reader.ReadInt32();
        if(reader.ReadInt32()!=0||reader.ReadInt32()!=0||reader.ReadInt32()!=0)throw new InvalidDataException();int f=reader.ReadInt32();if(reader.ReadInt32()!=0)throw new InvalidDataException();
        var words=new int[n];var floats=new float[f];for(int i=0;i<n;i++)words[i]=reader.ReadInt32();for(int i=0;i<f;i++)floats[i]=reader.ReadSingle();return(words,floats);
    }
    private static byte[] Write(List<List<(int Index,Dictionary<int,float> Values)>> entries,int registers,List<int[]> code,float[] constants,Dictionary<int,string> slots)
    {
        var words=new List<int>{0x31465041,1,entries.Count};words.AddRange(new int[entries.Count]);var floats=new List<float>();
        for(int entry=0;entry<entries.Count;entry++)
        {
            var rows=entries[entry];words[3+entry]=words.Count;words.AddRange(new[]{rows.Count,registers,code.Count});int indices=words.Count+8*code.Count;
            foreach(var raw in code)
            {
                var command=(int[])raw.Clone();
                if(command[0]==0){int offset=floats.Count;floats.Add(constants[command[2]]);command[2]=offset;}
                else if(command[0]==1)
                {
                    int slot=command[2];if(slots.ContainsKey(slot)){command[0]=0;command[2]=floats.Count;command[3]=1;floats.AddRange(rows.Select(r=>r.Values[slot]));}
                    else command[2]=slot-slots.Keys.Count(s=>s<slot);
                }
                else if(command[0] is 2 or 10)command[4]=indices;
                else if(command[0] is 3 or 11)command[3]=indices;
                words.AddRange(command);
            }
            words.AddRange(rows.Select(r=>r.Index));
        }
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
        {
            writer.Write(0x314e4242u);writer.Write(1);foreach(int count in new[]{words.Count,0,0,0,floats.Count,0})writer.Write(count);
            foreach(int word in words)writer.Write(word);foreach(float value in floats)writer.Write(value);
        }
        byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));return stream.ToArray();
    }
}
