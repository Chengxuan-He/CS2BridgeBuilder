using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
/// <summary>
/// Measured widths for every bridge prefab this installation had when the table was taken, in whole
/// metres.
///
/// These are recorded rather than computed because computing them at runtime is not reliable enough
/// to build on. A tower's width lives in its mesh bounds, which are only readable once the geometry
/// asset is loaded, and a bridge's road width has to be inferred from which of its sections are
/// bridge-only - a rule the packs follow and the game's own bridges do not. The result was numbers
/// that changed with what happened to be loaded, and a few that were plainly wrong.
///
/// So the scan was done once, read out of the discovery log, and frozen here. Whole metres on
/// purpose: the fractions were measurement noise, not authored detail, and rounding them away makes
/// the table something a person can read and correct.
///
/// A prefab that is not in the table still falls back to measuring itself, so installing new content
/// keeps working; it simply does not get the benefit of a checked number.
///
/// To retake it: run a scan, take the "[Style] from ...: name road Xm tower Ym" lines out of this
/// mod's log, and regenerate this file.
/// </summary>
internal static class BridgeMeasurements
{
    // DrawBridge02: four 3 m carriageway occurrences and two 3.5 m sidewalks.
    // Exclude both 0.6 m outward strips and count mutually exclusive network branches once.
    // Source: all-movable-audit.json, RoadPrefab 118590; pieces 120675 / 120789.
    internal static int Draw02RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[0];
    // Draw03: six 3 m carriageways and two 3.5 m sidewalks. The eleven lateral
    // occurrences each have three conditional branches, not three parallel strips.
    // Exclude the two outward 0.5 m sides. Native pieces 79448/79451/79454/79457.
    internal static int Draw03RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[1];
    // Native Pavement Path Section 8 between two zero-width side sections.
    internal static int PedestrianDraw02RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[2];
    // PedestrianDraw01 root 118833 has the same single 8m pavement occurrence.
    internal static int PedestrianDraw01RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[3];
    // Lift01 elevated composition: two 1 m shoulders plus one 4 m train strip.
    // Train Shoulder 2 selects its 1 m piece for Elevated; ground width is different.
    // Near/middle source road tops independently span X=-3..3 m.
    internal static int Lift01RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[4];
    // Lift05: two 3 m tram strips plus two 1 m shoulders; conditional branches count once.
    internal static int Lift05RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[5];
    // Lift02: two 4 m tracks and two 1 m shoulders; exclude outward 0.3 m sides.
    // Native pieces 79483/79475; each occurrence has three mutually exclusive branches.
    internal static int Lift02RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[6];
    // Lift04 elevated composition: two 4 m subway tracks and two 1 m shoulders.
    // Pieces 120985/120981; outward 0.5 m sides do not widen the road envelope.
    internal static int Lift04RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[7];
    // Lift03 main: four 4 m highway strips plus two 2 m shoulders.
    // Auxiliary elevated track: two 4 m strips plus two 1 m shoulders.
    // Native roots 118610/118609; mutually exclusive branches count once.
    internal static int Lift03RoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[8];
    internal static int Lift03AuxiliaryRoadWidth => Numeric_BridgeMeasurements_cs.Values.I32[9];

