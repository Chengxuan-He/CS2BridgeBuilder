// Regression policy: native fee data must flow through generation/migration unchanged.
// This checks manager code, not geometry or game-computed construction totals.
import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const read = path => readFileSync(resolve(root, path), 'utf8');
function files(path) {
    return readdirSync(path, { withFileTypes: true }).flatMap(entry => {
        if (['bin', 'obj'].includes(entry.name)) return [];
        const child = resolve(path, entry.name);
        return entry.isDirectory() ? files(child) : entry.name.endsWith('.cs') ? [child] : [];
    });
}
for (const file of files(resolve(root, 'src/BridgeBuilder'))) {
    const source = readFileSync(file, 'utf8');
    assert(!/\bm_(?:DefaultConstructionCost|DefaultUpkeepCost|ConstructionCost|ElevationCost|UpkeepCost)\s*(?:[+*/-]?=|\+\+|--)/.test(source),
        `Manager must not rewrite native fees: ${file}`);
    assert(!/BridgeNativePrice|BridgeBasePrice|BridgePriceSystem|new BridgeEconomy\b/.test(source), file);
    assert(!/Native (?:Piece|Section|Object) /.test(source), `Pricing-only copies returned: ${file}`);
}
const generation = read('src/BridgeBuilder/Systems/BridgeGenerationSystem.Composition.cs');
assert(generation.includes('BridgeUnlockPolicy.Apply('), 'Independent unlocking must remain');
assert(generation.includes('node.Target.Remove<BridgeConstructionCost>()'), 'Do not persist a manager-dependent cost component');
const migration = read('src/BridgeBuilder/Runtime/BridgePortableMigration.cs');
assert(!migration.includes('m_BaseConstructionCost') && !migration.includes('PlaceableNetData'),
    'Legacy conversion must not require historical or initialized total prices');
assert(migration.includes('BridgeNativeUnlock.Apply(') && migration.includes('node.Target.Remove<BridgeConstructionCost>()'));
assert(!/\.m_(?:Sections|Pieces|SubSections)\s*=/.test(migration), 'Migration must retain the existing native fee graph');
assert(migration.includes('source.asset') && migration.includes('originals') && migration.includes('saved.version != expected.Version'),
    'Preserve identities and transactional rollback');
assert(read('src/BridgeBuilder/Bridges/BridgeConstructionCost.cs').includes('public uint m_BaseConstructionCost;'),
    'Old serialized field must remain readable');
for (const path of ['src/BridgeBuilder/Runtime/BridgeNativePrice.cs', 'src/BridgeBuilder/Bridges/BridgeEconomy.cs',
    'src/BridgeBuilder/Bridges/BridgeBasePrice.cs', 'src/BridgeBuilder/Systems/BridgePriceSystem.cs'])
    assert(!existsSync(resolve(root, path)), `Unused pricing code remains: ${path}`);
console.log('PASS native fees preserved, no pricing copies/overrides, legacy price independence and identity guards retained.');
