/*!
 * Cities: Skylines II UI Module
 * Id: BridgeBuilder
 * Author: BridgeBuilder
 * Version: 0.3.0
 * Dependencies:
 */
const React = window.React;
const ui = window["cs2/ui"];
const api = window["cs2/api"];
const MOD = "BridgeBuilder";

const titleBinding = api.bindValue(MOD, "Title", "BridgeBuilder");
const panelOpenBinding = api.bindValue(MOD, "PanelOpen", false);
const decksBinding = api.bindValue(MOD, "Decks", []);
const stylesBinding = api.bindValue(MOD, "Styles", []);
const bridgesBinding = api.bindValue(MOD, "Bridges", []);
const statusBinding = api.bindValue(MOD, "Status", "");

const trigger = (name, ...args) => api.trigger(MOD, name, ...args);
const h = React.createElement;
const fallbackIcon = "coui://bridgebuilderui/BridgeBuilder.svg";

function Card({ item, selected, onSelect, subtitle }) {
    return h("button", {
        className: `bb-card${selected ? " bb-selected" : ""}`,
        onClick: () => onSelect(item),
        type: "button"
    },
    h("img", { src: item.icon || fallbackIcon }),
    h("span", { className: "bb-card-name" }, item.name),
    subtitle ? h("small", null, subtitle) : null);
}

function CardGrid({ items, selectedId, onSelect, emptyText }) {
    if (!items.length) return h("div", { className: "bb-empty" }, emptyText);
    return h("div", { className: "bb-grid" }, items.map(item =>
        h(Card, { key: item.id || item.prefabName, item,
            selected: (item.id || item.prefabName) === selectedId,
            onSelect,
            subtitle: item.kind || item.source || "" })));
}

function CreateView({ decks, styles, status }) {
    const [doubleDeck, setDoubleDeck] = React.useState(false);
    const [upperId, setUpperId] = React.useState("");
    const [lowerId, setLowerId] = React.useState("");
    const [styleId, setStyleId] = React.useState("");
    const [name, setName] = React.useState("");

    const upperDecks = decks.filter(deck => deck.canBeUpper);
    const availableStyles = styles.filter(style => doubleDeck ? style.supportsDouble : style.supportsSingle);
    const selectedStyle = availableStyles.find(style => style.id === styleId);
    const selectedUpper = upperDecks.find(deck => deck.id === upperId);

    React.useEffect(() => {
        if (styleId && !availableStyles.some(style => style.id === styleId)) setStyleId("");
        if (!doubleDeck) setLowerId("");
    }, [doubleDeck, styles]);

    React.useEffect(() => {
        if (!name && selectedStyle && selectedUpper)
            setName(`${selectedStyle.name} · ${selectedUpper.name}`);
    }, [styleId, upperId]);

    const create = () => {
        if (!upperId || !styleId || (doubleDeck && !lowerId)) return;
        trigger("CreateBridge", upperId, doubleDeck ? lowerId : "", styleId,
            name.trim() || `${selectedStyle?.name || "桥梁"} · ${selectedUpper?.name || "道路"}`);
    };

    return h("div", { className: "bb-workspace" },
        h("section", { className: "bb-road-column" },
            h("h2", null, doubleDeck ? "上层道路" : "道路选择"),
            h("div", { className: "bb-scroll" },
                h(CardGrid, { items: upperDecks, selectedId: upperId,
                    onSelect: item => setUpperId(item.id), emptyText: "未发现可用道路" })),
            doubleDeck ? h(React.Fragment, null,
                h("h2", null, "下层道路或轨道"),
                h("div", { className: "bb-scroll bb-lower" },
                    h(CardGrid, { items: decks, selectedId: lowerId,
                        onSelect: item => setLowerId(item.id), emptyText: "未发现可用下层网络" }))) : null),
        h("section", { className: "bb-right" },
            h("div", { className: "bb-preview" },
                h("img", { src: selectedStyle?.icon || fallbackIcon }),
                h("span", null, selectedStyle?.name || "桥梁原型预览")),
            h("div", { className: "bb-mode-tabs" },
                h("button", { className: !doubleDeck ? "bb-active" : "", onClick: () => setDoubleDeck(false) }, "单层"),
                h("button", { className: doubleDeck ? "bb-active" : "", onClick: () => setDoubleDeck(true) }, "双层")),
            h("div", { className: "bb-style-list bb-scroll" },
                h(CardGrid, { items: availableStyles, selectedId: styleId,
                    onSelect: item => setStyleId(item.id), emptyText: "没有可用的桥梁原型" })),
            h("div", { className: "bb-create-bar" },
                h("input", { value: name, maxLength: 250, placeholder: "游戏中显示的注册名称",
                    onChange: event => setName(event.target.value) }),
                h("button", { className: "bb-primary", disabled: !upperId || !styleId || (doubleDeck && !lowerId), onClick: create }, "创建并建造")),
            h("div", { className: "bb-status" }, status)));
}

