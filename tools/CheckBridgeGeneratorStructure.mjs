// Source-architecture checks only. Does not create, render or simulate bridge geometry.
import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve, dirname } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const bridges = 'src/BridgeBuilder/Bridges';
const read = p => readFileSync(resolve(root, p), 'utf8');
const definitions = read(`${bridges}/BridgeStyleDefinitions.cs`);
const admitted = definitions.match(/ImplementedStyles = new\(StringComparer.Ordinal\)\s*\{([^}]+)\}/)[1];
const ids = [...admitted.matchAll(/"([^"]+)"/g)].map(x => x[1]).sort();
const router = read(`${bridges}/BridgeGeneratorRouter.cs`);
const routes = [...router.matchAll(/\["([^"]+)"\] = \(report, towers\) => new (\w+)\(report, towers\)/g)];
assert.deepEqual(routes.map(x => x[1]).sort(), ids, 'Every supported style needs exactly one explicit route');
assert.equal(new Set(routes.map(x => x[2])).size, ids.length, 'Each style has its own implementation');
assert(router.includes('StringComparer.Ordinal'), 'Route by stable exact IDs');
assert(router.includes('return null;'), 'Unknown styles must be refused');
for (const [, id, type] of routes) {
  const implementation = read(`${bridges}/Types/${id}/${type}.cs`);
  assert(implementation.includes(`class ${type} : BridgeGeneratorBase`));
  assert(implementation.includes(`StyleId => "${id}"`));
}
const generation = read('src/BridgeBuilder/Systems/BridgeGenerationSystem.cs');
assert(generation.includes('BridgeGeneratorRouter.Create(style.Id, report, towers)'));
assert(generation.includes('if (composer == null) return false;'));
assert(generation.includes('composer.StructureFollowsUpperAuxiliary ? upper : main'));
assert(!generation.includes('new BridgeComposer('));
assert(read(`${bridges}/Common/BridgeGeneratorBase.cs`).includes('string.Equals(style.Id, StyleId, StringComparison.Ordinal)'));
assert.equal(readdirSync(resolve(root, `${bridges}/Common/Geometry`)).filter(x => /^TowerFactory.*\.cs$/.test(x)).length, 9);
assert.equal(readdirSync(resolve(root, `${bridges}/Common/Geometry`)).filter(x => /^TowerWidening.*\.cs$/.test(x)).length, 6);
for (const obsolete of ['BridgeComposer.cs', 'TowerFactory.cs', 'TowerWidening.cs', 'DoubleDeckComposer.cs'])
  assert(!existsSync(resolve(root, bridges, obsolete)), `Old monolith remains: ${obsolete}`);
console.log(`PASS ${ids.length} exact style routes, independent inherited implementations, shared generation pipeline and split geometry sources`);
