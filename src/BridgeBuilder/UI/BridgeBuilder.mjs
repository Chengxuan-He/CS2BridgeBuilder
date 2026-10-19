/*!
 * Cities: Skylines II UI Module
 * Id: BridgeBuilder
 * Author: BridgeBuilder
 * Version: 26.10.13
 * Dependencies:
 */
const React = window.React;
const ui = window["cs2/ui"];
const api = window["cs2/api"];
const modding = window["cs2/modding"];
const NativePickerBackdrop = modding.getModule("game-ui/common/panel/panel-backdrop.tsx", "PanelBackdrop");
const NativePickerFocusRoot = modding.getModule("game-ui/common/focus/focus-root.tsx", "FocusRoot");
const NativePickerNavigation = modding.getModule("game-ui/common/focus/auto-navigation-scope.tsx", "AutoNavigationScope");
const NativePickerBack = modding.getModule("game-ui/common/input-events/input-action-consumer.tsx", "BackConsumer");
const nativePickerTheme = modding.getModule("game-ui/menu/widgets/directory-picker/bound-directory-picker.module.scss", "classes");
const NativeWidgetList = modding.getModule("game-ui/editor/widgets/editor-widget-renderer.tsx", "EditorWidgetListRenderer");
const NativeDirectoryTitleContext = modding.getModule("game-ui/menu/widgets/directory-picker/bound-directory-picker.tsx", "DirectoryPickerDialogContext");
const NativeTextInput = modding.getModule("game-ui/common/input/text/text-input.tsx", "TextInput");
const NativeCheckbox = modding.getModule("game-ui/common/input/toggle/checkbox/checkbox.tsx", "Checkbox");
const inputTheme = modding.getModule("game-ui/editor/widgets/item/editor-item.module.scss", "classes");
const textInputProps = { multiline: 1, type: "text", focusKey: window["cs2/input"].FOCUS_DISABLED,
    className: `${inputTheme.input} bb-text-input` };
const MOD = "BridgeBuilder";
const exportSuccessPathBinding = api.bindValue(MOD, "ExportSuccessPath", "");
const exportBrowserBinding = api.bindValue("BridgeBuilderExportBrowser", "children", []);
const exportBrowserOpenBinding = api.bindValue(MOD, "ExportBrowserOpen", false);
const exportFolderBinding = api.bindValue(MOD, "ExportFolder", "");
const exportPathBinding = api.bindValue(MOD, "ExportPath", "");
const exportBusyBinding = api.bindValue(MOD, "ExportBusy", false);

const titleBinding = api.bindValue(MOD, "Title", "BridgeBuilder");
const panelOpenBinding = api.bindValue(MOD, "PanelOpen", false);
const decksBinding = api.bindValue(MOD, "Decks", []);
const stylesBinding = api.bindValue(MOD, "Styles", []);
const bridgesBinding = api.bindValue(MOD, "Bridges", []);
const catalogLoadingBinding = api.bindValue(MOD, "CatalogLoading", true);
const previewImageBinding = api.bindValue(MOD, "PreviewImage", "");
const previewStatusBinding = api.bindValue(MOD, "PreviewStatus", "");
const previewKeyBinding = api.bindValue(MOD, "PreviewKey", "");
const previewLoadingBinding = api.bindValue(MOD, "PreviewLoading", false);

