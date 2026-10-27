namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Native opaque and double-sided descriptors share one Clean texture set.
    static partial void Draw03(string samplerKey, ref string? unitKey)
    {
        switch (samplerKey)
        {
            case "afd8e2b9acd02aaf8308f8cd569380fe581a9aa5f3746d79e69e22cfd627d0ff":
                unitKey = "fb9e2799789cbf6742ac1aa97cd5858ee195373e9c75ad4effa81ec1bf6c4bb3";
                break;
            case "a1ee0fe8d1d49a2bc2f22e0a66b8b957828698e3d74a2c0741346f9f52097957":
                unitKey = "1902e7e06b0cc4c5f459f02d5951b15e4963edf412e4b21b781428afdc3d6d16";
                break;
            case "e462bf53ee77a8a04a61fbee9345cd8e9627a19e116eceb23b47e27ad75268a7":
                unitKey = "c0f8c7fc05722cd927906afc0de38001dbf9304cdcf0616f7e17d59d2c71beac";
                break;
        }
    }
}
