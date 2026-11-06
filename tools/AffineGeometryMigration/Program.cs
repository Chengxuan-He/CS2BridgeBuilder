using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Offline compilation only. Program data comes from this checkout's measured
// arrays, never a runtime geometry classifier or an executable bridge callback.
string root = Path.GetFullPath(args[0]);
if (args.Contains("--pack-aos")) { AosPacking.ConvertAll(root, true); return; }
if (args.Contains("--verify-aos-from")) { AosPacking.Verify(root, args[Array.IndexOf(args,"--verify-aos-from")+1]); return; }
bool apply = args.Contains("--apply");
string response = Path.Combine(root,"src/BridgeBuilder/obj/Release/DirectBuild/BridgeBuilder.rsp");
if (!File.Exists(response)) throw new Exception("Run tools/Build.ps1 once to resolve this machine's game/reference assemblies.");
var referencePaths = File.ReadAllLines(response).Select(l=>l.Trim('"')).Where(l=>l.StartsWith("/reference:"))
    .Select(l=>l["/reference:".Length..]).ToArray();
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string? path = referencePaths.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name.Name);
    return path == null ? null : AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
};
var files = Directory.GetFiles(Path.Combine(root,"src/BridgeBuilder"),"*.cs",SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(root,"vendor/CS2ModShared/src"),"*.cs",SearchOption.AllDirectories))
    .Where(p=>!p.Split(Path.DirectorySeparatorChar).Any(part=>part is "obj" or "bin")).ToArray();
var trees = files.ToDictionary(p=>p,p=>CSharpSyntaxTree.ParseText(File.ReadAllText(p),path:p));
var compilation = CSharpCompilation.Create("BridgeBuilder.StructureData",trees.Values,
    referencePaths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel:OptimizationLevel.Release,allowUnsafe:true));
compilation=compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root,"tools/AffineGeometryMigration/LegacyAffineSyntax.cs.txt"))));
if(args.Contains("--check")){StructureGuard.Check(trees,compilation);AosPacking.ConvertAll(root, false);return;}
var resources = Directory.GetFiles(Path.Combine(root,"assets/HardcodedNumbers"),"*.bin",SearchOption.AllDirectories)
    .Select(p=>new ResourceDescription("BridgeBuilder.Numbers."+Path.GetFileName(p),()=>File.OpenRead(p),false));