// English fallback is kept in sync with RuntimeUiText by the localization check.
const defaultText = {
    "Cancel": "Cancel",
    "Export": "Export",
    "OpenExportDirectory": "Open folder",
    "ExportSuccess": "Bridge exported to:\n{0}",
    "BrowseDirectory": "Choose folder…",
    "ExportTab": "Export bridge",
    "ExportFolder": "Top-level folder name",
    "ExportFolderInvalid": "Enter a folder name of 1\u2013255 characters. Do not use control characters, < > : \" / \\ | ? *, reserved device names, or a trailing space or period.",
    "ExportPath": "Save location and filename",
    "ExportBusy": "Exporting…",

    "CustomRoad": "+ Custom road",
    "Scanning": "Reading networks and bridge prototypes…",
    "SameDirection": "Same direction",
    "OppositeDirection": "Opposite directions",
    "LowerDeckOpposite": "Opposite directions on upper and lower decks",
    "SearchPlaceholder": "Search...",
    "PreviewAlt": "Isometric 3D model of the generated bridge",
    "PreviewSelect": "Select a road or track and a bridge style for a 3D preview",
    "PreviewImageFailed": "Preview image failed to load. Select the bridge style again.",
    "PreviewDisplaying": "Displaying preview…",
    "PreviewPreparing": "Preparing temporary bridge model…",
    "Bridge": "Bridge",
    "Network": "Network",
    "UpperPicker": "Upper road or track",
    "NetworkPicker": "Road or track selection",
    "NoNetworks": "No roads or tracks available",
    "LowerPicker": "Lower road or track",
    "NoLowerNetworks": "No lower networks available",
    "SingleDeck": "Single deck",
    "DoubleDeck": "Double deck",
    "NoStyles": "No bridge prototypes available",
    "DisplayName": "Bridge name",
    "Create": "Create",
    "CreateBuild": "Create and build",
    "DoubleBridge": "Double-deck bridge",
    "SingleBridge": "Single-deck bridge",
    "NoBridges": "No bridges created yet",
    "PrefabId": "Prefab unique identifier",
    "RenameNote": "Renaming changes only the display name; the UUID and bridge structure stay unchanged.",
    "Build": "Build",
    "Rename": "Rename",
    "Delete": "Delete",
    "SelectBridge": "Select a created bridge",
    "CreateTab": "Create bridge",
    "ManageTab": "Manage bridges",
    "Refresh": "Refresh",
    "Close": "Close",
    "SearchNetworks": "Search roads or tracks…",
    "SearchStyles": "Search bridge styles…",
    "SearchBridges": "Search bridges by name…",
    "AllTypes": "All network types",
    "TypeRoad": "Roads",
    "TypeHighway": "Highways",
    "TypePublicTransport": "Public transport roads",
    "TypePedestrian": "Pedestrian paths",
    "TypeTrain": "Railways",
    "TypeSubway": "Subway tracks",
    "TypeTram": "Tram tracks",
    "NoMatches": "No matching results",
    "AllModes": "Single and double deck",
    "AllStatuses": "All availability",
    "Available": "Available",
    "Unavailable": "Unavailable",
    "AllStyles": "All bridge styles"
};
const textsBinding = api.bindValue(MOD, "Texts", defaultText);
function useText() {
    const text = api.useValue(textsBinding) || defaultText;
    return key => text[key] || defaultText[key] || "";
}

const trigger = (name, ...args) => api.trigger(MOD, name, ...args);
const h = React.createElement;
const fallbackIcon = "coui://bridgebuilderui/BridgeBuilder.svg";

// Cohtml needs the game's scrollbar, not only browser overflow styling.
function ScrollArea({ className = "", children }) {
    return h("div", { className: `bb-scroll-slot ${className}` },
        h(ui.Scrollable, { className: "bb-scroll", vertical: true,
            horizontal: false, trackVisibility: "always" }, children));
}

function Card({ item, selected, onSelect, subtitle }) {
    return h("button", {
        className: `bb-card${selected ? " bb-selected" : ""}`,
        onClick: () => onSelect(item),
        type: "button",
        title: item.name,
        "aria-pressed": selected
    },
    h("img", { src: item.icon || fallbackIcon }),
    h("span", { className: "bb-card-name" }, item.name),
    subtitle ? h("small", null, subtitle) : null);
}

