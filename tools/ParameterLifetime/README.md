# Numerical parameter lifetime lowering

Run `dotnet run --project tools/ParameterLifetime -- ROOT --apply` after regenerating measured
source. `--check` rejects permanent numerical holders; Build.ps1 runs it before compilation.
AffineGeometryMigration --apply also invokes this pass after packing programs. Resource packing
emits non-retaining accessors; this pass migrates reconstructed records and dependent holders too.

The Roslyn dependency graph traces BBN loads, recorded compressed maps, field/property/method and
constructor dependencies. It converts immutable static numerical fields to typed request-cache
accessors while preserving each initializer. Empty/numerical arrays and post-initialization
membership/UV/dictionary caches follow the same ownership. Pure identifier strings and native
catalogue state remain distinct; the one mutable provenance root, BridgeStyleCatalog._styles,
contains copied display metadata and native donor registrations, not retained decoded BBN arrays.
The catalogue name callback captures only its string ID. New unsupported mutable numerical
roots fail the guard rather than acquiring a permanent cache.

The common TryBuildBridge entry starts a session before constructing composers and covers early
returns and exceptions. AffineGeometry.Groups explicitly attaches the owning session to workers;
reference-counted leases prevent parent completion from clearing active workers. Each independent
request has its own cache. Metadata access outside a request has a bounded temporary session;
catalogue construction/rebuild has explicit scopes to batch these small reads. No permanent
fallback, full preload, reflection-based runtime static reset or forced GC is used. The shared
register ArrayPool is request-owned as well; pooled scratch is distinct from measured parameters.

Native skeleton methods allocate their output arrays. Structure Plan/Fragment/PreparedDomain
constructors copy their required numeric records and indices; final coordinate/native byte buffers
are output-owned. Material-ready deferral precedes construction. Publication captures native nodes,
report/identity callbacks and time only; no parameter session survives for rendering/publication.

Native Mono verification lives in AffineGeometryNativeCheck/Probe/LifecycleCases.cs. It retains
actual generated Plans while testing cached object and raw BBN-array collection, isolates nested
requests, attaches a worker that outlives the parent's lease, and checks failure/cancellation and
catalogue output. Forced GC occurs only in the test host. Record demanded resource/array bytes
and the cost of reloading per request, not the old cross-request warm-cache timing as a new guarantee.
