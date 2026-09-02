using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// Attaches a second net on the side of the main network declared by the bridge archetype.
///
/// The mechanism is the game's own <see cref="AuxiliaryNets"/>: a net prefab can name other nets that
/// are created alongside it at a fixed offset, and each entry says under what condition the extra net
/// is inverted - which is exactly the "the lower deck runs the other way" this feature calls for. The
/// Bridge Expansion Pack's own double deck bridges are built the same way. Most use a placeable upper
/// prefab pointing at a non-placeable lower one; the A pylon reverses those ownership roles and has a
/// lower main network pointing at its upper auxiliary road.
///
/// Marked experimental for a reason worth stating plainly: an auxiliary net is created and destroyed
/// with its carrier and cannot be selected on its own, so the auxiliary deck is placed with the bridge but
/// cannot be connected to a line the player builds separately.
/// </summary>
internal sealed class DoubleDeckComposer
{
    /// <summary>
    /// The separation is the archetype's and is not adjustable, so there is nothing to clamp.
    ///
    /// It used to be a setting bounded by these two numbers. Kept as a record of what was tried: a
    /// double deck bridge's structure is drawn around two decks at one separation, so every value in
    /// that range except the archetype's own put the second deck through geometry modelled to clear
    /// it. The bound that mattered was not four metres or twenty-four, it was the one the archetype
    /// already states.
    /// </summary>
    internal const float FormerMinimumSpacing = 4f;

    internal const float FormerMaximumSpacing = 24f;

    private readonly ExportReport _report;

    internal DoubleDeckComposer(ExportReport report)
    {
        _report = report;
    }

