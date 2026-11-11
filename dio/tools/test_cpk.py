import struct
import unittest
from extract_dio import decompress
from inventory_cpk import utf_table


def compressed(bits, expected_size):
    bits += '0' * ((-len(bits)) % 8)
    packed = bytes(int(bits[i:i + 8], 2) for i in range(0, len(bits), 8))[::-1]
    return b'CRILAYLA' + struct.pack('<II', expected_size, len(packed)) + packed + bytes(range(256))


class CompressionTests(unittest.TestCase):
    def test_uncompressed_passthrough(self):
        self.assertEqual(decompress(b'NUCCdata'), b'NUCCdata')

    def test_literals_and_preserved_header(self):
        data = compressed(''.join('0' + f'{v:08b}' for v in b'CBA'), 3)
        self.assertEqual(decompress(data), bytes(range(256)) + b'ABC')

    def test_overlapping_backreference(self):
        bits = ''.join('0' + f'{v:08b}' for v in b'CBA') + '1' + '0' * 13 + '00'
        self.assertEqual(decompress(compressed(bits, 6)), bytes(range(256)) + b'ABCABC')

    def test_bad_header_rejected(self):
        with self.assertRaises(ValueError):
            decompress(b'CRILAYLA' + struct.pack('<II', 100, 1000))

    def test_invalid_utf_rejected(self):
        with self.assertRaises(ValueError):
            utf_table(b'not a table')


if __name__ == '__main__':
    unittest.main()
