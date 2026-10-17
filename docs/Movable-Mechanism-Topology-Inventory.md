# Movable mechanism topology inventory

Offline inspection of 49 actual main-mechanism mesh streams across all ten B&P styles and their exported LODs. This is not generated geometry or in-game acceptance.

## Method and limits

Components are connected by shared triangle edges within the same native bone group. Two views are retained: original vertex indices, and exact exported XYZ edge positions. Point-only contacts do not merge components. No tolerance, bounding-box proximity, width threshold or inferred hinge is used.

A topology component is not an authored logical part. UV/normal seams can split an authored part, and coincident edges can merge separate parts. The center-crossing counts below describe only these connectivity sets; they must not become runtime stretch/translate decisions until semantic parts and LOD correspondence are reviewed. Pillars and footings are separate geometry, outside this main-mechanism inventory.

## Coverage

| Source mesh | Vertices | Triangles | Indexed sets | Exact-position sets | Center-touching position sets |
| --- | ---: | ---: | ---: | ---: | ---: |
| `DrawBridge01 Mesh.mesh-0.obj` | 29840 | 15940 | 7046 | 392 | 96 |
| `DrawBridge01 Mesh.mesh-1.obj` | 128 | 64 | 32 | 32 | 0 |
| `DrawBridge01_LOD1 Mesh.mesh-0.obj` | 18772 | 9535 | 4743 | 331 | 96 |
| `DrawBridge01_LOD1 Mesh.mesh-1.obj` | 128 | 64 | 32 | 32 | 0 |
| `DrawBridge01_LOD2 Mesh.mesh-0.obj` | 1118 | 656 | 235 | 10 | 4 |
| `DrawBridge02 Mesh.mesh-0.obj` | 44370 | 24664 | 9880 | 577 | 60 |
| `DrawBridge02 Mesh.mesh-1.obj` | 68 | 34 | 17 | 17 | 0 |
| `DrawBridge02_LOD1 Mesh.mesh-0.obj` | 31007 | 14766 | 8133 | 464 | 60 |
| `DrawBridge02_LOD1 Mesh.mesh-1.obj` | 68 | 34 | 17 | 17 | 0 |
| `DrawBridge02_LOD2 Mesh.mesh-0.obj` | 893 | 548 | 176 | 7 | 6 |
| `DrawBridge03 Mesh.mesh-0.obj` | 186078 | 127148 | 32226 | 2985 | 176 |
| `DrawBridge03 Mesh.mesh-1.obj` | 704 | 360 | 172 | 172 | 0 |
| `DrawBridge03_LOD1 Mesh.mesh-0.obj` | 146988 | 76384 | 36293 | 3134 | 176 |
| `DrawBridge03_LOD1 Mesh.mesh-1.obj` | 160 | 88 | 36 | 36 | 0 |
| `DrawBridge03_LOD2 Mesh.mesh-0.obj` | 684 | 448 | 125 | 6 | 4 |
| `LiftBridge01 Mesh.mesh-0.obj` | 472209 | 380478 | 75665 | 10743 | 681 |
| `LiftBridge01 Mesh.mesh-1.obj` | 160 | 80 | 40 | 40 | 0 |
| `LiftBridge01_LOD1 Mesh.mesh-0.obj` | 351968 | 228254 | 68450 | 10743 | 681 |
| `LiftBridge01_LOD1 Mesh.mesh-1.obj` | 160 | 80 | 40 | 40 | 0 |
| `LiftBridge01_LOD2 Mesh.mesh-0.obj` | 1110 | 776 | 173 | 32 | 8 |
| `LiftBridge02 Mesh.mesh-0.obj` | 199372 | 134080 | 34975 | 4701 | 831 |
| `LiftBridge02 Mesh.mesh-1.obj` | 12 | 6 | 3 | 3 | 0 |
| `LiftBridge02_LOD1 Mesh.mesh-0.obj` | 159895 | 80439 | 41344 | 4725 | 823 |
| `LiftBridge02_LOD1 Mesh.mesh-1.obj` | 12 | 6 | 3 | 3 | 0 |
| `LiftBridge02_LOD2 Mesh.mesh-0.obj` | 4152 | 2600 | 784 | 219 | 63 |
| `LiftBridge03 Mesh.mesh-0.obj` | 474534 | 382928 | 68243 | 7894 | 46 |
| `LiftBridge03 Mesh.mesh-1.obj` | 1200 | 600 | 300 | 300 | 0 |
| `LiftBridge03_LOD1 Mesh.mesh-0.obj` | 349267 | 229516 | 65648 | 7876 | 46 |
| `LiftBridge03_LOD1 Mesh.mesh-1.obj` | 1200 | 600 | 300 | 300 | 0 |
| `LiftBridge03_LOD2 Mesh.mesh-0.obj` | 996 | 852 | 84 | 17 | 5 |
| `LiftBridge04 Mesh.mesh-0.obj` | 60636 | 36710 | 12402 | 1362 | 104 |
| `LiftBridge04 Mesh.mesh-1.obj` | 16 | 8 | 4 | 4 | 0 |
| `LiftBridge04_LOD1 Mesh.mesh-0.obj` | 45780 | 22015 | 12180 | 1289 | 103 |
| `LiftBridge04_LOD1 Mesh.mesh-1.obj` | 16 | 8 | 4 | 4 | 0 |
| `LiftBridge04_LOD2 Mesh.mesh-0.obj` | 3212 | 1736 | 772 | 190 | 42 |
| `LiftBridge05 Mesh.mesh-0.obj` | 47062 | 30998 | 8783 | 999 | 51 |
| `LiftBridge05 Mesh.mesh-1.obj` | 272 | 136 | 68 | 68 | 4 |
| `LiftBridge05_LOD1 Mesh.mesh-0.obj` | 36479 | 18492 | 9350 | 641 | 39 |
| `LiftBridge05_LOD1 Mesh.mesh-1.obj` | 272 | 136 | 68 | 68 | 4 |
| `LiftBridge05_LOD2 Mesh.mesh-0.obj` | 2500 | 1824 | 342 | 38 | 14 |
| `PedestrianDrawBridge01 Mesh.mesh-0.obj` | 34931 | 27721 | 4152 | 498 | 74 |
| `PedestrianDrawBridge01 Mesh.mesh-1.obj` | 12 | 6 | 3 | 3 | 0 |
| `PedestrianDrawBridge01_LOD1 Mesh.mesh-0.obj` | 33598 | 16583 | 8872 | 502 | 82 |
| `PedestrianDrawBridge01_LOD1 Mesh.mesh-1.obj` | 12 | 6 | 3 | 3 | 0 |
| `PedestrianDrawBridge01_LOD2 Mesh.mesh-0.obj` | 1702 | 1068 | 323 | 59 | 14 |
| `PedestrianDrawBridge02 Mesh.mesh-0.obj` | 40168 | 30064 | 5563 | 731 | 186 |
| `PedestrianDrawBridge02 Mesh.mesh-1.obj` | 24 | 12 | 6 | 6 | 0 |
| `PedestrianDrawBridge02_LOD1 Mesh.mesh-0.obj` | 32137 | 18025 | 7301 | 785 | 186 |
| `PedestrianDrawBridge02_LOD1 Mesh.mesh-1.obj` | 24 | 12 | 6 | 6 | 0 |