    /// <summary>
    /// Attaches <paramref name="auxiliary"/> alongside <paramref name="target"/>, by the archetype's own
    /// arrangement - below it, or above it where the archetype carries its second net above.
    ///
    /// Only the net changes. Where it sits and whether it runs the other way are
    /// <paramref name="archetype"/>'s, carried across whole, because a double deck bridge's towers,
    /// portals and cables are drawn around two decks at one particular separation and on one
    /// particular side.
    ///
    /// Both were decided here before. The separation was a setting, clamped between four and
    /// twenty-four metres, so a bridge could be built at any value but the one its structure was drawn
    /// for. And the offset was written as a negative y without reading the archetype at all, which put
    /// the two levels the wrong way round on every archetype that hangs its second net above.
    ///
    /// Reading it is not the whole answer either. An archetype that hangs its second net above has its
    /// own main net as the lower deck, so which of the two decks the bridge is built on is decided
    /// before either is cloned and the offset here needs no adjustment at all - see
    /// <see cref="DeckArrangement"/>.
    /// </summary>
    internal void Apply(
        NetPrefab target, NetPrefab auxiliary, string description, AuxiliaryNetInfo archetype,
        bool linkEndOffsets, bool opposite)
    {
        // Which way up the archetype hangs its second net, and what has to happen when it hangs it
        // above.
        //
        // The two arrangements exist in the game: the V pylon carries its train track ten metres below
        // the road, and the A pylon carries a second carriageway ten metres above it - the prefab is
        // called "ExtradosedBridge02 Above Road", which is the archetype saying so in its own name.
        // The road being converted is the one the player is looking at, and the deck they chose is the
        // second one, so on an archetype whose second net is above, the road takes the upper role and
        // the chosen deck takes the lower.
        //
        // That is not just a sign change on the offset. The archetype drew its towers around its own
        // lower deck, and here the road is the upper one, so the structure has to come down by the
        // separation to stand in the same relation to the two decks it did before. Flipping the offset
        // alone would leave the towers rising from the deck that is now on top, with the second deck
        // hanging ten metres below anything drawn to carry it.
        // Used as it stands. Which deck is in the main slot was decided before either was cloned, so
        // by the time this runs the two are in the archetype's own roles and its offset is correct
        // for them without adjustment.
        var arrangement = DeckArrangement.For(archetype.m_Position.y);

        var nets = target.AddOrGetComponent<AuxiliaryNets>();
        var existing = nets.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>();

        var entry = new AuxiliaryNetInfo
        {
            m_Prefab = auxiliary,
            m_Position = archetype.m_Position,
            // The archetype's separation, the player's direction. One is geometry and the other is
            // traffic: the structure is drawn around two decks at one distance and any other distance
            // puts one deck through the other, while nothing about it is drawn differently for a lower
            // deck running the opposite way.
            m_InvertWhen = opposite ? NetInvertMode.Always : NetInvertMode.Never,
        };

        nets.m_AuxiliaryNets = existing.Concat(new[] { entry }).ToArray();
        // This is copied from the selected bridge prototype, not inferred from "double deck". Both
        // ExtradosedBridge01 and the expansion pack's double-deck suspension bridge set it: the two
        // decks are one structure, so their node end offsets must move together. Carrying the bit from
        // the prototype also keeps a future double-deck archetype's deliberately different connection
        // rule intact.
        nets.m_LinkEndOffsets = linkEndOffsets;
        nets.active = true;

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: second deck = {1}, {2:0.#} m {3} it, {4}; node end offsets {5}. {6}",
            target.name,
            description,
            Math.Abs(arrangement.Offset),
            arrangement.CarriedIsAbove ? "above" : "below",
            opposite ? "running the opposite way" : "running the same way",
            linkEndOffsets ? "linked as the bridge prototype specifies" : "left independent",
            arrangement.SecondNetAbove
                ? "The archetype carries its second net above, so the bridge is built on the deck "
                    + "chosen and the converted road is the one carried."
                : "The archetype's own arrangement."));
        _report.Warning(
            $"'{target.name}' was exported with the experimental double deck option. The second deck is "
            + "created with the bridge and cannot be edited or connected on its own.");
    }

    /// <summary>
    /// Turns a clone of a net into an auxiliary: no toolbar entry, no
    /// zoning, no auxiliary nets of its own, and no pillars. Returns how many pillars were taken off,
    /// so the report can say.
    ///
    /// That last one is not tidiness. A prefab that carried the double deck component while also being
    /// its auxiliary would name itself, and nothing good is on the other side of that. Building a
    /// separate auxiliary prefab instead of pointing the bridge at itself is also what the reference
    /// pack does, and it is why this needs a second clone rather than one reference.
    /// </summary>


    internal static int PrepareDeck(NetPrefab deck, NetPrefab carrier)
    {
        deck.components.RemoveAll(component => component is UIObject or AuxiliaryNets);
        if (deck is RoadPrefab road) road.m_ZoneBlock = null;

        // PlaceableNet and ServiceObject are intentionally retained. The reference auxiliary nets on
        // ExtradosedBridge01 and on the double-deck suspension bridge both carry them. UIObject is what
        // controls whether this private clone appears in the build menu; PlaceableNet is still part of
        // the network's placement/node initialization, and ServiceObject supplies its service context.
        // Removing all three hid the clone, but also made it structurally unlike either working
        // reference and left the carried network's nodes detached.

        // The bridge behaviour of the main network.
        //
        // The two decks are one structure - m_LinkEndOffsets ties their ends together - so they have
        // to agree on how long a span may be. A clone of an ordinary road carries no Bridge component
        // at all, and without one its edges are held to an ordinary road's length: hung under a bridge
        // spanning 256 m, every segment of it reported "distance too long". The pack's own lower deck
        // carries a Bridge of its own, at 320 m.
        //
        // Taken from the carrier rather than recorded, for the reason rule 2 gives: the main network
        // is in hand, it is what this one has to match, and it already holds whatever its own style
        // said a bridge of this kind spans.
        var above = carrier.GetComponent<Bridge>();
        if (above != null) deck.AddComponentFrom(above);


        // And no pillars of its own.
        //
        // The auxiliary belongs to the main network's bridge; what holds the pair up is the structure
        // the main already carries. Left with its own pillars it grows a second set beside the first,
        // through whatever the bridge's own portals were drawn to clear.
        //
        // The archetype says the same thing: the pack's own lower deck carries one object, an outside
        // connection marker, and no structure at all.
        var subObjects = deck.GetComponent<NetSubObjects>();
        var entries = subObjects?.m_SubObjects;
        if (entries == null) return 0;

        var kept = entries.Where(info => !IsPillar(info)).ToArray();
        subObjects!.m_SubObjects = kept;
        return entries.Length - kept.Length;
    }

    /// <summary>
    /// Whether a sub object entry stands the net up: a pillar, a pier, a tower.
    ///
    /// Asked of the object rather than its name, because <c>PillarObject</c> is what makes the game
    /// place a thing under a raised net and a name is what somebody typed. A placeholder carries the
    /// component too, so the test reaches both halves of a swapped pair.
    /// </summary>

    /// <summary>How many of a net's sub objects stand it up. Counted rather than removed, for a prefab
    /// that is shared: see the caller.</summary>
    internal static int PillarsOn(NetPrefab? net)
    {
        var entries = net?.GetComponent<NetSubObjects>()?.m_SubObjects;
        return entries?.Count(IsPillar) ?? 0;
    }

    private static bool IsPillar(NetSubObjectInfo? info) =>
        info?.m_Object != null && info.m_Object.Has<PillarObject>();
}
