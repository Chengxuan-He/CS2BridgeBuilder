using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

internal static class ScatterLowering
{
    internal static SyntaxNode Rewrite(SyntaxNode root,string path,string repository,bool apply,List<object> converted,Assembly assembly,SemanticModel model)
    {
        var calls=root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c=>c.Expression.ToString()=="AffineGeometry.Scatter").ToArray();
        foreach(var originalCall in calls)
        {
            var call=root.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(c=>c.ToString()==originalCall.ToString());if(call==null)continue;
            var args=call.ArgumentList.Arguments;
            if(args.Count!=3 || args[0].Expression is not IdentifierNameSyntax table || args[2].Expression is not SimpleLambdaExpressionSyntax lambda)continue;
            var owner=call.Ancestors().OfType<ClassDeclarationSyntax>().First();string name=owner.Identifier.Text;
            var type=assembly.GetType("BridgeBuilder.Bridges."+name);
            var field=type?.GetField(table.Identifier.Text,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
            var labels=field?.GetCustomAttribute<TupleElementNamesAttribute>()?.TransformNames;
            if(field==null || labels==null || !labels.Contains("Index"))continue;
            var array=(Array)field.GetValue(null)!;int indexSlot=labels.IndexOf("Index");
            var selected=array.Cast<ITuple>().Select((r,i)=>(Row:r,Ordinal:i)).GroupBy(r=>(int)r.Row[indexSlot]!).Select(g=>g.Last()).OrderBy(r=>r.Ordinal).Select(r=>r.Row).ToArray();
            var original=model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First(c=>c.ToString()==call.ToString());
            var coordinates=new HashSet<string>();var scalars=new HashSet<string>();
            foreach(var element in original.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                var elementType=model.GetTypeInfo(element).Type;
                if(elementType?.ToDisplayString() is "UnityEngine.Vector3" or "Unity.Mathematics.float3")coordinates.Add(element.Expression.ToString());
                else if(elementType?.SpecialType==SpecialType.System_Single)scalars.Add(element.Expression.ToString());
            }
            var recipe=new RangeLowering.Recipe(lambda.Parameter.Identifier.Text+".Index",coordinates,scalars){Indices=selected.Select(r=>(int)r[indexSlot]!).ToArray()};
            for(int column=0;column<labels.Count;column++)if(column!=indexSlot)
            {
                int slot=column;recipe.Table(lambda.Parameter.Identifier.Text+"."+labels[column],selected.Select(r=>Convert.ToSingle(r[slot])).ToArray());
            }
            try{recipe.Body(lambda.Body);}catch(NotSupportedException){continue;}
            string style=Path.GetFileName(Path.GetDirectoryName(path))!,key=$"Types_{style}_{name}_ScatterProgram";
            byte[] bytes=RangeLowering.Write(new(){recipe});
            if(apply)File.WriteAllBytes(Path.Combine(repository,"assets/HardcodedNumbers",style,key+".bin"),bytes);
            string vectors=recipe.Coordinates.Count==0?"Array.Empty<UnityEngine.Vector3[]>()":"new[] { "+string.Join(", ",recipe.Coordinates)+" }";
            string scalarText=recipe.Scalars.Count==0?"Array.Empty<float[]>()":"new[] { "+string.Join(", ",recipe.Scalars)+" }";
            string parameters=recipe.Parameters.Count==0?"Array.Empty<float>()":"new float[] { "+string.Join(", ",recipe.Parameters)+" }";
            var changed=owner.ReplaceNode(call,SyntaxFactory.ParseExpression($"AffineGeometry.Execute(ScatterProgram.Data, 0, {vectors}, {scalarText}, {parameters})").WithTriviaFrom(call));
            changed=changed.AddMembers(SyntaxFactory.ParseMemberDeclaration($"private static class ScatterProgram {{ static ScatterProgram() {{ }} internal static readonly HardcodedNumbers Data=HardcodedNumbers.Load(\"{key}\"); }}")!);
            root=root.ReplaceNode(owner,changed);
            converted.Add(new{File=Path.GetRelativePath(repository,path),Program=key,Entries=1,Rows=selected.Length,Sha256=Convert.ToHexString(SHA256.HashData(bytes))});
        }
        return root;
    }
}
