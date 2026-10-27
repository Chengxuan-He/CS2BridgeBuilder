using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

// Offline lowering only. Keep the measured recipes and their binary array indices.
var root = Path.GetFullPath(args[0]);
var dir = Path.Combine(root, "src/BridgeBuilder/Bridges");
int rows = 0, loops = 0;
foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
{
    string name = Path.GetFileName(file);
    if (!(name.EndsWith(".g.cs") || name.Contains("Geometry") || name.StartsWith("TowerWidening")) || name == "AffineGeometry.cs") continue;
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
    var rewriter = new Lower();
    var changed = rewriter.Visit(tree.GetRoot())!;
    if (rewriter.Rows + rewriter.Loops == 0) continue;
    File.WriteAllText(file, changed.NormalizeWhitespace().ToFullString() + Environment.NewLine);
    Console.WriteLine($"{Path.GetRelativePath(dir,file)}: rows={rewriter.Rows}, parallelLoops={rewriter.Loops}");
    rows += rewriter.Rows; loops += rewriter.Loops;
}
Console.WriteLine($"Lowered {rows} affine coordinate expressions and {loops} disjoint numerical loops.");

sealed class Lower : CSharpSyntaxRewriter
{
    internal int Rows, Loops;
    static bool Coordinate(ExpressionSyntax e) => e is MemberAccessExpressionSyntax m &&
        m.Name.Identifier.Text is "x" or "y" or "z" && m.Expression is ElementAccessExpressionSyntax a &&
        a.Expression.ToString() is "positions" or "moved" or "result";
    static bool Scalar(ExpressionSyntax e) => e is ElementAccessExpressionSyntax a && a.Expression.ToString() == "x";
    static ElementAccessExpressionSyntax Element(ExpressionSyntax e) => e is ElementAccessExpressionSyntax a ? a : (ElementAccessExpressionSyntax)((MemberAccessExpressionSyntax)e).Expression;

    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        if (!Coordinate(node.Left) && !Scalar(node.Left)) return base.VisitAssignmentExpression(node);
        ExpressionSyntax value = node.Right;
        if (node.IsKind(SyntaxKind.AddAssignmentExpression)) value = Call("Translate", node.Left, value);
        else if (node.IsKind(SyntaxKind.SubtractAssignmentExpression)) value = Call("Translate", node.Left, Negate(value));
        else if (node.IsKind(SyntaxKind.MultiplyAssignmentExpression)) value = Call("Scale", node.Left, value);
        else if (node.IsKind(SyntaxKind.SimpleAssignmentExpression)) value = Row(value);
        else return node;
        if (value == node.Right) return node;
        Rows++;
        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, node.Left, value).WithTriviaFrom(node);
    }
    static ExpressionSyntax Negate(ExpressionSyntax e) => SyntaxFactory.ParseExpression("-(" + e + ")");
    static ExpressionSyntax Call(string name, params ExpressionSyntax[] values) => SyntaxFactory.ParseExpression(
        "AffineGeometry." + name + "(" + string.Join(",", values.Select(v => v.ToString())) + ")");
    static ExpressionSyntax Row(ExpressionSyntax e)
    {
        if (e is ConditionalExpressionSyntax c) return c.WithWhenTrue(Row(c.WhenTrue)).WithWhenFalse(Row(c.WhenFalse));
        if (e is not BinaryExpressionSyntax b) return e;
        if (b.IsKind(SyntaxKind.AddExpression))
        {
            if (b.Right is BinaryExpressionSyntax p && p.IsKind(SyntaxKind.MultiplyExpression))
                return Call("AddScaled", b.Left, p.Left, p.Right);
            return Call("Translate", b.Left, b.Right);
        }
        if (b.IsKind(SyntaxKind.SubtractExpression)) return Call("Translate", b.Left, Negate(b.Right));
        if (b.IsKind(SyntaxKind.MultiplyExpression)) return Call("Scale", b.Left, b.Right);
        return e;
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var result = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        // XYZ reconstruction is three rows for one destination. Keep the rows
        // together, including constant Y/Z rows and last-write table semantics.
        if (result.Statement is BlockSyntax block && block.Statements.Count > 0 &&
            block.Statements.All(s => s is ExpressionStatementSyntax e &&
                e.Expression is AssignmentExpressionSyntax a &&
                a.IsKind(SyntaxKind.SimpleAssignmentExpression) && Coordinate(a.Left) &&
                Element(a.Left).ArgumentList.Arguments.Single().ToString() == node.Identifier.Text + ".Index" &&
                !a.Right.ToString().Contains(Element(a.Left).Expression + "[")))
        {
            Loops++;
            return SyntaxFactory.ParseStatement($"AffineGeometry.Scatter({node.Expression}, {node.Identifier} => {node.Identifier}.Index, {node.Identifier} => {block});").WithTriviaFrom(node);
        }
        // Only pure source-to-destination scalar rows, not accumulations, topology
        // mutation or dependent output reads. Range loops keep authored ordering.
        if (result.Statement is not ExpressionStatementSyntax statement ||
            statement.Expression is not AssignmentExpressionSyntax assignment ||
            !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) || !Coordinate(assignment.Left)) return result;
        var array = (ElementAccessExpressionSyntax)((MemberAccessExpressionSyntax)assignment.Left).Expression;
        string key = array.ArgumentList.Arguments.Single().ToString();
        if (key == node.Identifier.Text && node.Expression.ToString().EndsWith(".Vertices"))
        {
            Loops++;
            return SyntaxFactory.ParseStatement($"AffineGeometry.Indices({node.Expression}, {node.Identifier} => {{ {statement} }});").WithTriviaFrom(node);
        }
        if (key != node.Identifier.Text + ".Index" ||
            assignment.Right.ToString().Contains(array.Expression.ToString() + "[")) return result;
        Loops++;
        return SyntaxFactory.ParseStatement($"AffineGeometry.Scatter({node.Expression}, {node.Identifier} => {key}, {node.Identifier} => {{ {statement} }});").WithTriviaFrom(node);
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var rewritten = (ForStatementSyntax)base.VisitForStatement(node)!;
        var variable = node.Declaration?.Variables.SingleOrDefault();
        if (variable?.Initializer == null || node.Condition is not BinaryExpressionSyntax condition ||
            condition.Left.ToString() != variable.Identifier.Text ||
            !(condition.IsKind(SyntaxKind.LessThanExpression) || condition.IsKind(SyntaxKind.LessThanOrEqualExpression)) ||
            node.Incrementors.Count != 1 || node.Incrementors[0].ToString() != variable.Identifier.Text + "++") return rewritten;
        string index = variable.Identifier.Text;
        var body = rewritten.Statement;
        if (!body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().Any(a => Coordinate(a.Left) || Scalar(a.Left))) return rewritten;
        // Reject nested loops, shared counters, control flow escaping the loop,
        // engine calls, and any scatter write not indexed by this loop variable.
        if (body.DescendantNodesAndSelf().Any(n => n is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or ReturnStatementSyntax or BreakStatementSyntax or LocalFunctionStatementSyntax)) return rewritten;
        var locals = body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.Text).ToHashSet();
        // Constructed interior rows append after the immutable edge-row count.
        // This is an injective destination, not an arbitrary scatter.
        var destinations = new HashSet<string> { index };
        foreach (var local in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (local.Initializer?.Value is BinaryExpressionSyntax sum &&
                sum.IsKind(SyntaxKind.AddExpression) && sum.Right.ToString() == index &&
                sum.Left.ToString() == "data.Edges.Length") destinations.Add(local.Identifier.Text);
        foreach (var a in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
            if (!((Coordinate(a.Left) || Scalar(a.Left)) && destinations.Contains(Element(a.Left).ArgumentList.Arguments.Single().ToString())) &&
                !(a.Left is IdentifierNameSyntax id && locals.Contains(id.Identifier.Text))) return rewritten;
        if (body.DescendantNodesAndSelf().Any(n => n is PostfixUnaryExpressionSyntax || n is PrefixUnaryExpressionSyntax p && (p.IsKind(SyntaxKind.PreIncrementExpression) || p.IsKind(SyntaxKind.PreDecrementExpression)))) return rewritten;
        var allowed = new[] { "AffineGeometry.", "Math.", "Mathf.", "profile.SpanAt", "profile.CarriedAt", "Spread" };
        if (body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c => !allowed.Any(prefix => c.Expression.ToString().StartsWith(prefix)))) return rewritten;
        // Out/ref parameters cannot be captured; handling them needs an explicit
        // local array and a reviewed publish step, never a syntactic guess.
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method != null && method.ParameterList.Parameters.Any(p => (p.Modifiers.Any(SyntaxKind.OutKeyword) || p.Modifiers.Any(SyntaxKind.RefKeyword)) && body.DescendantNodes().OfType<IdentifierNameSyntax>().Any(id => id.Identifier.Text == p.Identifier.Text))) return rewritten;
        string end = condition.IsKind(SyntaxKind.LessThanOrEqualExpression) ? "checked((" + condition.Right + ") + 1)" : condition.Right.ToString();
        string code = body.ToString();
        code = Regex.Replace(code, @"\bcontinue\s*;", "return;");
        if (body is not BlockSyntax) code = "{" + code + "}";
        Loops++;
        return SyntaxFactory.ParseStatement($"AffineGeometry.For({variable.Initializer.Value}, {end}, {index} => {code});").WithTriviaFrom(node);
    }
}