function CardGrid({ items, selectedId, onSelect, emptyText }) {
    const loading = api.useValue(catalogLoadingBinding);
    const t = useText();
    if (loading) return h("div", { className: "bb-list-loading", role: "status", "aria-label": t("Scanning"), "aria-busy": true },
        h("div", { className: "bb-spinner", "aria-hidden": true }));
    if (!items.length) return h("div", { className: "bb-empty" }, emptyText);
    // Explicit rows avoid relying on CSS grid or percentage-based flex wrapping
    // in Cohtml. Empty cells keep the final row the same width as every other row.
    const rows = [];
    for (let start = 0; start < items.length; start += 3) {
        const cells = [];
        for (let column = 0; column < 3; column++) {
            const item = items[start + column];
            cells.push(h("div", { className: "bb-card-cell", key: column },
                item ? h(Card, { item,
                    selected: (item.id || item.prefabName) === selectedId,
                    onSelect, subtitle: item.kind || item.source || "" }) : null));
        }
        rows.push(h("div", { className: "bb-card-row",
            key: items[start].id || items[start].prefabName }, cells));
    }
    return h("div", { className: "bb-grid" }, rows);
}

function matchesSearch(query, ...values) {
    const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
    const text = values.filter(Boolean).join(" ").toLocaleLowerCase();
    return terms.every(term => text.includes(term));
}

function SearchBox({ value, onChange, label }) {
    const t = useText();
    return h("div", { className: "bb-search" },
        h("span", { className: "bb-field-icon bb-search-icon", "aria-hidden": true }),
        h(NativeTextInput, { ...textInputProps, value, placeholder: t("SearchPlaceholder"), "aria-label": label,
        onChange: event => onChange(event.target.value),
        onKeyDown: event => { if (event.key === "Escape") { onChange(""); event.stopPropagation(); } } }));
}

// Cohtml-compatible dropdown: native HTML select popups are not used by the game.
function FilterBox({ value, onChange, options, label }) {
    const [open, setOpen] = React.useState(false);
    return h("div", { className: "bb-filter", onBlur: event => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
    }, onKeyDown: event => { if (event.key === "Escape") { setOpen(false); event.stopPropagation(); } } },
        h("button", { type: "button", "aria-label": label, "aria-haspopup": "listbox", "aria-expanded": open,
            onClick: () => setOpen(!open) },
            h("span", { className: "bb-field-icon bb-filter-icon", "aria-hidden": true }),
            h("span", { className: "bb-field-icon bb-filter-arrow", "aria-hidden": true }),
            h("span", { className: "bb-filter-label" }, options.find(option => option.id === value)?.name || label)),
        open ? h("div", { className: "bb-filter-menu" }, h(ScrollArea, null,
            h("div", { role: "listbox", "aria-label": label }, options.map(option => h("button", {
                key: option.id, type: "button", role: "option", "aria-selected": value === option.id,
                className: value === option.id ? "bb-active" : "",
                onClick: () => { onChange(option.id); setOpen(false); }
            }, option.name))))) : null);
}

function NetworkPicker({ decks, selectedId, onSelect, title, onOpenRoadBuilder }) {
    const t = useText();
    const [query, setQuery] = React.useState("");
    const [type, setType] = React.useState("");
    const options = [{ id: "", name: t("AllTypes") },
        ...["Road", "Highway", "PublicTransport", "Pedestrian", "Train", "Subway", "Tram"]
            .map(id => ({ id, name: t(`Type${id}`) }))];
    const filtered = decks.filter(deck => (!type || deck.networkType === type) &&
        matchesSearch(query, deck.name, deck.id));
    return h("section", { className: "bb-network-picker" },
        h("div", { className: "bb-network-heading" }, h("h2", null, title),
            h("button", { type: "button", className: "bb-custom-road", onClick: onOpenRoadBuilder,
                title: "Road Builder", "aria-label": t("CustomRoad") }, t("CustomRoad"))),
        h("div", { className: "bb-filters" },
            h(SearchBox, { value: query, onChange: setQuery, label: t("SearchNetworks") }),
            h(FilterBox, { value: type, onChange: setType, options, label: t("AllTypes") })),
        h(ScrollArea, { key: `${query}\n${type}` }, h(CardGrid, { items: filtered, selectedId, onSelect,
            emptyText: t(decks.length ? "NoMatches" : "NoNetworks") })));
}

