"""Inventory installed native shader interfaces; never modify game or runtime assets."""
import argparse
import hashlib
import json
import sys
from pathlib import Path


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-data", type=Path, required=True)
    parser.add_argument("--research", type=Path, required=True)
    parser.add_argument("--unitypy", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sys.path[:0] = [str(args.unitypy), str(args.research / "pydeps")]
    import UnityPy

    library_path = args.research / "material-library-resolved.json"
    library = json.loads(library_path.read_text(encoding="utf-8"))
    env = UnityPy.load(str(args.game_data / "resources.assets"))
    objects = {obj.path_id: obj for obj in env.objects}
    shaders = []
    for shader_id in sorted({row["material"]["m_Shader"]["m_PathID"] for row in library}):
        parsed = objects[shader_id].read_typetree()["m_ParsedForm"]
        props = parsed["m_PropInfo"]["m_Props"]
        shaders.append({
            "pathId": shader_id,
            "name": parsed["m_Name"],
            "parsedMetadataSha256": hashlib.sha256(json.dumps(parsed, sort_keys=True).encode()).hexdigest(),
            "templates": [{"hash": row["hash"], "name": row["material"]["m_Name"],
                           "vt": row["vt"], "stacks": row["stacks"]}
                          for row in library if row["material"]["m_Shader"]["m_PathID"] == shader_id],
            "textures": [prop["m_Name"] for prop in props if prop["m_Type"] == 4],
            "properties": [prop["m_Name"] for prop in props],
            "animationKeywords": [word for word in parsed["m_KeywordNames"] if "ANIM" in word or "SKIN" in word],
        })
    evidence_names = ["SurfaceAsset.cs", "SurfaceAssetVTHeader.cs", "VTDatabase.cs",
                      "shader-programs/581-133.asm",
                      "shader-programs/571-2701.asm", "Game/Game.Rendering/BatchDataHelpers.cs",
                      "Game/Game.Rendering/RenderingUtils.cs", "Game/Game.Rendering/OverlayAtlasElement.cs",
                      "Game/Game.Rendering/BatchMeshSystem.cs"]
    report = {
        "scope": "Installed native template property/keyword inventory; not an in-game rendering proof.",
        "gameData": str(args.game_data.resolve()), "librarySha256": digest(library_path),
        "templateCount": len(library), "shaderCount": len(shaders), "shaders": shaders,
        "evidence": [{"file": name, "sha256": digest(args.research / name)} for name in evidence_names],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Inspected {len(library)} templates and {len(shaders)} native shaders: {args.output}")


if __name__ == "__main__":
    main()
