namespace BridgeBuilder.Bridges;

// Four measured prototype units shared across sampling uses. Full native mips
// retain corrected combined-VT control data and source anisotropy. Fractional
// source regions retain their exact physical periods on upward-rounded grids.
internal static partial class MovableRepeatUnits
{
    static partial void Draw01(string samplerKey, ref string? unitKey)
    {
        unitKey = samplerKey switch
        {
            "021552578e8aa71247d00d7457265e4f21ac2315bcb9de196a3be4153e9a349c"
                => "a2bf1853a507f3b0b01ec78944c73a3fd104338ba62c15f202eea7cbbb57825d",
            "4b65e3aa01d9877f6852e95c86ee81af53b375eedcc5a0a150b312d0cbb65783"
                => "31f29a40fbc20da48b4fb9502cb58ca3d7e7dcc88993f46d06eca9bbaf8ef73f",
            "d5bcb0d2f68295a39979eee2732a395143a9c7be33d08fa329d910804eb7e589"
                => "63273420430aa425fa48015d24c11ef2720e0857a12eec4e2caf1cc6c23f0d03",
            "43c1455d7040005bdb2fbc435041fa69bdd508c8b436a4936963e386ee6569fa"
                => "b544a9a8c55c3ec54c8422592b63feb08ad310cf10bc7ef621278fcfb5a8f60a",
            _ => null
        };
    }
}
