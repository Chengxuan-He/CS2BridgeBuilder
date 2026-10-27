using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Offline source-to-resource conversion. Roslyn's literal values preserve C# numeric bits.
var root = Path.GetFullPath(args[0]);
var sourceRoot = Path.GetFullPath(Path.Combine(root, "src/BridgeBuilder/Bridges"));
var output = Path.Combine(root, "assets/HardcodedNumbers");
Directory.CreateDirectory(output);
var top = new HashSet<string> { "BridgeTowers.cs", "BridgeCables.cs", "BridgeMeasurements.cs",
    "BridgeStyleDefinitions.cs", "BridgeSpec.cs", "GoldenBridgeRailings.cs", "PrototypeBridgeSizing.cs",
    "BridgeTowerSpec.cs", "BridgeTowerTemplate.cs", "BridgeCableTemplate.cs" };
var files = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
    .Where(p => p.Contains(Path.DirectorySeparatorChar + "Types" + Path.DirectorySeparatorChar) ||
        p.EndsWith(".g.cs") || top.Contains(Path.GetFileName(p))).OrderBy(p => p).ToArray();
if (args.Contains("--check"))
{
    var resources = new HashSet<string>();
    var pendingFiles = new List<string>();
    foreach (var file in files)
    {
        var text = File.ReadAllText(file);
        foreach(var holder in CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c=>c.Identifier.Text.StartsWith("Numeric_",StringComparison.Ordinal)))
            if(!holder.Members.OfType<ConstructorDeclarationSyntax>().Any(c=>c.Modifiers.Any(SyntaxKind.StaticKeyword)))
                throw new Exception("Numerical resource holder requires demand initialization: "+file);
        if (!text.Contains("// Embedded numerical arrays:"))
        {
            var pending = CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Any(l => l.IsKind(SyntaxKind.NumericLiteralExpression) && !Pack.IsCodeConstant(l));
            if (pending) pendingFiles.Add(file);
            continue;
        }
        var names=Regex.Matches(text, "HardcodedNumbers.Load\\(\"([^\"]+)\"\\)")
            .Select(m=>m.Groups[1].Value).ToList();
        // A generated chart loader selects an explicit immutable resource name
        // before calling Load(resource). These switch literals are dependencies,
        // not an excuse to retain arbitrary unreferenced files in the resource tree.
        if(text.Contains("HardcodedNumbers.Load(resource)"))
            names.AddRange(Regex.Matches(text,"=>\\s*\"(NativeUvCharts_[^\"]+)\"").Select(m=>m.Groups[1].Value));
        foreach (string name in names.Distinct())
        {
            var path = ResourceFiles.PathFor(output, name);
            var bytes = ResourceFiles.Read(path);
            if (bytes.Length < 64 || BitConverter.ToUInt32(bytes, 0) != 0x314e4242u ||
                BitConverter.ToInt32(bytes, 4) != 1 ||
                !SHA256.HashData(bytes[..^32]).SequenceEqual(bytes[^32..]))
                throw new Exception("Invalid numerical resource: " + name);
            long length = 64;
            for (int i = 0; i < 6; i++)
            {
                int count = BitConverter.ToInt32(bytes, 8 + i * 4);
                if (count < 0) throw new Exception("Negative array length: " + name);
                length += (long)count * (i is 2 or 3 or 5 ? 8 : 4);
            }
            if (length != bytes.Length) throw new Exception("Array length mismatch: " + name);
            resources.Add(Path.GetRelativePath(output, path));
        }
    }
    if(pendingFiles.Count!=0)
        throw new Exception("Run HardcodedDataPack after regenerating measured data:\n"+string.Join("\n",pendingFiles));
    if (Directory.GetFiles(output, "*.gz", SearchOption.AllDirectories).Length != 0 ||
        !resources.SetEquals(Directory.GetFiles(output, "*.bin", SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(output,p))))
        throw new Exception("Unreferenced or missing numerical resource.");
    Console.WriteLine($"Verified {resources.Count} embedded typed-array resources and measured-source coverage.");
    return;
}
var refresh = args.Contains("--refresh");
string Original(string file)
{
    var text = File.ReadAllText(file);
    if (refresh && text.Contains("// Embedded numerical arrays:"))
        return File.ReadAllText(Path.Combine(root, "artifacts/backups/20261021-embedded-numeric-data", Path.GetRelativePath(root, file)));
    return text;
}
var originals = files.ToDictionary(p => p, Original);
var trees = Directory.GetFiles(Path.Combine(root, "src/BridgeBuilder"), "*.cs", SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(root, "vendor/CS2ModShared/src"), "*.cs", SearchOption.AllDirectories))
    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
        !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
    .Select(Path.GetFullPath).ToDictionary(p => p, p =>
    {
        var tree = CSharpSyntaxTree.ParseText(originals.TryGetValue(p, out var text) ? text : File.ReadAllText(p), path: p);
        return (SyntaxTree)CSharpSyntaxTree.Create((CSharpSyntaxNode)new Unchunk().Visit(tree.GetRoot())!, path: p);
    });
