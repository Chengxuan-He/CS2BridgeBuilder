"""Bound generated C# initializer methods for Unity Mono's stack budget.

Only literal array declarations in the emitter's known format are rewritten.
Entries and their order are preserved; no geometry data is inferred.
"""
import re


def split_static_arrays(source, chunk_size=128):
    pattern = re.compile(
        r"    private static readonly ([^\n]+)\[\] (\w+) =\n    \{\n(.*?)    \};",
        re.S,
    )

    def rewrite(match):
        kind, name, body = match.groups()
        entries, depth, start = [], 0, 0
        for index, char in enumerate(body):
            if char in "([{":
                depth += 1
            elif char in ")]}":
                depth -= 1
            elif char == "," and depth == 0:
                entry = body[start:index].strip()
                if entry:
                    entries.append(entry)
                start = index + 1
        if body[start:].strip():
            entries.append(body[start:].strip())
        if depth != 0:
            raise ValueError(f"Unbalanced initializer: {name}")
        if len(entries) <= chunk_size:
            return match.group(0)
        lines = [
            f"    private static readonly {kind}[] {name} = Create{name}();",
            f"    private static {kind}[] Create{name}()", "    {",
            f"        var values = new {kind}[{len(entries)}];",
        ]
        for offset in range(0, len(entries), chunk_size):
            lines.append(f"        Fill{name}_{offset}(values);")
        lines += ["        return values;", "    }"]
        for offset in range(0, len(entries), chunk_size):
            lines += [
                "    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]",
                f"    private static void Fill{name}_{offset}({kind}[] values)", "    {",
            ]
            lines += [
                f"        values[{i}] = {entries[i]};"
                for i in range(offset, min(offset + chunk_size, len(entries)))
            ]
            lines.append("    }")
        return "\n".join(lines)

    return pattern.sub(rewrite, source)
