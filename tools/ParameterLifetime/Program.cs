using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

string root=Path.GetFullPath(args[0]);bool apply=args.Contains("--apply");
var refs=File.ReadAllLines(Path.Combine(root,"src/BridgeBuilder/obj/Release/DirectBuild/BridgeBuilder.rsp")).Select(s=>s.Trim('"')).Where(s=>s.StartsWith("/reference:")).Select(s=>MetadataReference.CreateFromFile(s[11..]));
var files=Directory.GetFiles(Path.Combine(root,"src/BridgeBuilder"),"*.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles(Path.Combine(root,"vendor/CS2ModShared/src"),"*.cs",SearchOption.AllDirectories)).Where(p=>!p.Split(Path.DirectorySeparatorChar).Any(s=>s is "obj" or "bin")).ToArray();
var trees=files.Select(p=>CSharpSyntaxTree.ParseText(File.ReadAllText(p),path:p)).ToArray();
var compilation=CSharpCompilation.Create("LifetimeAudit",trees,refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,allowUnsafe:true));
var graph=new Dictionary<ISymbol,HashSet<ISymbol>>(SymbolEqualityComparer.Default);
var fields=new Dictionary<IFieldSymbol,VariableDeclaratorSyntax>(SymbolEqualityComparer.Default);
var roots=new HashSet<ISymbol>(SymbolEqualityComparer.Default);
foreach(var tree in trees)
{
 var model=compilation.GetSemanticModel(tree);
 foreach(var node in tree.GetRoot().DescendantNodes().Where(n=>n is VariableDeclaratorSyntax or BaseMethodDeclarationSyntax or PropertyDeclarationSyntax))
 {
  ISymbol? symbol=node switch{VariableDeclaratorSyntax v=>model.GetDeclaredSymbol(v),BaseMethodDeclarationSyntax m=>model.GetDeclaredSymbol(m),PropertyDeclarationSyntax p=>model.GetDeclaredSymbol(p),_=>null};
  if(symbol is null || symbol is ILocalSymbol || symbol.ContainingNamespace?.ToDisplayString()!="BridgeBuilder.Bridges")continue;
  if(node is VariableDeclaratorSyntax variable && symbol is IFieldSymbol field)fields[field]=variable;
  var dependencies=new HashSet<ISymbol>(SymbolEqualityComparer.Default);
  foreach(var expr in node.DescendantNodes().OfType<ExpressionSyntax>())
  {
   var dep=model.GetSymbolInfo(expr).Symbol;
   if(dep!=null)dependencies.Add(dep.OriginalDefinition);
   if(dep is IMethodSymbol method && (method.ContainingType.Name=="HardcodedNumbers" && method.Name is "Load" or "Decode" || method.ContainingType.Name=="Convert" && method.Name=="FromBase64String"))roots.Add(symbol);
  }
  graph[symbol]=dependencies;
 }
}
// Parameter caches populated after initialization must also be scoped.
foreach(var pair in fields)
{
 var f=pair.Key;
 if(!f.IsStatic || !f.IsReadOnly)continue;
 bool array=f.Type is IArrayTypeSymbol a && a.ElementType.SpecialType!=SpecialType.System_String && a.ElementType.Name!="PrefabID";
 bool cache=f.Type is INamedTypeSymbol t && (t.Name is "ConditionalWeakTable" or "ConcurrentDictionary" || t.Name=="Dictionary" && t.TypeArguments.Last().SpecialType!=SpecialType.System_String);
 if(array || cache)roots.Add(f);
}
// Compute transitive provenance, including record constructors and helper methods.
bool changed;do{changed=false;foreach(var pair in graph)if(!roots.Contains(pair.Key)&&pair.Value.Any(roots.Contains)){roots.Add(pair.Key);changed=true;}}while(changed);
var selected=fields.Where(p=>p.Key.IsStatic&&!p.Key.IsConst&&roots.Contains(p.Key)).ToArray();
var report=selected.Select(p=>new{symbol=p.Key.ToDisplayString(),file=Path.GetRelativePath(root,p.Value.SyntaxTree.FilePath),readOnly=p.Key.IsReadOnly,initializer=p.Value.Initializer?.Value.ToString()}).ToArray();
Directory.CreateDirectory(Path.Combine(root,"work/parameter90"));File.WriteAllText(Path.Combine(root,"work/parameter90/static-inventory.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
foreach(var group in selected.GroupBy(p=>p.Value.SyntaxTree.FilePath))
{
 string text=File.ReadAllText(group.Key);var edits=new List<(int Start,int Length,string Text)>();
 foreach(var pair in group)
 {
  var symbol=pair.Key;var variable=pair.Value;var field=(FieldDeclarationSyntax)variable.Parent!.Parent!;
  if(!symbol.IsReadOnly){Console.WriteLine("MUTABLE "+symbol);continue;}
  if(variable.Initializer==null)throw new Exception("Unsupported field "+symbol);
  if(edits.Any(e=>e.Start==field.SpanStart))continue;
  var parts=new List<string>();
  foreach(var item in field.Declaration.Variables)
  {
   var itemSymbol=compilation.GetSemanticModel(item.SyntaxTree).GetDeclaredSymbol(item)!;
   var initializer=item.Initializer!.Value;
   string itemValue=initializer.ToString();if(initializer is InitializerExpressionSyntax)itemValue="new "+field.Declaration.Type+" "+itemValue;
   string itemMods=string.Join(" ",field.Modifiers.Where(m=>!m.IsKind(SyntaxKind.ReadOnlyKeyword)).Select(m=>m.Text));
   parts.Add(itemMods+" "+field.Declaration.Type+" "+item.Identifier+" => GenerationParameters.Get<"+field.Declaration.Type+">("+(itemSymbol.ContainingType.IsGenericType ? "typeof("+itemSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)+").FullName + \":"+item.Identifier+"\"" : SymbolDisplay.FormatLiteral(itemSymbol.ToDisplayString(),true))+", () => "+itemValue+");");
  }
  edits.Add((field.SpanStart,field.Span.Length,string.Join("\n",parts)));continue;

 }
 if(apply && edits.Count>0)
 {
  var backup=Path.Combine(root,"artifacts/backups/before-parameter90-code",Path.GetRelativePath(root,group.Key));Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(group.Key,backup);
  foreach(var edit in edits.OrderByDescending(e=>e.Start))text=text.Remove(edit.Start,edit.Length).Insert(edit.Start,edit.Text);
  File.WriteAllText(group.Key,text);
 }
}
if(args.Contains("--check") && selected.Any(p=>p.Key.ToDisplayString()!="BridgeBuilder.Bridges.BridgeStyleCatalog._styles"))throw new Exception("Permanent static numerical parameters remain; run ParameterLifetime --apply.");
Console.WriteLine($"Parameter-dependent static fields={selected.Length}; readonly={selected.Count(p=>p.Key.IsReadOnly)}; apply={apply}");
