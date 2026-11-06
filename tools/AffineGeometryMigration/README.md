# Binary structure programs

`AffineGeometry.Execute` is the single structural coordinate arithmetic entry
point. Bridges provide measured BBN tables, input/output arrays, indices and
selected width/layout parameters. Offline lowering emits explicit packed XYZ stages;
`StructureCoordinateProgram` executes continuous `x0,y0,z0,x1,...` lanes rather
than axis columns. Operation dispatch occurs outside arithmetic loops. Runtime
expression recognition and specialized recognized-expression paths are prohibited.
Keep multiply/add separate, preserve inactive-axis bits and aliased-read snapshots,
and publish the owned target directly as native position bytes. Scalar coefficient
and frame outputs remain distinct from vertex storage. `Groups` schedules complete
independent mesh jobs, not numerical callbacks.

After running a historical producer, run:

```powershell
dotnet run --project tools/AffineGeometryMigration -- D:\CS2BridgeBuilder --apply
dotnet run --project tools/AffineGeometryMigration -- D:\CS2BridgeBuilder --check
```

Without `--apply`, lowering writes a report only. The offline compiler reflects
immutable tables from the current source/resources, preserves float bits and
operation order, separates topology records from interpolation, batches measured
ranges, removes superseded arrays and trims unused packed entries. Original files
are backed up before their first mutation. `LegacyAffineSyntax.cs.txt` exists only
to parse historical producers; it is not included in the game DLL.

Unknown arithmetic is not silently accepted. The production `Build.ps1` runs the
structural-output guard; unsupported producer output must be converted to shared
program data before it can ship. Layout/boundary selection, topology construction,
UV partition policy and single-object placement remain outside mesh coordinate
arithmetic. Their derived numeric columns enter the same executor when they move
structural vertices. Do not restore old producer output over reviewed bindings.

`GeometryMetaprogram/StructurePrograms.py` reproduces shared expression programs.
Run `AffineGeometryCheck`, `Test.ps1`, and the actual game-Mono differential checks
in `AffineGeometryNativeCheck`. The Unity editor uses different scalar float
evaluation rules and is not the bitwise numerical oracle. Native mesh upload,
frame transformation and retiling are checked separately in Unity.

The runtime accepts APF2 only. APF1 is an offline intermediate from historical
producers; `--apply` finishes by packing every intermediate, and the shared Python
producer invokes the same `--pack-aos` compiler. `--check` rejects an unpacked
resource. `--verify-aos-from INPUT_NUMBERS_DIRECTORY` compares deterministic
compilation of retained intermediate inputs with the complete current resources.

APF2 keeps BBN1's checksum and typed arrays. Its I32 header contains magic, version,
entry count and absolute entry offsets. Each entry specifies row count, register
count, instruction count, lane width (three for coordinates, one for explicit
scalar results) and an offline-proven block size. Each twenty-word instruction
contains operation, destination, activity mask, operands and three binding records.
Binding records distinguish immutable interleaved coefficients, parameters,
coordinate records and scalar coefficients. Arithmetic stages operate on adjacent
XYZ floats; explicit lane transfers preserve cross-axis dependencies. No runtime
expression recognition or algebraic reassociation is involved.

Read/write ordering is analyzed offline assuming coordinate bindings may alias.
Independent XYZ outputs share packed stages. Dependent writes keep original
snapshots and instruction boundaries. Only a proof that no row reads another
row's written coordinate permits bounded blocks; dynamic memberships retain the
full snapshot. Dense immutable inputs use direct spans where ownership permits;
sparse inputs adapt complete XYZ records. Final target axes outside the activity
mask retain their original bits. Scratch lanes are not published as coordinates.
The native mesh writers consume the completed target through a byte view rather
than allocating and filling another scalar XYZ array.

## Demand and lifetime

Load numerical resources only on demand within the current generation request. Reuse decoded
BBN arrays, SIMD programs and reconstructed parameter/membership records across that request's
stages and joined LOD workers. Release all cache-owned strong references on success, failure or
cancellation. Do not preload unused bridge types or retain permanent static numerical holders.
Independent requests must not clear each other's active data. Deferred material preparation acquires
this scope only when construction begins. Bounded metadata queries may use temporary scopes, not a
permanent fallback cache. Final native output and delayed preview/publication callbacks must own
their required data rather than retaining a session or borrowed parameter arrays. Copy required
parameter slices into output-owned storage before handoff. Do not force GC or promise an immediate
RSS decrease; later requests reload and validate only their demanded resources. Register scratch
buffers use a separate request-owned pool, returned in finally and released with that request.

Measure initial embedded-resource read, checksum and typed decode separately from within-request
cache reuse. Then measure member/parameter preparation, buffers, operand loads, packed arithmetic,
output stores and direct native position byte-view consumption. Arithmetic includes memory access;
it is not pure ALU time. Offline-proven blocks schedule the same instructions, never expression
recognition or a transposition into axis columns. Regenerated accessors must preserve this lifetime.