function ModelPreview({ upperId = "", lowerId = "", styleId = "", prefabName = "", lowerDeckOpposite = true, ready }) {
    const t = useText();
    const image = api.useValue(previewImageBinding);
    const status = api.useValue(previewStatusBinding);
    const loading = api.useValue(previewLoadingBinding);
    const resultKey = api.useValue(previewKeyBinding);
    const [imageError, setImageError] = React.useState(false);
    const [loadedImage, setLoadedImage] = React.useState("");
    const [imageSize, setImageSize] = React.useState({ width: 0, height: 0 });
    const frameRef = React.useRef(null);
    React.useEffect(() => setImageError(false), [image]);
    React.useEffect(() => {
        let frame = null;
        const measure = () => {
            const width = frameRef.current?.getBoundingClientRect().width;
            if (!(width > 0) || !Number.isFinite(width)) return;
            // Cohtml resolves absolute percentage heights against the content
            // box, not the padding box. Give both frame and image a REAL height;
            // do not reserve space with height:0 + padding-top and a 100% image.
            setImageSize(previous => previous.width === width ? previous :
                { width, height: width * 512 / 1024 });
        };
        const scheduleMeasure = () => {
            if (frame !== null) cancelAnimationFrame(frame);
            frame = requestAnimationFrame(() => {
                measure();
                // Cohtml layout reads can lag the DOM update by one frame.
                frame = requestAnimationFrame(() => { frame = null; measure(); });
            });
        };
        const observer = typeof window.ResizeObserver === "function"
            ? new window.ResizeObserver(scheduleMeasure) : null;
        if (observer && frameRef.current) observer.observe(frameRef.current);
        window.addEventListener("resize", scheduleMeasure);
        scheduleMeasure();
        return () => {
            if (frame !== null) cancelAnimationFrame(frame);
            window.removeEventListener("resize", scheduleMeasure);
            observer?.disconnect();
        };
    }, [image, ready]);
    const key = prefabName ? `existing\n${prefabName}` : `${upperId}\n${lowerId}\n${styleId}\n${lowerDeckOpposite ? "opposite" : "same"}`;
    React.useEffect(() => {
        // Invalidate the old render immediately; debounce only the expensive build.
        trigger("ClearPreview");
        const timer = ready ? setTimeout(() => prefabName
            ? trigger("PreviewExistingBridge", prefabName)
            : trigger("PreviewBridge", { upperDeckId: upperId, lowerDeckId: lowerId, styleId,
                displayName: "", lowerDeckOpposite }), 250) : null;
        return () => {
            if (timer !== null) clearTimeout(timer);
            trigger("ClearPreview");
        };
    }, [upperId, lowerId, styleId, prefabName, lowerDeckOpposite, ready]);
    const current = ready && resultKey === key;
    const imageReady = current && !!image && loadedImage === image && imageSize.width > 0;
    // No text (including a broken image's alt text) is drawn in the frame.
    // Failures stop the spinner; error dialogs and logging are handled by the backend.
    const failure = !current ? "" : imageError ? t("PreviewImageFailed") : !loading && !image ? status : "";
    const spinning = ready && !failure && (!current || loading || !imageReady);
    return h("div", { className: "bb-preview bb-model-preview" },
        h("div", { className: "bb-preview-image", ref: frameRef, "aria-label": t("PreviewAlt"), "aria-busy": spinning,
            style: imageSize.width > 0 ? { height: `${imageSize.height}px` } : undefined },
            current && image && !imageError && imageSize.width > 0 ? h("img", {
                src: image, width: 1024, height: 512, alt: "",
                style: { width: `${imageSize.width}px`, height: `${imageSize.height}px` },
                onLoad: () => setLoadedImage(image),
                onError: () => { setImageError(true); trigger("PreviewImageFailed", resultKey, image); } }) : null,
            spinning ? h("div", { className: "bb-preview-loading", role: "status", "aria-label": t("PreviewDisplaying") },
                h("div", { className: "bb-spinner", "aria-hidden": true })) : null));
}

