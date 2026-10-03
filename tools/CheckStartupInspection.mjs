import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../src/BridgeBuilder/Systems/BridgeStartupAssetSystem.cs', import.meta.url), 'utf8');
const boot = source.slice(source.indexOf('protected override void OnWorldReady()'), source.indexOf('protected override void OnGameLoadingComplete'));
assert.match(boot, /if \(_startupCheckClaimed\) return;/);
assert.ok(boot.indexOf('_startupCheckClaimed = true;') < boot.indexOf('InspectStartup();'));
assert.match(boot, /finally \{ IsStartupInspection = false;/);
assert.equal((source.match(/InspectStartup\(\);/g) ?? []).length, 1);
assert.ok(!source.includes('_startupCheckClaimed = false'));
const ui = source.slice(source.indexOf('protected override void OnUpdate()'), source.indexOf('private void InspectStartup()'));
for (const forbidden of ['BridgeDiskAudit', 'PrefabCatalog', 'InspectStartup();', 'RetireInvalid'])
    assert.ok(!ui.includes(forbidden), `UI must not execute ${forbidden}`);
assert.ok(ui.indexOf('_pendingNotice = null;') < ui.indexOf('Mod.ShowRecoveryMessage'));
const preload = source.slice(source.indexOf('protected override void OnGamePreload'), source.indexOf('protected override void OnWorldReady'));
assert.ok(!preload.includes('_pendingNotice = null'));
const retirement = readFileSync(new URL('../src/BridgeBuilder/Systems/BridgeGenerationSystem.cs', import.meta.url), 'utf8');
assert.match(retirement, /!BridgeStartupAssetSystem.IsStartupInspection/);
assert.match(retirement, /GameMode.Game \| GameMode.Editor/);
console.log('PASS boot-only inspection, session latch, preserved deferred notice, notification-only UI, no city retirement');
