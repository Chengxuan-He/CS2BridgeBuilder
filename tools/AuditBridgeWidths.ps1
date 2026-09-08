[CmdletBinding()]
param(
    [string] $AnatomyPath = (Join-Path (
        Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
            '..\LocalLow\Colossal Order\Cities Skylines II\ModsData\BridgeBuilder') `
        'asset-anatomy.txt'),
    [string] $OutputPath = (Join-Path $PSScriptRoot `
        '..\docs\agent-contract\bridge-width-invariant-measurements.tsv')
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

        public Pair(
            string style,
            string archetype,
            string generated,
            BoundaryKind boundary,
            string[] archetypeObjects,
            string[] generatedObjects)
        {
            Style = style;
            Archetype = archetype;
            Generated = generated;
            Boundary = boundary;
            ArchetypeObjects = new HashSet<string>(archetypeObjects, StringComparer.Ordinal);
            GeneratedObjects = new HashSet<string>(generatedObjects, StringComparer.Ordinal);
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

    private sealed class BridgeBoundary
    {
        public float ObjectX;
        public float OverheadX;
        public float SelectedX;
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
            new[] { "CableStayed-40-两块板六车道_CableStayed" }),
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

    public static string Run(string anatomyPath, string outputPath)
    {
        Failure = "";
        string[] lines = File.ReadAllLines(anatomyPath, Encoding.UTF8);
        Dictionary<string, Block> blocks = GetBlocks(lines);
        Dictionary<string, float> pieceWidths = GetPieceWidths(lines);
        var output = new List<string>();
        output.Add(string.Join("\t", new[]
        {
            "Style", "Archetype", "Generated", "Boundary",
            "ArchetypeBridgeX", "ArchetypeBridgeXBits",
            "ArchetypeRoadX", "ArchetypeRoadXBits",
            "GeneratedBridgeX", "GeneratedBridgeXBits",
            "GeneratedRoadX", "GeneratedRoadXBits",
            "WidthIncrement", "WidthIncrementBits",
            "NewBridgeX", "NewBridgeXBits", "Result"
        }));

        var summary = new StringBuilder();
        int skipped = 0;
        int changed = 0;
        foreach (Pair pair in Pairs)
        {
            Block archetypeBlock = RequiredBlock(blocks, pair.Archetype);
            Block generatedBlock = RequiredBlock(blocks, pair.Generated);
            if (Failure.Length != 0) return "ERROR: " + Failure;

            float archetypeRoadX = Half(GetRoadFullWidth(lines, archetypeBlock, pieceWidths));
            float generatedRoadX = Half(GetRoadFullWidth(lines, generatedBlock, pieceWidths));
            BridgeBoundary archetype = GetBridgeBoundary(
                lines, archetypeBlock, pieceWidths, pair.ArchetypeObjects, pair.Boundary, pair.Archetype);
            BridgeBoundary generated = GetBridgeBoundary(
                lines, generatedBlock, pieceWidths, pair.GeneratedObjects, pair.Boundary, pair.Generated);
            if (Failure.Length != 0 || archetype == null || generated == null)
                return "ERROR: " + Failure;

            // These are the only two audit formulae. All operands and both intermediate results are
            // binary32. Do not add a tolerance, decimal conversion, normalization or forced zero.
            float widthIncrement =
                (archetype.SelectedX - archetypeRoadX)
                - (generated.SelectedX - generatedRoadX);
            float newBridgeX = generated.SelectedX + widthIncrement;

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
                R(archetype.SelectedX), Hex(archetype.SelectedX),
                R(archetypeRoadX), Hex(archetypeRoadX),
                R(generated.SelectedX), Hex(generated.SelectedX),
                R(generatedRoadX), Hex(generatedRoadX),
                R(widthIncrement), Hex(widthIncrement),
                R(newBridgeX), Hex(newBridgeX), result
            }));

            summary.Append(pair.Style.PadRight(20));
            summary.Append(" increment=").Append(R(widthIncrement));
            summary.Append(" [").Append(Hex(widthIncrement)).Append("]");
            summary.Append(" newBridgeX=").Append(R(newBridgeX));
            summary.Append(" [").Append(Hex(newBridgeX)).Append("] ");
            summary.AppendLine(result);
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllLines(outputPath, output, new UTF8Encoding(false));
        summary.Append("Bridge width audit: ").Append(Pairs.Length)
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
        bool foundObject;
        bool foundOverhead;
        float objectX = GetObjectBoundary(lines, block, selectedObjects, out foundObject);
        float overheadX = GetOverheadBoundary(lines, block, pieceWidths, out foundOverhead);
        float selected;
        bool found;
        switch (kind)
        {
            case BoundaryKind.Object: selected = objectX; found = foundObject; break;
            case BoundaryKind.Overhead: selected = overheadX; found = foundOverhead; break;
            case BoundaryKind.Outer:
                selected = Max(objectX, overheadX);
                found = foundObject || foundOverhead;
                break;
            default:
                selected = 0.0f;
                found = false;
                Failure = "Unknown boundary kind for " + rootName;
                break;
        }
        if (!found)
        {
            Failure = "No " + kind + " bridge boundary found for " + rootName;
            return null;
        }
        return new BridgeBoundary { ObjectX = objectX, OverheadX = overheadX, SelectedX = selected };
    }

    private static float GetObjectBoundary(
        string[] lines,
        Block block,
        HashSet<string> selectedObjects,
        out bool found)
    {
        found = false;
        if (selectedObjects.Count == 0) return 0.0f;
        bool inObjects = false;
        bool selected = false;
        bool selectedHasBounds = false;
        float selectedOffset = 0.0f;
        float selectedMeshX = 0.0f;
        float result = 0.0f;
        bool measured = false;

        Action finish = () =>
        {
            if (selected && selectedHasBounds)
            {
                result = Max(result, Math.Abs(selectedOffset) + selectedMeshX);
                measured = true;
            }
            selected = false;
            selectedHasBounds = false;
            selectedOffset = 0.0f;
            selectedMeshX = 0.0f;
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
                for (int j = i + 1; j < block.End && j <= i + 14; j++)
                {
                    if (lines[j].TrimStart().StartsWith("x = ", StringComparison.Ordinal))
                    {
                        float coordinate;
                        if (TryParseFinalFloat(lines[j], out coordinate))
                        {
                            selectedMeshX = Max(selectedMeshX, Math.Abs(coordinate));
                            selectedHasBounds = true;
                        }
                    }
                }
            }
        }
        found = measured;
        return result;
    }

    private static float GetOverheadBoundary(
        string[] lines,
        Block block,
        Dictionary<string, float> pieceWidths,
        out bool found)
    {
        found = false;
        bool inOverhead = false;
        bool inEntry = false;
        bool hasWidth = false;
        float offset = 0.0f;
        float fullWidth = 0.0f;
        float result = 0.0f;
        bool measured = false;

        Action finish = () =>
        {
            if (inEntry && hasWidth)
            {
                result = Max(result, Math.Abs(offset) + Half(fullWidth));
                measured = true;
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
        found = measured;
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
    [IO.Path]::GetFullPath($OutputPath))
if ($result.StartsWith('ERROR: ', [StringComparison]::Ordinal)) {
    Write-Error $result
    exit 3
}
$result