function CreateView({ decks, styles, draft, onSaveDraft = () => {} }) {
    const t = useText();
    const [doubleDeck, setDoubleDeck] = React.useState(draft?.doubleDeck ?? false);
    const [upperId, setUpperId] = React.useState(draft?.upperId ?? "");
    const [lowerId, setLowerId] = React.useState(draft?.lowerId ?? "");
    const [styleId, setStyleId] = React.useState(draft?.styleId ?? "");
    const [name, setName] = React.useState(draft?.name ?? "");
    const [customName, setCustomName] = React.useState(draft?.customName ?? false);
    const [styleQuery, setStyleQuery] = React.useState(draft?.styleQuery ?? "");
    const [lowerDeckOpposite, setLowerDeckOpposite] = React.useState(draft?.lowerDeckOpposite ?? true);

    const openRoadBuilder = () => {
        onSaveDraft({ doubleDeck, upperId, lowerId, styleId, name, customName, styleQuery, lowerDeckOpposite });
        trigger("OpenRoadBuilder");
    };

    // The catalogue already contains usable deck networks. Tracks can occupy
    // either deck, including a single-deck bridge; IsRoad is not eligibility.
    const availableStyles = styles.filter(style => doubleDeck ? style.supportsDouble : style.supportsSingle);
    const selectedStyle = availableStyles.find(style => style.id === styleId);
    const selectedUpper = decks.find(deck => deck.id === upperId);
    const selectedLower = doubleDeck ? decks.find(deck => deck.id === lowerId) : null;

    React.useEffect(() => {
        if (styleId && !availableStyles.some(style => style.id === styleId)) setStyleId("");
        if (!doubleDeck) setLowerId("");
    }, [doubleDeck, styles]);

    // Default labels follow the selection/language. Text typed by the player is
    // never translated, and is not used as the prefab's UUID identity.
    const defaultName = selectedStyle && selectedUpper && (!doubleDeck || selectedLower)
        ? [selectedStyle.name, selectedUpper.name, ...(doubleDeck ? [selectedLower.name,
            t(lowerDeckOpposite ? "OppositeDirection" : "SameDirection")] : [])].join("·") : "";
    const displayName = customName ? name : defaultName;

    const create = buildAfterCreate => {
        if (!upperId || !styleId || (doubleDeck && !lowerId)) return;
        trigger(buildAfterCreate ? "CreateAndBuildBridge" : "CreateBridge", {
            upperDeckId: upperId, lowerDeckId: doubleDeck ? lowerId : "", styleId, lowerDeckOpposite,
            displayName: displayName.trim() || defaultName
        });
    };

    return h("div", { className: "bb-workspace" },
        h("section", { className: "bb-road-column" },
            h(NetworkPicker, { key: "upper", decks, selectedId: upperId, onOpenRoadBuilder: openRoadBuilder,
                onSelect: item => setUpperId(item.id), title: doubleDeck ? t("UpperPicker") : t("NetworkPicker") }),
            doubleDeck ? h(NetworkPicker, { key: "lower", decks, selectedId: lowerId, onOpenRoadBuilder: openRoadBuilder,
                onSelect: item => setLowerId(item.id), title: t("LowerPicker") }) : null),
        h("section", { className: "bb-right" },
            h(ModelPreview, { upperId, lowerId: doubleDeck ? lowerId : "", styleId, lowerDeckOpposite,
                ready: !!selectedStyle && !!selectedUpper && (!doubleDeck || !!lowerId) }),
            h("div", { className: "bb-mode-tabs" },
                h("button", { className: !doubleDeck ? "bb-active" : "", onClick: () => setDoubleDeck(false) }, t("SingleDeck")),
                h("button", { className: doubleDeck ? "bb-active" : "", onClick: () => setDoubleDeck(true) }, t("DoubleDeck"))),
            doubleDeck ? h("div", { className: "bb-direction-option" },
                h(NativeCheckbox, { className: "bb-direction-checkbox", checked: lowerDeckOpposite,
                    debugName: t("LowerDeckOpposite"), onChange: setLowerDeckOpposite }),
                h("button", { type: "button", className: "bb-direction-label",
                    "aria-pressed": lowerDeckOpposite, onClick: () => setLowerDeckOpposite(value => !value) },
                    t("LowerDeckOpposite"))) : null,
            h("div", { className: "bb-filters" },
                h(SearchBox, { value: styleQuery, onChange: setStyleQuery, label: t("SearchStyles") })),
            h(ScrollArea, { className: "bb-style-list", key: `${doubleDeck}\n${styleQuery}` },
                h(CardGrid, { items: availableStyles.filter(style => matchesSearch(styleQuery, style.name, style.id)), selectedId: styleId,
                    onSelect: item => setStyleId(item.id), emptyText: t(availableStyles.length ? "NoMatches" : "NoStyles") })),
            h("div", { className: "bb-create-bar" },
                h(NativeTextInput, { ...textInputProps, value: displayName, maxLength: 250, placeholder: t("DisplayName"),
                    onChange: event => { setCustomName(true); setName(event.target.value); } }),
                h("div", { className: "bb-create-actions" },
                    h("button", { disabled: !upperId || !styleId || (doubleDeck && !lowerId), onClick: () => create(false) }, t("Create")),
                    h("button", { className: "bb-primary", disabled: !upperId || !styleId || (doubleDeck && !lowerId), onClick: () => create(true) }, t("CreateBuild"))))));
}