## Provenance

The research file `movable-connectivity-evidence.json` records every face ordinal, bone group and bounding extent for both views. It is intermediate audit data, not a saved bridge dependency or a runtime part map. Reproduce it with `audit_movable_connectivity.py` against the extracted native OBJ files.

- `DrawBridge01 Mesh.mesh-0.obj`: SHA-256 `97215ED0B33145FB53737999961C3DA0E6859E433A89566BB1F7C2C04EE7F9E1`.
- `DrawBridge01 Mesh.mesh-1.obj`: SHA-256 `C50C34BCBA52945E9256BC05F1990E84C9BC8EE1D7B4643A205429A89EF5E57B`.
- `DrawBridge01_LOD1 Mesh.mesh-0.obj`: SHA-256 `4987290BEF55D474AEA63DE7F54197C31C37BD243730AADD45986F1D87E4970D`.
- `DrawBridge01_LOD1 Mesh.mesh-1.obj`: SHA-256 `C50C34BCBA52945E9256BC05F1990E84C9BC8EE1D7B4643A205429A89EF5E57B`.
- `DrawBridge01_LOD2 Mesh.mesh-0.obj`: SHA-256 `7107118D476D585B8021DED3DC55F82BA6B349B3BD2F06F891320668D8FEA1D6`.
- `DrawBridge02 Mesh.mesh-0.obj`: SHA-256 `3CC11A485120E5F63AB8E7DD9610FE99FCA54679ED5A22231E4FAF22F46619B7`.
- `DrawBridge02 Mesh.mesh-1.obj`: SHA-256 `826270B6CA7ACA1A71256E4FC66CED34F4D9DA3BD340923AF9F9B5A762FDAB51`.
- `DrawBridge02_LOD1 Mesh.mesh-0.obj`: SHA-256 `C336E0E978FADCA65C67E3CCEEFB495AE775709560B56459BE38FE84D6AB5B94`.
- `DrawBridge02_LOD1 Mesh.mesh-1.obj`: SHA-256 `77883FD399E6F4B99C10A5E885C8D4F98B7F5E89E951EC41333C4C9658A1AD99`.
- `DrawBridge02_LOD2 Mesh.mesh-0.obj`: SHA-256 `FD5E035291970D87E5C7F86BB1ED0F0FC5AE4D7B2F63C3A7AC0736F9400A2298`.
- `DrawBridge03 Mesh.mesh-0.obj`: SHA-256 `807912FA4E522CFE5D95257EC45BA97D919D4BA6D1588792818F1761D123385A`.
- `DrawBridge03 Mesh.mesh-1.obj`: SHA-256 `27B9022CBFEA04A3A06A996882F542C9CC70FF829877E344800A3A908AA523B7`.
- `DrawBridge03_LOD1 Mesh.mesh-0.obj`: SHA-256 `E94DBBA7AA2A303D47CCE26D22D172F56BAAE92F5AAE305E7522D8E29B572C96`.
- `DrawBridge03_LOD1 Mesh.mesh-1.obj`: SHA-256 `BE795827935BC17C80493DB92B52E4D52A3E7D74F691DDBCBB7D3B834AFE67AC`.
- `DrawBridge03_LOD2 Mesh.mesh-0.obj`: SHA-256 `1B2B24DA2ED96254B19E8125830460A5EDAB8B490D7A5F7EE0A4313DC8422E6F`.
- `LiftBridge01 Mesh.mesh-0.obj`: SHA-256 `D7EACC7D947369A1FC389C0AEE6DD19965EC66D8C8D87206CF07368B614328A4`.
- `LiftBridge01 Mesh.mesh-1.obj`: SHA-256 `9F669353F1695A6488F98C8D035AAF5308550872F6E31F714A6798012F2C1950`.
- `LiftBridge01_LOD1 Mesh.mesh-0.obj`: SHA-256 `66E7C7B1D7CB76358E3F20092F4E19C13BB12ECEA42C3978A6F356F359D12E48`.
- `LiftBridge01_LOD1 Mesh.mesh-1.obj`: SHA-256 `9F669353F1695A6488F98C8D035AAF5308550872F6E31F714A6798012F2C1950`.
- `LiftBridge01_LOD2 Mesh.mesh-0.obj`: SHA-256 `D7E281BEBA9E6951238FBC58BF997DA06A848B189EF9C5129475AA8FE34630FB`.
- `LiftBridge02 Mesh.mesh-0.obj`: SHA-256 `15445F19789DD3F0E10ECCD066FD6E59859CEE69D509CE8BA67576EA1B4138A5`.
- `LiftBridge02 Mesh.mesh-1.obj`: SHA-256 `DBFCF98E026823E29496E762C5E2F42E14E4935164A1EB29251618AE308118E9`.
- `LiftBridge02_LOD1 Mesh.mesh-0.obj`: SHA-256 `6E6807409B3C75AE6B28D587F2918FDC45CD2DD033C971C49A6B6FEB0BB3CC15`.
- `LiftBridge02_LOD1 Mesh.mesh-1.obj`: SHA-256 `DEDC8FDE6D1E6A73C667E6580BC8611F3E1CC915AACC42662595CE4675D540C0`.
- `LiftBridge02_LOD2 Mesh.mesh-0.obj`: SHA-256 `E34567C6A844EF820926042E6A583C61DC84D7E307F6DF64530E6FD300A92ACD`.
- `LiftBridge03 Mesh.mesh-0.obj`: SHA-256 `21A0A50FFFFFFB8E973ADAB9B75DCF9E3B425E10D927E951AF0ED835689F5DF8`.
- `LiftBridge03 Mesh.mesh-1.obj`: SHA-256 `57738B2B77899598FF131606E0D5CF8F24BE86A7232F3935FDF3C605D1CFDD02`.
- `LiftBridge03_LOD1 Mesh.mesh-0.obj`: SHA-256 `836BD9F0C61F2BF2FD59FC91A8AA84920F77C0341354CB85C7516D8B6A19299F`.
- `LiftBridge03_LOD1 Mesh.mesh-1.obj`: SHA-256 `57738B2B77899598FF131606E0D5CF8F24BE86A7232F3935FDF3C605D1CFDD02`.
- `LiftBridge03_LOD2 Mesh.mesh-0.obj`: SHA-256 `CC67DB6F50ADA4F313FAE0EFDBCEAA0E3798B691817D8A52FD2CA9D0E8609E42`.
- `LiftBridge04 Mesh.mesh-0.obj`: SHA-256 `E6633434859FA002A5088C741209BFC550691DE932BA568E7167F88BA18DEB35`.
- `LiftBridge04 Mesh.mesh-1.obj`: SHA-256 `EF77EB0413060101C64A6FE783E98BD06454737BE7C56D6147C7677B02FBF7B5`.
- `LiftBridge04_LOD1 Mesh.mesh-0.obj`: SHA-256 `D6C317020E614A221920C1AA54214A47FEDEC607DFEAFF1B239F15C7CA31DC74`.
- `LiftBridge04_LOD1 Mesh.mesh-1.obj`: SHA-256 `5E0D89A05559D1A1987A9C7F888029D480BFAE5D7A6B76A10A6FB7646CD73E47`.
- `LiftBridge04_LOD2 Mesh.mesh-0.obj`: SHA-256 `AC76A0E4D2562FF29227AB5A042FF2FA4452FF8174A6054145CF0D0215C9117C`.
- `LiftBridge05 Mesh.mesh-0.obj`: SHA-256 `4EFD9264F727551AEAD962D519AF21BD4DD0FA3FC67F261D04080F1D7966707E`.
- `LiftBridge05 Mesh.mesh-1.obj`: SHA-256 `1D37E3EA0179EA6B8F692C4A0F0B8BE8B6E70833DDF068C5434D7D978C0B8964`.
- `LiftBridge05_LOD1 Mesh.mesh-0.obj`: SHA-256 `A58C2CB27BFD5AEF25CB4EEE7AD85F9B01AEBA166AD3B3AFA9C6B444B3FE3F3C`.
- `LiftBridge05_LOD1 Mesh.mesh-1.obj`: SHA-256 `081C91B530077CBA09FE9AD9B41B04CB6F06E9BF5DA3C4F5476B7E7160D1AD98`.
- `LiftBridge05_LOD2 Mesh.mesh-0.obj`: SHA-256 `AEDCE50E33BF82256F25C018B52F5D762A524FB82B73BA2CDD520D3A0FC26495`.
- `PedestrianDrawBridge01 Mesh.mesh-0.obj`: SHA-256 `1CD6A65AAB0ED1E68F8786F01D9D9D0F895DB3FCB8DE4BB3274A00F9B1DDD5D9`.
- `PedestrianDrawBridge01 Mesh.mesh-1.obj`: SHA-256 `5810F7C468806B301EBE6DFB0F1EFD0EA10471A2D9BB31CD9E7167108A56DA36`.
- `PedestrianDrawBridge01_LOD1 Mesh.mesh-0.obj`: SHA-256 `5C275ECBE7EDB4439221F0F25A18C9EEC38051ED46B07B4156B388234B535E84`.
- `PedestrianDrawBridge01_LOD1 Mesh.mesh-1.obj`: SHA-256 `5810F7C468806B301EBE6DFB0F1EFD0EA10471A2D9BB31CD9E7167108A56DA36`.
- `PedestrianDrawBridge01_LOD2 Mesh.mesh-0.obj`: SHA-256 `961195D7A5BFF3E774811B02D2322D3895D078F5AE500E272FE66BBC46A519A1`.
- `PedestrianDrawBridge02 Mesh.mesh-0.obj`: SHA-256 `003E189399F5863113615E8B1AAD45D692950BD97195A451C024A1249E8A0A90`.
- `PedestrianDrawBridge02 Mesh.mesh-1.obj`: SHA-256 `8AB5C6D483EB988B07FCCFE4BFAF6D0161468485ED767DFBCC058FADF71AE219`.
- `PedestrianDrawBridge02_LOD1 Mesh.mesh-0.obj`: SHA-256 `3FC7A164073CCB1BF4BC3E01A250896E8160C46917626A1ACC33F10935FB124A`.
- `PedestrianDrawBridge02_LOD1 Mesh.mesh-1.obj`: SHA-256 `EB785062A94D5D217F8A223868E511CE69ECC35BEB1A99415CFD083C01F5932B`.

## Next classification step

Review the highest-detail mesh against its surface UVs and material appearance, identify road replacement faces and rigid side structures, then establish the corresponding logical parts in every LOD. Do not copy component numbers across LODs or infer semantic identity from their count. This inventory supplies exact candidate face sets for that review and makes coverage omissions visible.
