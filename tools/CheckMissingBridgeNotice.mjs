import assert from 'node:assert/strict';
import { existsSync, readFileSync, readdirSync } from 'node:fs';
const root = new URL('../src/BridgeBuilder/', import.meta.url);
const removed = ['Runtime/BridgePrefabLoadGuard.cs',
 'Runtime/BridgeReferenceRecovery.cs', 'Runtime/BridgeLoadFailures.cs',
 'Systems/BridgeMissingAssetSystem.cs', 'Systems/BridgeGenerationSystem.Recovery.cs'];
for (const path of removed) assert(!existsSync(new URL(path, root)), `Memory inspection source remains: ${path}`);
function scan(url) {
 for (const item of readdirSync(url, { withFileTypes: true })) {
  if (['bin', 'obj'].includes(item.name)) continue;
  const path = new URL(item.name + (item.isDirectory() ? '/' : ''), url);
  if (item.isDirectory()) scan(path);
  else if (item.name.endsWith('.cs')) {
   const source = readFileSync(path, 'utf8');
   for (const token of ['BridgePrefabLoadGuard', 'BridgeReferenceRecovery',
      'BridgeLoadFailures', 'BridgeMissingAssetSystem', 'Quarantine(', 'PrepareLegacy(', 'OnErrorOrHigher'])
    assert(!source.includes(token), `${path}: forbidden memory inspection ${token}`);
  }
 }
}
scan(root);
console.log('PASS: startup interception, quarantine, log interception and load rejection removed from runtime');
