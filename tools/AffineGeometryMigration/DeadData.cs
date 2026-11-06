using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Remove decoded coefficient/range arrays once a program owns those rows. Keep
// every still-referenced topology table and its original binary measurements.
internal static class DeadData
{
    internal static SyntaxNode Rewrite(SyntaxNode root)
    {
        var noOutput=root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c=>
            c.Expression.ToString()=="AffineGeometry.Execute" && c.ArgumentList.Arguments.Count>=4 &&
            c.ArgumentList.Arguments[0].ToString().StartsWith("InterpolationProgram") &&
            c.ArgumentList.Arguments[2].ToString()=="Array.Empty<UnityEngine.Vector3[]>()" &&
            c.ArgumentList.Arguments[3].ToString()=="Array.Empty<float[]>()")
            .Select(c=>c.Parent).OfType<ExpressionStatementSyntax>().ToArray();
        root=root.RemoveNodes(noOutput,SyntaxRemoveOptions.KeepExteriorTrivia)!;
        root=root.RemoveNodes(root.DescendantNodes().OfType<EmptyStatementSyntax>().Where(s=>s.Parent is BlockSyntax).ToArray(),SyntaxRemoveOptions.KeepExteriorTrivia)!;
        if(!root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c=>c.Expression.ToString()=="AffineGeometry.Execute"))return root;
        foreach(string name in root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v=>v.Initializer?.Value is TupleExpressionSyntax or SwitchExpressionSyntax).Select(v=>v.Identifier.Text).Distinct().ToArray())
        {
            var candidates=root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v=>v.Identifier.Text==name).ToArray();
            if(candidates.Length!=1)continue;
            var variable=candidates[0];var method=variable.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();if(method==null)continue;
            var uses=method.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i=>i.Identifier.Text==name).ToArray();
            if(uses.Any(i=>i.Parent is not MemberAccessExpressionSyntax access || access.Expression!=i))continue;
            var members=uses.Select(i=>((MemberAccessExpressionSyntax)i.Parent!).Name.ToString()).ToHashSet();
            var tuples=variable.Initializer!.Value.DescendantNodesAndSelf().OfType<TupleExpressionSyntax>().ToArray();
            if(tuples.Length==0 || tuples.Any(t=>t.Arguments.Any(a=>a.NameColon==null)))continue;
            if(tuples.Any(t=>members.Any(m=>!t.Arguments.Any(a=>a.NameColon!.Name.ToString()==m))))continue;
            if(members.Count==0)continue;
            var initializer=variable.Initializer.Value.ReplaceNodes(tuples,(tuple,_)=>
            {
                var retained=tuple.Arguments.Where(a=>members.Contains(a.NameColon!.Name.ToString())).ToArray();
                return retained.Length==1?retained[0].Expression:tuple.WithArguments(SyntaxFactory.SeparatedList(retained));
            });
            var revised=method.ReplaceNode(variable.Initializer.Value,initializer);
            if(members.Count==1)
                revised=revised.ReplaceNodes(revised.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a=>a.Expression.ToString()==name).ToArray(),(access,_)=>SyntaxFactory.IdentifierName(name).WithTriviaFrom(access));
            root=root.ReplaceNode(method,revised);
        }
        bool changed;
        do
        {
            changed=false;
            foreach(var owner in root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray())
            {
                var remove=new List<MemberDeclarationSyntax>();
                foreach(var member in owner.Members)
                {
                    string? name=member switch
                    {
                        FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.PrivateKeyword) && field.Declaration.Variables.Count==1=>field.Declaration.Variables[0].Identifier.Text,
                        MethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.PrivateKeyword) && method.Identifier.Text.StartsWith("ReadPacked")=>method.Identifier.Text,
                        ClassDeclarationSyntax type when type.Identifier.Text.Contains("Program") && type.Modifiers.Any(SyntaxKind.PrivateKeyword)=>type.Identifier.Text,
                        _=>null
                    };
                    if(name==null)continue;
                    bool used=root.DescendantNodes().OfType<IdentifierNameSyntax>().Any(i=>i.Identifier.Text==name && !member.FullSpan.Contains(i.Span));
                    if(!used)remove.Add(member);
                }
                if(remove.Count==0)continue;
                root=root.ReplaceNode(owner,owner.RemoveNodes(remove,SyntaxRemoveOptions.KeepExteriorTrivia)!);changed=true;break;
            }
        }while(changed);
        return root;
    }
}
