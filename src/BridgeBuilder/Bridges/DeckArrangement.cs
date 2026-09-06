namespace BridgeBuilder.Bridges;

/// <summary>
/// Which of a double deck bridge's two decks the bridge is built on, given the way its archetype
/// hangs its own second net.
///
/// The archetypes do not agree between themselves, and variants of one style do not agree either. The
/// V pylon hangs a train track ten metres below its road. The plain A pylon carries a second
/// carriageway ten metres above it - its second net is the prefab "ExtradosedBridge02 Above Road",
/// which is the archetype saying so in its own name - while that same style's subway, train and tram
/// variants hang theirs below. So this is a question about the variant that was selected, never about
/// the style.
///
/// Where the second net is above, the archetype's own main net is the lower of its two decks. The deck
/// the player chose goes in the main slot and the road they are converting is hung above it, which is
/// the archetype's arrangement rather than a correction applied to it: the towers are drawn around the
/// main net, so they sit where they were drawn and nothing has to be moved to meet them.
///
/// Turning the offset over instead was tried and is worse. It puts the chosen deck below, which is
/// what the player asked for, but leaves the towers rising from the deck that is now on top - so the
/// structure has to be dropped by the separation to compensate, and a bridge assembled out of two
/// corrections is one nobody can check against its archetype.
/// </summary>
internal readonly struct DeckArrangement
{
    private DeckArrangement(float offset)
    {
        Offset = offset;
    }

    /// <summary>Where the archetype puts its second net, used as it stands.</summary>
    internal float Offset { get; }

    /// <summary>Whether the archetype carries its second net above its main one.</summary>
    internal bool SecondNetAbove => Offset > 0f;

    /// <summary>
    /// Whether the bridge is built on the deck the player chose rather than the road they converted.
    /// </summary>
    internal bool MainIsChosenDeck => SecondNetAbove;

    /// <summary>Whether the deck carried alongside sits above the one the bridge is built on.</summary>
    internal bool CarriedIsAbove => SecondNetAbove;

    /// <summary>
    /// Assigns the two selected prefab references to the slots declared by the archetype.
    ///
    /// For an A pylon the auxiliary net is above, so this deliberately swaps the references: the
    /// player's lower selection becomes the main network and their upper road becomes the auxiliary
    /// network. Keeping this as one operation prevents callers from swapping only the clone source
    /// while still passing the old upper/lower references to the composer or asset writer.
    /// </summary>
    internal DeckRoles<T> Arrange<T>(T upper, T lower) where T : class =>
        SecondNetAbove
            ? new DeckRoles<T>(lower, upper)
            : new DeckRoles<T>(upper, lower);

    /// <summary>The arrangement for an archetype that hangs its second net at this height.</summary>
    internal static DeckArrangement For(float archetypeOffset) => new(archetypeOffset);
}

/// <summary>The actual main and auxiliary network references after following an archetype.</summary>
internal readonly struct DeckRoles<T> where T : class
{
    internal DeckRoles(T main, T auxiliary)
    {
        Main = main;
        Auxiliary = auxiliary;
    }

    internal T Main { get; }

    internal T Auxiliary { get; }
}
