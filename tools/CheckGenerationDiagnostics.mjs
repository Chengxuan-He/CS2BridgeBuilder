import { readGenerationSource } from './ReadGenerationSource.mjs';
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
    if (file === 'BridgePreviewState.cs')
        assert(source.includes('Mod.Log.Critical('), 'Visible preview errors must be critical');
    else
        assert(!/Log\.(?:Error|Critical)\(/.test(source), `Only preview result publication logs panel errors: ${file}`);
    if (file !== 'BridgePreviewRenderer.cs')
        assert(!/Log\.Warn\(/.test(source), `Unexpected preview warning: ${file}`);
    assert(!/FailureException|_failureException/.test(source), `Unused exception propagation remains: ${file}`);
}
for (const path of [...readdirSync(resolve(root, "src/BridgeBuilder/Bridges/Common/Geometry")).filter(x => /^TowerFactory.*\.cs$/.test(x)).map(x => `Bridges/Common/Geometry/${x}`), "Systems/BridgePublicationSystem.cs", "Runtime/BridgeAssetPack.cs"])
    assert(!/Log\.(?:Error|Critical|Warn)\(/.test(read(`src/BridgeBuilder/${path}`)), path);
const generation = readGenerationSource();
assert.equal((generation.match(/new ExportReport\(logIssues: false\)/g) || []).length, 2,
    "Reports aggregate diagnostics; permanent failures are surfaced once by Finish");
const renderer = read(`${runtime}/BridgePreviewRenderer.cs`);
assert(renderer.includes('Preview readback failed:') && renderer.includes('else Mod.Log.Warn(message)'),
    'Keep technical readback/render failure diagnostics without error popups');
const deletion = generation.slice(generation.indexOf('private void FailDeletion('), generation.indexOf('private HashSet<string> LoadedExportNames'));
assert.equal((deletion.match(/Log\.Critical\(/g) || []).length, 2, 'Keep explicit deletion failure diagnostics');
assert.equal((generation.match(/Log\.Critical\(/g) || []).length, 6, 'Deletion, activation, preview exceptions and completed operations report critical failures');
assert(generation.includes('report.FailureDetails'), 'Critical logs must include the original failure reason');
assert(generation.includes("report.FailedRoads != failuresBefore"), "Do not remove publication guards");
assert(generation.includes("_previewReleasePending = true"), "Failed previews must release resources");
assert(generation.includes("BridgePreviewState.Publish(revision, string.Empty, stage)"));
assert(!read("src/BridgeBuilder/Settings/RuntimeUiText.cs").includes('["Refreshed"]'));
const report = read("vendor/CS2ModShared/src/Infrastructure/ExportReport.cs");
assert(report.includes("ExportReport(bool logIssues = true)"), "Other hosts keep default logging");
assert(report.includes("FailedRoads++"), "Silent reports must still count failures");
assert.equal((report.match(/if \(_logIssues\) ModHost\.Log\./g) || []).length, 3);
console.log("PASS critical panel errors and retained generation failure guards");

const requests = read(`${runtime}/BridgeRuntimeRequests.cs`);
assert(requests.includes('if (_status.Text.Length != 0)') && requests.includes('Mod.Log.Critical('));
const create = generation.split('private void CreateRuntimeBridge(')[1].split('private bool CompleteRuntimeBridge(')[0];
assert(create.indexOf('BridgeSessionState.RestartRequired') < create.indexOf('ExportStateStore.Load()'),
    'Restart must block creation before any UUID/asset allocation');
const activate = generation.split('private bool ActivatePrefab(string prefabName)')[1].split('private void RenameRuntimeBridge')[0];
assert(activate.indexOf('BridgeSessionState.RestartRequired') < activate.indexOf('PrefabCatalog.GetAll'),
    'Restart guard must report its real reason, not an unloaded prefab');
for (const key of ['BridgeRestartRequired', 'ActivateUnloaded', 'ActivateNotReady', 'ActivateToolFailed', 'ActivateFailed'])
    assert(activate.includes(`"${key}"`), `Missing activation failure classification ${key}`);
assert.equal((generation.match(/: _activationLocked \? "(?:CreatedLocked|ActivateLocked)" : _activationFailure/g) || []).length, 2,
    'Both create-and-build and management build must preserve the failure reason');
const ui = read('src/BridgeBuilder/UI/BridgeBuilderUISystem.cs');
assert(ui.includes('Mod.Log.Critical(exception, "Bridge panel error: stage=') && ui.includes('UiRefreshFailed'));

assert(read('src/BridgeBuilder/UI/BridgeBuilder.mjs').includes('trigger("PreviewImageFailed", resultKey, image)'),
    'Browser image failures must reach the C# critical log');
assert(ui.includes('"PreviewImageFailed", BridgePreviewState.ImageFailed'));
