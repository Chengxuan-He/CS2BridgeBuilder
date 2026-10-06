import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const source = readFileSync(new URL('../src/BridgeBuilder/Systems/BridgeStartupAssetSystem.cs', import.meta.url), 'utf8');
for (const forbidden of ['BridgeLoadFailures', 'BridgeReferenceRecovery', 'EntityManager', 'AssetDatabase', 'onAfterActivePlaysetOrModStatusChanged', 'catch ('])
    assert.ok(!source.includes(forbidden), `Read-only inspection must not use ${forbidden}`);
assert.match(source, /BridgeNetworkValidation.IsInvalid/);
assert.match(source, /audit.RetireFiles/);
assert.match(source, /GameManager.State.WorldReady/);
assert.match(source, /CanCheck => _modLoaded/);
assert.match(source, /finally[\s\S]*IsStartupInspection = false/);
assert.ok(!source.includes('BridgeDiskAudit.Read('));
assert.match(source, /BridgeDiskAudit.ForMemoryFailures/);
console.log('PASS read-only memory inspection, no file integrity scan, completion and mod gate');

assert.match(source, /BridgeAssetLoading.Ready/);
assert.match(source, /BridgeLoadedCidRecovery.Inspect/);
assert.match(source, /BridgeDependencyPersistence.Save/);
const recovery = readFileSync(new URL('../src/BridgeBuilder/Runtime/BridgeLoadedCidRecovery.cs', import.meta.url), 'utf8');
for (const token of ['.SetValue(', '.Load(', 'AddPrefab(']) assert.ok(!recovery.includes(token));
