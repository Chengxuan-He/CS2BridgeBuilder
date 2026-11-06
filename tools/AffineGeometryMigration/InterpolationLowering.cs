using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

// Measured edge/interior records become explicit gather columns and float rows.
// No generated-runtime lambda, geometry inference or per-row expression dispatch.
internal static class InterpolationLowering
{
    private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    internal static SyntaxNode Rewrite(SyntaxNode root,string path,string repository,bool apply,List<object> converted,Assembly assembly,SemanticModel model)
    {
        foreach(string name in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c=>c.Identifier.Text).ToArray())
        {
            var owners=root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text==name).ToArray();if(owners.Length!=1)continue;
            var owner=owners[0];
            var replacements=new Dictionary<SyntaxNode,SyntaxNode>();var holders=new List<MemberDeclarationSyntax>();int serial=0;
            foreach(var call in owner.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c=>c.Expression.ToString()=="AffineGeometry.For"))
            {
                var args=call.ArgumentList.Arguments;
                if(args.Count!=3 || args[2].Expression is not SimpleLambdaExpressionSyntax lambda || lambda.Body is not BlockSyntax block)continue;
                var method=call.Ancestors().OfType<MethodDeclarationSyntax>().First();
                var data=method.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault(v=>v.Identifier.Text=="data");
                var variants=new List<(int Entry,TupleExpressionSyntax Tuple)>();string selector="0";
                if(data?.Initializer?.Value is TupleExpressionSyntax tuple)variants.Add((0,tuple));
                else if(data?.Initializer?.Value is ConditionalExpressionSyntax conditional && conditional.WhenTrue is TupleExpressionSyntax yes && conditional.WhenFalse is TupleExpressionSyntax no)
                {variants.Add((0,no));variants.Add((1,yes));selector=$"({conditional.Condition}) ? 1 : 0";}
                else if(data?.Initializer?.Value is SwitchExpressionSyntax select)
                {
                    selector=select.GoverningExpression.ToString();
                    foreach(var arm in select.Arms)
                        if(arm.Pattern is ConstantPatternSyntax pattern && int.TryParse(pattern.Expression.ToString(),out int entry) && arm.Expression is TupleExpressionSyntax fields)variants.Add((entry,fields));
                    if(variants.Count==0 && select.GoverningExpression is TupleExpressionSyntax)
                    {
                        var arms=select.Arms.Where(a=>a.Expression is TupleExpressionSyntax &&
                            System.Text.RegularExpressions.Regex.IsMatch(a.Pattern.ToString(), @"^\(\s*\d+\s*(,\s*\d+\s*)+\)$")).ToArray();
                        for(int i=0;i<arms.Length;i++)variants.Add((i,(TupleExpressionSyntax)arms[i].Expression));
                        selector=$"{select.GoverningExpression} switch {{ "+string.Join(", ",arms.Select((a,i)=>$"{a.Pattern} => {i}"))+", _ => throw new ArgumentOutOfRangeException() }";
                    }
                }
                else variants.Add((0,SyntaxFactory.TupleExpression()));
                variants.Sort((a,b)=>a.Entry.CompareTo(b.Entry));if(variants.Count==0 || variants.Where((v,i)=>v.Entry!=i).Any())continue;
                var type=assembly.GetType("BridgeBuilder.Bridges."+name);if(type==null)continue;
                SyntaxNode? original=model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(c=>c.ToString()==call.ToString());
                original??=model.SyntaxTree.GetRoot().DescendantNodes().OfType<StatementSyntax>().FirstOrDefault(s=>s.ToString()==call.GetAnnotations("OriginalLoop").FirstOrDefault()?.Data);if(original==null)continue;
                var coordinates=new HashSet<string>();var scalars=new HashSet<string>();
                foreach(var element in original.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
                {
                    var elementType=model.GetTypeInfo(element).Type;
                    if(elementType?.ToDisplayString() is "UnityEngine.Vector3" or "Unity.Mathematics.float3")coordinates.Add(element.Expression.ToString());
                    else if(elementType?.SpecialType==SpecialType.System_Single)scalars.Add(element.Expression.ToString());
                }
                var entries=new List<RangeLowering.Recipe>();RangeLowering.Recipe? binding=null;bool supported=true;
                // Topology records and numerical columns share measured row identities,
                // but only numerical assignments belong in the SIMD program.
                var topology = block.Statements.OfType<ExpressionStatementSyntax>().Where(s =>
                    s.Expression is AssignmentExpressionSyntax a &&
                    a.Left is ElementAccessExpressionSyntax e &&
                    !coordinates.Contains(e.Expression.ToString()) && !scalars.Contains(e.Expression.ToString()) &&
                    a.Right is ObjectCreationExpressionSyntax creation &&
                    (creation.Type.ToString().EndsWith("EdgeVertex") || creation.Type.ToString().EndsWith("InteriorVertex") || creation.Type.ToString().EndsWith("TriangleVertex"))).ToArray();
                var numericalBlock = block.WithStatements(SyntaxFactory.List(block.Statements.Where(s => !topology.Contains(s))));
                if(!numericalBlock.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any())continue;
                foreach(var variant in variants)
                {
                    try
                    {
                        var values=new Dictionary<string,object>();
                        foreach(var field in variant.Tuple.Arguments)
                        {
                            if(field.NameColon==null)throw new NotSupportedException();
                            values[field.NameColon.Name.ToString()]=field.Expression is IdentifierNameSyntax identifier
                                ? Read(type.GetField(identifier.Identifier.Text,Flags)!,null)
                                : Evaluate(field.Expression,new(),assembly);
                        }
                        var environment=new Dictionary<string,object>{{"data",values},{"$type",type}};
                        foreach(var guard in method.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(b=>b.IsKind(SyntaxKind.NotEqualsExpression)))
                            if(guard.Left is MemberAccessExpressionSyntax length && length.Name.ToString()=="Length" && length.Expression is IdentifierNameSyntax arrayName)
                            {
                                try{environment[arrayName.Identifier.Text]=new Dictionary<string,object>{{"Length",Convert.ToInt32(Evaluate(guard.Right,environment,assembly))}};}
                                catch(NotSupportedException){}
                            }
                        foreach(var local in method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v=>v.SpanStart<call.SpanStart))
                        {
                            if(local.Initializer?.Value is ArrayCreationExpressionSyntax array && array.Type.RankSpecifiers.Count==1 && array.Type.RankSpecifiers[0].Sizes.Count==1)
                            {
                                try{environment[local.Identifier.Text]=new Dictionary<string,object>{{"Length",Convert.ToInt32(Evaluate(array.Type.RankSpecifiers[0].Sizes[0],environment,assembly))}};}
                                catch(NotSupportedException){}
                            }
                            else if(local.Initializer!=null && local.Identifier.Text!="data")
                            {try{environment[local.Identifier.Text]=Evaluate(local.Initializer.Value,environment,assembly);}catch(NotSupportedException){}}
                        }
                        int first=Convert.ToInt32(Evaluate(args[0].Expression,environment,assembly)),last=Convert.ToInt32(Evaluate(args[1].Expression,environment,assembly));
                        if(first<0 || last<first)throw new NotSupportedException();
                        var rows=Enumerable.Range(first,last-first).Select(i=>new Dictionary<string,object>(environment){{lambda.Parameter.Identifier.Text,i}}).ToArray();
                        var recipe=new RangeLowering.Recipe(lambda.Parameter.Identifier.Text,coordinates,scalars){Indices=Enumerable.Range(first,last-first).ToArray()};
                        if(rows.Length==0)
                        {
                            recipe.Registers=1;recipe.Constants.Add(0f);recipe.Code.Add(new[]{0,0,0,0,0,0,0,0});entries.Add(recipe);continue;
                        }
                        foreach(var declaration in block.Statements.OfType<LocalDeclarationStatementSyntax>())
                            foreach(var variable in declaration.Declaration.Variables)
                            {
                                if(variable.Initializer==null)throw new NotSupportedException();
                                foreach(var row in rows)row[variable.Identifier.Text]=Evaluate(variable.Initializer.Value,row,assembly);
                                recipe.Record(variable.Identifier.Text);
                            }
                        var recorded=new HashSet<string>();var indices=new HashSet<string>();
                        foreach(var expression in block.DescendantNodes().OfType<ExpressionSyntax>())
                        {
                            string text=expression.ToString();if(recorded.Contains(text))continue;
                            object[] results;
                            try{results=rows.Select(row=>Evaluate(expression,row,assembly)).ToArray();}catch(NotSupportedException){continue;}
                            if(results.All(r=>r is float or int) && expression is not LiteralExpressionSyntax)
                            {
                                recipe.Table(text,results.Select(Convert.ToSingle).ToArray());recorded.Add(text);
                            }
                        }
                        foreach(var element in block.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
                        {
                            if(!coordinates.Contains(element.Expression.ToString()) && !scalars.Contains(element.Expression.ToString()))continue;
                            string text=element.ArgumentList.Arguments[0].ToString();if(text==lambda.Parameter.Identifier.Text || !indices.Add(text))continue;
                            recipe.IndexTable(text,rows.Select(row=>Convert.ToInt32(Evaluate(element.ArgumentList.Arguments[0].Expression,row,assembly))).ToArray());
                        }
                        recipe.Body(numericalBlock);
                        string Signature(RangeLowering.Recipe r)=>string.Join(";",r.Coordinates)+"|"+string.Join(";",r.Scalars)+"|"+string.Join(";",r.Parameters);
                        if(binding!=null && Signature(recipe)!=Signature(binding))throw new NotSupportedException();
                        binding??=recipe;entries.Add(recipe);
                    }
                    catch(Exception e) when(e is NotSupportedException or NullReferenceException or ArgumentException or KeyNotFoundException or TargetInvocationException){supported=false;break;}
                }
                if(!supported)continue;
                if(binding==null)
                {
                    if(call.Parent is ExpressionStatementSyntax statement && statement.Parent is BlockSyntax)
                    {replacements.Add(statement,SyntaxFactory.EmptyStatement().WithTriviaFrom(statement));converted.Add(new{File=Path.GetRelativePath(repository,path),Removed="Measured empty interpolation",Entries=entries.Count});}
                    continue;
                }
                while(owner.Members.OfType<ClassDeclarationSyntax>().Any(c=>c.Identifier.Text=="InterpolationProgram"+serial))serial++;
                string style=Path.GetFileName(Path.GetDirectoryName(path))!,holder="InterpolationProgram"+(serial++),key=$"Types_{style}_{name}_{holder}";
                byte[] bytes=RangeLowering.Write(entries);
                if(apply)File.WriteAllBytes(Path.Combine(repository,"assets/HardcodedNumbers",style,key+".bin"),bytes);
                string vectors=binding.Coordinates.Count==0?"Array.Empty<UnityEngine.Vector3[]>()":"new[] { "+string.Join(", ",binding.Coordinates)+" }";
                string scalarText=binding.Scalars.Count==0?"Array.Empty<float[]>()":"new[] { "+string.Join(", ",binding.Scalars)+" }";
                string parameters=binding.Parameters.Count==0?"Array.Empty<float>()":"new float[] { "+string.Join(", ",binding.Parameters)+" }";
                string execute=$"AffineGeometry.Execute({holder}.Data, {selector}, {vectors}, {scalarText}, {parameters})";
                if(topology.Length==0)
                    replacements.Add(call,SyntaxFactory.ParseExpression(execute).WithTriviaFrom(call));
                else
                {
                    var preparation=block.WithStatements(SyntaxFactory.List(block.Statements.Where(s=>s is LocalDeclarationStatementSyntax || topology.Contains(s))));
                    replacements.Add(call.Parent!,SyntaxFactory.ParseStatement($"{{ for(var {lambda.Parameter.Identifier.Text}={args[0].Expression}; {lambda.Parameter.Identifier.Text}<{args[1].Expression}; {lambda.Parameter.Identifier.Text}++) {preparation} {execute}; }}").WithTriviaFrom(call.Parent!));
                }
                holders.Add(SyntaxFactory.ParseMemberDeclaration($"private static class {holder} {{ static {holder}() {{ }} internal static readonly HardcodedNumbers Data=HardcodedNumbers.Load(\"{key}\"); }}")!);
                converted.Add(new{File=Path.GetRelativePath(repository,path),Program=key,Entries=entries.Count,Rows=entries.Sum(r=>r.Indices!.Length),Sha256=Convert.ToHexString(SHA256.HashData(bytes))});
            }
            if(replacements.Count!=0)root=root.ReplaceNode(owner,owner.ReplaceNodes(replacements.Keys,(old,_)=>replacements[old]).AddMembers(holders.ToArray()));
        }
        return root;
    }
    private static object Evaluate(ExpressionSyntax expression,Dictionary<string,object> environment,Assembly assembly)
    {
        if(expression is ParenthesizedExpressionSyntax parentheses)return Evaluate(parentheses.Expression,environment,assembly);
        if(expression is LiteralExpressionSyntax literal && literal.Token.Value!=null)return literal.Token.Value;
        if(expression is IdentifierNameSyntax identifier)
        {
            if(environment.TryGetValue(identifier.Identifier.Text,out object? value))return value;
            if(environment.TryGetValue("$type",out var context) && ((Type)context).GetField(identifier.Identifier.Text,Flags) is FieldInfo field && field.IsStatic)
                return Read(field,null);
            return assembly.GetType("BridgeBuilder.Bridges."+identifier.Identifier.Text)??throw new NotSupportedException();
        }
        if(expression is MemberAccessExpressionSyntax member)
        {
            object instance=Evaluate(member.Expression,environment,assembly);string name=member.Name.ToString();
            if(instance is Dictionary<string,object> dictionary)return dictionary.TryGetValue(name,out object? value)?value:throw new NotSupportedException();
            if(instance is Array array && name=="Length")return array.Length;
            var type=instance as Type??instance.GetType();var field=type.GetField(name,Flags);if(field==null)throw new NotSupportedException();
            if(instance is Type && !field.IsStatic)throw new NotSupportedException();
            return Read(field,instance is Type?null:instance);
        }
        if(expression is ElementAccessExpressionSyntax element)
        {
            if(Evaluate(element.Expression,environment,assembly) is not Array array || element.ArgumentList.Arguments.Count!=1)throw new NotSupportedException();
            return array.GetValue(Convert.ToInt32(Evaluate(element.ArgumentList.Arguments[0].Expression,environment,assembly)))??throw new NotSupportedException();
        }
        if(expression is BinaryExpressionSyntax binary && Evaluate(binary.Left,environment,assembly) is int left && Evaluate(binary.Right,environment,assembly) is int right)
            return binary.Kind() switch{SyntaxKind.AddExpression=>checked(left+right),SyntaxKind.SubtractExpression=>checked(left-right),SyntaxKind.MultiplyExpression=>checked(left*right),_=>throw new NotSupportedException()};
        throw new NotSupportedException();
    }
    private static object Read(FieldInfo field,object? instance)
    {
        object value=field.GetValue(instance)??throw new NotSupportedException();
        var names=field.GetCustomAttribute<TupleElementNamesAttribute>()?.TransformNames;
        if(value is not Array array || names==null || array.Rank!=1)return value;
        if(array.Length>0 && array.GetValue(0) is not ITuple)return value;
        var records=new Dictionary<string,object>[array.Length];
        for(int row=0;row<array.Length;row++)
        {
            var tuple=(ITuple)array.GetValue(row)!;var record=new Dictionary<string,object>();
            for(int slot=0;slot<tuple.Length;slot++)record[names[slot]??("Item"+(slot+1))]=tuple[slot]!;
            records[row]=record;
        }
        return records;
    }
}
