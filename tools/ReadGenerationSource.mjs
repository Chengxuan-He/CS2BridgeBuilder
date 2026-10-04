import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

export function readGenerationSource() {
    const directory = fileURLToPath(new URL('../src/BridgeBuilder/Systems/', import.meta.url));
    return readdirSync(directory).filter(name => /^BridgeGenerationSystem(?:\.[A-Za-z]+)?\.cs$/.test(name))
        .sort().map(name => readFileSync(join(directory, name), 'utf8')).join('\n');
}