function ManageView({ bridges, styles = [] }) {
    const busy = api.useValue(exportBusyBinding);
    const t = useText();
    const [selectedId, setSelectedId] = React.useState("");
    const selected = bridges.find(bridge => bridge.prefabName === selectedId) || bridges[0];
    const [name, setName] = React.useState(selected?.displayName || "");
    const [query, setQuery] = React.useState("");
    const [mode, setMode] = React.useState("");
    const [availability, setAvailability] = React.useState("");
    const [style, setStyle] = React.useState("");

    React.useEffect(() => {
        if (selected && selected.prefabName !== selectedId) setSelectedId(selected.prefabName);
        setName(selected?.displayName || "");
    // Only switching bridges resets the draft: stale save acknowledgements must
    // not overwrite newer typing.
    }, [selected?.prefabName]);

    const rename = event => {
        const value = event.target.value;
        setName(value);
        if (selected && value.trim())
            trigger("RenameBridge", selected.prefabName, value.trim());
    };

    const cards = bridges.map(bridge => ({ ...bridge, id: bridge.prefabName,
        name: bridge.displayName, source: bridge.isDoubleDeck ? t("DoubleBridge") : t("SingleBridge") }));
    const styleName = id => styles.find(item => item.id === id)?.name || id;
    const filtered = cards.filter(bridge => (!mode || bridge.isDoubleDeck === (mode === "double")) &&
        (!availability || bridge.available === (availability === "available")) && (!style || bridge.styleId === style) &&
        matchesSearch(query, bridge.name, bridge.prefabName, styleName(bridge.styleId)));

    return h("div", { className: "bb-manage" },
        h("section", { className: "bb-manage-list" },
            h("div", { className: "bb-filters" }, h(SearchBox, { value: query, onChange: setQuery, label: t("SearchBridges") })),
            h("div", { className: "bb-filters" },
                h(FilterBox, { value: mode, onChange: setMode, label: t("AllModes"), options: [
                    { id: "", name: t("AllModes") }, { id: "single", name: t("SingleDeck") }, { id: "double", name: t("DoubleDeck") }] }),
                h(FilterBox, { value: availability, onChange: setAvailability, label: t("AllStatuses"), options: [
                    { id: "", name: t("AllStatuses") }, { id: "available", name: t("Available") }, { id: "unavailable", name: t("Unavailable") }] }),
                h(FilterBox, { value: style, onChange: setStyle, label: t("AllStyles"), options: [
                    { id: "", name: t("AllStyles") }, ...Array.from(new Set(bridges.map(item => item.styleId)))
                        .map(id => ({ id, name: styleName(id) }))] })),
            h(ScrollArea, { key: `${query}\n${mode}\n${availability}\n${style}` },
                h(CardGrid, { items: filtered, selectedId: selected?.prefabName,
                    onSelect: item => setSelectedId(item.prefabName), emptyText: t(bridges.length ? "NoMatches" : "NoBridges") }))),
        h("section", { className: "bb-manage-right" },
        h("section", { className: "bb-manage-detail" }, selected ? h(React.Fragment, null,
            h(ModelPreview, { prefabName: selected.prefabName, ready: !!selected.prefabName }),
            h("label", null, t("DisplayName")),
            h(NativeTextInput, { ...textInputProps, value: name, maxLength: 250,
                placeholder: t("DisplayName"), onChange: rename })) :
            h("div", { className: "bb-empty" }, t("SelectBridge"))),
            selected ? h("div", { className: "bb-actions" },
                h("button", { className: "bb-primary", disabled: !selected.available,
                    onClick: () => trigger("ActivateBridge", selected.prefabName) }, t("Build")),
                h("button", { disabled: busy, onClick: () => trigger("PrepareExport", selected.prefabName) }, t(busy ? "ExportBusy" : "Export")),
                h("button", { className: "bb-danger",
                    onClick: () => trigger("DeleteBridge", selected.prefabName) }, t("Delete"))) : null));
}