    /// <summary>Road width and tower width in whole metres, keyed by prefab name.</summary>
    private static readonly Dictionary<string, (int Road, int Tower)> Table = new(StringComparer.Ordinal)
    {
        ["PedestrianDrawBridge01"] = (PedestrianDraw01RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[10]),
        ["PedestrianDrawBridge02"] = (PedestrianDraw02RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[11]),
        ["PedestrianBridgeCoveredWood01"] = (Numeric_BridgeMeasurements_cs.Values.I32[12], Numeric_BridgeMeasurements_cs.Values.I32[13]),
        ["BXP PedestrianBridgeCoveredWood01 Revised"] = (Numeric_BridgeMeasurements_cs.Values.I32[14], Numeric_BridgeMeasurements_cs.Values.I32[15]),
        ["BXP Golden Gate Bridge Subway Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[16], Numeric_BridgeMeasurements_cs.Values.I32[17]),
        ["BXP Golden Gate Bridge Train Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[18], Numeric_BridgeMeasurements_cs.Values.I32[19]),
        ["Suspension Bridge - Highway Oneway - 2 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[20], Numeric_BridgeMeasurements_cs.Values.I32[21]),
        ["Suspension Bridge - Highway Oneway - 3 lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[22], Numeric_BridgeMeasurements_cs.Values.I32[23]),
        ["Suspension Bridge - Highway Oneway - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[24], Numeric_BridgeMeasurements_cs.Values.I32[25]),
        ["Suspension Bridge - Highway Oneway - 5 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[26], Numeric_BridgeMeasurements_cs.Values.I32[27]),
        // These two prefabs carry Smooth/non-Smooth alternatives for the same lateral section.
        // Summing both alternatives reported exactly twice the real left-to-right road width
        // (26/32). The retained prefab cross-sections are 3.5+3+3+3.5 = 13 m for 01 and
        // 2+4+4+4+2 = 16 m for 02. They are prototype facts, not runtime measurements.
        ["SuspensionBridge01"] = (Numeric_BridgeMeasurements_cs.Values.I32[28], Numeric_BridgeMeasurements_cs.Values.I32[29]),
        ["SuspensionBridge02"] = (Numeric_BridgeMeasurements_cs.Values.I32[30], Numeric_BridgeMeasurements_cs.Values.I32[31]),
        ["SuspensionBridge03"] = (Numeric_BridgeMeasurements_cs.Values.I32[32], Numeric_BridgeMeasurements_cs.Values.I32[33]),
        ["SuspensionBridge04"] = (Numeric_BridgeMeasurements_cs.Values.I32[34], Numeric_BridgeMeasurements_cs.Values.I32[35]),
        // Double-deck width audit: this prefab is the upper/root road. Its active road sections
        // span 24 m; 26 m included the two outward side extensions that are not the road-width
        // operand of the bridge-minus-road invariant.
        ["BXP Double Deck Suspension Bridge - Highway"] = (Numeric_BridgeMeasurements_cs.Values.I32[36], Numeric_BridgeMeasurements_cs.Values.I32[37]),
        ["BXP Suspension Bridge - Highway Twoway - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[38], Numeric_BridgeMeasurements_cs.Values.I32[39]),
        ["BXP SuspensionBridge03 - Six Lane Highway"] = (Numeric_BridgeMeasurements_cs.Values.I32[40], Numeric_BridgeMeasurements_cs.Values.I32[41]),
        ["BXP SuspensionBridge02 Above Road Four-Lane"] = (Numeric_BridgeMeasurements_cs.Values.I32[42], Numeric_BridgeMeasurements_cs.Values.I32[43]),
        ["BXP Double Deck Suspension Bridge - Subway Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[44], Numeric_BridgeMeasurements_cs.Values.I32[45]),
        ["BXP Suspension Bridge - Train - 4 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[46], Numeric_BridgeMeasurements_cs.Values.I32[47]),
        ["BXP Double Deck Suspension Bridge - Lower Highway"] = (Numeric_BridgeMeasurements_cs.Values.I32[48], Numeric_BridgeMeasurements_cs.Values.I32[49]),
        ["BXP Double Deck Suspension Bridge - Train"] = (Numeric_BridgeMeasurements_cs.Values.I32[50], Numeric_BridgeMeasurements_cs.Values.I32[51]),
        ["BXP Double Deck Suspension Bridge - Train Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[52], Numeric_BridgeMeasurements_cs.Values.I32[53]),
        ["BXP Suspension Bridge - Highway Twoway - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[54], Numeric_BridgeMeasurements_cs.Values.I32[55]),
        ["BXP Suspension Bridge - Train - 2 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[56], Numeric_BridgeMeasurements_cs.Values.I32[57]),
        ["BXP Suspension Bridge - Highway Oneway - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[58], Numeric_BridgeMeasurements_cs.Values.I32[59]),
        ["BXP SuspensionBridge02 - Four-Lane"] = (Numeric_BridgeMeasurements_cs.Values.I32[60], Numeric_BridgeMeasurements_cs.Values.I32[61]),
        ["BXP Double Deck Suspension Bridge - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[62], Numeric_BridgeMeasurements_cs.Values.I32[63]),
        ["SuspensionBridge02 Above Road"] = (Numeric_BridgeMeasurements_cs.Values.I32[64], Numeric_BridgeMeasurements_cs.Values.I32[65]),
        ["Extradosed Bridge - Large Road Divided - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[66], Numeric_BridgeMeasurements_cs.Values.I32[67]),
        // Double-deck width audit: 01's root is the upper road and 02's root is the lower road.
        // These are the active root-road spans from the retained real-prefab dump; mutually
        // exclusive and outward-only sections are not road-width operands.
        ["ExtradosedBridge01"] = (Numeric_BridgeMeasurements_cs.Values.I32[68], Numeric_BridgeMeasurements_cs.Values.I32[69]),
        ["ExtradosedBridge02"] = (Numeric_BridgeMeasurements_cs.Values.I32[70], Numeric_BridgeMeasurements_cs.Values.I32[71]),
        ["ExtradosedBridge03"] = (Numeric_BridgeMeasurements_cs.Values.I32[72], Numeric_BridgeMeasurements_cs.Values.I32[73]),
        ["ExtradosedBridge04"] = (Numeric_BridgeMeasurements_cs.Values.I32[74], Numeric_BridgeMeasurements_cs.Values.I32[75]),
        ["BXP ExtradosedBridge01 - PedsBikes"] = (Numeric_BridgeMeasurements_cs.Values.I32[76], Numeric_BridgeMeasurements_cs.Values.I32[77]),
        ["BXP ExtradosedBridge01 - Tram Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[78], Numeric_BridgeMeasurements_cs.Values.I32[79]),
        ["BXP ExtradosedBridge01 - Public Transport"] = (Numeric_BridgeMeasurements_cs.Values.I32[80], Numeric_BridgeMeasurements_cs.Values.I32[81]),
        ["BXP Extradosed Bridge - Train - 2 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[82], Numeric_BridgeMeasurements_cs.Values.I32[83]),
        ["BXP ExtradosedBridge01 - PedsBikes Only"] = (Numeric_BridgeMeasurements_cs.Values.I32[84], Numeric_BridgeMeasurements_cs.Values.I32[85]),
        ["BXP ExtradosedBridge03 - Highway"] = (Numeric_BridgeMeasurements_cs.Values.I32[86], Numeric_BridgeMeasurements_cs.Values.I32[87]),
        ["BXP ExtradosedBridge02 - Train Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[88], Numeric_BridgeMeasurements_cs.Values.I32[89]),
        ["BXP ExtradosedBridge01 - Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[90], Numeric_BridgeMeasurements_cs.Values.I32[91]),
        ["BXP ExtradosedBridge02 - Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[92], Numeric_BridgeMeasurements_cs.Values.I32[93]),
        ["BXP Extradosed Bridge - Subway - 2 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[94], Numeric_BridgeMeasurements_cs.Values.I32[95]),
        ["BXP ExtradosedBridge01 - Subway Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[96], Numeric_BridgeMeasurements_cs.Values.I32[97]),
        ["BXP ExtradosedBridge01 - Public Transport Road"] = (Numeric_BridgeMeasurements_cs.Values.I32[98], Numeric_BridgeMeasurements_cs.Values.I32[99]),
        ["BXP Extradosed Bridge - Highway Twoway - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[100], Numeric_BridgeMeasurements_cs.Values.I32[101]),
        ["BXP Extradosed Bridge - Highway Twoway - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[102], Numeric_BridgeMeasurements_cs.Values.I32[103]),
        ["BXP ExtradosedBridge02 - Subway Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[104], Numeric_BridgeMeasurements_cs.Values.I32[105]),
        ["BXP ExtradosedBridge02 - Tram Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[106], Numeric_BridgeMeasurements_cs.Values.I32[107]),
        ["BXP ExtradosedBridge01 - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[108], Numeric_BridgeMeasurements_cs.Values.I32[109]),
        ["BXP Extradosed Bridge - Medium Road Divided - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[110], Numeric_BridgeMeasurements_cs.Values.I32[111]),
        ["BXP ExtradosedBridge02 - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[112], Numeric_BridgeMeasurements_cs.Values.I32[113]),
        ["BXP Extradosed Bridge - Tram - 2 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[114], Numeric_BridgeMeasurements_cs.Values.I32[115]),
        ["BXP ExtradosedBridge02 - Train"] = (Numeric_BridgeMeasurements_cs.Values.I32[116], Numeric_BridgeMeasurements_cs.Values.I32[117]),
        ["ExtradosedBridge01 Train Track"] = (Numeric_BridgeMeasurements_cs.Values.I32[118], Numeric_BridgeMeasurements_cs.Values.I32[119]),
        ["ExtradosedBridge02 Above Road"] = (Numeric_BridgeMeasurements_cs.Values.I32[120], Numeric_BridgeMeasurements_cs.Values.I32[121]),
        ["Cable-stayed Bridge - XL Road Divided - 8 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[122], Numeric_BridgeMeasurements_cs.Values.I32[123]),
        ["Cable Stayed Pedestrian Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[124], Numeric_BridgeMeasurements_cs.Values.I32[125]),
        ["Cable Stayed Bike Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[126], Numeric_BridgeMeasurements_cs.Values.I32[127]),
        ["BXP Cable Stayed Pedestrian Bridge Revised"] = (Numeric_BridgeMeasurements_cs.Values.I32[128], Numeric_BridgeMeasurements_cs.Values.I32[129]),
        ["Truss Arch Bridge - Small Road - 2 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[130], Numeric_BridgeMeasurements_cs.Values.I32[131]),
        ["Truss Arch Bridge - Highway Twoway - 2 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[132], Numeric_BridgeMeasurements_cs.Values.I32[133]),
        ["TrussArchBridge01"] = (Numeric_BridgeMeasurements_cs.Values.I32[134], Numeric_BridgeMeasurements_cs.Values.I32[135]),
        ["TrussArchBridge02"] = (Numeric_BridgeMeasurements_cs.Values.I32[136], Numeric_BridgeMeasurements_cs.Values.I32[137]),
        ["TrussArchBridge03"] = (Numeric_BridgeMeasurements_cs.Values.I32[138], Numeric_BridgeMeasurements_cs.Values.I32[139]),
        ["BXP Truss Arch Bridge - Highway Oneway - 3 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[140], Numeric_BridgeMeasurements_cs.Values.I32[141]),
        ["BXP TrussArchBridge03 - Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[142], Numeric_BridgeMeasurements_cs.Values.I32[143]),
        ["BXP TrussArchBridge03 - Train"] = (Numeric_BridgeMeasurements_cs.Values.I32[144], Numeric_BridgeMeasurements_cs.Values.I32[145]),
        ["BXP Truss Arch Bridge - Large Road - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[146], Numeric_BridgeMeasurements_cs.Values.I32[147]),
        ["BXP Truss Arch Bridge - Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[148], Numeric_BridgeMeasurements_cs.Values.I32[149]),
        ["BXP TrussArchBridge01 - Highway Twoway - 2 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[150], Numeric_BridgeMeasurements_cs.Values.I32[151]),
        ["BXP TrussArchBridge01 - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[152], Numeric_BridgeMeasurements_cs.Values.I32[153]),
        ["BXP TrussArchBridge01 - Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[154], Numeric_BridgeMeasurements_cs.Values.I32[155]),
        ["BXP Truss Arch Bridge - Medium Road - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[156], Numeric_BridgeMeasurements_cs.Values.I32[157]),
        ["BXP TrussArchBridge01 - Highway Twoway - 6 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[158], Numeric_BridgeMeasurements_cs.Values.I32[159]),
        ["BXP TrussArchBridge03 - Highway"] = (Numeric_BridgeMeasurements_cs.Values.I32[160], Numeric_BridgeMeasurements_cs.Values.I32[161]),
        ["BXP Truss Arch Bridge - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[162], Numeric_BridgeMeasurements_cs.Values.I32[163]),
        ["BXP Truss Arch Bridge - Highway Oneway - 2 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[164], Numeric_BridgeMeasurements_cs.Values.I32[165]),
        ["Tied Arch Bridge - 4 lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[166], Numeric_BridgeMeasurements_cs.Values.I32[167]),
        ["BXP Tied Arch Bridge - Train - 4 Tracks"] = (Numeric_BridgeMeasurements_cs.Values.I32[168], Numeric_BridgeMeasurements_cs.Values.I32[169]),
        ["BXP Tied Arch Bridge - Highway Oneway - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[170], Numeric_BridgeMeasurements_cs.Values.I32[171]),
        ["BXP Tied Arch Bridge - Train - 4 Tracks Alternating"] = (Numeric_BridgeMeasurements_cs.Values.I32[172], Numeric_BridgeMeasurements_cs.Values.I32[173]),
        ["BXP Tied Arch Bridge - Highway Twoway - 4 Lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[174], Numeric_BridgeMeasurements_cs.Values.I32[175]),
        ["Grand Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[176], Numeric_BridgeMeasurements_cs.Values.I32[177]),
        ["BXP Grand Bridge Viaduct"] = (Numeric_BridgeMeasurements_cs.Values.I32[178], Numeric_BridgeMeasurements_cs.Values.I32[179]),
        ["BXP Grand Bridge - Center Tram"] = (Numeric_BridgeMeasurements_cs.Values.I32[180], Numeric_BridgeMeasurements_cs.Values.I32[181]),
        ["DrawBridge03"] = (Draw03RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[182]),
        ["DrawBridge02"] = (Draw02RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[183]),
        ["DrawBridge01"] = (Numeric_BridgeMeasurements_cs.Values.I32[184], Numeric_BridgeMeasurements_cs.Values.I32[185]),
        ["BXP DrawBridge02 One-Way"] = (Numeric_BridgeMeasurements_cs.Values.I32[186], Numeric_BridgeMeasurements_cs.Values.I32[187]),
        ["BXP DrawBridge01 One-Way"] = (Numeric_BridgeMeasurements_cs.Values.I32[188], Numeric_BridgeMeasurements_cs.Values.I32[189]),
        ["BXP DrawBridgeAlley"] = (Numeric_BridgeMeasurements_cs.Values.I32[190], Numeric_BridgeMeasurements_cs.Values.I32[191]),
        ["BXP BikeDrawBridge02"] = (Numeric_BridgeMeasurements_cs.Values.I32[192], Numeric_BridgeMeasurements_cs.Values.I32[193]),
        ["BXP DrawBridge03 One-Way"] = (Numeric_BridgeMeasurements_cs.Values.I32[194], Numeric_BridgeMeasurements_cs.Values.I32[195]),
        ["BXP BikeDrawBridge01"] = (Numeric_BridgeMeasurements_cs.Values.I32[196], Numeric_BridgeMeasurements_cs.Values.I32[197]),
        ["LiftBridge01"] = (Lift01RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[198]),
        ["LiftBridge03"] = (Lift03RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[199]),
        ["LiftBridge02"] = (Lift02RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[200]),
        ["LiftBridge04"] = (Lift04RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[201]),
        ["LiftBridge05"] = (Lift05RoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[202]),
        ["LiftBridge03 Train Track"] = (Lift03AuxiliaryRoadWidth, Numeric_BridgeMeasurements_cs.Values.I32[203]),
        ["Arc Bike Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[204], Numeric_BridgeMeasurements_cs.Values.I32[205]),
        ["Arc Pedestrian Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[206], Numeric_BridgeMeasurements_cs.Values.I32[207]),
        ["BXP Arc Pedestrian Bridge Revised"] = (Numeric_BridgeMeasurements_cs.Values.I32[208], Numeric_BridgeMeasurements_cs.Values.I32[209]),
        ["BXP Covered Pedestrian Bridge Revised"] = (Numeric_BridgeMeasurements_cs.Values.I32[210], Numeric_BridgeMeasurements_cs.Values.I32[211]),
        ["BXP Truss Viaduct - Highway Twoway - 2 lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[212], Numeric_BridgeMeasurements_cs.Values.I32[213]),
        ["BXP Truss Viaduct - Train"] = (Numeric_BridgeMeasurements_cs.Values.I32[214], Numeric_BridgeMeasurements_cs.Values.I32[215]),
        ["BXP Truss Viaduct - Highway Oneway - 3 lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[216], Numeric_BridgeMeasurements_cs.Values.I32[217]),
        ["BXP Truss Viaduct - Subway"] = (Numeric_BridgeMeasurements_cs.Values.I32[218], Numeric_BridgeMeasurements_cs.Values.I32[219]),
        ["Covered Bike Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[220], Numeric_BridgeMeasurements_cs.Values.I32[221]),
        ["Covered Pedestrian Bridge"] = (Numeric_BridgeMeasurements_cs.Values.I32[222], Numeric_BridgeMeasurements_cs.Values.I32[223]),
        ["Ferry Pier"] = (Numeric_BridgeMeasurements_cs.Values.I32[224], Numeric_BridgeMeasurements_cs.Values.I32[225]),
        ["Fishing Pier"] = (Numeric_BridgeMeasurements_cs.Values.I32[226], Numeric_BridgeMeasurements_cs.Values.I32[227]),
        ["Hydroelectric_Power_Plant_01 Dam"] = (Numeric_BridgeMeasurements_cs.Values.I32[228], Numeric_BridgeMeasurements_cs.Values.I32[229]),
        ["LeisurePier01Large01"] = (Numeric_BridgeMeasurements_cs.Values.I32[230], Numeric_BridgeMeasurements_cs.Values.I32[231]),
        ["LeisurePier01Medium01"] = (Numeric_BridgeMeasurements_cs.Values.I32[232], Numeric_BridgeMeasurements_cs.Values.I32[233]),
        ["LeisurePier01Small01"] = (Numeric_BridgeMeasurements_cs.Values.I32[234], Numeric_BridgeMeasurements_cs.Values.I32[235]),
        ["LeisurePier02Large01"] = (Numeric_BridgeMeasurements_cs.Values.I32[236], Numeric_BridgeMeasurements_cs.Values.I32[237]),
        ["LeisurePier02Medium01"] = (Numeric_BridgeMeasurements_cs.Values.I32[238], Numeric_BridgeMeasurements_cs.Values.I32[239]),
        ["LeisurePier02Small01"] = (Numeric_BridgeMeasurements_cs.Values.I32[240], Numeric_BridgeMeasurements_cs.Values.I32[241]),
        ["LeisurePier03Large01"] = (Numeric_BridgeMeasurements_cs.Values.I32[242], Numeric_BridgeMeasurements_cs.Values.I32[243]),
        ["LeisurePier03Medium01"] = (Numeric_BridgeMeasurements_cs.Values.I32[244], Numeric_BridgeMeasurements_cs.Values.I32[245]),
        ["LeisurePier03Small01"] = (Numeric_BridgeMeasurements_cs.Values.I32[246], Numeric_BridgeMeasurements_cs.Values.I32[247]),
        ["LeisurePier04Large01"] = (Numeric_BridgeMeasurements_cs.Values.I32[248], Numeric_BridgeMeasurements_cs.Values.I32[249]),
        ["LeisurePier04Medium01"] = (Numeric_BridgeMeasurements_cs.Values.I32[250], Numeric_BridgeMeasurements_cs.Values.I32[251]),
        ["LeisurePier04Small01"] = (Numeric_BridgeMeasurements_cs.Values.I32[252], Numeric_BridgeMeasurements_cs.Values.I32[253]),
        ["Oil Pier"] = (Numeric_BridgeMeasurements_cs.Values.I32[254], Numeric_BridgeMeasurements_cs.Values.I32[255]),
        ["Wooden Covered Bridge - 2 lanes"] = (Numeric_BridgeMeasurements_cs.Values.I32[256], Numeric_BridgeMeasurements_cs.Values.I32[257]),
    };
    /// <summary>The recorded widths, or false when this prefab was not in the scan.</summary>
    internal static bool TryGet(string? name, out float road, out float tower)
    {
        road = Numeric_BridgeMeasurements_cs.Values.F32[0];
        tower = Numeric_BridgeMeasurements_cs.Values.F32[1];
        if (string.IsNullOrEmpty(name) || !Table.TryGetValue(name!, out var entry))
            return false;
        road = entry.Road;
        tower = entry.Tower;
        return true;
    }

    internal static int Count => Table.Count;
    /// <summary>
    /// Every measurement, for the tests to hold the tower table against.
    ///
    /// The two tables were recorded independently - this one per bridge, the tower one per tower - so
    /// each is a check on the other. A tower road width that matches no bridge in here came from
    /// somewhere other than the game.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, (int Road, int Tower)>> All => Table;
}

// Embedded numerical arrays: BridgeMeasurements_cs
internal static class Numeric_BridgeMeasurements_cs
{
    static Numeric_BridgeMeasurements_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("BridgeMeasurements_cs");
}
