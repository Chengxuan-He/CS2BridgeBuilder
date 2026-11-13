import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const source = readFileSync(new URL('../src/BridgeBuilder/Systems/BridgeStartupAssetSystem.cs', import.meta.url), 'utf8');
for (const forbidden of ['BridgeLoadFailures', 'BridgeReferenceRecovery', 'EntityManager', 'AssetDatabase', 'onAfterActivePlaysetOrModStatusChanged'])
    assert.ok(!source.includes(forbidden), `Read-only inspection must not use ${forbidden}`);
assert.ok(!source.includes("BridgeNetworkValidation"));
assert.match(source, /system.TryGetEntity/);
assert.match(source, /audit.RetireFiles/);
assert.match(source, /GameManager.State.WorldReady/);
assert.match(source, /CanCheck => _modLoaded/);
assert.match(source, /finally[\s\S]*IsStartupInspection = false/);
assert.ok(!source.includes('BridgeDiskAudit.Read('));
assert.match(source, /BridgeDiskAudit.ForMemoryFailures/);
assert.match(source, /BridgeAssetLoading.Ready/);
assert.ok(!source.includes("BridgeLoadedCidRecovery.Inspect"));
assert.match(source, /BridgeDependencyPersistence.Save/);
for (const token of ['.SetValue(', '.Load(', 'AddPrefab('])
    assert.ok(!source.includes(token), `Startup inspection must not mutate cached prefabs through ${token}`);
console.log('PASS native registration and dependency IO inspection, scoped migration/retirement, completion and mod gate');