function ExportSuccessDialog() {
    const t = useText();
    const path = api.useValue(exportSuccessPathBinding);
    if (!path) return null;
    return h(ui.ConfirmationDialog, {
        title: t("ExportTab"), confirm: t("OpenExportDirectory"), cancel: t("Close"),
        onConfirm: () => trigger("OpenExportDirectory"),
        onCancel: () => trigger("DismissExportSuccess")
    }, h("div", { className: "bb-export-success-path" }, t("ExportSuccess").replace("{0}", () => path)));
}

function isValidExportFolder(folder) {
    return !!folder.trim() && folder.length <= 255
        && !/[<>:"/\\|?*\x00-\x1f]/.test(folder)
        && !/[ .]$/.test(folder)
        && !/^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)/i.test(folder);
}

function ExportDialog() {
    const t = useText();
    const browserOpen = api.useValue(exportBrowserOpenBinding);
    const browserWidgets = api.useValue(exportBrowserBinding);
    const requested = api.useValue(exportPathBinding);
    const defaultFolder = api.useValue(exportFolderBinding);
    const [folder, setFolder] = React.useState("");
    React.useEffect(() => setFolder(defaultFolder || ""), [!!requested, defaultFolder]);
    const [path, setPath] = React.useState("");
    React.useEffect(() => setPath(requested || ""), [requested]);
    if (!requested) return null;
    // Match the native DirectoryPickerDialog container/focus hierarchy. A message
    // dialog's centered text slot cannot lay out the directory picker's page view.
    if (browserOpen) return h(NativePickerBackdrop, { className: `${nativePickerTheme.backdrop} bb-export-browser-layer`, onMouseDown: () => trigger("CloseExportBrowser") },
        h("div", { className: nativePickerTheme.panel },
            h(NativePickerBack, { onAction: () => trigger("CloseExportBrowser") },
                h(NativePickerFocusRoot, null,
                    h(NativePickerNavigation, null,
                        h("div", { className: nativePickerTheme.header }, t("BrowseDirectory")),
                        h(NativeWidgetList, { parent: { group: "BridgeBuilderExportBrowser", path: [] }, data: browserWidgets }))))));
    return h(ui.ConfirmationDialog, {
            title: t("ExportTab"), confirm: t("Export"), cancel: t("Cancel"),
            disableContinue: !path.trim() || !isValidExportFolder(folder),
            onConfirm: () => { if (path.trim() && isValidExportFolder(folder)) trigger("ConfirmExport", path.trim(), folder); },
            onCancel: () => trigger("CancelExport")
        }, h("div", { className: "bb-export-fields" },
            h("label", null, t("ExportPath")),
            h("div", { className: "bb-export-path-row" },
                h(NativeTextInput, { ...textInputProps, multiline: undefined, value: path, maxLength: 4096, onChange: event => setPath(event.target.value) }),
                h(ui.Tooltip, { tooltip: t("BrowseDirectory") },
                    h(ui.Button, { className: "bb-export-browse", src: "Media/Editor/Folder.svg", tinted: true,
                        "aria-label": t("BrowseDirectory"), onSelect: () => trigger("BrowseExportDirectory", path.trim()) }))),
            h("label", null, t("ExportFolder")),
            h(NativeTextInput, { ...textInputProps, multiline: undefined, value: folder, maxLength: 255, onChange: event => setFolder(event.target.value) }),
            !isValidExportFolder(folder) ? h("div", { className: "bb-export-folder-error", role: "alert" }, t("ExportFolderInvalid")) : null));
}

