using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

internal static class NumericTrim
{
    internal static SyntaxNode Rewrite(SyntaxNode root,string source,string repository,bool apply,List<object> converted)
    {
        string[] types={"I32","U32","I64","U64","F32","F64"};int[] sizes={4,4,8,8,4,8};
        foreach(string name in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text.StartsWith("Numeric_")).Select(c=>c.Identifier.Text).ToArray())
        {
            var owner=root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c=>c.Identifier.Text==name);
            var load=owner.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(c=>c.Expression.ToString()=="HardcodedNumbers.Load");
            if(load?.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal || literal.Token.Value is not string key)continue;
            var values=root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(m=>m.ToString()==name+".Values").ToArray();
            var uses=new List<(ElementAccessExpressionSyntax Node,int Type,int Index)>();bool supported=true;
            foreach(var value in values)
            {
                if(value.Parent is not MemberAccessExpressionSyntax member || member.Parent is not ElementAccessExpressionSyntax element
                    || element.Expression!=member || element.ArgumentList.Arguments.Count!=1
                    || element.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax index || index.Token.Value is not int number){supported=false;break;}
                int type=Array.IndexOf(types,member.Name.ToString());if(type<0){supported=false;break;}
                uses.Add((element,type,number));
            }
            if(!supported || uses.Count==0)continue;
            string resource=Directory.GetFiles(Path.Combine(repository,"assets/HardcodedNumbers"),key+".bin",SearchOption.AllDirectories).Single();
            byte[] original=File.ReadAllBytes(resource);int[] counts=new int[6],starts=new int[6];int offset=32;
            for(int i=0;i<6;i++){counts[i]=BitConverter.ToInt32(original,8+i*4);starts[i]=offset;offset=checked(offset+counts[i]*sizes[i]);}
            var selected=Enumerable.Range(0,6).Select(t=>uses.Where(u=>u.Type==t).Select(u=>u.Index).Distinct().Order().ToArray()).ToArray();
            if(selected.Select((a,i)=>a.Length==counts[i]).All(v=>v))continue;
            using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write(0x314e4242u);writer.Write(1);foreach(var indices in selected)writer.Write(indices.Length);
                for(int type=0;type<6;type++)foreach(int index in selected[type])
                {
                    if(index<0 || index>=counts[type])throw new InvalidDataException("Invalid numeric trim reference.");
                    writer.Write(original,starts[type]+index*sizes[type],sizes[type]);
                }
            }
            byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));byte[] bytes=stream.ToArray();
            var map=uses.ToDictionary(u=>u.Node,u=>u);
            root=root.ReplaceNodes(map.Keys,(old,_)=>old.WithArgumentList(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,SyntaxFactory.Literal(Array.IndexOf(selected[map[old].Type],map[old].Index))))))));
            if(apply)
            {
                string backup=Path.Combine(repository,"artifacts/backups/before-simd88-programs",Path.GetRelativePath(repository,resource));
                if(!File.Exists(backup)){Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.WriteAllBytes(backup,original);}
                File.WriteAllBytes(resource,bytes);
            }
            converted.Add(new{File=Path.GetRelativePath(repository,source),Trimmed=key,BeforeBytes=original.Length,AfterBytes=bytes.Length});
        }
        return root;
    }
}
