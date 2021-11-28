using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    public class FileEntry
    {
        public int _entry { get; set; }

        public int _status { get; set; }
        // 0 - 15: used for file, status is the user number
        // 16 - 31: used for file, status is the user number(P2DOS) or used for password extent (CP / M 3 or higher)
        // 32: disc label
        // 33: time stamp(P2DOS)
        // 0xE5: unused

        public String _name { get; set; }
        public String _ext { get; set; }
        // F0: requires set wheel byte(Backgrounder II)
        // F1: public file (P2DOS, ZSDOS), foreground - only command(Backgrounder II)
        // F2: date stamp(ZSDOS), background - only commands(Backgrounder II)
        // F7: wheel protect(ZSDOS)
        // E0: read - only
        // E1: system file
        // E2: archived

        public int _ex { get; set; }
        public int _s1 { get; set; }
        public int _s2 { get; set; }
        public int _rc { get; set; }

        public short[] _al { get; set; }

        public int _entry_number { get; set; }

        public int _num_records { get; set; }
        public int _size { get; set; }
        public int _start { get; set; }


        public byte[] GetDataEntry()
        {
            byte[] ret = new byte[32];
            string name = _name.PadRight(8);
            string ext = _ext.PadRight(3);

            ret[0x00] = (byte)_status;
            ret[0x01] = (byte)name[0];
            ret[0x02] = (byte)name[1];
            ret[0x03] = (byte)name[2];
            ret[0x04] = (byte)name[3];
            ret[0x05] = (byte)name[4];
            ret[0x06] = (byte)name[5];
            ret[0x07] = (byte)name[6];
            ret[0x08] = (byte)name[7];
            ret[0x09] = (byte)ext[0];
            ret[0x0A] = (byte)ext[1];
            ret[0x0B] = (byte)ext[2];
            ret[0x0C] = (byte)_ex;
            ret[0x0D] = (byte)_s1;
            ret[0x0E] = (byte)_s2;
            ret[0x0F] = (byte)_rc;


            ret[0x10] = (byte)(_al[0] & 0b11111111);
            ret[0x11] = (byte)((_al[0] >> 8) & 0b11111111);
            ret[0x12] = (byte)(_al[1] & 0b11111111);
            ret[0x13] = (byte)((_al[1] >> 8) & 0b11111111);
            ret[0x14] = (byte)(_al[2] & 0b11111111);
            ret[0x15] = (byte)((_al[2] >> 8) & 0b11111111);
            ret[0x16] = (byte)(_al[3] & 0b11111111);
            ret[0x17] = (byte)((_al[3] >> 8) & 0b11111111);
            ret[0x18] = (byte)(_al[4] & 0b11111111);
            ret[0x19] = (byte)((_al[4] >> 8) & 0b11111111);
            ret[0x1A] = (byte)(_al[5] & 0b11111111);
            ret[0x1B] = (byte)((_al[5] >> 8) & 0b11111111);
            ret[0x1C] = (byte)(_al[6] & 0b11111111);
            ret[0x1D] = (byte)((_al[6] >> 8) & 0b11111111);
            ret[0x1E] = (byte)(_al[7] & 0b11111111);
            ret[0x1F] = (byte)((_al[7] >> 8) & 0b11111111);
            return ret;
        }


        public static int CalcNumOfBlocks(int size)
        {
            return (int)Math.Ceiling((decimal)size / 0x1000);
        }
    }
}