function Panel() {
    const t = useText();
    const title = api.useValue(titleBinding) || "BridgeBuilder";
    const open = api.useValue(panelOpenBinding);
    const decks = api.useValue(decksBinding) || [];
    const styles = api.useValue(stylesBinding) || [];
    const bridges = api.useValue(bridgesBinding) || [];
    const [view, setView] = React.useState("create");
    // Panel stays mounted when closed; retain the handoff draft while Road Builder is open.
    const [draft, setDraft] = React.useState(null);
    if (!open) return null;

    return h("div", { className: "bb-panel" },
        h("header", { className: "bb-header" },
            h("div", { className: "bb-title" }, h("span", { className: "bb-white-icon", "aria-hidden": true }), h("span", null, title)),
            h("nav", null,
                h("button", { title: t("CreateTab"), className: view === "create" ? "bb-active" : "", onClick: () => setView("create") }, t("CreateTab")),
                h("button", { title: t("ManageTab"), className: view === "manage" ? "bb-active" : "", onClick: () => setView("manage") }, t("ManageTab"))),
            h("button", { className: "bb-close", title: t("Close"), "aria-label": t("Close"), onClick: () => trigger("TogglePanel") }, "×")),
        view === "create" ? h(CreateView, { decks, styles, draft, onSaveDraft: setDraft }) : h(ManageView, { bridges, styles }));
}

function ToolbarButton() {
    const open = api.useValue(panelOpenBinding);
    const title = api.useValue(titleBinding) || "BridgeBuilder";
    return h(ui.Tooltip, { tooltip: title },
        h(ui.Button, { variant: "floating", className: `bb-toolbar${open ? " bb-open" : ""}`,
            onSelect: () => trigger("TogglePanel") }, h("span", { className: "bb-white-icon", "aria-hidden": true })));
}

// Native directory fields eagerly share one title. Scope the correction to our
// two settings fields, and publish their title when the player interacts with them.
function withDirectoryTitle(Original) {
    return function BridgeDirectoryField(props) {
        const context = React.useContext(NativeDirectoryTitleContext);
        const quietContext = React.useMemo(() => ({ setHeader: () => {} }), []);
        const owned = typeof props.path === "string" &&
            /(?:^|\.)BridgeSetting\.(DefaultExportDirectory|RecoveryCopyLocation)$/.test(props.path);
        if (!owned) return h(Original, props);
        const activate = () => context.setHeader(props.props.displayName);
        return h("div", { className: "bb-directory-option-host", onMouseDownCapture: activate,
            onClickCapture: activate, onFocusCapture: activate },
            h(NativeDirectoryTitleContext.Provider, { value: quietContext }, h(Original, props)));
    };
}

const register = uiSlots => {
    uiSlots.extend("game-ui/menu/widgets/option-widget-renderer.tsx", "optionsWidgetComponents", components => ({
        ...components,
        "Game.UI.Widgets.DirectoryPickerField": withDirectoryTitle(components["Game.UI.Widgets.DirectoryPickerField"])
    }));
    uiSlots.append("GameTopLeft", ToolbarButton);
    uiSlots.append("Game", Panel);
    uiSlots.append("Game", ExportDialog);
    uiSlots.append("Game", ExportSuccessDialog);
};

const hasCSS = true;
export { hasCSS, register as default };
