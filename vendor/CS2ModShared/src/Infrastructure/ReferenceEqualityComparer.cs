using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CS2Mods.Shared.Infrastructure;

internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
{
    internal static ReferenceEqualityComparer<T> Instance { get; } = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
