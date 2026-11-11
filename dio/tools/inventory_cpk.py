"""Read plaintext CRI CPK TOCs without extracting or changing game files.

Format reference: https://github.com/esperknight/CriPakTools
Only named TOC archives are supported.
"""
import argparse
import json
import struct
from pathlib import Path


def utf_table(data):
    if data[:4] != b'@UTF':
        mask = 0x655f
        decoded = bytearray()
        for byte in data:
            decoded.append(byte ^ (mask & 255))
            mask = (mask * 0x4115) & 0xffffffff
        data = bytes(decoded)
    if data[:4] != b'@UTF':
        raise ValueError('Unsupported table: expected plaintext @UTF')
    size, rows, strings, blobs, name, columns, row_size, count = struct.unpack_from('>IIIIIHHI', data, 4)
    if size + 8 > len(data):
        raise ValueError('Truncated UTF table')
    rows, strings, blobs = rows + 8, strings + 8, blobs + 8

    def string(offset):
        start = strings + offset
        return data[start:data.index(b'\0', start)].decode('utf-8', errors='replace')

    def value(kind, pos):
        formats = {0: 'B', 1: 'b', 2: 'H', 3: 'h', 4: 'I', 5: 'i', 6: 'Q', 7: 'q', 8: 'f', 9: 'd', 10: 'I', 11: 'II'}
        fmt = '>' + formats[kind]
        values = struct.unpack_from(fmt, data, pos)
        result = values[0]
        if kind == 10:
            result = string(result)
        elif kind == 11:
            result = {'offset': blobs + values[0], 'length': values[1]}
        return result, pos + struct.calcsize(fmt)

    schema = []
    pos = 32
    for _ in range(columns):
        flags, offset = struct.unpack_from('>BI', data, pos)
        pos += 5
        storage, kind = flags & 0xf0, flags & 0x0f
        constant = 0
        if storage == 0x30:
            constant, pos = value(kind, pos)
        elif storage not in (0x10, 0x50):
            raise ValueError(f'Unsupported column storage {storage:#x}')
        schema.append((string(offset), storage, kind, constant))
    result = []
    for index in range(count):
        pos = rows + index * row_size
        row = {}
        for key, storage, kind, constant in schema:
            if storage == 0x50:
                row[key], pos = value(kind, pos)
            else:
                row[key] = constant
        if pos > rows + (index + 1) * row_size:
            raise ValueError('Row exceeds declared size')
        result.append(row)
    return result


def packet(stream, offset, signature):
    stream.seek(offset)
    header = stream.read(16)
    if header[:4] != signature:
        raise ValueError(f'Expected {signature!r} at {offset}')
    length = struct.unpack_from('<Q', header, 8)[0]
    if length > 64 * 1024 * 1024:
        raise ValueError('Unexpected metadata size')
    return utf_table(stream.read(length))


def inventory(path):
    with path.open('rb') as stream:
        header = packet(stream, 0, b'CPK ')[0]
        toc = header['TocOffset']
        base = min(toc, header['ContentOffset'])
        entries = packet(stream, toc, b'TOC ')
        for entry in entries:
            offset = base + entry['FileOffset']
            if offset + entry['FileSize'] > path.stat().st_size:
                raise ValueError('Entry extends beyond archive')
            entry['AbsoluteOffset'] = offset
        return {'archive': str(path), 'declared_files': header['Files'], 'entries': entries}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    results = []
    for path in sorted(args.root.rglob('*.cpk')):
        try:
            result = inventory(path)
            print(f'{path.name}: {len(result["entries"])} entries')
        except (ValueError, KeyError, struct.error) as error:
            result = {'archive': str(path), 'error': str(error)}
            print(f'{path}: {error}')
        results.append(result)
    args.output.write_text(json.dumps(results, indent=2), encoding='utf-8')
