# PedestrianDraw02 mechanism render configuration

The generated render-data class configures fresh private render graphs for the mechanism,
column and footing. It preserves each native level's StackProperties, BaseProperties,
LOD references and content prerequisite. The caller supplies generated geometry and applies
the separately recorded procedural skeleton to each main-render level. Do not apply this
configuration to shared native renders or to a render already carrying copied components.

| Part | Native render records in LOD order |
| --- | --- |
| Mechanism | 81062, 81063 |
| Column | 81064, 81065, 81066 |
| Footing | 81067, 81068 |

The names Column and Footing identify measured object mesh slots; they are not runtime name
matching or permission to infer geometry. Footing is distinct from the road-adjacent deck base.

Source audit: game 1.6.2f1, SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
The extractor follows actual LOD references and checks the complete component list. No runtime
archetype lookup, width inference or new material is introduced.

This class has no generator caller yet. It changes no existing bridge output. Compilation
does not establish geometry, native animation, shipping or cold-start acceptance.
