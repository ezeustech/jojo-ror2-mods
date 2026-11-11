"""Extract DIO candidates, preserving each archive version and recording hashes."""
import hashlib
import json
import struct
import io
from pathlib import Path
from inventory_cpk import packet, utf_table


def decompress(data):
    if data[:4] == b'CPK ':
        stream = io.BytesIO(data)
        header = packet(stream, 0, b'CPK ')[0]
        index = packet(stream, header['ItocOffset'], b'ITOC')[0]
        stream.seek(header['ItocOffset'] + 16)
        table = stream.read(header['ItocSize'] - 16)
        chunks = []
        for key in ('DataL', 'DataH'):
            blob = index[key]
            if isinstance(blob, dict) and blob['length']:
                chunks.extend(utf_table(table[blob['offset']:blob['offset'] + blob['length']]))
        position = header['ContentOffset']
        result = bytearray()
        for chunk in sorted(chunks, key=lambda c: c['ID']):
            content = decompress(data[position:position + chunk['FileSize']])
            if len(content) != chunk['ExtractSize']:
                raise ValueError('Nested chunk size mismatch')
            result.extend(content)
            position += ((chunk['FileSize'] + header['Align'] - 1) // header['Align']) * header['Align']
        return bytes(result)
    if data[:8] != b'CRILAYLA':
        return data
    length, packed = struct.unpack_from('<II', data, 8)
    if length > 256 * 1024 * 1024 or 16 + packed + 256 > len(data):
        raise ValueError('Invalid CRILAYLA sizes')
    output = bytearray(length + 256)
    output[:256] = data[16 + packed:16 + packed + 256]
    cursor, pool, available = 15 + packed, 0, 0

    def bits(count):
        nonlocal cursor, pool, available
        result = 0
        for _ in range(count):
            if not available:
                if cursor < 16:
                    raise ValueError('Truncated compressed stream')
                pool, cursor, available = data[cursor], cursor - 1, 8
            available -= 1
            result = (result << 1) | ((pool >> available) & 1)
        return result

    target = len(output) - 1
    while target >= 256:
        if bits(1) == 0:
            output[target] = bits(8)
            target -= 1
            continue
        source = target + bits(13) + 3
        count = 3
        for width in (2, 3, 5, 8):
            part = bits(width)
            count += part
            if part != (1 << width) - 1:
                break
        else:
            while True:
                part = bits(8)
                count += part
                if part != 255:
                    break
        if count > target - 255 or source >= len(output):
            raise ValueError('Invalid backreference')
        for _ in range(count):
            output[target] = output[source]
            target, source = target - 1, source - 1
    return bytes(output)


def main():
    root = Path(__file__).resolve().parents[1]
    archives = json.loads((root / 'docs/cpk-contents.json').read_text(encoding='utf-8-sig'))
    destination = root / 'assets/source'
    records = []
    for archive in archives:
        if 'error' in archive:
            raise ValueError(f'Unresolved archive: {archive}')
        path = Path(archive['archive'])
        label = '_'.join(path.parts[-3:]).replace('.cpk', '')
        with path.open('rb') as stream:
            for entry in archive['entries']:
                name = entry['FileName']
                if not any(code in name.lower() for code in ('3dio', 'bdio', '3wld', 'bwld')):
                    continue
                relative = Path(entry['DirName']) / name
                output = (destination / label / relative).resolve()
                if not output.is_relative_to(destination.resolve()):
                    raise ValueError('Unsafe archive path')
                if output.exists() and output.stat().st_size == entry['ExtractSize']:
                    data = output.read_bytes()
                else:
                    stream.seek(entry['AbsoluteOffset'])
                    raw = stream.read(entry['FileSize'])
                    data = decompress(raw)
                if len(data) != entry['ExtractSize']:
                    raise ValueError(f'Size mismatch: {name}')
                output.parent.mkdir(parents=True, exist_ok=True)
                output.write_bytes(data)
                records.append({'source': str(path), 'entry': str(relative),
                    'output': str(output.relative_to(root)), 'size': len(data),
                    'sha256': hashlib.sha256(data).hexdigest(), 'magic': data[:4].hex()})
    (root / 'docs/extracted-assets.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    print(f'Extracted and size-verified {len(records)} entries')


if __name__ == '__main__':
    main()
