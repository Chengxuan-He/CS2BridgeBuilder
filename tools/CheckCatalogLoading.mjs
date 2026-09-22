// UI state regression only; no bridge geometry or synthetic mesh tests.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const source = fs.readFileSync(new URL('../src/BridgeBuilder/UI/BridgeBuilder.mjs', import.meta.url), 'utf8');
const values = { CatalogLoading: true };
const context = vm.createContext({ window: {
    React: { createElement: (type, props, ...children) => ({ type, props, children }) },
    'cs2/ui': {},
    'cs2/api': {
        bindValue: (_group, key, fallback) => ({ key, fallback }),
        useValue: binding => values[binding.key] ?? binding.fallback
    },
    'cs2/modding': { getModule: () => ({}) },
    'cs2/input': { FOCUS_DISABLED: 0 }
} });
vm.runInContext(source.replace('export { hasCSS, register as default };', ''), context);
const render = items => {
    context.items = items;
    return vm.runInContext('CardGrid({ items, emptyText: "EMPTY" })', context);
};
for (const items of [[], [{ id: 'existing', name: 'Existing bridge' }]]) {
    const result = render(items);
    assert.equal(result.props.className, 'bb-list-loading');
    assert.equal(result.props['aria-busy'], true);
    assert.equal(result.children[0].props.className, 'bb-spinner');
}
values.CatalogLoading = false;
assert.equal(render([]).props.className, 'bb-empty');
assert.equal(render([]).children[0], 'EMPTY');
assert.equal(render([{ id: 'ready', name: 'Ready' }]).props.className, 'bb-grid');
values.CatalogLoading = true;
assert.equal(render([]).props.className, 'bb-list-loading');
console.log('Catalog UI checks passed: initial loading, refresh, populated, and genuinely empty states.');
