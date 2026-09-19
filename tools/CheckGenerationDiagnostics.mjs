// Source-policy regression check only; never creates or tests bridge geometry.
import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const read = path => readFileSync(resolve(root, path), "utf8");
const runtime = "src/BridgeBuilder/Runtime";
for (const file of readdirSync(resolve(root, runtime)).filter(x => /^BridgePreview.*\.cs$/.test(x))) {
    const source = read(`${runtime}/${file}`);
    assert(!/Log\.(?:Error|Critical|Warn)\(/.test(source), `Preview logging remains: ${file}`);
    assert(!/FailureException|_failureException/.test(source), `Unused exception propagation remains: ${file}`);
}
for (const path of ["Bridges/TowerFactory.cs", "Systems/BridgePublicationSystem.cs", "Runtime/BridgeAssetPack.cs"])
    assert(!/Log\.(?:Error|Critical|Warn)\(/.test(read(`src/BridgeBuilder/${path}`)), path);
const generation = read("src/BridgeBuilder/Systems/BridgeGenerationSystem.cs");
assert.equal((generation.match(/new ExportReport\(logIssues: false\)/g) || []).length, 3,
    "Export, create and preview reports must be silent; deletion keeps its diagnostics");
assert(!/Log\.Critical\(/.test(generation));
assert(generation.includes("report.FailedRoads != failuresBefore"), "Do not remove publication guards");
assert(generation.includes("_previewReleasePending = true"), "Failed previews must release resources");
assert(generation.includes("BridgePreviewState.Publish(revision, string.Empty, stage)"));
assert(!read("src/BridgeBuilder/Settings/RuntimeUiText.cs").includes('["Refreshed"]'));
const report = read("../CS2ModShared/src/Infrastructure/ExportReport.cs");
assert(report.includes("ExportReport(bool logIssues = true)"), "Other hosts keep default logging");
assert(report.includes("FailedRoads++"), "Silent reports must still count failures");
assert.equal((report.match(/if \(_logIssues\) ModHost\.Log\./g) || []).length, 3);
console.log("PASS silent generation/preview diagnostics, retained failure guards and cleanup, dead diagnostic fields removed");
