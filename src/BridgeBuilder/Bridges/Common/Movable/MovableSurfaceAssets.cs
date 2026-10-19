using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Native object equivalents measured from the installed game's material reference sets.
// Unmatched curved appearances use the prototype object's material, never a new descriptor.
internal static class MovableSurfaceAssets
{
    // Both native shader families support the procedural skeleton and packed tangents.
    // Template 11 is the forward transparent material used by composed road paint.
    internal static bool IsObjectSurface(SurfaceAsset surface) =>
        surface.materialTemplateHash is 0 or 1 or 11 or 13 or 14 or 35 or 36;

    internal static SurfaceAsset ExistingObject(SurfaceAsset source, SurfaceAsset prototype)
    {
        source.LoadHeader();
        if (IsObjectSurface(source)) return source;
        var cid = source.id.ToString() switch
        {
            "3fd47dc01126f4121ed6c363f854e9cb" => "652dd064690d2eab0f150ad10b93e50d",
            "2ac33e421296c2093127e5636a6f0c2a" => "40b82be441c0ee29b9912d64df01dddf",
            "60e2dd44172b971b4fc2f8f371cb20fd" => "652dd064690d2eab0f150ad10b93e50d",
            "d6504683070f020f76ab72529f652422" => "f1d7c1752617aa735981a57bd5e790d5",
            "8715dd5fb68196a24f500eaf68f3e370" => "1fb1e8d21132df210108eb3653d68747",
            "e392e0837b20903d722ee52c55cca5ee" => "ba66138e9829fbbc045feac71b61a8f1",
            "2bff4a9d1428c777b50c4b3ff9403427" => "95d62e9b56c7e310067c4ff267a6c5a4",
            "040edb0ee431f9b5e2db66db8d29fb7c" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "b6cc2026a37df67bced91293285f2c59" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "fef52fa5dea59971adb784f77f4f0565" => "1fb1e8d21132df210108eb3653d68747",
            "7e8798845df0bc1f3e841fc67b5f8b09" => "eddaad09c08fba90891616cbdc91dd66",
            "f3b7d5e256d8a237e215ed1c39e3f947" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "3e7a1e315b3fada7603f332f2912d526" => "dc4ded3951ec27f3c9890f603715511c",
            "853695e95a31cfd98c148c1a86440a39" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "4a7e00e185c71338d76131f330d648f8" => "ba66138e9829fbbc045feac71b61a8f1",
            "02c34c57185e9c77182f60fee18bd902" => "fa11f4bd5acf8d268fdd057315112229",
            "eabed2bb10ac068d9bb2b470428e77c8" => "1fb1e8d21132df210108eb3653d68747",
            "22d3861d265f3089c2a9ea12ad130873" => "1fb1e8d21132df210108eb3653d68747",
            "ec77da15a7239426a65e338cb4d9104d" => "ba66138e9829fbbc045feac71b61a8f1",
            "235ecf4cc2aece7e0a545fabeb574880" => "b6fd4dad3da03bd277a1a08f38aaea9d",
            "370c198239873f6916eed79cee2af83f" => "f1d7c1752617aa735981a57bd5e790d5",
            "2acb50db8a9a7ed03dd6e18f4be54313" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "a607c5c0b51afa15d5516ad1e6d4640f" => "b6fd4dad3da03bd277a1a08f38aaea9d",
            "b4d42885990dfa18462c7bef595165e2" => "1fb1e8d21132df210108eb3653d68747",
            "1704c1b7e7e26426020e3a5e95aebb79" => "b6cbeb8d21701a513e1ba8f7113ac9a5",
            "b5490279cbefd35f67734ca5ebf25a19" => "eddaad09c08fba90891616cbdc91dd66",
            "a775168a02fe5375ef0317ff35949e81" => "1fb1e8d21132df210108eb3653d68747",
            "ce1b04df8e3c418330d3ead72ea2852c" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "833442698adce3230178e27ca08a0852" => "dc4ded3951ec27f3c9890f603715511c",
            "1fc99cac22b76ad8a109cf3b95bbbca4" => "1fb1e8d21132df210108eb3653d68747",
            "b0a283ecb74097b313f46cd560636c4d" => "1fb1e8d21132df210108eb3653d68747",
            "48e659011275f8d26fc1fc7c7b11bf94" => "f1d7c1752617aa735981a57bd5e790d5",
            "945d3beafd2738d6beaed769fc39a09a" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "dabca8cbc5a5614e1f594e4a862eb8d9" => "1fb1e8d21132df210108eb3653d68747",
            "d8c76e7d8219c5a00c6080f4d09d27d8" => "1fb1e8d21132df210108eb3653d68747",
            "4e98990e945fec2fee2098b54c9bde48" => "1fb1e8d21132df210108eb3653d68747",
            "ad353d7fb042f866f2b9a241a279a72f" => "1fb1e8d21132df210108eb3653d68747",
            "8a53543bd240e97dc5016912d772da4d" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "719395bac38362e541a3d781308e3b84" => "eddaad09c08fba90891616cbdc91dd66",
            "aa9aebadddb8bb0889f684b704616ec7" => "fa11f4bd5acf8d268fdd057315112229",
            "2189fae9fab58a6186fbf984b5fa2cb2" => "b2315eacf830d5865dcd366fbbdc246a",
            "5a7c702be0794334277b58485350850e" => "b6fd4dad3da03bd277a1a08f38aaea9d",
            "b22e05af65a5a188e37d64a592ed64ad" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "4ad939bbe4780391c0992a56f50982a3" => "1fb1e8d21132df210108eb3653d68747",
            "7a82270e084e1cf46795d1703b4b3bbc" => "1fb1e8d21132df210108eb3653d68747",
            "5f379f6e52938e0c8cec77ed6c8c5e41" => "fa11f4bd5acf8d268fdd057315112229",
            "7e654a72537aadfa246b6d6a24c59632" => "fa11f4bd5acf8d268fdd057315112229",
            "1765ed23c643213c465eb7199a63812b" => "8b1db898381dd4fbf52a198a565ebd8c",
            "5c4e565ee151cfc6781bcbfdd9e37e8c" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "443363f2e5dfdb3f7884011fda14db26" => "8b1db898381dd4fbf52a198a565ebd8c",
            "7dfa2cfe08e8949c1081d20933ff853c" => "1fb1e8d21132df210108eb3653d68747",
            "6ac973b57e5bf0971e836e015fa2ba97" => "f57a6fc932620a1535a0d04cbca8575e",
            "8417a21ea35307979aa710c2c8483444" => "ba66138e9829fbbc045feac71b61a8f1",
            "6b2fe3a052d9d499c91531327cdcd001" => "1fb1e8d21132df210108eb3653d68747",
            "57c4e016d860d446d747605f11e40e68" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "259e9d78bf69aa465dcc5eb229e6ffab" => "ba66138e9829fbbc045feac71b61a8f1",
            "2844496bc4cd0e75b3a7f55ed634a3fb" => "1fb1e8d21132df210108eb3653d68747",
            "e85f6dcbc52b6dd7490df4f98ce788eb" => "dc4ded3951ec27f3c9890f603715511c",
            "72925f180b77bec0e8bd1a8b91db246a" => "f1d7c1752617aa735981a57bd5e790d5",
            "cdb1fafde30bc577aa1cf18b042a8665" => "8b1db898381dd4fbf52a198a565ebd8c",
            "826838900bd562b7ca17de336c59d872" => "b6cbeb8d21701a513e1ba8f7113ac9a5",
            "34fe55b41e50cb349c881281c820f6c8" => "b2315eacf830d5865dcd366fbbdc246a",
            "ed29baeb6954c2b93f15483746774faa" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "f44f712bb69802a708df58b377b2c963" => "f1d7c1752617aa735981a57bd5e790d5",
            "83ba68460baf2d90a1155f39f66c0e02" => "1fb1e8d21132df210108eb3653d68747",
            "027341a3153268d6ef4f7fb5c63e8608" => "eddaad09c08fba90891616cbdc91dd66",
            "d7e965a0313d2d1b0599b4c7835d55e1" => "b2315eacf830d5865dcd366fbbdc246a",
            "9357a904ce756f73c9a2f18ec3ca79c7" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "7392eaf0a5f9a24548937f6f64b67788" => "fa11f4bd5acf8d268fdd057315112229",
            "1e1665c387405da57a0b103afff4fe34" => "8ef7a7b5f34310dcc9a56abae2298c3b",
            "9282c99818ee46627de5f456cb8d0c2c" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "460bc8176486350474910da87dad5dc7" => "1fb1e8d21132df210108eb3653d68747",
            "7eab13e05e43de983cc39503c76f9f6a" => "0fd9902e3a58bed68cdcd29c1ff56ac9",
            "b42a3ae11a671e92431f524b6ee5e4ce" => "b2315eacf830d5865dcd366fbbdc246a",
            "7977f59f8dd1a794970190046412354b" => "ba66138e9829fbbc045feac71b61a8f1",
            "aaac1477f13f70cf2d48666d5741e090" => "8b1db898381dd4fbf52a198a565ebd8c",
            "7478e26b9e8fb950348798ed71f8ad09" => "1fb1e8d21132df210108eb3653d68747",
            "f84b38e57b4a27349133f63a89139d50" => "f1d7c1752617aa735981a57bd5e790d5",
            "5cbabfe680bf526e144e62a91dd69bc9" => "1fb1e8d21132df210108eb3653d68747",
            "460ddb488ad7e8f0589ca76ec0854824" => "1fb1e8d21132df210108eb3653d68747",
            "bc2eeb9b10b009158f478df32b9715b3" => "eddaad09c08fba90891616cbdc91dd66",
            "b41ed0e6e90eeee2a292281103203440" => "dc4ded3951ec27f3c9890f603715511c",
            "7b256a270f82a0203ff201695e012bca" => "b2315eacf830d5865dcd366fbbdc246a",
            "5ee370492ae8b7744d1d7d390e610df2" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "e752b2321a8aa1cf2702c215a9b2c0c3" => "69ef0aaa7ed7a4acc924ba2fe87b6455",
            "38b6c16a4fe85d1d068a0d2ca7a853d1" => "1fb1e8d21132df210108eb3653d68747",
            "eb3fe0a1694ac55251cf1790056edb16" => "1fb1e8d21132df210108eb3653d68747",
            "e5d308132a440417f51f7a3844524659" => "eddaad09c08fba90891616cbdc91dd66",
            "487f626cdd4cabed07f98fb8cbbd0fae" => "fa11f4bd5acf8d268fdd057315112229",
            "175e452af0f45b10f4d9139e2d493af4" => "1fb1e8d21132df210108eb3653d68747",
            "f8109685ff0fd47a631d13108e24ba98" => "b2315eacf830d5865dcd366fbbdc246a",
            _ => null
        };
        if (cid == null) return prototype;
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out SurfaceAsset surface))
            throw new InvalidOperationException("Missing native object material: " + cid);
        return surface;
    }
}