var response = Path.Combine(root, "src/BridgeBuilder/obj/Release/DirectBuild/BridgeBuilder.rsp");
var references = File.ReadAllLines(response).Select(l => l.Trim('"'))
    .Where(l => l.StartsWith("/reference:")).Select(l => MetadataReference.CreateFromFile(l["/reference:".Length..]));
var compilation = CSharpCompilation.Create("NumericMigration", trees.Values, references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
var reports = new List<object>();
foreach (var file in files)
{
    var original = originals[file];
    if (original.Contains("// Embedded numerical arrays:")) continue;
    var tree = trees[file];
    var syntax = (CompilationUnitSyntax)tree.GetRoot();
    if (syntax.ContainsDiagnostics) throw new Exception("Cannot parse " + file);
    // Undo the previous bounded initializer syntax before packing the identical entries.
    var name = Path.GetRelativePath(sourceRoot, file).Replace('\\', '_').Replace('/', '_').Replace('.', '_');
    var converter = new Pack(name, compilation.GetSemanticModel(tree));
    var updated = (CompilationUnitSyntax)converter.Visit(syntax)!;
    var expectedValues = syntax.DescendantNodes().OfType<LiteralExpressionSyntax>()
        .Where(l => l.IsKind(SyntaxKind.NumericLiteralExpression) && l.Token.Value != null &&
            Pack.Kind(l.Token.Value) >= 0 && !Pack.IsCodeConstant(l))
        .Select(l => l.Token.Value!).ToArray();
    for (var k = 0; k < 6; k++)
    {
        var expected = expectedValues.Where(v => Pack.Kind(v) == k).Select(Pack.Bits);
        if (!expected.SequenceEqual(converter.Data[k].Select(Pack.Bits)))
            throw new Exception("Numeric value/order mismatch: " + file + " array " + k);
    }
    if (converter.Total == 0)
    {
        if (!updated.IsEquivalentTo(syntax))
        {
            var prior = Path.Combine(root, "artifacts/backups/20261021-embedded-numeric-data", Path.GetRelativePath(root, file));
            Directory.CreateDirectory(Path.GetDirectoryName(prior)!);
            if (!File.Exists(prior)) File.Copy(file, prior);
            File.WriteAllText(file, updated.NormalizeWhitespace().ToFullString() + Environment.NewLine);
        }
        continue;
    }
    var resource = converter.Bytes();
    var backup = Path.Combine(root, "artifacts/backups/20261021-embedded-numeric-data", Path.GetRelativePath(root, file));
    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
    if (!File.Exists(backup)) File.Copy(file, backup);
    // A separate static holder avoids ordering dependencies between fields in the owner class.
    var holder = SyntaxFactory.ParseMemberDeclaration(
        "// Embedded numerical arrays: " + name + "\ninternal static class Numeric_" + name +
        " { static Numeric_" + name + "() {} internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load(\"" + name + "\"); }")!;
    var ns = updated.Members.OfType<BaseNamespaceDeclarationSyntax>().Single();
    updated = updated.ReplaceNode(ns, ns.AddMembers(holder));
    var text = updated.NormalizeWhitespace().ToFullString() + Environment.NewLine;
    if (CSharpSyntaxTree.ParseText(text).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        throw new Exception("Generated syntax is invalid: " + file);
    ResourceFiles.Write(ResourceFiles.PathFor(output, name), resource);
    File.WriteAllText(file, text, new UTF8Encoding(false));
    reports.Add(new { source = Path.GetRelativePath(root, file).Replace('\\', '/'), resource = Path.GetRelativePath(output,ResourceFiles.PathFor(output,name)).Replace('\\','/'),
        originalSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original))),
        counts = converter.Counts, arrays = converter.Arrays, payloadBytes = resource.Length,
        verified = "Exact typed numerical bits and order against the unchunked original C# syntax" });
    Console.WriteLine($"{Path.GetFileName(file)}: {converter.Total} numbers, {converter.Arrays} loop-built arrays");
}
if (reports.Count != 0)
{
    // Keep prior migration evidence when converting newly regenerated files.
    var path = Path.Combine(output, "inventory.json");
    var old = File.Exists(path) ? JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(path))! : new();
    var names = reports.Select(r => JsonSerializer.SerializeToElement(r).GetProperty("resource").GetString()).ToHashSet();
    var combined = old.Where(r => !names.Contains(r.GetProperty("resource").GetString()))
        .Concat(reports.Select(r => JsonSerializer.SerializeToElement(r))).OrderBy(r => r.GetProperty("source").GetString());
    File.WriteAllText(path, JsonSerializer.Serialize(combined, new JsonSerializerOptions { WriteIndented = true }) + "\n");
}
Console.WriteLine($"Converted {reports.Count} files.");