function ManageView({ bridges, status }) {
    const [selectedId, setSelectedId] = React.useState("");
    const selected = bridges.find(bridge => bridge.prefabName === selectedId) || bridges[0];
    const [name, setName] = React.useState(selected?.registrationName || "");

    React.useEffect(() => {
        if (selected && selected.prefabName !== selectedId) setSelectedId(selected.prefabName);
        setName(selected?.registrationName || "");
    }, [selected?.prefabName, selected?.registrationName]);

    const cards = bridges.map(bridge => ({ ...bridge, id: bridge.prefabName,
        name: bridge.registrationName, source: bridge.isDoubleDeck ? "双层桥梁" : "单层桥梁" }));

    return h("div", { className: "bb-manage" },
        h("section", { className: "bb-manage-list bb-scroll" },
            h(CardGrid, { items: cards, selectedId: selected?.prefabName,
                onSelect: item => setSelectedId(item.prefabName), emptyText: "尚未创建桥梁" })),
        h("section", { className: "bb-manage-detail" }, selected ? h(React.Fragment, null,
            h("div", { className: "bb-preview" }, h("img", { src: selected.icon || fallbackIcon })),
            h("label", null, "注册名称（游戏中显示）"),
            h("input", { value: name, maxLength: 250, onChange: event => setName(event.target.value) }),
            h("label", null, "Prefab 唯一标识符"),
            h("code", null, selected.prefabName),
            h("p", { className: "bb-note" }, "更名只修改注册名称；UUID 和桥梁构造保持不变。"),
            h("div", { className: "bb-actions" },
                h("button", { className: "bb-primary", disabled: !selected.available,
                    onClick: () => trigger("ActivateBridge", selected.prefabName) }, "建造"),
                h("button", { disabled: !name.trim(),
                    onClick: () => trigger("RenameBridge", selected.prefabName, name.trim()) }, "改名"),
                h("button", { className: "bb-danger",
                    onClick: () => trigger("DeleteBridge", selected.prefabName) }, "删除"))) :
            h("div", { className: "bb-empty" }, "选择一座已创建桥梁"),
            h("div", { className: "bb-status" }, status)));
}

function Panel() {
    const title = api.useValue(titleBinding) || "BridgeBuilder";
    const open = api.useValue(panelOpenBinding);
    const decks = api.useValue(decksBinding) || [];
    const styles = api.useValue(stylesBinding) || [];
    const bridges = api.useValue(bridgesBinding) || [];
    const status = api.useValue(statusBinding) || "";
    const [view, setView] = React.useState("create");
    if (!open) return null;

    return h("div", { className: "bb-panel" },
        h("header", { className: "bb-header" },
            h("div", { className: "bb-title" }, h("img", { src: fallbackIcon }), h("span", null, title)),
            h("nav", null,
                h("button", { className: view === "create" ? "bb-active" : "", onClick: () => setView("create") }, "创建桥梁"),
                h("button", { className: view === "manage" ? "bb-active" : "", onClick: () => setView("manage") }, "管理桥梁"),
                h("button", { onClick: () => trigger("Refresh") }, "刷新")),
            h("button", { className: "bb-close", onClick: () => trigger("TogglePanel") }, "×")),
        view === "create" ? h(CreateView, { decks, styles, status }) : h(ManageView, { bridges, status }));
}

function ToolbarButton() {
    const open = api.useValue(panelOpenBinding);
    return h(ui.Tooltip, { tooltip: "BridgeBuilder" },
        h(ui.Button, { variant: "floating", className: `bb-toolbar${open ? " bb-open" : ""}`,
            onSelect: () => trigger("TogglePanel") }, h("img", { src: fallbackIcon })));
}

const register = registry => {
    registry.append("GameTopLeft", ToolbarButton);
    registry.append("Game", Panel);
};

const hasCSS = true;
export { hasCSS, register as default };
