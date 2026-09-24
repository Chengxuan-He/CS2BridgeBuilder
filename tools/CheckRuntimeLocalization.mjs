// Non-visual localization/selection regression checks. Does not load the game or generate geometry.
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import vm from "node:vm";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const read = file => readFileSync(resolve(root, file), "utf8");
const catalog = read("src/BridgeBuilder/Settings/RuntimeUiText.cs");
const strings = value => [...value.matchAll(/"(?:[^"\\]|\\.)*"/g)].map(match => JSON.parse(match[0]));
const locales = strings(catalog.match(/string\[\] LocaleIds\s*=\s*\{([^}]+)\}/)[1]);
const entries = new Map();
for (const match of catalog.matchAll(/\["(\w+)"\]\s*=\s*new\[\]\s*\{([\s\S]*?)\n\s*\},/g)) {
    assert(!entries.has(match[1]), `Duplicate key: ${match[1]}`);
    const row = strings(match[2]);
    assert.equal(row.length, locales.length, `Missing locale: ${match[1]}`);
    const placeholders = value => [...value.matchAll(/\{\d+\}/g)].map(x => x[0]).sort();
    row.forEach((value, index) => {
        assert(value.trim(), `${match[1]} empty in ${locales[index]}`);
        assert.deepEqual(placeholders(value), placeholders(row[0]), `${match[1]} arguments in ${locales[index]}`);
        assert(!/[{}]/.test(value.replace(/\{\d+\}/g, "")), `Invalid format: ${match[1]}`);
    });
    entries.set(match[1], row);
}
assert(entries.size >= 78);
const known = key => assert(entries.has(key), `Untranslated key: ${key}`);
const uiSource = read("src/BridgeBuilder/UI/BridgeBuilder.mjs");
for (const match of uiSource.matchAll(/\bt\("(\w+)"\)/g)) known(match[1]);
const consumers = [
    "UI/BridgeBuilderUISystem.cs", "Systems/BridgeGenerationSystem.cs",
    "Runtime/BridgePreviewRenderer.cs", "Runtime/BridgePreviewState.cs", "Bridges/BridgePrototypeSource.cs"
].map(path => read(`src/BridgeBuilder/${path}`));
for (const source of [uiSource, ...consumers.slice(0, 4)])
    assert(!/\p{Script=Han}/u.test(source), "Hardcoded Chinese remains in runtime UI");
