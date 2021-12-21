using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    public class Disk
    {

        public int spt { get; set; }    //DEFW  spt; Number of 128-byte records per track

        public int bsh { get; set; }    //DEFB  bsh; Block shift. 3 => 1k, 4 => 2k, 5 => 4k....
        public int blm { get; set; }    //DEFB  blm; Block mask. 7 => 1k, 0Fh => 2k, 1Fh => 4k...
        public int exm { get; set; }    //DEFB  exm; Extent mask, see later
        public int dsm { get; set; }    //DEFW  dsm; (no.of blocks on the disc)-1

        public int drw { get; set; }    //DEFW  drm; (no.of directory entries)-1

        public int al0 { get; set; }    //DEFB  al0; Directory allocation bitmap, first byte
        public int al1 { get; set; }    //DEFB  al1; Directory allocation bitmap, second byte
        public int cks { get; set; }    //DEFW  cks; Checksum vector size, 0 for a fixed disc
                                        //         ; No.directory entries/4, rounded up.
        public int off { get; set; }    //DEFW  off; Offset, number of reserved tracks

        //The directory allocation bitmap is interpreted as: (240 0)
        //             al0              al1
        //b7b6b5b4b3b2b1b0 b7b6b5b4b3b2b1b0
        // 1 1 1 1 0 0 0 0  0 0 0 0 0 0 0 0

        // - ie, in this example, the first 4 blocks of the disc contain the directory.


        Byte[] fileData;

        List<FileEntry> FAT = new List<FileEntry>();
        Dictionary<string, List<FileEntry>> file_entries = new Dictionary<string, List<FileEntry>>();

        public Disk()
        {
            spt = 128;
            bsh = 5;
            blm = 31;
            exm = 1;
            dsm = 2047;
            drw = 511;
            al0 = 240;
            al1 = 0;
            cks = 0;
            off = 0;
        }

        public void LoadDisk(Byte[] disk, int start)
        {
            int size = GetDiskSize();

            if (start == 0 || start == 0x4000)
            {
                dsm = 2043;
                start = 0x4000;
                size = (dsm + 1) * 0x1000;
                off = 1;
            }
            else if (size + start > disk.Length)
            {
                size = disk.Length - start;
                dsm = (size / 0x1000) - 1;
                off = 0;
            }
            else
            {
                dsm = 2047;
                off = 0;
            }

            fileData = new byte[size];
            Buffer.BlockCopy(disk, start, fileData, 0, size);
        }



        public static short[] get_al(byte[] fileBytes2, int start)
        {
            short[] _al = new short[8];
            _al[0] = (short)(fileBytes2[start + 0] + (fileBytes2[start + 1] << 8));
            _al[1] = (short)(fileBytes2[start + 2] + (fileBytes2[start + 3] << 8));
            _al[2] = (short)(fileBytes2[start + 4] + (fileBytes2[start + 5] << 8));
            _al[3] = (short)(fileBytes2[start + 6] + (fileBytes2[start + 7] << 8));
            _al[4] = (short)(fileBytes2[start + 8] + (fileBytes2[start + 9] << 8));
            _al[5] = (short)(fileBytes2[start + 10] + (fileBytes2[start + 11] << 8));
            _al[6] = (short)(fileBytes2[start + 12] + (fileBytes2[start + 13] << 8));
            _al[7] = (short)(fileBytes2[start + 14] + (fileBytes2[start + 15] << 8));

            return _al;
        }

        public int get_file_bytes(FileEntry f)
        {
            int c = 0;
            List<byte> b = new List<byte>();

            foreach (int ad in f._al)
            {
                if (c > 0 && ad == 0)
                    break;
                c++;
            }

            return (c - 1) * 0x1000 + 0x80 * f._rc;
        }


        public List<FileEntry> cmd_ls()
        {

            FAT.Clear();
            file_entries.Clear();
            int d = 0;

            while (d < ((drw + 1) * 0x20))
            {

                if (fileData[d] != 0xE5)
                {

                    FileEntry f = new FileEntry()
                    {
                        _entry = d,
                        _status = fileData[d + 0],

                        _name = Utils.getStringFromByteArray(fileData, d + 1, 8),
                        _ext = Utils.getStringFromByteArray(fileData, d + 9, 3),
                        _ex = fileData[d + 12],
                        _s1 = 0x0,
                        _s2 = fileData[d + 14],
                        _rc = fileData[d + 15],
                        _entry_number = 0,
                        _num_records = 0,
                        _size = 0,
                        _start = 0
                    };
                    f._entry_number = ((32 * f._s2) + f._ex) / (exm + 1);
                    f._num_records = (f._ex & exm) * 128 + f._rc;

                    f._al = get_al(fileData, d + 16);
                    f._size = get_file_bytes(f);
                    f._start = (f._al[0] * 0x1000);

                    string filename = Utils.getStringFromByteArray(fileData, d + 1, 8) + Utils.getStringFromByteArray(fileData, d + 9, 3);
                    if (!file_entries.ContainsKey(filename))
                    {
                        FAT.Add(f);
                        file_entries.Add(filename, new List<FileEntry>(new FileEntry[] { f }));
                    }
                    else
                    {
                        file_entries[filename].Add(f);
                    }

                }
                d += 0x20;
            }

            return FAT;
        }


        public bool DeleteFile(string filename)
        {
            if (file_entries.ContainsKey(filename))
            {
                foreach (FileEntry fe in file_entries[filename])
                {
                    fe._status = 0xE5;
                }

                return true;
            }
            return false;
        }

        public bool RenameFile(string filename, String name, String ext)
        {
            if (file_entries.ContainsKey(filename))
            {
                String _name = name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 8)) : "";
                String _ext = ext.Length > 0 ? ext.Substring(0, Math.Min(ext.Length, 3)) : "";

                _name = _name.PadRight(8);
                _ext = _ext.PadRight(3);

                foreach (FileEntry fe in file_entries[filename])
                {
                    fe._name = _name;
                    fe._ext = _ext;
                }


                List<FileEntry> value = file_entries[filename];
                file_entries.Remove(filename);
                file_entries.Add(_name + _ext, value);
                return true;
            }
            return false;
        }

        public List<FileEntry> GetFileEntries(String filename)
        {
            return file_entries[filename];
        }

        public List<FileEntry> GetFileEntry(string filename)
        {
            if (file_entries.ContainsKey(filename))
                return file_entries[filename];
            return null;
        }


        public void SetUser(string filename, int new_user)
        {

            for(int i = 0; i < file_entries[filename].Count; i++)
            {
                file_entries[filename][i]._status = new_user;
            }
        }

        public byte[] GetFile(string filename)
        {
            int c = 0;
            List<byte> f = new List<byte>();
            List<short> blocks = new List<short>();
            int size = 0;
            foreach (FileEntry fe in file_entries[filename])
            {
                blocks.AddRange(fe._al.Where(p => p > 0));
                size += fe._size;
            }

            blocks.Sort();


            foreach (short b in blocks)
            {
                int _start = (b * 0x1000);
                for (int j = 0; j < 0x1000; j++)
                    f.Add(fileData[_start + j]);

                c++;
            }


            int i = Math.Min(f.Count - 1, size - 1);
            while (i > 0 && (f[i] == 0x0 || f[i] == 0x1A))
                --i;
            // now foo[i] is the last non-zero byte
            byte[] bar = new byte[i + 1];
            Array.Copy(f.ToArray(), bar, i + 1);

            return bar;
        }


        public int GetDiskSize()
        {
            return (dsm + 1) * 0x1000;
        }

        public int GetDiskTotalBlocks()
        {
            return (GetDiskSize() - 0x4000) / 0x1000;
        }

        public List<short> GetFreeBlocks()
        {
            List<short> ret = new List<short>();
            List<short> used = new List<short>();


            int total_blocks = GetDiskTotalBlocks();

            foreach (List<FileEntry> lfe in file_entries.Values)
            {
                foreach (FileEntry fe in lfe)
                {
                    if (fe._status != 0xE5)
                    {
                        foreach (short s in fe._al)
                        {
                            if (s > 0)
                                used.Add(s);
                        }
                    }
                }
            }

            for (short i = 4; i <= (total_blocks + 4); i++)
            {
                if (!used.Contains(i))
                    ret.Add(i);
            }

            return ret;
        }


        public List<short> GetFreeDirEntries()
        {
            List<short> ret = new List<short>();
            List<short> used = new List<short>();


            foreach (List<FileEntry> lfe in file_entries.Values)
            {
                foreach (FileEntry fe in lfe)
                {
                    if (fe._status != 0xE5)
                    {
                        used.Add((short)(fe._entry / 0x20));
                    }
                }
            }

            for (short i = 0; i <= drw; i++)
            {
                if (!used.Contains(i))
                    ret.Add(i);
            }

            return ret;
        }
    }
}