sealed class Unchunk : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var methods = node.Members.OfType<MethodDeclarationSyntax>().GroupBy(m => m.Identifier.Text).ToDictionary(g => g.Key, g => g.First());
        var removed = new HashSet<string>();
        var replacements = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>();
        foreach (var field in node.Members.OfType<FieldDeclarationSyntax>())
        {
            if (field.Declaration.Type is not ArrayTypeSyntax || field.Declaration.Variables.Count != 1) continue;
            var variable = field.Declaration.Variables[0];
            var creator = "Create" + variable.Identifier.Text;
            if (variable.Initializer?.Value.ToString() != creator + "()" || !methods.TryGetValue(creator, out var method)) continue;
            var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression.ToString().StartsWith("Fill" + variable.Identifier.Text + "_")).ToArray();
            if (calls.Length == 0) continue;
            var entries = new List<ExpressionSyntax>();
            foreach (var call in calls)
            {
                var fill = methods[call.Expression.ToString()];
                foreach (var assignment in fill.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (assignment.Left is not ElementAccessExpressionSyntax e ||
                        e.Expression.ToString() != "values" || int.Parse(e.ArgumentList.Arguments[0].ToString()) != entries.Count)
                        throw new Exception("Unexpected chunk assignment.");
                    entries.Add(assignment.Right);
                }
                removed.Add(fill.Identifier.Text);
            }
            removed.Add(creator);
            var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(entries));
            replacements[field] = field.ReplaceNode(variable, variable.WithInitializer(SyntaxFactory.EqualsValueClause(initializer)));
        }
        node = node.WithMembers(SyntaxFactory.List(node.Members.Where(m => m is not MethodDeclarationSyntax method ||
            !removed.Contains(method.Identifier.Text)).Select(m => m is FieldDeclarationSyntax f && replacements.TryGetValue(f, out var replacement) ? replacement : m)));
        return base.VisitClassDeclaration(node);
    }
}

