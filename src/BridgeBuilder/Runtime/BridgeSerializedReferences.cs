using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BridgeBuilder.Runtime;

/// <summary>Read-only Odin text index. Never deserialize a second live prefab or rewrite type/object IDs.</summary>
internal sealed class BridgeSerializedReferences
{
    internal sealed class Value
    {
        internal readonly Dictionary<string, Value> Fields = new(StringComparer.Ordinal);
        internal readonly List<Value> Items = new();
        internal string Text = string.Empty;
        internal string Token = string.Empty;
    }
    private readonly Dictionary<string, Value> _objects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _types = new(StringComparer.Ordinal);
    private readonly string _text;
    private int _offset;
    private bool _valid = true;
    private Value _root = new();
    private BridgeSerializedReferences(string text) => _text = text;

    internal static bool TryRead(string text, out BridgeSerializedReferences result)
    {
        result = new(text);
        if (text.Length > 16 * 1024 * 1024) return false;
        result._root = result.Read(0);
        result.Space();
        return result._valid && result._offset == text.Length;
    }
    internal string Name => Field(_root, "name")?.Text ?? string.Empty;

    // Disk evidence is independent of whether the native importer registered the object.
    // Only explicit required nulls/empty deck sections qualify; missing fields do not.
    internal bool HasRequiredNull(out string reason)
    {
        reason = string.Empty;
        var checks = new List<(Value Owner, string Field, string Member, bool Required)>();
        checks.Add((_root, "m_Sections", "m_Section", true));
        checks.Add((_root, "m_SubSections", "m_Section", false));
        checks.Add((_root, "m_Pieces", "m_Piece", false));
        var components = Field(Field(_root, "components"), "$rcontent");
        if (components != null) foreach (var entry in components.Items)
        {
            var component = Resolve(entry);
            if (component == null) continue;
            var type = Field(component, "$type")?.Text ?? "";
            if (_types.TryGetValue(type, out var expanded)) type = expanded;
            switch (type)
            {
                case "Game.Prefabs.AuxiliaryNets, Game":
                    if (Field(component, "active")?.Text != "false")
                        checks.Add((component, "m_AuxiliaryNets", "m_Prefab", true));
                    break;
                case "Game.Prefabs.OverheadNetSections, Game":
                case "Game.Prefabs.UndergroundNetSections, Game":
                    if (Field(component, "active")?.Text != "false")
                        checks.Add((component, "m_Sections", "m_Section", true));
                    break;
                case "Game.Prefabs.NetPieceLanes, Game":
                    if (Field(component, "active")?.Text != "false")
                        checks.Add((component, "m_Lanes", "m_Lane", false));
                    break;
            }
        }
        foreach (var check in checks)
        {
            var array = Field(check.Owner, check.Field);
            var items = Field(array, "$rcontent");
            bool Null(Value? value) => value is { Token: "", Text: "null" };
            if (check.Required && Null(array)
                || check.Field == "m_Sections" && items != null && items.Items.Count == 0)
            { reason = check.Field + " is explicitly null or empty"; return true; }
            if (items == null) continue;
            for (var i = 0; i < items.Items.Count; i++)
                if (Null(Resolve(items.Items[i])) || Null(Field(items.Items[i], check.Member)))
                { reason = check.Field + "[" + i + "] has an explicit null reference"; return true; }
        }
        return false;
    }

    internal bool Reference(string? component, string field, int index, string member, out string identity)
    {
        identity = string.Empty;
        var owner = _root;
        if (component != null)
        {
            var components = Field(Field(_root, "components"), "$rcontent");
            Value? found = null;
            if (components == null) return false;
            foreach (var value in components.Items)
            {
                var item = Resolve(value);
                var type = Field(item, "$type")?.Text ?? string.Empty;
                if (_types.TryGetValue(type, out var expanded)) type = expanded;
                if (type != component + ", Game") continue;
                if (found != null) return false; // ambiguous duplicate component
                found = item;
            }
            if (found == null) return false;
            owner = found;
        }
        var slot = Field(owner, field);
        if (index >= 0)
        {
            var items = Field(slot, "$rcontent");
            if (items == null || index >= items.Items.Count) return false;
            if (Resolve(items.Items[index]) is { Token: "", Text: "null" }) return true;
            slot = Field(items.Items[index], member);
        }
        if (slot == null) return false;
        if (slot.Token == "$fstrref") { identity = slot.Text; return true; }
        // Explicit serialized null has no identity to recover; unknown tokens are NOT absence.
        return slot.Token.Length == 0 && slot.Text == "null";
    }

    private Value? Resolve(Value? value)
    {
        if (value?.Token == "$iref")
            return _objects.TryGetValue(value.Text, out var target) ? target : null;
        return value;
    }
    private Value? Field(Value? value, string key)
    {
        value = Resolve(value);
        return value != null && value.Fields.TryGetValue(key, out var child) ? Resolve(child) : null;
    }
    private void Space() { while (_offset < _text.Length && char.IsWhiteSpace(_text[_offset])) _offset++; }
    private bool Take(char c)
    {
        Space();
        if (_offset < _text.Length && _text[_offset] == c) { _offset++; return true; }
        return false;
    }
    private Value Read(int depth)
    {
        var value = new Value();
        Space();
        if (depth > 128 || _offset >= _text.Length) { _valid = false; return value; }
        if (Take('{'))
        {
            while (_valid && !Take('}'))
            {
                if (!Take('"')) { _valid = false; break; }
                var key = String();
                if (!Take(':')) { _valid = false; break; }
                if (value.Fields.ContainsKey(key)) { _valid = false; break; }
                var child = Read(depth + 1);
                value.Fields.Add(key, child);
                if (key == "$type" && child.Text.Contains("|"))
                {
                    var split = child.Text.IndexOf('|');
                    var number = child.Text.Substring(0, split);
                    var type = child.Text.Substring(split + 1);
                    if (_types.TryGetValue(number, out var prior) && prior != type) _valid = false;
                    _types[number] = type;
                    child.Text = child.Text.Substring(split + 1);
                }
                if (key == "$id")
                {
                    if (_objects.ContainsKey(child.Text)) _valid = false;
                    _objects[child.Text] = value;
                }
                if (Take('}')) break;
                if (!Take(',')) { _valid = false; break; }
            }
        }
        else if (Take('['))
        {
            while (_valid && !Take(']'))
            {
                value.Items.Add(Read(depth + 1));
                if (Take(']')) break;
                if (!Take(',')) { _valid = false; break; }
            }
        }
        else if (Take('"')) value.Text = String();
        else
        {
            var start = _offset;
            while (_offset < _text.Length && !char.IsWhiteSpace(_text[_offset])
                && ",]}:".IndexOf(_text[_offset]) < 0) _offset++;
            value.Text = _text.Substring(start, _offset - start);
            if (value.Text.Length == 0) _valid = false;
            if (Take(':'))
            {
                value.Token = value.Text;
                var payload = Read(depth + 1);
                value.Text = payload.Text;
            }
        }
        return value;
    }
    private string String()
    {
        var result = new StringBuilder();
        while (_offset < _text.Length)
        {
            var c = _text[_offset++];
            if (c == '"') return result.ToString();
            if (c == '\\')
            {
                if (_offset >= _text.Length) break;
                c = _text[_offset++];
                if (c == 'u')
                {
                    if (_offset + 4 > _text.Length || !ushort.TryParse(_text.Substring(_offset, 4),
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) break;
                    result.Append((char)code); _offset += 4; continue;
                }
                c = c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', _ => c };
            }
            result.Append(c);
        }
        _valid = false;
        return result.ToString();
    }
}
