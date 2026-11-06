using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class StructureGuard
{
    internal static void Check(Dictionary<string,SyntaxTree> trees,Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
    {
        var failures=new List<string>();int files=0;
        foreach(var pair in trees.Where(p=>p.Key.Replace('\\','/').EndsWith("/StructureCoordinateProgram.cs")))
            foreach(var token in pair.Value.GetRoot().DescendantTokens())
                if(token.ValueText is "ExecuteAffine" or "AffineWeights" or "ExecuteEdges" or "EdgeXyz")
                    failures.Add(pair.Key+": prohibited runtime expression-recognition path "+token.ValueText);
        foreach(var pair in trees)
        {
            string path=pair.Key.Replace('\\','/');
            if(!path.Contains("/Bridges/Types/") && !path.Contains("/Bridges/Common/Geometry/"))continue;
            if(path.Contains("Appearance") || path.Contains("Partitions") || path.Contains("ObjectAssembly") || path.Contains("RoadUnderlay"))continue;
            var model=compilation.GetSemanticModel(pair.Value);files++;
            foreach(var call in pair.Value.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                if(call.Expression.ToString().StartsWith("AffineGeometry.") && call.Expression.ToString() is not "AffineGeometry.Execute" and not "AffineGeometry.Groups")
                    failures.Add(path+": legacy numerical callback/helper "+call.Expression);
            foreach(var assignment in pair.Value.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                var method=assignment.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if(method==null || method.Identifier.Text.StartsWith("ReadPacked"))continue;
                var target=assignment.Left.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>().FirstOrDefault();
                if(target==null)continue;
                string array=target.Expression.ToString();
                if(!(array is "positions" or "moved" or "points" or "constructedX" or "x" || array.EndsWith(".Positions")))continue;
                bool arithmetic=assignment.Right.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Any(b=>
                    model.GetTypeInfo(b).Type?.SpecialType is SpecialType.System_Single or SpecialType.System_Double);
                arithmetic|=assignment.Right.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(c=>c.Expression.ToString().Contains("Lerp"));
                if(arithmetic)failures.Add(path+":"+(assignment.GetLocation().GetLineSpan().StartLinePosition.Line+1)+" scalar structural output: "+assignment.Left);
            }
        }
        if(failures.Count!=0)throw new Exception("Unlowered structure arithmetic:\n"+string.Join("\n",failures));
        Console.WriteLine($"PASS structural arithmetic guard: {files} files; only Execute and whole-mesh Groups APIs. UV/layout partition policy and object placement are outside this mesh-coordinate guard.");
    }
}