for (const source of consumers) {
    for (const match of source.matchAll(/(?:RuntimeUiText\.Get|BridgeRuntimeRequests\.Complete)\("(\w+)"/g)) known(match[1]);
    for (const match of source.matchAll(/BridgePreviewState\.Publish\([^\n]*, "(\w+)"\)/g)) known(match[1]);
}

// Run the actual UI functions with an in-memory binding adapter. This checks
// strings/handlers only, not Cohtml layout, game loading, or rendered geometry.
let state = [], cursor = 0, activeTexts, triggers = [];
const bindings = { PanelOpen: true, Title: "Bridge Builder", Decks: [], Styles: [], Bridges: [], Status: "" };
const context = vm.createContext({ window: {
    React: {
        createElement: (type, props, ...children) => ({ type, props: props || {}, children }),
        useState: initial => {
            const index = cursor++;
            if (state[index] === undefined) state[index] = initial;
            return [state[index], value => { state[index] = typeof value === "function" ? value(state[index]) : value; }];
        },
        useEffect: () => {}, useRef: () => ({ current: null })
    },
    "cs2/ui": {},
    "cs2/input": { FOCUS_DISABLED: "focus-disabled" },
    "cs2/modding": { getModule: (path, name) => {
        if (path === "game-ui/common/input/text/text-input.tsx" && name === "TextInput") return "input";
        if (path === "game-ui/common/input/toggle/checkbox/checkbox.tsx" && name === "Checkbox") return "native-checkbox";
        if (path === "game-ui/editor/widgets/item/editor-item.module.scss" && name === "classes") return { input: "native-input-theme" };
        assert.fail(`Unexpected native module: ${path} / ${name}`);
    } },
    "cs2/api": {
        bindValue: (group, key, fallback) => ({ key, fallback }),
        useValue: binding => binding.key === "Texts" ? activeTexts : bindings[binding.key] ?? binding.fallback,
        trigger: (...args) => triggers.push(args)
    }
} });
vm.runInContext(uiSource.replace("export { hasCSS, register as default };", ""), context);
const fallback = vm.runInContext("defaultText", context);
for (const [key, value] of Object.entries(fallback)) { known(key); assert.equal(value, entries.get(key)[0]); }
const collect = tree => {
    const nodes = [];
    const walk = node => {
        if (!node || typeof node !== "object") return;
        if (Array.isArray(node)) { node.forEach(walk); return; }
        nodes.push(node); (node.children || []).forEach(walk);
    };
    walk(tree); return nodes;
};
const render = expression => { cursor = 0; return collect(vm.runInContext(expression, context)); };
context.decks = ["Road", "Highway", "PublicTransport", "Pedestrian", "Train", "Subway", "Tram"]
    .map(kind => ({ id: kind, name: `asset-${kind}`, kind, networkType: kind }));
context.styles = [{ id: "style", name: "translated style", supportsSingle: true, supportsDouble: true }];
const custom = "My bridge 我的桥 {0}";
const prefabId = "r11111111-2222-3333-4444-555555555555";
context.bridges = [{ prefabName: prefabId, registrationName: custom, upperDeckId: "Train", lowerDeckId: "", styleId: "style", available: true }];

for (let column = 0; column < locales.length; column++) {
    activeTexts = Object.fromEntries([...entries].map(([key, row]) => [key, row[column]]));
    state = [];
    const search = render('SearchBox({ value: "query", onChange: () => {}, label: "Search networks" })');
    const searchInput = search.find(n => n.type === "input");
    assert.equal(searchInput.props.placeholder, activeTexts.SearchPlaceholder);
    assert.equal(searchInput.props["aria-label"], "Search networks");
    assert(searchInput.props.className.includes("native-input-theme"));
    assert.equal(searchInput.props.multiline, 1);
    assert.equal(searchInput.props.focusKey, "focus-disabled");
    assert(search.findIndex(n => n.props.className === "bb-field-icon bb-search-icon") < search.indexOf(searchInput));
    state = ["create"];
    const panel = render("Panel()");
    for (const key of ["CreateTab", "ManageTab"])
        assert(panel.some(n => n.type === "button" && n.children.includes(activeTexts[key])), `${key}: ${locales[column]}`);
    assert(!panel.some(n => n.type === "button" && n.children.includes(activeTexts.Refresh)), "No manual refresh button");
    assert(panel.some(n => n.props["aria-label"] === activeTexts.Close));
    state = [];
    const preview = render('ModelPreview({ upperId: "", lowerId: "", styleId: "", ready: false })');
    assert(!preview.some(n => n.children.some(child => typeof child === "string" && child.length)), "Preview must have no visible text");
    assert(!preview.some(n => n.props.className === "bb-spinner"), "Idle preview must not spin");
    for (const double of [false, true]) for (const kind of ["Train", "Subway", "Tram"]) {
        state = [double, kind, double ? "Road" : "", "style", custom, true];
        const create = render('CreateView({ decks, styles, status: "" })');
        assert(create.some(n => n.type?.name === "ModelPreview" && n.props.ready && n.props.upperId === kind));
        assert(create.some(n => n.type === "input" && n.props.value === custom));
        for (const [label, action] of [["Create", "CreateBridge"], ["CreateBuild", "CreateAndBuildBridge"]]) {
            const button = create.find(n => n.type === "button" && n.children.includes(activeTexts[label]));
            assert(button && !button.props.disabled);
            triggers = []; button.props.onClick();
            assert.deepEqual(JSON.parse(JSON.stringify(triggers)), [["BridgeBuilder", action, {
                upperDeckId: kind, lowerDeckId: double ? "Road" : "", styleId: "style",
                lowerDeckOpposite: true, registrationName: custom
            }]],
                "Only enqueue the chosen action; closing/activation must wait for backend success");
        }
    }
    for (const lower of ["Road", "Train"]) {
        state = [true, "Train", lower, "style", "", false];
        const create = render('CreateView({ decks, styles, status: "" })');
        const baseName = `translated style·asset-Train·asset-${lower}`;
        const expected = `${baseName}·${activeTexts.OppositeDirection}`;
        assert.equal(create.find(n => n.type === "input").props.value, expected,
            "Default name must include both decks, even when they use the same network");
        const checkbox = create.find(n => n.type === "native-checkbox");
        assert(checkbox && checkbox.props.checked);
        assert.equal(checkbox.props.debugName, activeTexts.LowerDeckOpposite);
        for (const key of ["Create", "CreateBuild"]) {
            triggers = [];
            create.find(n => n.type === "button" && n.children.includes(activeTexts[key])).props.onClick();
            assert.equal(triggers[0][2].registrationName, expected);
        }
        state[4] = "   "; state[5] = true;
        triggers = [];
        render('CreateView({ decks, styles, status: "" })')
            .find(n => n.type === "button" && n.children.includes(activeTexts.Create)).props.onClick();
        assert.equal(triggers[0][2].registrationName, expected, "Blank custom name uses the full default");
        state[5] = false;
        checkbox.props.onChange(false);
        const sameDirection = render('CreateView({ decks, styles, status: "" })');
        assert.equal(sameDirection.find(n => n.type === "input").props.value,
            `${baseName}·${activeTexts.SameDirection}`, "Default direction suffix must follow the checkbox and locale");
        triggers = [];
        sameDirection.find(n => n.type === "button" && n.children.includes(activeTexts.Create)).props.onClick();
        assert.equal(triggers[0][2].registrationName, `${baseName}·${activeTexts.SameDirection}`);
        assert.equal(triggers[0][2].lowerDeckOpposite, false);
        state[4] = custom; state[5] = true;
        checkbox.props.onChange(true);
        assert.equal(render('CreateView({ decks, styles, status: "" })')
            .find(n => n.type === "input").props.value, custom, "Direction changes must not overwrite a custom name");
    }
    state = [false, "Road", "", "style", "", false];
    assert.equal(render('CreateView({ decks, styles, status: "" })')
        .find(n => n.type === "input").props.value, "translated style·asset-Road", "Single decks have no direction suffix");
    state = [prefabId, custom];
    const manage = render('ManageView({ bridges, status: "" })');
    assert(!manage.some(n => n.children.includes(prefabId)), "UUID must not be visible");
    assert(!manage.some(n => n.type === "button" && n.children.includes(activeTexts.Rename)));
    for (const key of ["Build", "Delete"])
        assert(manage.some(n => n.type === "button" && n.children.includes(activeTexts[key])));
    assert(manage.some(n => n.type === "input" && n.props.value === custom));
    const nameField = manage.find(n => n.type === "input" && n.props.value === custom);
    triggers = [];
    nameField.props.onChange({ target: { value: "Changed bridge" } });
    assert.deepEqual(triggers, [["BridgeBuilder", "RenameBridge", prefabId, "Changed bridge"]]);
    triggers = [];
    nameField.props.onChange({ target: { value: "   " } });
    assert.equal(triggers.length, 0, "Empty typing must not erase the persisted bridge name");
    triggers = [];
    manage.find(n => n.type === "button" && n.children.includes(activeTexts.Build)).props.onClick();
    assert.deepEqual(triggers, [["BridgeBuilder", "ActivateBridge", prefabId]], "Build reuses the existing prefab");
    const detailScroll = manage.find(n => n.props.className === "bb-detail-scroll");
    assert(!collect(detailScroll).some(n => n.props.className === "bb-actions"), "Build actions must stay outside the scroller");
    console.log(`PASS ${locales[column]}: ${entries.size} keys, format arguments, create/manage labels, track selections, custom name/UUID`);
}
activeTexts = {};
state = ["create"];
assert(render("Panel()").some(n => n.children.includes(entries.get("CreateTab")[0])), "English UI fallback");
console.log("PASS English UI fallback; no game or bridge geometry was created.");
state = [];
const filter = render('FilterBox({ value: "", onChange: () => {}, options: [{ id: "", name: "All" }], label: "Filter" })');
const filterButton = filter.find(n => n.type === "button");
assert.deepEqual(filterButton.children.map(n => n.props.className),
    ["bb-field-icon bb-filter-icon", "bb-field-icon bb-filter-arrow", "bb-filter-label"]);
assert(!uiSource.includes("▾"), "Dropdown arrow must not depend on a font glyph");
assert(!uiSource.includes('h("input"'), "Every editable field must use the native TextInput");
for (const icon of ["Search", "Filter", "ArrowDown"]) {
    assert(read(`assets/BridgeBuilder${icon}.svg`).includes('<svg '));
    assert(read("src/BridgeBuilder/UI/BridgeBuilder.css").includes(`coui://bridgebuilderui/BridgeBuilder${icon}.svg`));
}
console.log("PASS native input theme, localized placeholder, left-hand SVG search/filter/arrow icons");
const dropdownCss = read("src/BridgeBuilder/UI/BridgeBuilder.css");
for (const selector of [".bb-filter > button", ".bb-filter-menu"]) {
    const rule = dropdownCss.slice(dropdownCss.indexOf(`${selector} {`)).split("}")[0];
    assert(rule.includes("box-sizing: border-box;"), `${selector} must include borders in its width`);
    assert(rule.includes("width: 100%;"), `${selector} must fill the same dropdown container`);
    assert(!rule.includes("width: 220rem"), "Dropdowns must not use a fixed width");
}
assert.equal(entries.get("BaseGame")[locales.indexOf("zh-HANS")], "基础版游戏");
assert.equal(entries.get("BaseGame")[locales.indexOf("zh-HANT")], "基礎版遊戲");

for (const selection of [[false, "", "", "style"], [false, "Road", "", ""], [true, "Road", "", "style"]]) {
    state = selection;
    const nodes = render('CreateView({ decks, styles, status: "" })');
    for (const label of ["Create", "Create and build"]) {
        const button = nodes.find(n => n.type === "button" && n.children.includes(label));
        assert(button.props.disabled, "Incomplete selection must disable both creation actions");
        triggers = []; button.props.onClick();
        assert.equal(triggers.length, 0);
    }
}
context.bridges[0].available = false;
state = [prefabId, custom];
assert(render('ManageView({ bridges, status: "" })')
    .find(n => n.type === "button" && n.children.includes("Build")).props.disabled);
context.bridges[0].available = true;
console.log("PASS separate create/create-and-build actions, existing-prefab placement and unavailable selections");

for (const view of ['CreateView({ decks, styles, status })', 'ManageView({ bridges, status })']) {
    state = [];
    context.status = "";
    assert(!render(view).some(n => n.props.className === "bb-status"), "No empty footer after routine actions");
    context.status = "Action failed";
    assert(render(view).some(n => n.props.className === "bb-status" && n.props.role === "alert"
        && n.children.includes("Action failed")), "Actual failures must remain visible");
}
const generation = read("src/BridgeBuilder/Systems/BridgeGenerationSystem.cs");
const activation = generation.split("private bool ActivatePrefab(string prefabName)")[1]
    .split("private void RenameRuntimeBridge")[0];
const lockCheck = activation.indexOf("BridgeUnlockPolicy.TryPrepareBuild(prefab");
const popup = activation.indexOf('Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("ActivateLocked"))');
const tool = activation.indexOf(".ActivatePrefabTool(prefab)");
const close = activation.indexOf("?.CloseForBuild()");
assert(lockCheck >= 0 && popup > lockCheck && tool > popup && close > tool,
    "Locked activation must show the localized dialog before any tool change; close only after activation");
assert(activation.slice(popup, tool).includes("return false;"));
assert(activation.slice(tool, close).includes("return false;"));
console.log("PASS conditional error footer and shared lock-dialog/tool-activation guard ordering (source check)");

// Pure UI filtering/state checks: no geometry, renderer or game is loaded.
activeTexts = Object.fromEntries([...entries].map(([key, row]) => [key, row[0]]));
for (const kind of ["Road", "Highway", "PublicTransport", "Pedestrian", "Train", "Subway", "Tram"]) {
    known(`Type${kind}`);
    state = ["ASSET", kind];
    const nodes = render('NetworkPicker({ decks, selectedId: "Road", onSelect: () => {}, title: "Networks" })');
    const grid = nodes.find(n => n.type?.name === "CardGrid");
    assert.equal(grid.props.items.length, 1);
    assert.equal(grid.props.items[0].id, kind);
    assert.equal(grid.props.selectedId, "Road", "Filtering must not change selection");
}
state = ["not-found", ""];
assert.equal(render('NetworkPicker({ decks, selectedId: "Road", onSelect: () => {}, title: "Networks" })')
    .find(n => n.type?.name === "CardGrid").props.items.length, 0);
state = [false, "Road", "", "style", custom, true, "TRANSLATED", ""];
assert.equal(render('CreateView({ decks, styles, status: "" })').find(n => n.type?.name === "CardGrid").props.items.length, 1);
context.bridges.push({ prefabName: "r222", registrationName: "Unavailable double", isDoubleDeck: true, available: false, styleId: "other" });
state = [prefabId, custom, "R222", "double", "unavailable", "other", ""];
const filteredManage = render('ManageView({ bridges, styles, status: "" })');
assert.equal(filteredManage.find(n => n.type?.name === "CardGrid").props.items[0].prefabName, "r222");
assert.equal(filteredManage.find(n => n.type?.name === "ModelPreview").props.prefabName, prefabId,
    "Filtering must not reselect or regenerate the existing bridge");
bindings.PreviewKey = "Road\n\nstyle\nopposite";
bindings.PreviewLoading = true;
bindings.PreviewStatus = "Loading";
state = [];
assert(render('ModelPreview({ upperId: "Road", styleId: "style", ready: true })').some(n => n.props.className === "bb-spinner"));
bindings.PreviewLoading = false;
bindings.PreviewStatus = "Failed";
state = [];
const failedPreview = render('ModelPreview({ upperId: "Road", styleId: "style", ready: true })');
assert(!failedPreview.some(n => n.props.className === "bb-spinner"));
assert(!failedPreview.some(n => n.children.some(child => typeof child === "string" && child.length)));
bindings.PreviewImage = "data:image/png;base64,test";
state = [false, bindings.PreviewImage, { width: 1024, height: 512 }];
const readyPreview = render('ModelPreview({ upperId: "Road", styleId: "style", ready: true })');
assert(readyPreview.some(n => n.type === "img" && n.props.alt === ""));
assert(!readyPreview.some(n => n.props.className === "bb-spinner"));
assert(!readyPreview.some(n => n.children.some(child => typeof child === "string" && child.length)));
console.log("PASS network/style/management filtering, selection retention, text-free preview and loading/failure state");

for (const double of [false, true]) {
    state = [double, "Road", double ? "Train" : "", "style", custom, true, "", "", true];
    const create = render('CreateView({ decks, styles, status: "" })');
    const toggle = create.find(n => n.type === "native-checkbox");
    assert.equal(!!toggle, double, "Direction option is only available with two decks");
    if (!double) continue;
    assert.equal(toggle.props.checked, true);
    toggle.props.onChange(false);
    const changed = render('CreateView({ decks, styles, status: "" })');
    assert.equal(changed.find(n => n.type === "native-checkbox").props.checked, false);
    assert.equal(changed.find(n => n.type?.name === "ModelPreview").props.lowerDeckOpposite, false);
    for (const label of ["Create", "Create and build"]) {
        triggers = [];
        changed.find(n => n.type === "button" && n.children.includes(label)).props.onClick();
        assert.equal(triggers[0][2].lowerDeckOpposite, false);
        assert.equal(triggers[0][2].lowerDeckId, "Train");
    }
    changed.find(n => n.props.className === "bb-direction-label").props.onClick();
    assert.equal(render('CreateView({ decks, styles, status: "" })')
        .find(n => n.type === "native-checkbox").props.checked, true, "Clicking the label must also toggle the checkbox");
}
assert(uiSource.includes('[upperId, lowerId, styleId, prefabName, lowerDeckOpposite, ready]'));
console.log("PASS direction toggle, preview selection invalidation and immutable creation recipes");

// Optional Road Builder handoff must preserve both decks and the player's name.
context.savedDraft = null;
state = [true, "Road", "Train", "style", custom, true, "filter", "", false];
triggers = [];
const handoff = render('CreateView({ decks, styles, status: "", onSaveDraft: value => { savedDraft = value; } })');
const pickers = handoff.filter(n => n.type?.name === "NetworkPicker");
assert.equal(pickers.length, 2);
pickers[1].props.onOpenRoadBuilder();
assert.deepEqual(triggers[0], ["BridgeBuilder", "OpenRoadBuilder"]);
assert.equal(context.savedDraft.name, custom);
assert.equal(context.savedDraft.lowerId, "Train");
state = [];
const restored = render('CreateView({ decks, styles, status: "", draft: savedDraft })');
assert.equal(restored.find(n => n.type?.name === "ModelPreview").props.upperId, "Road");
assert.equal(restored.find(n => n.type?.name === "ModelPreview").props.lowerId, "Train");
assert.equal(restored.find(n => n.type === "native-checkbox").props.checked, false);
assert.equal(restored.find(n => n.type === "input").props.value, custom);
state = [];
triggers = [];
const emptyPicker = render('NetworkPicker({ decks: [], title: "Networks", onOpenRoadBuilder: () => trigger("OpenRoadBuilder") })');
emptyPicker.find(n => n.props.className === "bb-custom-road").props.onClick();
assert.deepEqual(triggers[0], ["BridgeBuilder", "OpenRoadBuilder"]);
console.log("PASS Road Builder entry with empty catalogue and single/double-deck draft handoff");
