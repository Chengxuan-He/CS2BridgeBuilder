using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;



using CS2Mods.Shared.Conversion;

using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;



using Game.Prefabs;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Unity.Entities;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private void ExportOne()
    {
        var setting = Mod.Setting;
        if ((_gameMode & GameMode.Editor) == 0
            && (_gameMode & GameMode.Game) != 0
            && !(setting?.AllowGameplayExport ?? false))
        {
            RoadSelectionModel.PublishMessage(this, UiStringCatalog.Current.StateGameplayBlocked);
            return;
        }
        // Both entry points allocate a UUID and publish through the same creation path.
        CreateRuntimeBridge(new BridgeRuntimeRequest
        {
            UpperDeckId = setting?.UpperDeckId ?? string.Empty,
            LowerDeckId = setting?.LowerDeckId ?? string.Empty,
            StyleId = BridgeStyleCatalog.Resolve(setting?.BridgeStyleId)?.Id ?? string.Empty,
            DisplayName = setting?.BridgeName ?? string.Empty,
            LowerDeckOpposite = setting?.LowerDeckOpposite ?? true,
        }, setting?.ToBridgeOptions());
    }
    /// <summary>
    /// The one bridge construction path used by both the legacy options page and the runtime UI.
    /// A true result means that publication has been queued (or a private preview was built).
    /// Permanent assetInfo and activation happen in onPublished, after native initialization.
    /// </summary>
    private bool TryBuildBridge(
        Deck upper,
        Deck? chosen,
        BridgeStyle style,
        string exportName,
        BridgeOptions options,
        bool overwrite,
        ExportReport report,
        BridgePreviewSession? preview = null,
        Action<bool>? onPublished = null)
    {
        var unsupported = BridgeStyleDefinitions.GenerationUnsupportedReason(style.Id, options.DoubleDeck);
        if (unsupported != null)
        {
            report.Failed(exportName, new NotSupportedException(
                $"'{style.Id}' cannot be generated: {unsupported}."));
            return false;
        }
        var failuresBefore = report.FailedRoads;
        var cloner = new PrefabGraphCloner(_prefabSystem, _settings, report, overwrite, exportName);
        var towers = new TowerFactory(_prefabSystem, report, preview?.Geometry);
        var composer = BridgeGeneratorRouter.Create(style.Id, report, towers);
        if (composer == null) return false;
        var doubleDeck = new DoubleDeckComposer(report);

        try
        {
            // Which of the two decks the bridge is built on.
            //
            // An archetype states where its second net runs, and the two arrangements both exist in
            // the game: the V pylon hangs a train track ten metres below its road, and the plain A
            // pylon carries a second carriageway ten metres above it - "ExtradosedBridge02 Above
            // Road", which is the archetype saying so in its own name. Where the second net is above,
            // the archetype's own main net is the lower of the two decks, so the deck the player chose
            // goes in the main slot and the road they are converting is hung above it. That is the
            // archetype's arrangement rather than a correction to it, and the towers sit where they
            // were drawn without anything being moved.
            //
            // "Main" is an AuxiliaryNets ownership role, not a synonym for road. A track can be that
            // main network: it owns the bridge components and carries the road as its auxiliary. The
            // earlier RoadPrefab-only guard rejected the A pylon before its references could be
            // swapped, which is why a train lower deck could not be exported at all.
            // Selection is asked here as well as inside the composer. It is pure and both calls give
            // it the same width, measured from the same sections - the source road carries the
            // sections its clone will - and the same constraint, so both arrive at the same variant.
            // It has to be asked before either deck is cloned, because the answer decides which one is
            // cloned under the export name. Variants of one style disagree about this: the plain A
            // pylon hangs its second net above and its subway, train and tram variants hang theirs
            // below, so the question is about the variant and never about the style.
            var chosenWidth = BridgeGeneratorBase.WidthOf(upper.Prefab, upper.Width);
            if (!(chosenWidth > 0f) || float.IsInfinity(chosenWidth))
            {
                report.Failed(exportName, new InvalidOperationException(
                    "The selected road's initialized width is unavailable. Generation was stopped before cloning."));
                return false;
            }
            var stated = options.DoubleDeck
                ? style.Select(chosenWidth, upper.IsRoad, doubleDeck: true).Variant?.LowerDeck
                : null;
            var arrangement = DeckArrangement.For(stated?.m_Position.y ?? 0f);
            var secondNetAbove = stated != null && arrangement.MainIsChosenDeck;

            if (options.DoubleDeck && chosen == null)
            {
                report.Failed(exportName, new InvalidOperationException(
                    $"The selected second deck '{options.LowerDeckId}' is not registered any more."));
                return false;
            }

            var main = upper;
            Deck? auxiliary = chosen;
            if (options.DoubleDeck && chosen != null)
            {
                var roles = arrangement.Arrange(upper, chosen);
                main = roles.Main;
                auxiliary = roles.Auxiliary;
            }

            var clone = CloneDeck(cloner, main, exportName, report);
            if (clone == null) return false;

            // Size from the archetype's root ownership role. For Suspension and ExtradosedBridge01
            // that is the converted upper road; for ExtradosedBridge02 the auxiliary is above, so
            // the chosen lower road/track is the root and supplies the width reference.
            // Grey double suspension owns the lower network, but its structure follows the upper
            // road. Network ownership must not choose the structural width measurement.
            var structuralDeck = options.DoubleDeck && composer.StructureFollowsUpperAuxiliary ? upper : main;
            var variant = composer.Apply(
                clone, style, structuralDeck.Width, options, measure: structuralDeck.Prefab);
            if (variant == null) return false;

            ApplyPrototypeIcon(clone, variant, report);
            AttachSecondDeck(
                clone, auxiliary, secondNetAbove, exportName, cloner, doubleDeck, options,
                style.Id, variant, report);
            // Extra auxiliary networks inherited from a road may still be shared native assets.
            // Never edit their unlock components as if the bridge owned them.
            var ownedNets = new HashSet<PrefabBase>(cloner.Nodes.Where(node => node.NeedsSave)
                .Select(node => node.Target));
            if ((clone.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
                .Any(entry => entry?.m_Prefab == null || !ownedNets.Contains(entry.m_Prefab)))
            {
                report.Failed(exportName, new InvalidOperationException(
                    "The selected network contains an auxiliary prefab not owned by this bridge."));
                return false;
            }
            var nativeAssets = new List<PrefabBase>();
            if (preview == null && !BridgeUnlockPolicy.Apply(clone, variant, report, nativeAssets)) return false;
            // Preserve native piece/object fees from the selected networks and bridge prototype.
            // Native initialization and construction calculate the resulting cost, including auxiliary decks.
            if (preview == null)
                report.Note($"{exportName}: native pricing; original piece/object construction, elevation and upkeep fees retained; no manager price adjustment.");
            DescribeResult(clone, exportName, report);

            var nodes = cloner.Nodes
                .Concat(nativeAssets.Select(p => new PrefabCloneNode(p, p, false, true, null)))
                .Concat(towers.Created.Select(prefab =>
                    new PrefabCloneNode(prefab, prefab, false, true, null)))
                .ToList();
            // A copy is not a replacement for its donor. Native PrefabID already uses
            // Target.name (including the bridge UUID) and the copy's asset GUID.
            // Do not publish inherited donor aliases for any private node in this graph.
            foreach (var node in nodes.Where(node => node.NeedsSave))
            {
                node.Target.Remove<ObsoleteIdentifiers>();
                node.Target.version = 1;
            }
            if (preview != null)
            {
                // Preview and permanent generation share composition, not identity
                // or publication. A later Create request always runs this method anew.
                if (report.FailedRoads != failuresBefore) return false;
                preview.SetResult(clone, variant);
                return true;
            }
            if (report.FailedRoads != failuresBefore) return false;
            var pack = BridgeAssetPack.Ensure(_prefabSystem);
            if (pack == null)
            {
                report.Failed(exportName, new InvalidOperationException("BridgeBuilder asset pack is unavailable."));
                return false;
            }
            foreach (var node in nodes.Where(node => node.NeedsSave))
                if (node.Target is NetGeometryPrefab network) BridgeAssetPack.Assign(network, pack);
            if (BridgeAssetInfo.IsPrefabName(clone.name))
                BridgeAssetCatalog.Attach(clone, new BridgeAssetInfo(clone.name,
                    BridgeNaming.BaseName(upper, chosen, style), upper.Id, options.DoubleDeck ? chosen?.Id : null,
                    style.Id, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
            var unowned = nodes.Where(node => node.NeedsSave && !BridgeAssetInfo.MatchesOwner(node.Target.name, clone.name))
                .Select(node => $"{node.Target.name} ({node.Target.GetType().Name})").ToArray();
            if (unowned.Length != 0)
            {
                report.Failed(exportName, new InvalidOperationException(
                    $"Bridge persistence refused assets missing owner '{clone.name}': {string.Join(", ", unowned)}"));
                return false;
            }
            foreach (var node in nodes.Where(n => n.NeedsSave))
            {
                node.Target.Remove<BridgeConstructionCost>();
                BridgeNativePresentation.Prepare(node.Target);
            }
            report.SavedDependencies = new PrefabAssetWriter().Save(nodes);
            if (!BridgeDependencyPersistence.Save(clone.name,
                nodes.Where(node => node.NeedsSave && node.Target.asset != null)
                    .Select(node => node.Target.asset.id.guid.ToString()), out var copied, out var copyError))
            {
                report.Failed(exportName, new IOException("External dependency persistence failed: " + copyError));
                return false;
            }
            report.SavedDependencies += copied;
            if (!BridgeNativePresentation.Validate(clone.name, nodes.Where(n => n.NeedsSave).Select(n => n.Target), out var independenceError))
            {
                report.Failed(exportName, new InvalidOperationException(independenceError));
                return false;
            }
            Mod.Log.Info($"Bridge '{clone.name}' persisted {copied} external dependency copies with original CIDs.");
            return World.GetOrCreateSystemManaged<BridgePublicationSystem>().Publish(nodes, report, ready =>
            {
                if (ready) report.Exported(exportName);
                onPublished?.Invoke(ready);
            });
        }
        catch (Exception exception)
        {
            report.Failed(exportName, exception);
            return false;
        }
        finally
        {
            // Capture even a partial graph so failed previews can be completely
            // discarded without touching any shared archetype or source road.
            preview?.Adopt(cloner.Nodes, towers.Created);
        }
    }

    /// <summary>
    /// Clones the chosen deck into a standalone prefab. A Road Builder road still contributes its
    /// authored speed limit, but never its road thumbnail: once composition chooses an archetype, the
    /// generated asset receives that bridge prototype's icon.
    /// </summary>
    private NetGeometryPrefab? CloneDeck(
        PrefabGraphCloner cloner, Deck deck, string exportName, ExportReport report)
    {
        if (deck.Prefab is RoadPrefab roadSource)
        {
            var road = cloner.CloneRoad(roadSource, exportName, string.Empty);
            if (!IsPrivateDeck(roadSource, road, report)) return null;
            if (deck.Road != null) SpeedLimitFix.Apply(road, deck.Road, report);
            return road;
        }

        if (deck.Prefab is NetPrefab netSource)
        {
            var clone = (NetGeometryPrefab)cloner.CloneNet(netSource, exportName);
            return IsPrivateDeck(netSource, clone, report) ? clone : null;
        }

        report.Defect(
            $"'{deck.DisplayName}' is not a network prefab and cannot own a double-deck bridge. "
            + "The current bridge export was stopped without publishing a partial prefab.");
        return null;
    }

    private static bool IsPrivateDeck(NetPrefab source, NetPrefab clone, ExportReport report)
    {
        if (!ReferenceEquals(source, clone)) return true;
        report.Defect($"Network '{source.name}' could not be cloned into an owned native prefab. " +
            "Generation stopped before modifying the shared source network.");
        return false;
    }

    /// <summary>
    /// The generated bridge is presented as the selected bridge design, not as another copy of its
    /// carried road. The icon URI remains owned by the archetype's content; catalogue selection has
    /// already rejected that donor when its DLC or mod prerequisite is unavailable.
    /// </summary>
    private static void ApplyPrototypeIcon(
        NetGeometryPrefab target, BridgeStyleVariant variant, ExportReport report)
    {
        var targetUi = target.components.OfType<UIObject>().FirstOrDefault();
        if (targetUi == null)
        {
            report.Warning(
                $"'{target.name}' has no UIObject, so the icon from bridge prototype "
                + $"'{variant.Name}' could not be assigned.");
            return;
        }

        var prototypeUi = variant.Donor.components.OfType<UIObject>().FirstOrDefault();
        targetUi.m_Icon = prototypeUi?.m_Icon ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetUi.m_Icon))
            report.Warning($"Bridge prototype '{variant.Name}' does not expose a UI icon.");
    }

    /// <summary>
    /// Resolves the auxiliary network and attaches it on the side declared by the archetype.
    ///
    /// An already registered net - a track, or another road the player picked - is referenced as it
    /// is. The one case that needs work is picking the same net for both decks: pointing the bridge at
    /// itself would make a prefab that is its own auxiliary, so it is cloned a second time under its
    /// own name and stripped of the things a carried deck must not have.
    /// </summary>
    private void AttachSecondDeck(
        NetGeometryPrefab main,
        Deck? auxiliary,
        bool above,
        string exportName,
        PrefabGraphCloner cloner,
        DoubleDeckComposer doubleDeck,
        BridgeOptions options,
        string styleId,
        BridgeStyleVariant variant,
        ExportReport report)
    {
        if (!options.DoubleDeck || options.LowerDeckId == null) return;

        // The archetype the bridge was built from is the double deck version of its style, so it
        // already states where the second deck runs and which way. The composer refuses to build at
        // all when the style has no such version, so reaching here without one is a contradiction.
        var arrangement = variant.LowerDeck;
        if (arrangement == null)
        {
            report.Warning(
                $"'{exportName}' was exported without its second deck: '{variant.Name}' states no "
                + "arrangement for one.");
            return;
        }

        // A double-deck bridge's node-link rule belongs to the prototype component that owns the
        // auxiliary entry. ExtradosedBridge01 and the expansion pack's double-deck suspension bridge
        // both set this to true. Read it from the selected variant rather than recreating the same bit
        // from memory so the generated network follows the prototype at its nodes as well as in deck
        // position.
        var prototypeAuxiliary = variant.Donor.GetComponent<AuxiliaryNets>();
        var linkEndOffsets = prototypeAuxiliary?.m_LinkEndOffsets ?? false;
        if (prototypeAuxiliary == null)
        {
            report.Warning(
                $"'{exportName}' has a second-deck entry but its prototype has no AuxiliaryNets "
                + "component, so its node end offsets cannot be linked from the prototype.");
        }

        // The auxiliary is whichever pointer did not become the main network. Normally that is the
        // selected lower deck; for the A pylon the pointers were exchanged and it is the converted
        // upper road.
        var deck = auxiliary;
        if (deck == null)
        {
            report.Warning(
                $"'{exportName}' was exported without its second deck: the chosen net "
                + $"'{options.LowerDeckId}' is not registered any more.");
            return;
        }

        // Every auxiliary deck is cloned, whatever it is and whether or not it matches the main one.
        //
        // It has to be changed - an auxiliary carries no independent pillars, because the main
        // network owns the structure - and the thing it is changed from is a
        // registered prefab shared with everything else built from it. Taking the pillars off a track
        // in place would take them off every other track in the world, which is a worse fault than the
        // one being fixed, and that is why this used to be reported instead of fixed.
        //
        // The pack does the same: its double deck bridges name a separate auxiliary prefab carrying
        // no structure of its own, rather than pointing at the shared road or track.
        if (deck.Prefab is not NetGeometryPrefab source)
        {
            report.Warning($"'{exportName}': the auxiliary deck could not be built from '{deck.DisplayName}'.");
            return;
        }

        var auxiliaryName = BridgeNaming.CarriedDeckName(exportName, above);
        var auxiliaryClone = CloneDeck(cloner, deck, auxiliaryName, report);
        if (auxiliaryClone == null) return;

        if (arrangement.m_Prefab is not NetGeometryPrefab prototypeAuxiliaryDeck)
        {
            report.Defect(
                $"'{exportName}' was exported without its second deck: prototype '{variant.Name}' "
                + "does not reference a usable auxiliary network whose node seam behavior can be "
                + "copied.");
            return;
        }

        var pillars = DoubleDeckComposer.PrepareDeck(
            auxiliaryClone, main, prototypeAuxiliaryDeck);
        if (pillars > 0)
        {
            report.Note(
                $"{auxiliaryName}: {pillars} pillar(s) removed. The main network owns the bridge "
                + "structure, so an independent second set would conflict with it.");
        }

        var seamSource = DoubleDeckComposer.CopyCompatibleSeamBehavior(
            auxiliaryClone, prototypeAuxiliaryDeck, copyAggregate: true)
            ? $"copied from transport-compatible prototype '{prototypeAuxiliaryDeck.name}'"
            : $"preserved from selected {deck.Kind} deck because prototype "
                + $"'{prototypeAuxiliaryDeck.name}' carries another transport type";

        // ExtradosedBridge01's lower network belongs to the same named bridge as its root deck. Its
        // prototype auxiliary is a train track, and blindly retaining that aggregate makes the lower
        // road receive an ordinary street/road/track name. The style table records the exception; the
        // generated deck takes the already-copied aggregate from the main bridge, without inferring
        // anything from a generated name or from geometry.
        // Golden Gate's lower road also belongs to the named bridge. In particular, a road
        // below a rail carrier retains its own seam rules, but not its Highway/Street naming
        // aggregate. Reuse the main bridge's native aggregate and its localized bridge name.
        // This changes neither the private prefab identity nor the rail auxiliary behavior.
        if (BridgeStyleDefinitions.CarriedDeckUsesBridgeAggregate(styleId, auxiliaryClone is RoadPrefab)
            && main.m_AggregateType != null)
        {
            auxiliaryClone.m_AggregateType = main.m_AggregateType;
            seamSource += $"; aggregate copied from main bridge '{main.m_AggregateType.name}'";
        }

        report.Note(
            $"{auxiliaryName}: auxiliary seam behavior {seamSource} - "
            + $"{auxiliaryClone.m_EdgeStates?.Length ?? 0} edge rule(s), "
            + $"{auxiliaryClone.m_NodeStates?.Length ?? 0} node rule(s), aggregate "
            + $"'{auxiliaryClone.m_AggregateType?.name ?? "none"}'.");

        // Two post conditions, checked rather than assumed. Each is a fault nothing else reports: a
        // lower deck on pillars of its own runs them to the ground beside the structure already
        // holding it up, and a lower deck with no bridge behaviour is held to an ordinary road's edge
        // length and reports "distance too long" on every span of the bridge carrying it.
        if (auxiliaryClone.GetComponent<Bridge>() == null)
        {
            report.Defect(
                $"'{auxiliaryName}' carries no bridge behaviour, so its edges are held to an ordinary "
                + "network's length while its main bridge spans further.");
        }

        var left = DoubleDeckComposer.PillarsOn(auxiliaryClone);
        if (left > 0)
        {
            report.Defect(
                $"'{auxiliaryName}' still carries {left} pillar(s) after being prepared. The main "
                + "network already owns the structure for both decks.");
        }

        doubleDeck.Apply(
            main, auxiliaryClone, $"'{auxiliaryName}', from {deck.DisplayName}", arrangement,
            linkEndOffsets, options.LowerDeckOpposite);
    }


    /// <summary>
    /// Writes what the conversion produced into the report. For a bridge the counts that matter are
    /// the ones that separate "the deck came out wrong" from "the style did not attach": sections and
    /// components as before, plus how much structure ended up above and below the deck.
    /// </summary>
    private static void DescribeResult(NetGeometryPrefab clone, string name, ExportReport report)
    {
        try
        {
            var sections = clone.m_Sections?.Length ?? 0;
            var missingSections = clone.m_Sections?.Count(section => section.m_Section == null) ?? 0;
            var overhead = clone.GetComponent<OverheadNetSections>()?.m_Sections?.Length ?? 0;
            var subObjects = clone.GetComponent<NetSubObjects>()?.m_SubObjects?.Length ?? 0;
            var auxiliary = clone.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?.Length ?? 0;

            report.Note(
                $"{name}: {clone.components.Count} components, {sections} sections "
                + $"({missingSections} missing), {overhead} overhead section(s), {subObjects} sub object(s), "
                + $"{auxiliary} auxiliary net(s), speed "
                + (clone is RoadPrefab road ? road.m_SpeedLimit.ToString(CultureInfo.InvariantCulture) : "n/a"));
        }
        catch (Exception)
        {
            // Generation diagnostics are silent; retain the external API exception boundary.
        }
    }

}