sealed class Pack(string name, SemanticModel model) : CSharpSyntaxRewriter
{
    internal readonly List<object>[] Data = Enumerable.Range(0, 6).Select(_ => new List<object>()).ToArray();
    static readonly string[] Tags = ["I32", "U32", "I64", "U64", "F32", "F64"];
    readonly Stack<List<MemberDeclarationSyntax>> generated = new();
    internal int Arrays;
    internal int[] Counts => Data.Select(d => d.Count).ToArray();
    internal int Total => Data.Sum(d => d.Count);
    string Holder => "Numeric_" + name + ".Values";
    internal static int Kind(object value) => value switch { int => 0, uint => 1, long => 2, ulong => 3, float => 4, double => 5, _ => -1 };
    internal static string Bits(object value) => value switch
    {
        float f => BitConverter.SingleToInt32Bits(f).ToString("X8"),
        double d => BitConverter.DoubleToInt64Bits(d).ToString("X16"),
        _ => value.GetType().Name + ":" + value.ToString()
    };
    internal static bool IsCodeConstant(SyntaxNode node) => node.Ancestors().Any(a =>
        a is PatternSyntax or CaseSwitchLabelSyntax or AttributeSyntax or EnumMemberDeclarationSyntax or ParameterSyntax);
    string Add(object value)
    {
        int kind = Kind(value), index = Data[kind].Count;
        Data[kind].Add(value);
        return Holder + "." + Tags[kind] + "[" + index + "]";
    }
    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.NumericLiteralExpression) || node.Token.Value == null || Kind(node.Token.Value) < 0) return node;
        // C# syntax/compile-time constructs remain ordinary code, not stored measurements.
        if (IsCodeConstant(node)) return node;
        return Convert(model, node, SyntaxFactory.ParseExpression(Add(node.Token.Value))).WithTriviaFrom(node);
    }
    internal static ExpressionSyntax Convert(SemanticModel model, ExpressionSyntax original, ExpressionSyntax replacement)
    {
        var info = model.GetTypeInfo(original);
        if (info.ConvertedType != null && info.Type != null &&
            !SymbolEqualityComparer.Default.Equals(info.Type, info.ConvertedType) &&
            (info.ConvertedType.TypeKind == TypeKind.Enum ||
             info.ConvertedType.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or
                SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64))
            return SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(info.ConvertedType.ToDisplayString()),
                SyntaxFactory.ParenthesizedExpression(replacement));
        return replacement;
    }
    public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        => Convert(model, node, (ExpressionSyntax)base.VisitPrefixUnaryExpression(node)!);
    public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        var changed = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node)!;
        if (changed.Modifiers.Any(SyntaxKind.ConstKeyword) && changed.ToString() != node.ToString())
            changed = changed.WithModifiers(SyntaxFactory.TokenList(changed.Modifiers.Where(t => !t.IsKind(SyntaxKind.ConstKeyword))));
        return changed;
    }
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        generated.Push(new());
        var changed = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
        return changed.AddMembers(generated.Pop().ToArray());
    }
    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        // Homogeneous literal/record arrays become loops; no per-element IL initializer survives.
        if (generated.Count != 0 && node.Declaration.Type is ArrayTypeSyntax array &&
            node.Declaration.Variables.Count == 1)
        {
            var variable = node.Declaration.Variables[0];
            var init = variable.Initializer?.Value switch
            {
                InitializerExpressionSyntax i => i,
                ArrayCreationExpressionSyntax a => a.Initializer,
                ImplicitArrayCreationExpressionSyntax a => a.Initializer,
                _ => null
            };
            if (init != null && init.Expressions.Count > 1 && init.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Any(l => l.IsKind(SyntaxKind.NumericLiteralExpression)))
            {
                var element = array.ElementType.ToString();
                var method = "ReadPacked" + variable.Identifier.Text;
                BuildArray(array, init, method);
                return node.ReplaceNode(variable, variable.WithInitializer(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.ParseExpression(method + "()"))));
            }
        }
        var result = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!;
        if (result.Modifiers.Any(SyntaxKind.ConstKeyword) && (result.ToString() != node.ToString() ||
            result.Declaration.Type.ToString() is not ("string" or "bool")))
        {
            // A const may be referenced before its declaration. A static getter over the
            // resource array preserves that freedom without a zero-initialization dependency.
            if (result.Declaration.Variables.Count != 1) throw new Exception("Split multi-variable recorded constants before conversion.");
            var v = result.Declaration.Variables[0];
            return SyntaxFactory.PropertyDeclaration(result.Declaration.Type, v.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(result.Modifiers.Where(t => !t.IsKind(SyntaxKind.ConstKeyword)))
                    .Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(v.Initializer!.Value))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithTriviaFrom(node);
        }
        return result;
    }
    void BuildArray(ArrayTypeSyntax array, InitializerExpressionSyntax init, string method)
    {
        if (array.RankSpecifiers.Count > 1)
        {
            var childType = array.WithRankSpecifiers(SyntaxFactory.List(array.RankSpecifiers.Skip(1)));
            var jaggedBuilder = new StringBuilder($"private static {array} {method}() {{ var values = new {array.ElementType}[{init.Expressions.Count}]{string.Concat(array.RankSpecifiers.Skip(1))};");
            for (int i = 0; i < init.Expressions.Count; i++)
            {
                var child = init.Expressions[i] switch
                {
                    ArrayCreationExpressionSyntax a => a.Initializer,
                    ImplicitArrayCreationExpressionSyntax a => a.Initializer,
                    InitializerExpressionSyntax a => a,
                    _ => null
                };
                if (child == null) throw new Exception("Unsupported jagged measured array initializer.");
                var childMethod = method + "_" + i;
                BuildArray(childType, child, childMethod);
                jaggedBuilder.Append($"values[{i}]={childMethod}();");
            }
            jaggedBuilder.Append("return values; }");
            generated.Peek().Add(SyntaxFactory.ParseMemberDeclaration(jaggedBuilder.ToString())!);
            Arrays++;
            return;
        }
                var element = array.ElementType.ToString();
                var rows = init.Expressions.Select(e => Template.Make(e, model)).ToArray();
                var builder = new StringBuilder($"private static {array} {method}() {{ var d = {Holder}; var values = new {element}[{rows.Length}];");
                for (int row = 0; row < rows.Length;)
                {
                    var first = rows[row];
                    int end = row + 1;
                    while (end < rows.Length && rows[end].Text == first.Text &&
                        rows[end].Values.Select(Kind).SequenceEqual(first.Values.Select(Kind))) end++;
                    var starts = Counts;
                    var perRow = new int[6];
                    foreach (var v in first.Values) perRow[Kind(v)]++;
                    for (int n = row; n < end; n++)
                        foreach (var v in rows[n].Values) Data[Kind(v)].Add(v);
                    var used = new int[6];
                    var expression = Regex.Replace(first.Text, @"__VALUE_(\d+)__", m =>
                    {
                        var value = first.Values[int.Parse(m.Groups[1].Value)];
                        int k = Kind(value), offset = used[k]++;
                        return $"d.{Tags[k]}[{starts[k] + offset} + i * {perRow[k]}]";
                    });
                    builder.Append($"for (int i = 0; i < {end-row}; i++) values[{row} + i] = {expression};");
                    row = end;
                }
                builder.Append("return values; }");
                generated.Peek().Add(SyntaxFactory.ParseMemberDeclaration(builder.ToString())!);
                Arrays++;
    }
    internal byte[] Bytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(0x314e4242u); writer.Write(1);
        foreach (var count in Counts) writer.Write(count);
        foreach (var values in Data) foreach (var v in values)
        {
            switch (v)
            {
                case int x: writer.Write(x); break; case uint x: writer.Write(x); break;
                case long x: writer.Write(x); break; case ulong x: writer.Write(x); break;
                case float x: writer.Write(x); break; case double x: writer.Write(x); break;
            }
        }
        writer.Flush();
        writer.Write(SHA256.HashData(stream.ToArray())); writer.Flush();
        return stream.ToArray();
    }
}

sealed class Template(SemanticModel model) : CSharpSyntaxRewriter
{
    internal readonly List<object> Values = new();
    internal string Text = "";
    internal static Template Make(ExpressionSyntax expression, SemanticModel model)
    {
        var t = new Template(model);
        t.Text = t.Visit(expression)!.WithoutTrivia().NormalizeWhitespace().ToFullString();
        return t;
    }
    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.NumericLiteralExpression) || node.Token.Value == null || Pack.Kind(node.Token.Value) < 0) return node;
        var index = Values.Count; Values.Add(node.Token.Value);
        // Include the original numeric kind in the shape through an inert marker in the key.
        return Pack.Convert(model, node, SyntaxFactory.IdentifierName("__VALUE_" + index + "__")).WithTriviaFrom(node);
    }
    public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        => Pack.Convert(model, node, (ExpressionSyntax)base.VisitPrefixUnaryExpression(node)!);
}
