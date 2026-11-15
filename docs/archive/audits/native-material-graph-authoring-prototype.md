# Native material graph authoring prototype

Historical authoring evidence retained from the material-program design. These results precede production graph integration and do not establish in-game acceptance.

### Prototype status and required evidence

The isolated Unity 2022.3.62f2 authoring and separate-process cold-load probes now
serialize and restore the actual game `AssetDatabaseResources` type, its native
`MaterialLibrary` entries, Unity Materials and Shader references. The earlier null
`MonoScript` was an incomplete plugin import: Unity rejected the game assembly after
missing dependencies, while reflection could still instantiate the type. Importing
the complete real assembly-reference closure (41 DLLs, 14,045,864 bytes) and the
SDK's `com.unity.ugui` 1.0.0 package supplies the missing `UnityEngine.UI` reference.
Reference validation remains enabled; no replacement game types or reflection
assembly-resolution bypass is used. The installed SDK authoring project also uses
2022.3.62f2; this does not establish current-game 2022.3.71f1 acceptance.

The Editor produces the native serialized graph and a nonzero MonoScript reference.
An offline assembler replaces its placeholder Shader record/type tree with each
existing generated native Shader record, preserving that record byte-for-byte.
Each resulting bundle is loaded in a fresh Editor process using the actual game
assembly. Both `WorldLayeredPbr` and `BoneDecal` retain their two test material
entries, hashes and queues (2000 and 2007), and resolve the intended generated
Shader. This is native object-graph persistence evidence, not just a UnityFS header
or successful construction of transient Unity objects.

Retained scripts, dependency inventory, native graph bundles, hashes and Editor logs
are in `artifacts/native-shader-format/native-graph-full-evidence`. The tests use
synthetic material defaults, empty VT stacks and no rendering device. They do not
prove complete source-property/keyword/VT preservation, arbitrary owner-time queue
serialization, production dependency transactions, native template registration,
DOTS/ordinary drawing, or game cold loading. Those remain production integration
and acceptance work; do not treat this authoring prototype as a delivered repair.

The intermediate raw-bundle plus registration-recipe implementation is not deployment-ready
for this target. Existing native bundle bytes and offline authoring work remain reusable.
The importer must retain loaded objects for their native consumers and release only failed,
unpublished resources. Native template identity and source assets must not be overwritten.

