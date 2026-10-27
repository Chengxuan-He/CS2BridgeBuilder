public static class NativeEntry
{
    public static string Run(string[] keys, string digest) => DomainCheck.Run(new System.Collections.Generic.HashSet<string>(keys), digest);
}
