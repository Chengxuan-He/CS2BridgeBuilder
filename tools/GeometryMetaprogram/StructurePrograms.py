"""Emit shared ordered numerical programs; no prototype measurements live here.

Usage: python StructurePrograms.py REPOSITORY
All input rows/indices/coefficient values are supplied by measured bridge data.
"""
from pathlib import Path
import hashlib
import struct
import sys
import subprocess

class Program:
    def __init__(self):
        self.code, self.values, self.registers = [], [], 0
    def op(self, opcode, a, b=0, c=0):
        r = self.registers
        self.registers += 1
        self.code.append([opcode, r, a, b, c, 0, 0, 0])
        return r
    def parameter(self, slot): return self.op(1, slot)
    def constant(self, value):
        offset = len(self.values)
        self.values.append(value)
        return self.op(0, offset)
    def point(self, axis=0, mapping=-2): return self.op(2, 0, axis, mapping)
    def coefficient(self, slot=0, mapping=-2): return self.op(3, slot, mapping)
    def add(self, a, b): return self.op(4, a, b)
    def sub(self, a, b): return self.op(5, a, b)
    def mul(self, a, b): return self.op(6, a, b)
    def div(self, a, b): return self.op(7, a, b)
    def store(self, value, axis=0, mapping=-2): self.code.append([10, value, 1, axis, mapping, 0, 0, 0])

programs = []
def entry():
    p = Program()
    programs.append(p)
    return p

# 0: original X + coefficient[vertex] * (extra * 0.5)
p = entry(); x = p.point(); coefficient = p.coefficient()
p.store(p.add(x, p.mul(coefficient, p.mul(p.parameter(0), p.constant(0.5)))))
# 1: original X + coefficient[vertex] * delta
p = entry(); x = p.point(); p.store(p.add(x, p.mul(p.coefficient(), p.parameter(0))))
# 2: original X * scale
p = entry(); p.store(p.mul(p.point(), p.parameter(0)))
# 3: original X * ((halfSpan + delta) / halfSpan)
p = entry(); x = p.point(); span = p.parameter(0)
p.store(p.mul(x, p.div(p.add(span, p.parameter(1)), span)))
# 4: original X + translation
p = entry(); p.store(p.add(p.point(), p.parameter(0)))
# 5: constant X
p = entry(); p.store(p.parameter(0))
# 6: ornament XY map, preserving the two original ordered expressions.
p = entry(); x = p.point(); y = p.point(1); z = p.point(2)
span = p.parameter(0); extra = p.parameter(1); anchor = p.parameter(2)
scale = p.div(p.add(span, extra), span)
p.store(p.mul(x, scale)); p.store(p.add(anchor, p.mul(p.sub(y, anchor), scale)), 1); p.store(z, 2)
# 7: Vector3.LerpUnclamped with independently bound A/B/output indices.
p = entry(); weight = p.coefficient(mapping=-1)
for axis in range(3):
    a, b = p.point(axis, -2), p.point(axis, -3)
    p.store(p.add(a, p.mul(p.sub(b, a), weight)), axis, -4)
# 8: a + (b-a)*weightB + (c-a)*weightC; each vector operator rounds separately.
p = entry(); wb, wc = p.coefficient(0, -1), p.coefficient(1, -1)
for axis in range(3):
    a, b, c = p.point(axis, -2), p.point(axis, -3), p.point(axis, -4)
    p.store(p.add(p.add(a, p.mul(p.sub(b, a), wb)), p.mul(p.sub(c, a), wc)), axis, -5)
