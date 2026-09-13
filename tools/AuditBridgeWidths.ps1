[CmdletBinding()]
param(
    [string] $AnatomyPath = (Join-Path (
        Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
            '..\LocalLow\Colossal Order\Cities Skylines II\ModsData\BridgeBuilder') `
        'asset-anatomy.txt'),
    [string] $OutputPath = (Join-Path $PSScriptRoot `
        '..\docs\agent-contract\bridge-width-invariant-measurements.tsv'),
    [string[]] $Styles = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AnatomyPath -PathType Leaf)) {
    Write-Error "The retained real-prefab anatomy dump was not found: $AnatomyPath"
    exit 2
}

# PowerShell promotes arithmetic to Double. Keep the complete measurement and audit pipeline in C#
# so every parsed value and every operation below is IEEE-754 binary32. The report writes round-trip
# text and the raw bit pattern; neither the calculation nor its evidence passes through rounded
# survey output.
$source = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class BridgeWidthBitwiseAudit
{
    private static string Failure = "";
    private enum BoundaryKind { Object, Overhead, Outer }

    private sealed class Pair
    {
        public readonly string Style;
        public readonly string Archetype;
        public readonly string Generated;
        public readonly BoundaryKind Boundary;
        public readonly HashSet<string> ArchetypeObjects;
        public readonly HashSet<string> GeneratedObjects;
        public readonly float MeasuredArchetypeRoadWidth;

        public Pair(
            string style,
            string archetype,
            string generated,
            BoundaryKind boundary,
            string[] archetypeObjects,
            string[] generatedObjects,
            float measuredArchetypeRoadWidth = Single.NaN)
        {
            Style = style;
            Archetype = archetype;
            Generated = generated;
            Boundary = boundary;
            ArchetypeObjects = new HashSet<string>(archetypeObjects, StringComparer.Ordinal);
            GeneratedObjects = new HashSet<string>(generatedObjects, StringComparer.Ordinal);
            MeasuredArchetypeRoadWidth = measuredArchetypeRoadWidth;
        }
    }

    private sealed class Block
    {
        public int Start;
        public int End;
    }

    private sealed class Section
    {
        public string Name = "";
        public readonly List<float> Widths = new List<float>();
        public readonly List<string> Pieces = new List<string>();
        public bool HasRequireAll;
        public bool HasRequireAny;
    }

    private sealed class SpanX
    {
        public bool Found;
        public float MinX;
        public float MaxX;
        public float Width { get { return MaxX - MinX; } }
    }

    private sealed class BridgeBoundary
    {
        public SpanX Object = new SpanX();
        public SpanX Overhead = new SpanX();
        public SpanX Selected = new SpanX();
    }

    private static readonly Regex Header = new Regex(
        @"^### (.+) \([^)]+\)$", RegexOptions.CultureInvariant);
    private static readonly Regex Piece = new Regex(
        @"^(\s*)m_Piece: (.+?) \(NetPiecePrefab\)$", RegexOptions.CultureInvariant);
    private static readonly Regex Number = new Regex(
        @"([-+0-9.Ee]+)$", RegexOptions.CultureInvariant);
    private static readonly Regex TopSection = new Regex(
        @"^    \[(\d+)\]: NetSectionInfo", RegexOptions.CultureInvariant);
    private static readonly Regex SectionName = new Regex(
        @"^      m_Section(?::| ->) (.+?) \(NetSectionPrefab\)", RegexOptions.CultureInvariant);
    private static readonly Regex DirectPieceName = new Regex(
        @"^ {12}m_Piece(?::| ->) (.+?) \(NetPiecePrefab\)", RegexOptions.CultureInvariant);
    private static readonly Regex OverheadPieceName = new Regex(
        @"^ {14}m_Piece(?::| ->) (.+?) \(NetPiecePrefab\)", RegexOptions.CultureInvariant);
    private static readonly Regex ObjectName = new Regex(
        @"^        m_Object(?::| ->) (.+?) \([^)]+\)", RegexOptions.CultureInvariant);

    private static readonly Pair[] Pairs =
    {
        new Pair(
            "CableStayed",
            "Cable-stayed Bridge - XL Road Divided - 8 Lanes",
            "两块板六车道_CableStayed",
            BoundaryKind.Outer,
            new[] { "8LaneCableStayedBridgePillar Placeholder" },
            new[] { "CableStayed-40-两块板六车道_CableStayed" },
            // The prefab's top-level section list contains mutually exclusive street-layout
            // variants. The retained model review identified the active bridge road span as 32 m;
            // summing every default-looking variant produced the invalid 40 m input from the
            // superseded audit.
            32.0f),
        new Pair(
            "CoveredWood",
            "PedestrianBridgeCoveredWood01",
            "两块板六车道_CoveredWood",
            BoundaryKind.Overhead,
            new string[0],
            new string[0]),
        new Pair(
            "Extradosed03",
            "ExtradosedBridge03",
            "两块板六车道_Extradosed03",
            BoundaryKind.Outer,
            new[] { "ExtradosedBridge03NetPillar" },
            new[] { "Extradosed03-40-两块板六车道_Extradosed03" }),
        new Pair(
            "ExtradosedLarge",
            "Extradosed Bridge - Large Road Divided - 6 Lanes",
            "两块板六车道_ExtradosedLarge",
            BoundaryKind.Overhead,
            new string[0],
            new string[0]),
        new Pair(
            "Grand",
            "Grand Bridge",
            "两块板六车道_Grand",
            BoundaryKind.Object,
            new[] { "GrandBridgePylon Placeholder", "GrandBridgePillar Placeholder" },
            new[] {
                "Grand-40-两块板六车道_Grand GrandBridgePylon Placeholder",
                "Grand-40-两块板六车道_Grand"
            }),
        new Pair(
            "GoldenGate",
            "Golden Gate Bridge",
            "两块板六车道_GoldenGate",
            BoundaryKind.Outer,
            new[] { "GoldenGateBridgePylon Placeholder", "GoldenGateBridgePillar Placeholder" },
            new[] {
                "GoldenGate-40-两块板六车道_GoldenGate GoldenGateBridgePylon Placeholder",
                "GoldenGate-40-两块板六车道_GoldenGate"
            }),
        new Pair(
            "Suspension",
            "Suspension Bridge - Highway Oneway - 5 Lanes",
            "两块板六车道_Suspension",
            BoundaryKind.Object,
            new[] { "5LaneSuspensionBridgePillar Placeholder" },
            new[] { "Suspension-40-两块板六车道_Suspension" }),
        new Pair(
            "SuspensionGolden",
            "SuspensionBridge03",
            "两块板六车道_SuspensionGolden",
            BoundaryKind.Object,
            new[] { "SuspensionBridge03NetPillar", "SuspensionBridge03NetPylon" },
            new[] {
                "SuspensionGolden-40-两块板六车道_SuspensionGolden",
                "SuspensionGolden-40-两块板六车道_SuspensionGolden SuspensionBridge03NetPylon"
            }),
        new Pair(
            "TiedArch",
            "Tied Arch Bridge - 4 lanes",
            "两块板六车道_TiedArch",
            BoundaryKind.Overhead,
            new string[0],
            new string[0]),
        new Pair(
            "TrussArch",
            "Truss Arch Bridge - Highway Twoway - 2 Lanes",
            "两块板六车道_TrussArch",
            BoundaryKind.Outer,
            new[] { "2LaneTrussArchBridgePillar Placeholder" },
            new[] { "TrussArch-40-两块板六车道_TrussArch" }),
        new Pair(
            "TrussArch01",
            "TrussArchBridge01",
            "两块板六车道_TrussArch01",
            BoundaryKind.Outer,
            new[] { "TrussArchBridge01NetPillar" },
            new[] { "TrussArch01-40-两块板六车道_TrussArch01" }),
        new Pair(
            "TrussArch02",
            "TrussArchBridge02",
            "两块板六车道_TrussArch02",
            BoundaryKind.Overhead,
            new string[0],
            new string[0]),
        new Pair(
            "TrussArch03",
            "TrussArchBridge03",
            "两块板六车道_TrussArch03",
            BoundaryKind.Outer,
            new[] { "TrussArchBridge03NetPillar" },
            new[] { "TrussArch03-40-两块板六车道_TrussArch03" })
    };

    public static string Run(string anatomyPath, string outputPath, string[] requestedStyles)
    {
        Failure = "";
        string[] lines = File.ReadAllLines(anatomyPath, Encoding.UTF8);
        Dictionary<string, Block> blocks = GetBlocks(lines);
        Dictionary<string, float> pieceWidths = GetPieceWidths(lines);
        Pair[] selectedPairs = Pairs;
        if (requestedStyles != null && requestedStyles.Length != 0)
        {
            var requested = new HashSet<string>(requestedStyles, StringComparer.Ordinal);
            selectedPairs = Pairs.Where(pair => requested.Contains(pair.Style)).ToArray();
            string[] unknown = requested
                .Where(style => !Pairs.Any(pair => String.Equals(pair.Style, style, StringComparison.Ordinal)))
                .OrderBy(style => style, StringComparer.Ordinal)
                .ToArray();
            if (unknown.Length != 0) return "ERROR: Unknown bridge style(s): " + String.Join(", ", unknown);
        }
        var output = new List<string>();
        output.Add(string.Join("\t", new[]
        {
            "Style", "Archetype", "Generated", "Boundary",
            "ArchetypeBridgeMinX", "ArchetypeBridgeMinXBits",
            "ArchetypeBridgeMaxX", "ArchetypeBridgeMaxXBits",
            "ArchetypeBridgeWidth", "ArchetypeBridgeWidthBits",
            "ArchetypeRoadWidth", "ArchetypeRoadWidthBits",
            "GeneratedBridgeMinX", "GeneratedBridgeMinXBits",
            "GeneratedBridgeMaxX", "GeneratedBridgeMaxXBits",
            "GeneratedBridgeWidth", "GeneratedBridgeWidthBits",
            "GeneratedRoadWidth", "GeneratedRoadWidthBits",
            "WidthIncrement", "WidthIncrementBits",
            "NewBridgeWidth", "NewBridgeWidthBits", "Result"
        }));

        var summary = new StringBuilder();
        int skipped = 0;
        int changed = 0;
        foreach (Pair pair in selectedPairs)
        {
            Block archetypeBlock = RequiredBlock(blocks, pair.Archetype);
            Block generatedBlock = RequiredBlock(blocks, pair.Generated);
            if (Failure.Length != 0) return "ERROR: " + Failure;

            float archetypeRoadWidth = Single.IsNaN(pair.MeasuredArchetypeRoadWidth)
                ? GetRoadFullWidth(lines, archetypeBlock, pieceWidths)
                : pair.MeasuredArchetypeRoadWidth;
            float generatedRoadWidth = GetRoadFullWidth(lines, generatedBlock, pieceWidths);
            BridgeBoundary archetype = GetBridgeBoundary(
                lines, archetypeBlock, pieceWidths, pair.ArchetypeObjects, pair.Boundary, pair.Archetype);
            BridgeBoundary generated = GetBridgeBoundary(
                lines, generatedBlock, pieceWidths, pair.GeneratedObjects, pair.Boundary, pair.Generated);
            if (Failure.Length != 0 || archetype == null || generated == null)
                return "ERROR: " + Failure;

            // These are the only two audit formulae. All operands and both intermediate results are
            // binary32. Do not add a tolerance, decimal conversion, normalization or forced zero.
            float widthIncrement =
                (archetype.Selected.Width - archetypeRoadWidth)
                - (generated.Selected.Width - generatedRoadWidth);
            float newBridgeWidth = generated.Selected.Width + widthIncrement;

            bool isSkipped = Math.Abs(widthIncrement) > 1.0f;
            bool isBitwisePositiveZero = Bits(widthIncrement) == 0;
            string result;
            if (isSkipped)
            {
                result = "Skipped";
                skipped++;
            }
            else if (isBitwisePositiveZero)
            {
                result = "Unchanged";
            }
            else
            {
                result = "Apply";
                changed++;
            }

            output.Add(string.Join("\t", new[]
            {
                pair.Style, pair.Archetype, pair.Generated, pair.Boundary.ToString(),
                R(archetype.Selected.MinX), Hex(archetype.Selected.MinX),
                R(archetype.Selected.MaxX), Hex(archetype.Selected.MaxX),
                R(archetype.Selected.Width), Hex(archetype.Selected.Width),
                R(archetypeRoadWidth), Hex(archetypeRoadWidth),
                R(generated.Selected.MinX), Hex(generated.Selected.MinX),
                R(generated.Selected.MaxX), Hex(generated.Selected.MaxX),
                R(generated.Selected.Width), Hex(generated.Selected.Width),
                R(generatedRoadWidth), Hex(generatedRoadWidth),
                R(widthIncrement), Hex(widthIncrement),
                R(newBridgeWidth), Hex(newBridgeWidth), result
            }));

            summary.Append(pair.Style.PadRight(20));
            summary.Append(" increment=").Append(R(widthIncrement));
            summary.Append(" [").Append(Hex(widthIncrement)).Append("]");
            summary.Append(" newBridgeWidth=").Append(R(newBridgeWidth));
            summary.Append(" [").Append(Hex(newBridgeWidth)).Append("] ");
            summary.AppendLine(result);
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllLines(outputPath, output, new UTF8Encoding(false));
        summary.Append("Bridge width audit: ").Append(selectedPairs.Length)
            .Append(" measured, ").Append(skipped)
            .Append(" skipped, ").Append(changed).Append(" nonzero applicable.");
        return summary.ToString();
    }

    private static Dictionary<string, Block> GetBlocks(string[] lines)
    {
        var ordered = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < lines.Length; i++)
        {
            Match match = Header.Match(lines[i]);
            if (match.Success) ordered.Add(new KeyValuePair<string, int>(match.Groups[1].Value, i));
        }

        var result = new Dictionary<string, Block>(StringComparer.Ordinal);
        for (int i = 0; i < ordered.Count; i++)
        {
            result[ordered[i].Key] = new Block
            {
                Start = ordered[i].Value,
                End = i + 1 < ordered.Count ? ordered[i + 1].Value : lines.Length
            };
        }
        return result;
    }

    private static Block RequiredBlock(Dictionary<string, Block> blocks, string name)
    {
        Block block;
        if (!blocks.TryGetValue(name, out block))
        {
            Failure = "Required real prefab block is absent: " + name;
            return null;
        }
        return block;
    }

    private static Dictionary<string, float> GetPieceWidths(string[] lines)
    {
        var result = new Dictionary<string, float>(StringComparer.Ordinal);
        string pending = null;
        int pendingIndent = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int indent = Indent(line);
            if (pending != null && indent <= pendingIndent)
            {
                pending = null;
                pendingIndent = -1;
            }

            Match piece = Piece.Match(line);
            if (piece.Success)
            {
                pending = piece.Groups[2].Value;
                pendingIndent = piece.Groups[1].Value.Length;
                continue;
            }

            if (pending != null && indent == pendingIndent + 2 && line.TrimStart().StartsWith("m_Width = ", StringComparison.Ordinal))
                result[pending] = ParseFinalFloat(line);
        }
        return result;
    }

    private static float GetRoadFullWidth(
        string[] lines,
        Block block,
        Dictionary<string, float> pieceWidths)
    {
        var sections = new List<Section>();
        Section current = null;
        string requirement = "";
        for (int i = block.Start + 1; i < block.End; i++)
        {
            string line = lines[i];
            if (line.StartsWith("  <", StringComparison.Ordinal)) break;

            if (TopSection.IsMatch(line))
            {
                if (current != null) sections.Add(current);
                current = new Section();
                requirement = "";
                continue;
            }
            if (current == null) continue;

            Match sectionName = SectionName.Match(line);
            if (sectionName.Success)
            {
                if (current.Name.Length == 0) current.Name = sectionName.Groups[1].Value;
                continue;
            }
            if (line.StartsWith("              m_Width = ", StringComparison.Ordinal))
            {
                current.Widths.Add(ParseFinalFloat(line));
                continue;
            }
            Match directPiece = DirectPieceName.Match(line);
            if (directPiece.Success)
            {
                current.Pieces.Add(directPiece.Groups[1].Value);
                continue;
            }
            if (line.StartsWith("      m_RequireAll =", StringComparison.Ordinal))
            {
                requirement = "All";
                continue;
            }
            if (line.StartsWith("      m_RequireAny =", StringComparison.Ordinal))
            {
                requirement = "Any";
                continue;
            }
            if (line.StartsWith("      m_RequireNone =", StringComparison.Ordinal))
            {
                requirement = "None";
                continue;
            }
            if (line.Length >= 7 && line.StartsWith("      ", StringComparison.Ordinal) && line[6] != ' ')
            {
                requirement = "";
                continue;
            }
            if (line.StartsWith("        [", StringComparison.Ordinal) && line.Contains(" = "))
            {
                if (requirement == "All") current.HasRequireAll = true;
                else if (requirement == "Any") current.HasRequireAny = true;
            }
        }
        if (current != null) sections.Add(current);
        if (sections.Count == 0)
        {
            Failure = "No top-level road sections found after " + lines[block.Start];
            return Single.NaN;
        }

        var widthByName = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (Section section in sections)
        {
            float width = SectionWidth(section, pieceWidths);
            if (width > 0.0f && section.Name.Length != 0) widthByName[section.Name] = width;
        }

        float fullWidth = 0.0f;
        foreach (Section section in sections)
        {
            if (section.HasRequireAll || section.HasRequireAny || IsOutwardSide(section.Name)) continue;
            float width = SectionWidth(section, pieceWidths);
            if (width <= 0.0f && section.Name.Length != 0) widthByName.TryGetValue(section.Name, out width);
            fullWidth = fullWidth + width;
        }
        return fullWidth;
    }

    private static float SectionWidth(Section section, Dictionary<string, float> pieceWidths)
    {
        float width = 0.0f;
        foreach (float candidate in section.Widths) width = Max(width, candidate);
        foreach (string piece in section.Pieces)
        {
            float candidate;
            if (pieceWidths.TryGetValue(piece, out candidate)) width = Max(width, candidate);
        }
        return width;
    }

    private static bool IsOutwardSide(string name)
    {
        if (String.IsNullOrEmpty(name)) return false;
        return name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(token => String.Equals(token, "Side", StringComparison.Ordinal));
    }

    private static BridgeBoundary GetBridgeBoundary(
        string[] lines,
        Block block,
        Dictionary<string, float> pieceWidths,
        HashSet<string> selectedObjects,
        BoundaryKind kind,
        string rootName)
    {
        SpanX objectSpan = GetObjectBoundary(lines, block, selectedObjects);
        SpanX overheadSpan = GetOverheadBoundary(lines, block, pieceWidths);
        SpanX selected;
        switch (kind)
        {
            case BoundaryKind.Object: selected = objectSpan; break;
            case BoundaryKind.Overhead: selected = overheadSpan; break;
            case BoundaryKind.Outer:
                selected = Union(objectSpan, overheadSpan);
                break;
            default:
                selected = new SpanX();
                Failure = "Unknown boundary kind for " + rootName;
                break;
        }
        if (!selected.Found)
        {
            Failure = "No " + kind + " bridge boundary found for " + rootName;
            return null;
        }
        return new BridgeBoundary { Object = objectSpan, Overhead = overheadSpan, Selected = selected };
    }

    private static SpanX GetObjectBoundary(
        string[] lines,
        Block block,
        HashSet<string> selectedObjects)
    {
        var result = new SpanX();
        if (selectedObjects.Count == 0) return result;
        bool inObjects = false;
        bool selected = false;
        bool selectedHasBounds = false;
        float selectedOffset = 0.0f;
        float selectedMeshMinX = 0.0f;
        float selectedMeshMaxX = 0.0f;
        bool selectedHasMinX = false;
        bool selectedHasMaxX = false;

        Action finish = () =>
        {
            if (selected && selectedHasBounds)
            {
                Include(result, selectedOffset + selectedMeshMinX, selectedOffset + selectedMeshMaxX);
            }
            selected = false;
            selectedHasBounds = false;
            selectedOffset = 0.0f;
            selectedMeshMinX = 0.0f;
            selectedMeshMaxX = 0.0f;
            selectedHasMinX = false;
            selectedHasMaxX = false;
        };

        for (int i = block.Start + 1; i <= block.End; i++)
        {
            string line = i < block.End ? lines[i] : "";
            if (line.StartsWith("  <", StringComparison.Ordinal))
            {
                if (inObjects) finish();
                inObjects = line.StartsWith("  <NetSubObjects>:", StringComparison.Ordinal);
                continue;
            }
            if (!inObjects) continue;
            if (line.StartsWith("      [", StringComparison.Ordinal) && line.Contains(": NetSubObjectInfo"))
            {
                finish();
                continue;
            }

            Match objectName = ObjectName.Match(line);
            if (objectName.Success)
            {
                selected = selectedObjects.Contains(objectName.Groups[1].Value);
                continue;
            }
            if (!selected) continue;

            if (line == "        m_Position: float3 (float3)" && i + 1 < block.End && lines[i + 1].StartsWith("          x = ", StringComparison.Ordinal))
            {
                selectedOffset = ParseFinalFloat(lines[i + 1]);
                continue;
            }

            // Read exact RenderPrefab bounds, not the two-decimal human survey line. Every numeric
            // token is parsed directly to Single and kept as its original binary32 value.
            if (line.TrimStart() == "m_Bounds: Bounds3 (Bounds3)")
            {
                bool readingMin = false;
                bool readingMax = false;
                for (int j = i + 1; j < block.End && j <= i + 14; j++)
                {
                    string boundsLine = lines[j].TrimStart();
                    if (boundsLine == "min: float3 (float3)")
                    {
                        readingMin = true;
                        readingMax = false;
                        continue;
                    }
                    if (boundsLine == "max: float3 (float3)")
                    {
                        readingMin = false;
                        readingMax = true;
                        continue;
                    }
                    if (boundsLine.StartsWith("x = ", StringComparison.Ordinal))
                    {
                        float coordinate;
                        if (TryParseFinalFloat(lines[j], out coordinate))
                        {
                            if (readingMin)
                            {
                                selectedMeshMinX = selectedHasMinX
                                    ? Min(selectedMeshMinX, coordinate)
                                    : coordinate;
                                selectedHasMinX = true;
                            }
                            else if (readingMax)
                            {
                                selectedMeshMaxX = selectedHasMaxX
                                    ? Max(selectedMeshMaxX, coordinate)
                                    : coordinate;
                                selectedHasMaxX = true;
                            }
                            selectedHasBounds = selectedHasMinX && selectedHasMaxX;
                        }
                    }
                }
            }
        }
        return result;
    }

    private static SpanX GetOverheadBoundary(
        string[] lines,
        Block block,
        Dictionary<string, float> pieceWidths)
    {
        var result = new SpanX();
        bool inOverhead = false;
        bool inEntry = false;
        bool hasWidth = false;
        float offset = 0.0f;
        float fullWidth = 0.0f;

        Action finish = () =>
        {
            if (inEntry && hasWidth)
            {
                float halfWidth = Half(fullWidth);
                Include(result, offset - halfWidth, offset + halfWidth);
            }
            inEntry = false;
            hasWidth = false;
            offset = 0.0f;
            fullWidth = 0.0f;
        };

        for (int i = block.Start + 1; i <= block.End; i++)
        {
            string line = i < block.End ? lines[i] : "";
            if (line.StartsWith("  <", StringComparison.Ordinal))
            {
                if (inOverhead) finish();
                inOverhead = line.StartsWith("  <OverheadNetSections>:", StringComparison.Ordinal);
                continue;
            }
            if (!inOverhead) continue;
            if (line.StartsWith("      [", StringComparison.Ordinal) && line.Contains(": NetSectionInfo"))
            {
                finish();
                inEntry = true;
                continue;
            }
            if (!inEntry) continue;

            if (line == "        m_Offset: float3 (float3)" && i + 1 < block.End && lines[i + 1].StartsWith("          x = ", StringComparison.Ordinal))
            {
                offset = ParseFinalFloat(lines[i + 1]);
                continue;
            }
            if (line.StartsWith("                m_Width = ", StringComparison.Ordinal))
            {
                fullWidth = Max(fullWidth, ParseFinalFloat(line));
                hasWidth = true;
                continue;
            }
            Match directPiece = OverheadPieceName.Match(line);
            if (directPiece.Success)
            {
                float candidate;
                if (pieceWidths.TryGetValue(directPiece.Groups[1].Value, out candidate))
                {
                    fullWidth = Max(fullWidth, candidate);
                    hasWidth = true;
                }
            }
        }
        finish();
        return result;
    }

    private static void Include(SpanX target, float minX, float maxX)
    {
        if (!target.Found)
        {
            target.MinX = minX;
            target.MaxX = maxX;
            target.Found = true;
            return;
        }
        target.MinX = Min(target.MinX, minX);
        target.MaxX = Max(target.MaxX, maxX);
    }

    private static SpanX Union(SpanX left, SpanX right)
    {
        var result = new SpanX();
        if (left.Found) Include(result, left.MinX, left.MaxX);
        if (right.Found) Include(result, right.MinX, right.MaxX);
        return result;
    }

    private static int Indent(string line)
    {
        int result = 0;
        while (result < line.Length && line[result] == ' ') result++;
        return result;
    }

    private static float ParseFinalFloat(string line)
    {
        float value;
        if (!TryParseFinalFloat(line, out value))
        {
            Failure = "Expected a float: " + line;
            return Single.NaN;
        }
        return value;
    }

    private static bool TryParseFinalFloat(string line, out float value)
    {
        Match match = Number.Match(line);
        value = 0.0f;
        return match.Success && Single.TryParse(
            match.Groups[1].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static float Half(float value) { return value * 0.5f; }
    private static float Min(float left, float right) { return left < right ? left : right; }
    private static float Max(float left, float right) { return left > right ? left : right; }
    private static int Bits(float value) { return BitConverter.ToInt32(BitConverter.GetBytes(value), 0); }
    private static string Hex(float value) { return "0x" + unchecked((uint)Bits(value)).ToString("X8", CultureInfo.InvariantCulture); }
    private static string R(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
}
'@

if (-not ('BridgeWidthBitwiseAudit' -as [type])) {
    Add-Type -TypeDefinition $source -Language CSharp
}

$result = [BridgeWidthBitwiseAudit]::Run(
    (Resolve-Path -LiteralPath $AnatomyPath).Path,
    [IO.Path]::GetFullPath($OutputPath),
    $Styles)
if ($result.StartsWith('ERROR: ', [StringComparison]::Ordinal)) {
    Write-Error $result
    exit 3
}
$result
