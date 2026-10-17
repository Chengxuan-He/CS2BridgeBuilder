# Moving road GPU baking

BridgeBake.compute contains generation-only road and decal kernels. Unity 2022.3.62f2
builds their Windows asset bundle; the normal mod build embeds assets/bridgebake in
BridgeBuilder.dll. Generated bridge assets do not reference this bundle or its compute
shader. They retain native Default, DefaultCutout and DefaultDecal material templates.

Run Build-GpuBake.ps1 with a dedicated -ProjectPath under the task's work directory
whenever the compute source changes, then run the normal Release build/deployment.
The script uses the installed Unity editor and requires its valid local license.
Do not pass an existing unrelated Unity project: this dedicated build project has an
minimal package manifest with the built-in AssetBundle module. GPU support is required at generation time; there is no CPU
baking fallback. Bundle/kernel loading failures remain explicit operation errors.

GPU work includes chart barycentrics, texture filtering, world-space material blending,
normal evaluation/encoding, curved-decal projection, endpoint fading and channel packing.
CPU work includes geometric partitioning, uploads/readback, array transfer into native
TextureAsset serialization, native compression, and saving. These remaining operations
can still stall a synchronous generation request; GPU migration is not a claim of an
asynchronous UI or a measured frame-time guarantee.

The C# reference evaluators retain the previously audited equations for inspection;
the production atlas and receiver paths dispatch MovableGpuBake instead of invoking
their per-pixel evaluation loops. Float GPU arithmetic and hardware filtering need
human visual acceptance against native materials.