using var output = new MemoryStream();
var emitted = compilation.Emit(output,manifestResources:resources);
if (!emitted.Success) throw new Exception(string.Join("\n",emitted.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
output.Position=0;
var assembly = AssemblyLoadContext.Default.LoadFromStream(output);
var converted = new List<object>(); var pending = new List<string>();
foreach (var path in files.Where(p=>p.Contains(Path.DirectorySeparatorChar+"Types"+Path.DirectorySeparatorChar)))
{
    var producer = new ProducerLoops(compilation.GetSemanticModel(trees[path]));
    var syntax = producer.Visit(trees[path].GetRoot())!;
    var calls = syntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
        .Where(c=>c.Expression.ToString()=="AffineGeometry.Scatter").ToArray();
    var replacements = new Dictionary<SyntaxNode,SyntaxNode>();
    var newFields = new Dictionary<ClassDeclarationSyntax,List<MemberDeclarationSyntax>>();
    foreach(var call in calls)
    {
        if(!Weighted(call,out var source,out var destination,out var parameter)) {pending.Add(Path.GetRelativePath(root,path)+":"+call.GetLocation().GetLineSpan().StartLinePosition.Line);continue;}
        var owner = call.Ancestors().OfType<ClassDeclarationSyntax>().First();
        string typeName = owner.Identifier.Text;
        string table = call.ArgumentList.Arguments[0].ToString();
        string entryExpression="0";
        var variants = new List<(int Entry,string Field)>();
        if(table.Contains('.'))
        {
            string local=table.Split('.')[0], member=table.Split('.')[1];
            var declaration=call.Ancestors().OfType<MethodDeclarationSyntax>().First().DescendantNodes()
                .OfType<VariableDeclaratorSyntax>().FirstOrDefault(d=>d.Identifier.Text==local);
            if(declaration?.Initializer?.Value is TupleExpressionSyntax single)
                variants.Add((0,single.Arguments.Single(a=>a.NameColon?.Name.Identifier.Text==member).Expression.ToString()));
            else if(declaration?.Initializer?.Value is SwitchExpressionSyntax select)
            {
                entryExpression=select.GoverningExpression.ToString();
                foreach(var arm in select.Arms)
                {
                    if(arm.Pattern is not ConstantPatternSyntax pattern || !int.TryParse(pattern.Expression.ToString(),out int entry))continue;
                    if(arm.Expression is not TupleExpressionSyntax tuple)throw new Exception("Expected immutable tuple selection: "+typeName);
                    string field=tuple.Arguments.Single(a=>a.NameColon?.Name.Identifier.Text==member).Expression.ToString();
                    variants.Add((entry,field));
                }
            }
            else {pending.Add(typeName+" unsupported table "+table);continue;}
        }
        else variants.Add((0,table));
        variants.Sort((a,b)=>a.Entry.CompareTo(b.Entry));
        if(variants.Where((v,i)=>v.Entry!=i).Any()) throw new Exception("Non-contiguous program variants: "+typeName);
        var entries=new List<(int Index,float Weight)[]>();
        var type=assembly.GetType("BridgeBuilder.Bridges."+typeName,true)!;
        foreach(var variant in variants)
        {
            var field=type.GetField(variant.Field,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
                ?? throw new Exception("Missing measured field "+typeName+"."+variant.Field);
            var array=(Array)field.GetValue(null)!;
            var rows=new List<(int Index,float Weight,int Ordinal)>();
            foreach(var item in array)
            {
                if(item is not ITuple tuple || tuple.Length!=2 || tuple[0] is not int vertex || tuple[1] is not (float or int))
                    throw new Exception("Expected exact (int,float/int) measured rows: "+field.Name);
                float weight=tuple[1] is int sign ? sign : (float)tuple[1]!;
                rows.Add((vertex,weight,rows.Count));
            }
            entries.Add(rows.GroupBy(r=>r.Index).Select(g=>g.Last()).OrderBy(r=>r.Ordinal).Select(r=>(r.Index,r.Weight)).ToArray());
        }
        string style=Path.GetFileName(Path.GetDirectoryName(path))!;
        string key="Types_"+style+"_"+typeName+"_CoordinateProgram";
        byte[] bytes=WriteProgram(entries);
        string resource=Path.Combine(root,"assets/HardcodedNumbers",style,key+".bin");
        string holder="CoordinatePrograms";
        string text=$"AffineGeometry.Execute({holder}.Data, {entryExpression}, new[] {{ {source}, {destination} }}, Array.Empty<float[]>(), new[] {{ {parameter} }})";
        replacements.Add(call,SyntaxFactory.ParseExpression(text).WithTriviaFrom(call));
        if(!newFields.ContainsKey(owner))newFields[owner]=new();
        newFields[owner].Add(SyntaxFactory.ParseMemberDeclaration($"private static class {holder} {{ static {holder}() {{ }} internal static readonly HardcodedNumbers Data = HardcodedNumbers.Load(\"{key}\"); }}")!);
        if(apply){Directory.CreateDirectory(Path.GetDirectoryName(resource)!);File.WriteAllBytes(resource,bytes);}
        converted.Add(new{File=Path.GetRelativePath(root,path),Program=key,Entries=entries.Count,Rows=entries.Sum(e=>e.Length),Sha256=Convert.ToHexString(SHA256.HashData(bytes))});
    }
    var rewritten=syntax.ReplaceNodes(replacements.Keys,(old,_)=>replacements[old]);
    // Locate the original class by stable name after replacing descendants.
    foreach(var pair in newFields)
    {
        var owner=rewritten.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c=>c.Identifier.Text==pair.Key.Identifier.Text);
        rewritten=rewritten.ReplaceNode(owner,owner.AddMembers(pair.Value.ToArray()));
    }
    rewritten=ScatterLowering.Rewrite(rewritten,path,root,apply,converted,assembly,compilation.GetSemanticModel(trees[path]));
    rewritten=InterpolationLowering.Rewrite(rewritten,path,root,apply,converted,assembly,compilation.GetSemanticModel(trees[path]));
    rewritten=RangeLowering.Rewrite(rewritten,path,root,apply,converted,pending,compilation.GetSemanticModel(trees[path]));
    rewritten=BatchLowering.Rewrite(rewritten,path,root,apply,converted,assembly);
    rewritten=producer.RestoreUnlowered(rewritten);
    rewritten=DeadData.Rewrite(rewritten);
    rewritten=NumericTrim.Rewrite(rewritten,path,root,apply,converted);
    if(rewritten.IsEquivalentTo(trees[path].GetRoot()))continue;
    if(apply)
    {
        string backup=Path.Combine(root,"artifacts/backups/before-simd88-programs",Path.GetRelativePath(root,path));
        if(!File.Exists(backup)){Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.Copy(path,backup);}
        File.WriteAllText(path,rewritten.NormalizeWhitespace().ToFullString()+Environment.NewLine,new UTF8Encoding(false));
    }
}
if(apply)
{
    var currentSource=string.Join("\n",Directory.GetFiles(Path.Combine(root,"src/BridgeBuilder"),"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
    var git=new System.Diagnostics.ProcessStartInfo("git"){WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
    git.ArgumentList.Add("ls-files");using var process=System.Diagnostics.Process.Start(git)!;
    var tracked=process.StandardOutput.ReadToEnd().Split('\n').Select(p=>p.Trim()).ToHashSet();process.WaitForExit();
    if(process.ExitCode!=0)throw new Exception("Cannot verify generated resource ownership.");
    foreach(string resource in Directory.GetFiles(Path.Combine(root,"assets/HardcodedNumbers"),"*.bin",SearchOption.AllDirectories))
    {
        string key=Path.GetFileNameWithoutExtension(resource);
        if(!System.Text.RegularExpressions.Regex.IsMatch(key,@"_(CoordinateProgram|RangePrograms\d*|BatchProgram\d*|ScatterProgram|InterpolationProgram\d*)$") || currentSource.Contains("\""+key+"\""))continue;
        string relative=Path.GetRelativePath(root,resource);
        if(tracked.Contains(relative.Replace('\\','/')))
        {
            string backup=Path.Combine(root,"artifacts/backups/before-simd88-programs",relative);
            if(!File.Exists(backup)){Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.Copy(resource,backup);}
        }
        File.Delete(resource);
    }
}
string report=Path.Combine(root,"artifacts/material-remap/shared/simd88-program-migration.json");
if (apply)
{
    AosPacking.ConvertAll(root, true);
    var start=new System.Diagnostics.ProcessStartInfo("dotnet"){UseShellExecute=false};
    foreach(string argument in new[]{"run","--project",Path.Combine(root,"tools/ParameterLifetime"),"--",root,"--apply"})start.ArgumentList.Add(argument);
    using var lifetime=System.Diagnostics.Process.Start(start)!;lifetime.WaitForExit();
    if(lifetime.ExitCode!=0)throw new Exception("Parameter lifetime lowering failed.");
}
Directory.CreateDirectory(Path.GetDirectoryName(report)!);
File.WriteAllText(report,JsonSerializer.Serialize(new{Applied=apply,Converted=converted,Pending=pending},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Common program lowering: {converted.Count} call sites; pending scatter patterns {pending.Count}; applied={apply}. See {report}");

static bool Weighted(InvocationExpressionSyntax call,out string source,out string destination,out string parameter)
{
    source=destination=parameter="";
    if(call.ArgumentList.Arguments.Count!=3 || call.ArgumentList.Arguments[2].Expression is not SimpleLambdaExpressionSyntax lambda
        || lambda.Body is not BlockSyntax block || block.Statements.Count!=1 || block.Statements[0] is not ExpressionStatementSyntax statement
        || statement.Expression is not AssignmentExpressionSyntax assignment || assignment.Right is not InvocationExpressionSyntax expression
        || expression.Expression.ToString()!="AffineGeometry.AddScaled" || expression.ArgumentList.Arguments.Count!=3
        || assignment.Left is not MemberAccessExpressionSyntax left || left.Name.Identifier.Text!="x"
        || left.Expression is not ElementAccessExpressionSyntax output || expression.ArgumentList.Arguments[0].Expression is not MemberAccessExpressionSyntax origin
        || origin.Name.Identifier.Text!="x" || origin.Expression is not ElementAccessExpressionSyntax input
        || output.ArgumentList.ToString()!=input.ArgumentList.ToString()
        || (expression.ArgumentList.Arguments[1].ToString()!=lambda.Parameter.Identifier.Text+".Weight"
            && expression.ArgumentList.Arguments[1].ToString()!=lambda.Parameter.Identifier.Text+".Side")
        || expression.ArgumentList.Arguments[2].Expression is not IdentifierNameSyntax) return false;
    source=input.Expression.ToString();destination=output.Expression.ToString();parameter=expression.ArgumentList.Arguments[2].ToString();return true;
}

static byte[] WriteProgram(List<(int Index,float Weight)[]> entries)
{
    var words=new List<int>{0x31465041,1,entries.Count};words.AddRange(new int[entries.Count]);var floats=new List<float>();
    for(int entry=0;entry<entries.Count;entry++)
    {
        var rows=entries[entry];words[3+entry]=words.Count;words.AddRange(new[]{rows.Length,5,6});
        int indices=words.Count+6*8,coefficients=floats.Count;
        void Op(int op,int dst,int a,int b=0,int c=0)=>words.AddRange(new[]{op,dst,a,b,c,0,0,0});
        Op(2,0,0,0,indices);Op(0,1,coefficients,1);Op(1,2,0);Op(6,3,1,2);Op(4,4,0,3);Op(10,4,1,0,indices);
        words.AddRange(rows.Select(r=>r.Index));floats.AddRange(rows.Select(r=>r.Weight));
    }
    using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
    {
        writer.Write(0x314e4242u);writer.Write(1);foreach(int count in new[]{words.Count,0,0,0,floats.Count,0})writer.Write(count);
        foreach(int word in words)writer.Write(word);foreach(float value in floats)writer.Write(value);
    }
    byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));return stream.ToArray();
}

// Producers retain their readable measured arithmetic. This temporary syntax is
// never written: recognized rows are lowered directly to immutable program data.
sealed class ProducerLoops : CSharpSyntaxRewriter
{
    private readonly SemanticModel _model;
    internal ProducerLoops(SemanticModel model)=>_model=model;
    private static bool LayoutOnly(SyntaxNode node)
    {string name=Path.GetFileName(node.SyntaxTree.FilePath);return name.Contains("Appearance")||name.Contains("Partitions");}
    private readonly Dictionary<string,StatementSyntax> _originals=new();
    internal SyntaxNode RestoreUnlowered(SyntaxNode root)
    {
        var pending=root.GetAnnotatedNodes("OriginalLoop").OfType<ExpressionStatementSyntax>()
            .Where(s=>s.Expression is InvocationExpressionSyntax call && call.Expression.ToString() is "AffineGeometry.For" or "AffineGeometry.Scatter" or "AffineGeometry.Indices").ToArray();
        return root.ReplaceNodes(pending,(node,_)=>_originals[node.GetAnnotations("OriginalLoop").Single().Data!]);
    }
    private StatementSyntax Temporary(string text,StatementSyntax original)
    {
        string key=original.ToString();_originals[key]=original;
        var statement=(ExpressionStatementSyntax)SyntaxFactory.ParseStatement(text);
        statement=statement.WithExpression(statement.Expression.WithAdditionalAnnotations(new SyntaxAnnotation("OriginalLoop",key)));
        return statement.WithTriviaFrom(original).WithAdditionalAnnotations(new SyntaxAnnotation("OriginalLoop",key));
    }
    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var method=node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if(LayoutOnly(node) || method==null || method.Identifier.Text.StartsWith("ReadPacked") || node.Statement.DescendantNodes().OfType<ForStatementSyntax>().Any() || node.Declaration?.Variables.Count!=1
            || node.Declaration.Variables[0].Initializer==null || node.Condition is not BinaryExpressionSyntax condition
            || node.Incrementors.Count!=1 || node.Incrementors[0] is not PostfixUnaryExpressionSyntax increment
            || !increment.IsKind(SyntaxKind.PostIncrementExpression))return base.VisitForStatement(node);
        string index=node.Declaration.Variables[0].Identifier.Text;
        if(condition.Left.ToString()!=index || increment.Operand.ToString()!=index || condition.Kind() is not (SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression))return base.VisitForStatement(node);
        string last=condition.Right.ToString();if(condition.IsKind(SyntaxKind.LessThanOrEqualExpression))last=$"checked(({last}) + 1)";
        string body=node.Statement is BlockSyntax ? node.Statement.ToString() : "{ "+node.Statement+" }";
        return Temporary($"AffineGeometry.For({node.Declaration.Variables[0].Initializer!.Value}, {last}, {index} => {body});",node);
    }
    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        if(LayoutOnly(node))return base.VisitForEachStatement(node);
        if(_model.GetTypeInfo(node.Expression).Type is IArrayTypeSymbol array && array.ElementType.SpecialType==SpecialType.System_Int32
            && !node.Statement.DescendantNodes().Any(n=>n is ForStatementSyntax or ForEachStatementSyntax)
            && !node.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c=>c.Expression.ToString() is "AffineGeometry.For" or "AffineGeometry.Indices" or "AffineGeometry.Scatter" or "AffineGeometry.Execute"))
        {
            string body=node.Statement is BlockSyntax ? node.Statement.ToString() : "{ "+node.Statement+" }";
            return Temporary($"AffineGeometry.Indices({node.Expression}, {node.Identifier.Text} => {body});",node);
        }
        var statement=node.Statement is BlockSyntax block && block.Statements.Count==1 ? block.Statements[0] : node.Statement;
        if(statement is not ExpressionStatementSyntax expression || expression.Expression is not AssignmentExpressionSyntax assignment
            || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax left || left.Name.Identifier.Text!="x"
            || left.Expression is not ElementAccessExpressionSyntax output
            || assignment.Right is not BinaryExpressionSyntax add || !add.IsKind(SyntaxKind.AddExpression)
            || add.Left is not MemberAccessExpressionSyntax origin || origin.Name.Identifier.Text!="x"
            || origin.Expression is not ElementAccessExpressionSyntax input || input.ArgumentList.ToString()!=output.ArgumentList.ToString()
            || add.Right is not BinaryExpressionSyntax product || !product.IsKind(SyntaxKind.MultiplyExpression)
            || product.Left.ToString()!=node.Identifier.Text+".Weight" || product.Right is not IdentifierNameSyntax)
            return base.VisitForEachStatement(node);
        string row=node.Identifier.Text;
        return Temporary($"AffineGeometry.Scatter({node.Expression}, {row} => {row}.Index, {row} => {{ {assignment.Left} = AffineGeometry.AddScaled({add.Left}, {product.Left}, {product.Right}); }});",node);
    }
}
