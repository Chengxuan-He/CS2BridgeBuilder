import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync('src/BridgeBuilder/UI/BridgeBuilder.mjs', 'utf8');
const css = fs.readFileSync('src/BridgeBuilder/UI/BridgeBuilder.css', 'utf8');
const code = source.slice(source.indexOf('function CardIcon('), source.indexOf('\nfunction Card(', source.indexOf('function CardIcon(')));
let state = [], cursor = 0;
const context = vm.createContext({ fallbackIcon: 'fallback.svg',
    React: { useState(initial) { const index = cursor++; if (!(index in state)) state[index] = initial;
        return [state[index], value => { state[index] = value; }]; } },
    h: (type, props) => ({ type, props }) });
vm.runInContext(code, context);
function render(source, ratio = 1) { cursor = 0; return context.CardIcon({ source, ratio }); }
function assertVisible(image, expectedSource, ratio = 1) {
    assert.equal(image.type, 'img');
    assert.equal(image.props.src, expectedSource);
    assert.equal(image.props.hidden, undefined);
    assert.equal(image.props.style.height, '62rem');
    assert.equal(image.props.style.width, `${62 * ratio}rem`);
    assert.equal(image.props.style.visibility, undefined);
    assert.equal(image.props.onLoad, undefined);
    assert.equal(image.props.alt, '');
}
// Regression: no load event and no naturalWidth/naturalHeight properties exist.
for (const [src, ratio] of [['square.png',1], ['landscape.svg',2], ['portrait.svg',.5], ['cached.svg',1]]) {
    state = [];
    assertVisible(render(src, ratio), src, ratio);
    assertVisible(render(src, ratio), src, ratio);
}
for (const ratio of [undefined, 0]) {
    state = []; cursor = 0;
    assertVisible(context.CardIcon({source:'unresolved.png', ratio}), 'fallback.svg');
}
state = [];
let image = render('bad.png');
image.props.onError();
assertVisible(render('bad.png'), 'fallback.svg');
render('bad.png').props.onError();
assertVisible(render('bad.png'), 'fallback.svg');
// The Card keys the component by source, so a changed source gets fresh failure state.
state = [];
assertVisible(render('changed.svg'), 'changed.svg');
state = [];
assertVisible(render(''), 'fallback.svg');
assert.match(source, /h\(CardIcon, \{ key: item\.icon \|\| fallbackIcon, source: item\.icon, ratio: item\.iconRatio \}\)/);
const rule = css.match(/\.bb-card img\s*\{([^}]+)\}/)[1];
assert.match(rule, /height:\s*62rem/);
assert.match(rule, /width:\s*62rem/);
assert.match(rule, /flex-shrink:\s*0/);
assert.doesNotMatch(rule, /max-width|object-fit|visibility|display:\s*none/);
assert.doesNotMatch(code, /naturalWidth|naturalHeight|onLoad|visibility/);
console.log('PASS explicit fixed-height source-ratio widths, unresolved fallback, no load callbacks or natural-size properties, cached/source-change paths. Actual Cohtml layout requires game verification.');
// Full source titles remain accessible even when the compact subtitle is ellipsized.
const cardCode = source.slice(source.indexOf('function Card('), source.indexOf('\nfunction CardGrid('));
const cardContext = vm.createContext({ fallbackIcon: 'fallback.svg', CardIcon() {}, bindCardName() {},
    React: { useRef: () => ({ current: null }), useState: () => [false, () => {}], useEffect() {} },
    h: (type, props, ...children) => ({ type, props, children }) });
vm.runInContext(cardCode, cardContext);
for (const subtitle of ['Mod: GO 224m Viaduct Bridge', 'Mod: Bridge Expansion Pack: B&P',
    'Mod: A Complete Published Title That Is Much Longer Than The Compact Card']) {
    const card = cardContext.Card({ item: { name: 'Bridge', icon: 'bridge.svg' }, subtitle, selected: false, onSelect() {} });
    assert.equal(card.props.title, `Bridge\n${subtitle}`);
    const label = card.children.find(child => child?.type === 'small');
    assert.equal(label.props.title, subtitle);
    assert.equal(label.children[0], subtitle);
}
console.log('PASS complete source title in card/subtitle tooltips, including long names and source changes');
