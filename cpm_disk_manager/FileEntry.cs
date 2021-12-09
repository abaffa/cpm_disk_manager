using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    //https://www.cpm8680.com/cpmtools/cpm.htm
    //https://www.seasip.info/Cpm/format22.html
    public class FileEntry
    {
        public int _entry { get; set; }

        //////////////////////////////////////////////////
        // UU F1 F2 F3 F4 F5 F6 F7 F8 T1 T2 T3 EX S1 S2 RC   .FILENAMETYP....
        // AL AL AL AL AL AL AL AL AL AL AL AL AL AL AL AL................

        public int _status { get; set; }    // UU = User number. 0-15 (on some systems, 0-31). The user number allows multiple
        // files of the same name to coexist on the disc.
        // 0 - 15: used for file, status is the user number
        // 16 - 31: used for file, status is the user number(P2DOS) or used for password extent (CP / M 3 or higher)
        // 32: disc label
        // 33: time stamp(P2DOS)
        // 0xE5: unused

        public String _name { get; set; }   //F0-F7
        // F0-E2 are the file name and its extension. They may consist of any printable 7 bit ASCII character but: < > . , ; : = ? * [ ]. 
        // The file name must not be empty, the extension may be empty. Both are padded with blanks. The highest bit of each character of
        // the file name and extension is used as attribute. The attributes have the following meaning:
        // F0: requires set wheel byte(Backgrounder II)
        // F1: public file (P2DOS, ZSDOS), foreground - only command(Backgrounder II)
        // F2: date stamp(ZSDOS), background - only commands(Backgrounder II)
        // F7: wheel protect(ZSDOS)

        public String _ext { get; set; }    //E0-E2
        // En - filetype.The characters used for these are 7-bit ASCII.
        // The top bit of E1 (often referred to as E1') is set if the file is
        // read-only.
        // E2' is set if the file is a system file (this corresponds to "hidden" on
        // other systems).
        // E0: read - only
        // E1: system file
        // E2: archived

        public int _ex { get; set; } //or Xl EX = Extent counter, low byte - takes values from 0-31

        public int _s1 { get; set; } //or Bc S1 - reserved, set to 0.

        public int _s2 { get; set; } //or Xh S2 = Extent counter, high byte.
        // An extent is the portion of a file controlled by one directory entry.
        // If a file takes up more blocks than can be listed in one directory entry,
        // it is given multiple entries, distinguished by their EX and S2 bytes.The
        // formula is: Entry number = ((32 * S2) + EX) / (exm + 1) where exm is the
        // extent mask value from the Disc Parameter Block.

        public int _rc { get; set; } //or Rc RC - Number of records (1 record=128 bytes) used in this extent, low byte.
        //The total number of records used in this extent is
        // (EX & exm) * 128 + RC
        // If RC is 80h, this extent is full and there may be another one on the disc.
        // File lengths are only saved to the nearest 128 bytes.


        //Xl and Xh store the extent number.A file may use more than one directory entry, if it contains more 
        // blocks than an extent can hold.In this case, more extents are allocated and each of them is numbered 
        // sequentially with an extent number. If a physical extent stores more than 16k, it is considered to 
        // contain multiple logical extents, each pointing to 16k data, and the extent number of the last used 
        // logical extent is stored.Note: Some formats decided to always store only one logical extent in a 
        // physical extent, thus wasting extent space. CP/M 2.2 allows 512 extents per file, CP/M 3 and higher 
        // allow up to 2048. Bit 5-7 of Xl are 0, bit 0-4 store the lower bits of the extent number. 
        // Bit 6 and 7 of Xh are 0, bit 0-5 store the higher bits of the extent number.


        //Rc and Bc determine the length of the data used by this extent.The physical extent is divided into 
        // logical extents, each of them being 16k in size (a physical extent must hold at least one logical 
        // extent, e.g.a blocksize of 1024 byte with two-byte block pointers is not allowed). Rc stores the 
        // number of 128 byte records of the last used logical extent.Bc stores the number of bytes in the 
        // last used record. The value 0 means 128 for backward compatibility with CP/M 2.2, which did not 
        // support Bc.ISX records the number of unused instead of used bytes in Bc.

        public short[] _al { get; set; }
        // Al stores block pointers.If the disk capacity minus boot tracks but including the directory area is less than 256 blocks,
        // Al is interpreted as 16 byte-values, otherwise as 8 double-byte-values.Since the directory area is not subtracted,
        // the directory area starts with block 0 and files can never allocate block 0, which is why this value can be given
        // a new meaning: A block pointer of 0 marks a hole in the file.If a hole covers the range of a full extent, the extent
        // will not be allo- cated.In particular, the first extent of a file does not neccessarily have extent number 0. A file may
        // not share blocks with other files, as its blocks would be freed if the other files were erased without a following disk
        // system reset. CP/M returns EOF when it reaches a hole, whereas UNIX returns zero-value bytes, which makes holes invisible.


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