# 9/10: signed half-width translations; retain unary-negation ordering.
p = entry(); x = p.point(); p.store(p.add(x, p.mul(p.parameter(0), p.constant(0.5))))
p = entry(); x = p.point(); p.store(p.add(x, p.op(8, p.mul(p.parameter(0), p.constant(0.5)))))
# 11: negative translation.
p = entry(); p.store(p.add(p.point(), p.op(8, p.parameter(0))))
# 12: row-aligned coefficients, separately indexed source/output vertices.
p = entry(); p.store(p.add(p.point(), p.mul(p.coefficient(mapping=-1), p.parameter(0))))
# 13: span start + (span end - span start) * recorded row fraction.
p = entry(); a, b = p.parameter(0), p.parameter(1)
p.store(p.add(a, p.mul(p.sub(b, a), p.coefficient(mapping=-1))))
# 14: fixed sidewalk origin shift and independently ordered width correction.
p = entry(); x = p.point(); a, b, minimum, width = (p.parameter(i) for i in range(4))
origin = p.add(x, p.sub(a, minimum))
p.store(p.add(origin, p.mul(p.sub(p.sub(b, a), width), p.coefficient(mapping=-1))))
# 15: dense row-aligned affine coefficients.
p = entry(); p.store(p.add(p.point(mapping=-1), p.mul(p.coefficient(mapping=-1), p.parameter(0))), mapping=-1)
# 16: selected profile spans; span lookup is preparation, ordered scale is common.
p = entry(); x = p.point(); span = p.coefficient()
p.store(p.mul(x, p.div(p.add(span, p.mul(p.parameter(0), p.constant(0.5))), span)))
# 17: dense scalar scale.
p = entry(); p.store(p.mul(p.point(mapping=-1), p.parameter(0)), mapping=-1)
# 18: indexed coordinates with row-aligned scale factors.
p = entry(); p.store(p.mul(p.point(), p.coefficient(mapping=-1)))
# 19: edge interpolation followed by a row scale, into a scalar output column.
p = entry(); a, b = p.point(mapping=-2), p.point(mapping=-3)
value = p.mul(p.add(a, p.mul(p.sub(b, a), p.coefficient(0, -1))), p.coefficient(1, -1))
p.code.append([11, value, 2, -4, 0, 0, 0, 0])
# 20: indexed source coordinate scaled into a scalar output column.
p = entry(); value = p.mul(p.point(), p.coefficient(0, -1))
p.code.append([11, value, 1, -3, 0, 0, 0, 0])
# 21: indexed coordinate plus row origin plus row-weighted parameter.
p = entry(); x = p.add(p.point(), p.coefficient(0, -1))
p.store(p.add(x, p.mul(p.coefficient(1, -1), p.parameter(0))))
# 22: indexed coordinate plus common origin plus row-weighted parameter.
p = entry(); x = p.add(p.point(), p.parameter(0))
p.store(p.add(x, p.mul(p.coefficient(0, -1), p.parameter(1))))
# 23: selected layout target minus original anchor, then coordinate translation.
p = entry(); p.store(p.add(p.point(), p.sub(p.coefficient(0, -1), p.parameter(0))))
# 24: side-signed sidewalk origin/width shift with recorded row fractions.
p = entry(); origin = p.sub(p.parameter(0), p.parameter(1))
shift = p.add(origin, p.mul(p.sub(p.parameter(2), p.parameter(3)), p.coefficient(0, -1)))
p.store(p.add(p.point(), p.mul(p.coefficient(1, -1), shift)))
# 25: side-signed span change, preserving (difference * fraction) * side.
p = entry(); shift = p.mul(p.sub(p.parameter(0), p.parameter(1)), p.coefficient(0, -1))
p.store(p.add(p.point(), p.mul(p.coefficient(1, -1), shift)))

# 26: measured edge interpolation and row displacement into a scalar output.
p = entry(); a, b = p.point(mapping=-2), p.point(mapping=-3)
value = p.add(p.add(a, p.mul(p.sub(b, a), p.coefficient(0, -1))), p.mul(p.coefficient(1, -1), p.parameter(0)))
p.code.append([11, value, 2, -4, 0, 0, 0, 0])
# 27: row spans into explicitly indexed scalar output slots.
p = entry(); a, b = p.coefficient(0, -1), p.coefficient(1, -1)
value = p.add(a, p.mul(p.coefficient(2, -1), p.sub(b, a)))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])

# 28/29: selected scalar rows; output slots preserve original scatter order.
p = entry(); value = p.add(p.coefficient(0), p.coefficient(1))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])
p = entry(); value = p.add(p.coefficient(0), p.mul(p.coefficient(1), p.coefficient(2)))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])

# 30: unchanged selected scalar, preserving signed zero without an arithmetic op.
p = entry(); value = p.coefficient(0)
p.code.append([11, value, 3, -2, 0, 0, 0, 0])
# 31: selected target minus its anchor, then original coordinate translation.
p = entry(); value = p.add(p.coefficient(0), p.sub(p.coefficient(1), p.coefficient(2)))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])
# 32: a recorded frame shear from two completed coordinate rows and its span.
p = entry(); value = p.div(p.sub(p.coefficient(0), p.coefficient(1)), p.coefficient(2))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])

# 33: independent scalar product for recorded frame coefficients.
p = entry(); value = p.mul(p.coefficient(0), p.coefficient(1))
p.code.append([11, value, 3, -2, 0, 0, 0, 0])

# 34: measured boundary weight times a selected full span into scalar output.
p = entry(); value = p.mul(p.coefficient(0, -1), p.add(p.parameter(0), p.parameter(1)))
p.code.append([11, value, 1, -2, 0, 0, 0, 0])

words = [0x31465041, 1, len(programs)] + [0] * len(programs)
values = []
for i, p in enumerate(programs):
    words[3+i] = len(words)
    words.extend([-1, p.registers, len(p.code)])
    for instruction in p.code:
        if instruction[0] == 0: instruction[2] += len(values)
        words.extend(instruction)
    values.extend(p.values)
payload = struct.pack('<8i', 0x314e4242, 1, len(words), 0, 0, 0, len(values), 0)
payload += struct.pack(f'<{len(words)}i', *words) + struct.pack(f'<{len(values)}f', *values)
target = Path(sys.argv[1]) / 'assets/HardcodedNumbers/Shared/StructurePrograms.bin'
target.parent.mkdir(parents=True, exist_ok=True)
target.write_bytes(payload + hashlib.sha256(payload).digest())
print(f'Wrote {len(programs)} shared numerical entries: {target}')
subprocess.run(['dotnet', 'run', '--project', str(Path(sys.argv[1]) / 'tools/AffineGeometryMigration'),
                '--', str(Path(sys.argv[1]).resolve()), '--pack-aos'], check=True)
