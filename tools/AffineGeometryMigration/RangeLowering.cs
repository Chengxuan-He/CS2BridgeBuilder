using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

// Compile independent same-index numerical rows. Membership/range selection stays
// in the recipe; every arithmetic operation and its ordering goes into the BBN.
internal static class RangeLowering
{
    internal static SyntaxNode Rewrite(SyntaxNode root,string path,string repository,bool apply,List<object> converted,List<string> pending,SemanticModel model)
    {
        foreach(string name in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c=>c.Identifier.Text).ToArray())
        {
            var owners=root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text==name).ToArray();if(owners.Length!=1)continue;
            var owner=owners[0];
            string holder="RangePrograms";int suffix=0;
            while(owner.Members.OfType<ClassDeclarationSyntax>().Any(c=>c.Identifier.Text==holder))holder="RangePrograms"+(++suffix);
            var replacements=new Dictionary<SyntaxNode,SyntaxNode>(); var entries=new List<Recipe>();
            foreach(var call in owner.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c=>c.Expression.ToString() is "AffineGeometry.For" or "AffineGeometry.Indices"))
            {
                bool indexed=call.Expression.ToString()=="AffineGeometry.Indices";
                if(call.ArgumentList.Arguments.Count!=(indexed?2:3) || call.ArgumentList.Arguments[indexed?1:2].Expression is not SimpleLambdaExpressionSyntax lambda)continue;
                SyntaxNode? original=model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(c=>c.ToString()==call.ToString());
                original??=model.SyntaxTree.GetRoot().DescendantNodes().OfType<StatementSyntax>().FirstOrDefault(s=>s.ToString()==call.GetAnnotations("OriginalLoop").FirstOrDefault()?.Data);
                if(original==null)continue;
                var vectors=new HashSet<string>();var scalarNames=new HashSet<string>();
                foreach(var element in original.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
                {
                    var type=model.GetTypeInfo(element).Type;
                    if(type?.ToDisplayString() is "UnityEngine.Vector3" or "Unity.Mathematics.float3")vectors.Add(element.Expression.ToString());
                    else if(type?.SpecialType==SpecialType.System_Single)scalarNames.Add(element.Expression.ToString());
                }
                int before=entries.Count;
                string Lower(CSharpSyntaxNode body)
                {
                    var choice=body.DescendantNodesAndSelf().OfType<ConditionalExpressionSyntax>().FirstOrDefault();
                    if(choice!=null)
                    {
                        var locals=body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v=>v.Identifier.Text).Append(lambda.Parameter.Identifier.Text).ToHashSet();
                        if(choice.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i=>locals.Contains(i.Identifier.Text))
                            || choice.Condition.DescendantNodesAndSelf().Any(n=>n is InvocationExpressionSyntax or AssignmentExpressionSyntax or PostfixUnaryExpressionSyntax))throw new NotSupportedException();
                        string yes=Lower(body.ReplaceNode(choice,choice.WhenTrue)),no=Lower(body.ReplaceNode(choice,choice.WhenFalse));
                        return $"if ({choice.Condition}) {{ {yes} }} else {{ {no} }}";
                    }
                    var recipe=new Recipe(lambda.Parameter.Identifier.Text,vectors,scalarNames){RuntimeIndexed=indexed};
                    var method=call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if(method!=null)
                        foreach(var variable in method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v=>v.SpanStart<call.SpanStart))
                            if(variable.Initializer?.Value is BinaryExpressionSyntax or PrefixUnaryExpressionSyntax)
                                recipe.InvariantExpressions[variable.Identifier.Text]=variable.Initializer.Value;
                    recipe.Body(body);
                    if(recipe.Code.Count==0)throw new NotSupportedException();
                    int entry=entries.Count;entries.Add(recipe);
                    string arrays=recipe.Coordinates.Count==0 ? "Array.Empty<UnityEngine.Vector3[]>()" : "new[] { "+string.Join(", ",recipe.Coordinates)+" }";
                    string scalars=recipe.Scalars.Count==0 ? "Array.Empty<float[]>()" : "new[] { "+string.Join(", ",recipe.Scalars)+" }";
                    string parameters=recipe.Parameters.Count==0 ? "Array.Empty<float>()" : "new float[] { "+string.Join(", ",recipe.Parameters)+" }";
                    if(indexed)
                        return $"foreach (var coordinateRows in StructureRows.Stages({call.ArgumentList.Arguments[0]})) AffineGeometry.Execute({holder}.Data, {entry}, {arrays}, {scalars}, {parameters}, count: coordinateRows.Length, indices: new[] {{ coordinateRows }});";
                    string first=call.ArgumentList.Arguments[0].ToString(),last=call.ArgumentList.Arguments[1].ToString();
                    string start=recipe.IndexOffset==null?first:$"checked(({first}) + ({recipe.IndexOffset}))";
                    return $"AffineGeometry.Execute({holder}.Data, {entry}, {arrays}, {scalars}, {parameters}, {start}, Math.Max(0, checked(({last}) - ({first}))));";
                }
                try
                {
                    if(call.Parent is not ExpressionStatementSyntax statement)throw new NotSupportedException();
                    replacements.Add(statement,SyntaxFactory.ParseStatement(Lower(lambda.Body)).WithTriviaFrom(statement));
                }
                catch(NotSupportedException)
                {
                    entries.RemoveRange(before,entries.Count-before);
                    pending.Add(Path.GetRelativePath(repository,path)+":range:"+(call.GetLocation().GetLineSpan().StartLinePosition.Line+1));
                }
            }
            if(entries.Count==0)continue;
            string style=Path.GetFileName(Path.GetDirectoryName(path))!;
            string key=$"Types_{style}_{name}_{holder}";
            byte[] bytes=Write(entries);
            string resource=Path.Combine(repository,"assets/HardcodedNumbers",style,key+".bin");
            if(apply){Directory.CreateDirectory(Path.GetDirectoryName(resource)!);File.WriteAllBytes(resource,bytes);}
            var replacement=owner.ReplaceNodes(replacements.Keys,(old,_)=>replacements[old]);
            replacement=replacement.AddMembers(SyntaxFactory.ParseMemberDeclaration($"private static class {holder} {{ static {holder}() {{ }} internal static readonly HardcodedNumbers Data = HardcodedNumbers.Load(\"{key}\"); }}")!);
            root=root.ReplaceNode(owner,replacement);
            converted.Add(new{File=Path.GetRelativePath(repository,path),Program=key,Entries=entries.Count,Range=true,Sha256=Convert.ToHexString(SHA256.HashData(bytes))});
        }
        return root;
    }
    internal static byte[] Write(List<Recipe> entries)
    {
        var words=new List<int>{0x31465041,1,entries.Count};words.AddRange(new int[entries.Count]);var floats=new List<float>();
        for(int entry=0;entry<entries.Count;entry++)
        {
            var recipe=entries[entry];words[3+entry]=words.Count;words.AddRange(new[]{recipe.Indices?.Length??-1,recipe.Registers,recipe.Code.Count});
            int indices=words.Count+recipe.Code.Count*8;
            var extraOffsets=new List<int>();int next=indices+(recipe.Indices?.Length??0);
            foreach(var table in recipe.IndexTables){extraOffsets.Add(next);next+=table.Length;}
            foreach(var raw in recipe.Code)
            {
                var instruction=(int[])raw.Clone();if(instruction[0]==0)instruction[2]+=floats.Count;
                if(recipe.Indices!=null && instruction[0] is 2 or 10)instruction[4]=instruction[4]<-1?extraOffsets[-2-instruction[4]]:indices;
                if(recipe.Indices!=null && instruction[0] is 3 or 11)instruction[3]=instruction[3]<-1?extraOffsets[-2-instruction[3]]:indices;
                if(recipe.RuntimeIndexed && instruction[0] is 2 or 10)instruction[4]=-2;
                if(recipe.RuntimeIndexed && instruction[0] is 3 or 11)instruction[3]=-2;
                words.AddRange(instruction);
            }
            if(recipe.Indices!=null)words.AddRange(recipe.Indices);
            foreach(var table in recipe.IndexTables)words.AddRange(table);
            floats.AddRange(recipe.Constants);
        }
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
        {
            writer.Write(0x314e4242u);writer.Write(1);foreach(int count in new[]{words.Count,0,0,0,floats.Count,0})writer.Write(count);
            foreach(int word in words)writer.Write(word);foreach(float value in floats)writer.Write(value);
        }
        byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));return stream.ToArray();
    }
    internal sealed class Recipe
    {
        internal readonly List<int[]> Code=new();internal readonly List<float> Constants=new();
        internal readonly List<string> Coordinates=new(),Scalars=new(),Parameters=new();
        internal int Registers;
        internal int[]? Indices;
        internal bool RuntimeIndexed;
        internal readonly Dictionary<string,ExpressionSyntax> InvariantExpressions=new();
        private readonly HashSet<string> _expanding=new();
        internal readonly List<int[]> IndexTables=new();
        private readonly Dictionary<string,int> _indexTables=new();
        private readonly HashSet<string> _records=new();
        internal void IndexTable(string expression,int[] values){_indexTables.Add(expression,-2-IndexTables.Count);IndexTables.Add(values);}
        internal void Record(string name)=>_records.Add(name);
        private readonly Dictionary<string,int> _tables=new();
        internal void Table(string name,float[] values){_tables.Add(name,Constants.Count);Constants.AddRange(values);}
        private string _index;
        internal string? IndexOffset;
        private readonly HashSet<string> _coordinateArrays,_scalarArrays;
        private readonly Dictionary<string,int> _locals=new();
        private readonly Dictionary<string,int[]> _vectors=new();
        private readonly Dictionary<string,ExpressionSyntax[]> _matrices=new();
        internal Recipe(string index,HashSet<string> coordinates,HashSet<string> scalars){_index=index;_coordinateArrays=coordinates;_scalarArrays=scalars;}
        private static int Slot(List<string> list,string name){int slot=list.IndexOf(name);if(slot>=0)return slot;list.Add(name);return list.Count-1;}
        private int Op(int operation,int a,int b=0,int c=0){int register=Registers++;Code.Add(new[]{operation,register,a,b,c,0,0,0});return register;}
        private int Constant(float value){int offset=Constants.Count;Constants.Add(value);return Op(0,offset);}
        private static ExpressionSyntax Unwrap(ExpressionSyntax expression)=>expression is ParenthesizedExpressionSyntax parent ? Unwrap(parent.Expression) : expression;
        private bool Element(ExpressionSyntax expression,out string name)
        {
            name="";if(expression is not ElementAccessExpressionSyntax element || element.ArgumentList.Arguments.Count!=1
                || (element.ArgumentList.Arguments[0].ToString()!=_index && !_indexTables.ContainsKey(element.ArgumentList.Arguments[0].ToString())))return false;
            name=element.Expression.ToString();return !element.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i=>i.Identifier.Text==_index);
        }
        private int Mapping(ExpressionSyntax expression)
        {var key=((ElementAccessExpressionSyntax)expression).ArgumentList.Arguments[0].ToString();return _indexTables.TryGetValue(key,out int offset)?offset:-1;}
        private static int Axis(string value)=>value switch{"x"=>0,"y"=>1,"z"=>2,_=>throw new NotSupportedException()};
        private int[] Vector(ExpressionSyntax expression)
        {
            expression=Unwrap(expression);
            if(expression is IdentifierNameSyntax local && _vectors.TryGetValue(local.Identifier.Text,out var vector))return (int[])vector.Clone();
            if(Element(expression,out string array) && _coordinateArrays.Contains(array))return new[]{Op(2,Slot(Coordinates,array),0,Mapping(expression)),Op(2,Slot(Coordinates,array),1,Mapping(expression)),Op(2,Slot(Coordinates,array),2,Mapping(expression))};
            if(expression is ObjectCreationExpressionSyntax create && create.Type.ToString() is "Vector3" or "float3" && create.ArgumentList?.Arguments.Count==3)
                return create.ArgumentList.Arguments.Select(a=>Value(a.Expression)).ToArray();
            throw new NotSupportedException();
        }
        private int Value(ExpressionSyntax expression)
        {
            expression=Unwrap(expression);
            if(_tables.TryGetValue(expression.ToString(),out int table))return Op(0,table,1);
            if(expression is IdentifierNameSyntax local && _locals.TryGetValue(local.Identifier.Text,out int value))return value;
            if(expression is IdentifierNameSyntax invariant && InvariantExpressions.TryGetValue(invariant.Identifier.Text,out var definition))
            {
                if(!_expanding.Add(invariant.Identifier.Text))throw new NotSupportedException();
                int result=Value(definition);_expanding.Remove(invariant.Identifier.Text);return result;
            }
            if(expression is LiteralExpressionSyntax literal && literal.Token.Value is float f)return Constant(f);
            if(expression is LiteralExpressionSyntax integer && integer.Token.Value is int n)return Constant(n);
            if(expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.UnaryMinusExpression))return Op(8,Value(unary.Operand));
            if(expression is BinaryExpressionSyntax binary)
            {
                int op=binary.Kind() switch{SyntaxKind.AddExpression=>4,SyntaxKind.SubtractExpression=>5,SyntaxKind.MultiplyExpression=>6,SyntaxKind.DivideExpression=>7,_=>throw new NotSupportedException()};
                return Op(op,Value(binary.Left),Value(binary.Right));
            }
            if(expression is MemberAccessExpressionSyntax member)
            {
                if(Element(member.Expression,out string coordinates) && _coordinateArrays.Contains(coordinates))return Op(2,Slot(Coordinates,coordinates),Axis(member.Name.Identifier.Text),Mapping(member.Expression));
                if(member.Expression is IdentifierNameSyntax vector && _vectors.TryGetValue(vector.Identifier.Text,out var registers))return registers[Axis(member.Name.Identifier.Text)];
            }
            if(Element(expression,out string scalar) && _scalarArrays.Contains(scalar))return Op(3,Slot(Scalars,scalar),Mapping(expression));
            if(expression is InvocationExpressionSyntax call)
            {
                var args=call.ArgumentList.Arguments;
                if(call.Expression.ToString()=="AffineGeometry.AddScaled" && args.Count==3)
                    return Op(4,Value(args[0].Expression),Op(6,Value(args[1].Expression),Value(args[2].Expression)));
                if(call.Expression.ToString() is "AffineGeometry.Translate" or "AffineGeometry.Scale" && args.Count==2)
                    return Op(call.Expression.ToString().EndsWith("Scale")?6:4,Value(args[0].Expression),Value(args[1].Expression));
                if(call.Expression.ToString()=="Mathf.LerpUnclamped" && args.Count==3)
                {
                    int a=Value(args[0].Expression),b=Value(args[1].Expression),t=Value(args[2].Expression);
                    return Op(4,a,Op(6,Op(5,b,a),t));
                }
                throw new NotSupportedException();
            }
            if(expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i=>i.Identifier.Text==_index || _vectors.ContainsKey(i.Identifier.Text) || _locals.ContainsKey(i.Identifier.Text)))throw new NotSupportedException();
            if(expression.DescendantNodesAndSelf().Any(n=>n is InvocationExpressionSyntax or AssignmentExpressionSyntax or PostfixUnaryExpressionSyntax or ObjectCreationExpressionSyntax))throw new NotSupportedException();
            return Op(1,Slot(Parameters,expression.ToString()));
        }
        private void Assign(ExpressionSyntax left,ExpressionSyntax right,bool add)
        {
            if(left is MemberAccessExpressionSyntax member)
            {
                int axis=Axis(member.Name.Identifier.Text);int value=Value(right);if(add)value=Op(4,Value(left),value);
                if(Element(member.Expression,out string coordinates) && _coordinateArrays.Contains(coordinates)){Code.Add(new[]{10,value,Slot(Coordinates,coordinates),axis,Mapping(member.Expression),0,0,0});return;}
                if(member.Expression is IdentifierNameSyntax vector && _vectors.TryGetValue(vector.Identifier.Text,out var registers)){registers[axis]=value;return;}
            }
            if(Element(left,out string output))
            {
                if(add)throw new NotSupportedException();
                if(_coordinateArrays.Contains(output))
                {
                    int[] vector=Vector(right);
                    for(int axis=0;axis<3;axis++)Code.Add(new[]{10,vector[axis],Slot(Coordinates,output),axis,Mapping(left),0,0,0});
                    return;
                }
                if(_scalarArrays.Contains(output)){int value=Value(right);Code.Add(new[]{11,value,Slot(Scalars,output),Mapping(left),0,0,0,0});return;}
            }
            if(left is IdentifierNameSyntax local && _locals.ContainsKey(local.Identifier.Text)){int value=Value(right);_locals[local.Identifier.Text]=add?Op(4,_locals[local.Identifier.Text],value):value;return;}
            throw new NotSupportedException();
        }
        internal void Body(CSharpSyntaxNode body)
        {
            if(body is BlockSyntax block){foreach(var child in block.Statements)Body(child);return;}
            if(body is ExpressionStatementSyntax statement){Body(statement.Expression);return;}
            if(body is AssignmentExpressionSyntax assignment && assignment.Kind() is SyntaxKind.SimpleAssignmentExpression or SyntaxKind.AddAssignmentExpression)
            {Assign(assignment.Left,assignment.Right,assignment.IsKind(SyntaxKind.AddAssignmentExpression));return;}
            if(body is InvocationExpressionSyntax invoke && invoke.Expression is MemberAccessExpressionSyntax method && method.Name.ToString()=="Evaluate"
                && _matrices.TryGetValue(method.Expression.ToString(),out var rows))
            {
                var arguments=invoke.ArgumentList.Arguments;if(arguments.Count!=6)throw new NotSupportedException();
                for(int axis=0;axis<3;axis++)
                {
                    if(arguments[axis+3].Expression is not DeclarationExpressionSyntax output || output.Designation is not SingleVariableDesignationSyntax variable
                        || rows[axis] is not InvocationExpressionSyntax row)throw new NotSupportedException();
                    var values=row.ArgumentList.Arguments;
                    int register=row.Expression.ToString() switch
                    {
                        "AffineGeometry.Row.Constant"=>Value(values[0].Expression),
                        "AffineGeometry.Row.Affine"=>Op(4,Value(values[0].Expression),Op(6,Value(values[1].Expression),Value(arguments[0].Expression))),
                        "AffineGeometry.Row.Linear"=>Op(6,Value(values[0].Expression),Value(arguments[0].Expression)),
                        _=>throw new NotSupportedException()
                    };
                    _locals.Add(variable.Identifier.Text,register);
                }
                return;
            }
            if(body is LocalDeclarationStatementSyntax declaration)
            {
                foreach(var variable in declaration.Declaration.Variables)
                {
                    if(_records.Contains(variable.Identifier.Text) || _indexTables.ContainsKey(variable.Identifier.Text))continue;
                    var expression=variable.Initializer?.Value??throw new NotSupportedException();
                    if(expression is ObjectCreationExpressionSyntax create && create.Type.ToString()=="AffineGeometry.Matrix" && create.ArgumentList?.Arguments.Count==3)
                    {_matrices.Add(variable.Identifier.Text,create.ArgumentList.Arguments.Select(a=>a.Expression).ToArray());continue;}
                    if(Code.Count==0 && IndexOffset==null && expression is BinaryExpressionSyntax offset && offset.IsKind(SyntaxKind.AddExpression)
                        && offset.Right.ToString()==_index && !offset.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i=>i.Identifier.Text==_index))
                    {IndexOffset=offset.Left.ToString();_index=variable.Identifier.Text;continue;}
                    try{_vectors.Add(variable.Identifier.Text,Vector(expression));}
                    catch(NotSupportedException){_locals.Add(variable.Identifier.Text,Value(expression));}
                }
                return;
            }
            throw new NotSupportedException();
        }
    }
}
