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
    /// <summary>Road width and tower width in whole metres, keyed by prefab name.</summary>
    private static readonly Dictionary<string, (int Road, int Tower)> Table =
        new(StringComparer.Ordinal)
        {
            ["PedestrianDrawBridge01"] = (27, 10),
            ["PedestrianDrawBridge02"] = (27, 16),
            ["PedestrianBridgeCoveredWood01"] = (18, 10),
            ["BXP PedestrianBridgeCoveredWood01 Revised"] = (18, 10),
            ["BXP Golden Gate Bridge Subway Track"] = (14, 0),
            ["BXP Golden Gate Bridge Train Track"] = (26, 0),
            ["Suspension Bridge - Highway Oneway - 2 Lanes"] = (14, 22),
            ["Suspension Bridge - Highway Oneway - 3 lanes"] = (18, 26),
            ["Suspension Bridge - Highway Oneway - 4 Lanes"] = (22, 30),
            ["Suspension Bridge - Highway Oneway - 5 Lanes"] = (26, 34),
            ["SuspensionBridge01"] = (28, 38),
            ["SuspensionBridge02"] = (34, 49),
            ["SuspensionBridge03"] = (52, 50),
            ["SuspensionBridge04"] = (52, 50),
            ["BXP Double Deck Suspension Bridge - Highway"] = (26, 38),
            ["BXP Suspension Bridge - Highway Twoway - 4 Lanes"] = (30, 30),
            ["BXP SuspensionBridge03 - Six Lane Highway"] = (29, 50),
            ["BXP SuspensionBridge02 Above Road Four-Lane"] = (24, 0),
            ["BXP Double Deck Suspension Bridge - Subway Track"] = (14, 0),
            ["BXP Suspension Bridge - Train - 4 Tracks"] = (24, 30),
            ["BXP Double Deck Suspension Bridge - Lower Highway"] = (24, 0),
            ["BXP Double Deck Suspension Bridge - Train"] = (38, 37),
            ["BXP Double Deck Suspension Bridge - Train Track"] = (14, 0),
            ["BXP Suspension Bridge - Highway Twoway - 6 Lanes"] = (38, 34),
            ["BXP Suspension Bridge - Train - 2 Tracks"] = (16, 22),
            ["BXP Suspension Bridge - Highway Oneway - 6 Lanes"] = (30, 34),
            ["BXP SuspensionBridge02 - Four-Lane"] = (44, 49),
            ["BXP Double Deck Suspension Bridge - Subway"] = (38, 37),
            ["SuspensionBridge02 Above Road"] = (18, 0),
            ["Extradosed Bridge - Large Road Divided - 6 Lanes"] = (63, 21),
            ["ExtradosedBridge01"] = (42, 53),
            ["ExtradosedBridge02"] = (40, 46),
            ["ExtradosedBridge03"] = (40, 56),
            ["ExtradosedBridge04"] = (40, 56),
            ["BXP ExtradosedBridge01 - PedsBikes"] = (42, 53),
            ["BXP ExtradosedBridge01 - Tram Track"] = (22, 0),
            ["BXP ExtradosedBridge01 - Public Transport"] = (42, 53),
            ["BXP Extradosed Bridge - Train - 2 Tracks"] = (16, 21),
            ["BXP ExtradosedBridge01 - PedsBikes Only"] = (9, 0),
            ["BXP ExtradosedBridge03 - Highway"] = (42, 56),
            ["BXP ExtradosedBridge02 - Train Track"] = (14, 0),
            ["BXP ExtradosedBridge01 - Tram"] = (42, 53),
            ["BXP ExtradosedBridge02 - Tram"] = (40, 46),
            ["BXP Extradosed Bridge - Subway - 2 Tracks"] = (16, 21),
            ["BXP ExtradosedBridge01 - Subway Track"] = (26, 0),
            ["BXP ExtradosedBridge01 - Public Transport Road"] = (12, 0),
            ["BXP Extradosed Bridge - Highway Twoway - 6 Lanes"] = (33, 32),
            ["BXP Extradosed Bridge - Highway Twoway - 4 Lanes"] = (25, 24),
            ["BXP ExtradosedBridge02 - Subway Track"] = (30, 0),
            ["BXP ExtradosedBridge02 - Tram Track"] = (15, 0),
            ["BXP ExtradosedBridge01 - Subway"] = (42, 53),
            ["BXP Extradosed Bridge - Medium Road Divided - 4 Lanes"] = (57, 21),
            ["BXP ExtradosedBridge02 - Subway"] = (40, 46),
            ["BXP Extradosed Bridge - Tram - 2 Tracks"] = (12, 21),
            ["BXP ExtradosedBridge02 - Train"] = (40, 46),
            ["ExtradosedBridge01 Train Track"] = (26, 0),
            ["ExtradosedBridge02 Above Road"] = (28, 0),
            ["Cable-stayed Bridge - XL Road Divided - 8 Lanes"] = (77, 42),
            ["Cable Stayed Pedestrian Bridge"] = (4, 6),
            ["Cable Stayed Bike Bridge"] = (4, 6),
            ["BXP Cable Stayed Pedestrian Bridge Revised"] = (4, 6),
            ["Truss Arch Bridge - Small Road - 2 Lanes"] = (18, 15),
            ["Truss Arch Bridge - Highway Twoway - 2 Lanes"] = (14, 15),
            ["TrussArchBridge01"] = (22, 18),
            ["TrussArchBridge02"] = (40, 30),
            ["TrussArchBridge03"] = (26, 18),
            ["BXP Truss Arch Bridge - Highway Oneway - 3 Lanes"] = (18, 15),
            ["BXP TrussArchBridge03 - Tram"] = (22, 18),
            ["BXP TrussArchBridge03 - Train"] = (26, 18),
            ["BXP Truss Arch Bridge - Large Road - 6 Lanes"] = (30, 27),
            ["BXP Truss Arch Bridge - Tram"] = (12, 15),
            ["BXP TrussArchBridge01 - Highway Twoway - 2 Lanes"] = (14, 18),
            ["BXP TrussArchBridge01 - Subway"] = (22, 18),
            ["BXP TrussArchBridge01 - Tram"] = (18, 18),
            ["BXP Truss Arch Bridge - Medium Road - 4 Lanes"] = (24, 22),
            ["BXP TrussArchBridge01 - Highway Twoway - 6 Lanes"] = (30, 32),
            ["BXP TrussArchBridge03 - Highway"] = (24, 18),
            ["BXP Truss Arch Bridge - Subway"] = (14, 15),
            ["BXP Truss Arch Bridge - Highway Oneway - 2 Lanes"] = (14, 15),
            ["Tied Arch Bridge - 4 lanes"] = (21, 23),
            ["BXP Tied Arch Bridge - Train - 4 Tracks"] = (24, 23),
            ["BXP Tied Arch Bridge - Highway Oneway - 4 Lanes"] = (22, 23),
            ["BXP Tied Arch Bridge - Train - 4 Tracks Alternating"] = (24, 23),
            ["BXP Tied Arch Bridge - Highway Twoway - 4 Lanes"] = (22, 23),
            ["Grand Bridge"] = (21, 44),
            ["BXP Grand Bridge Viaduct"] = (33, 34),
            ["BXP Grand Bridge - Center Tram"] = (33, 44),
            ["DrawBridge03"] = (78, 57),
            ["DrawBridge02"] = (61, 37),
            ["DrawBridge01"] = (42, 19),
            ["BXP DrawBridge02 One-Way"] = (61, 37),
            ["BXP DrawBridge01 One-Way"] = (42, 19),
            ["BXP DrawBridgeAlley"] = (30, 16),
            ["BXP BikeDrawBridge02"] = (27, 16),
            ["BXP DrawBridge03 One-Way"] = (78, 57),
            ["BXP BikeDrawBridge01"] = (27, 10),
            ["LiftBridge01"] = (26, 41),
            ["LiftBridge03"] = (62, 93),
            ["LiftBridge02"] = (32, 42),
            ["LiftBridge04"] = (42, 24),
            ["LiftBridge05"] = (30, 26),
            ["LiftBridge03 Train Track"] = (50, 0),
            ["Arc Bike Bridge"] = (4, 6),
            ["Arc Pedestrian Bridge"] = (4, 6),
            ["BXP Arc Pedestrian Bridge Revised"] = (4, 6),
            ["BXP Covered Pedestrian Bridge Revised"] = (4, 7),
            ["BXP Truss Viaduct - Highway Twoway - 2 lanes"] = (14, 18),
            ["BXP Truss Viaduct - Train"] = (14, 18),
            ["BXP Truss Viaduct - Highway Oneway - 3 lanes"] = (15, 18),
            ["BXP Truss Viaduct - Subway"] = (14, 18),
            ["Covered Bike Bridge"] = (4, 7),
            ["Covered Pedestrian Bridge"] = (4, 7),
            ["Ferry Pier"] = (20, 10),
            ["Fishing Pier"] = (20, 10),
            ["Hydroelectric_Power_Plant_01 Dam"] = (17, 63),
            ["LeisurePier01Large01"] = (10, 10),
            ["LeisurePier01Medium01"] = (8, 9),
            ["LeisurePier01Small01"] = (4, 4),
            ["LeisurePier02Large01"] = (10, 10),
            ["LeisurePier02Medium01"] = (8, 8),
            ["LeisurePier02Small01"] = (4, 4),
            ["LeisurePier03Large01"] = (10, 10),
            ["LeisurePier03Medium01"] = (8, 8),
            ["LeisurePier03Small01"] = (4, 4),
            ["LeisurePier04Large01"] = (15, 15),
            ["LeisurePier04Medium01"] = (11, 11),
            ["LeisurePier04Small01"] = (7, 7),
            ["Oil Pier"] = (22, 93),
            ["Wooden Covered Bridge - 2 lanes"] = (10, 10),
        };

    /// <summary>The recorded widths, or false when this prefab was not in the scan.</summary>
    internal static bool TryGet(string? name, out float road, out float tower)
    {
        road = 0f;
        tower = 0f;
        if (string.IsNullOrEmpty(name) || !Table.TryGetValue(name!, out var entry)) return false;
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
