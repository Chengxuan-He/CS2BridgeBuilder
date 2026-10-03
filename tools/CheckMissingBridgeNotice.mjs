import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../src/BridgeBuilder/Systems/BridgeMissingAssetSystem.cs', import.meta.url), 'utf8');
for (const forbidden of ['BridgeInstanceRemoval', 'DestroyEntity', 'AddComponent', 'RemoveComponent', 'SetComponentData', 'EntityCommandBuffer', 'SuspendForCleanup', 'MissingBridgesRemoved', 'MissingBridgesTimeout']) {
  assert.ok(!source.includes(forbidden), `Read-only load detector must not contain ${forbidden}`);
}
assert.match(source, /if \(_loadPlanned\) return;/);
assert.match(source, /_loadPlanned = false;/);
assert.match(source, /new HashSet<string>\(StringComparer.Ordinal\)/);
assert.match(source, /if \(names.Count > 0\) Notice\("MissingBridgesDetected", names.Count\)/);
assert.match(source, /if \(!_loadComplete \|\| _pendingNotice == null/);
assert.ok(source.indexOf('_pendingNotice = null;', source.indexOf('private void ShowPendingNotice')) < source.indexOf('Mod.ShowMessage', source.indexOf('private void ShowPendingNotice')));
assert.match(source, /bridge.EndsWith\("_Lower"/);
assert.match(source, /bridge.EndsWith\("_Upper"/);
assert.match(source, /BridgeRegistration.IsPrefabName\(bridge\)/);
console.log('PASS source regression guards: read-only detector, once per load, UUID-scoped counts, deferred one-shot notification');
