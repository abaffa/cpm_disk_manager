using RawDiskLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    public class DiskImage
    {
        const int CF_CARD_LBA_SIZE = 0x800;         // temporary small size

        const int DISK_SIZE = 0x800000;

        Byte[] fileData;


        Disk disk = new Disk();
        List<FileEntry> fileEntryList = new List<FileEntry>();
        int disk_start = 0;
        int disk_number = 0;
        int disk_count = 0;

        public Disk Disk { get { return this.disk; } }

        public int DiskNumber { get { return this.disk_number; } }
        public int DiskCount { get { return this.disk_count; } }

        //int clusters = 0x2000; // 4 MB image
        int clusters = 0x1E848; //64MB Image

        public void NewImage()
        {

            fileData = new byte[512 * clusters];
            disk = new Disk();


            disk_number = 0;

            disk_start = disk_number * 0x800000;
            disk_count = (int)Math.Ceiling((decimal)fileData.Length / 0x800000);
            if (disk_start == 0) disk_start = 0x4000;

            createEmptyFileEntries();

            UpdateDiskList();
        }



        private void createEmptyFileEntries()
        {
            for (int disk_number = 0; disk_number < disk_count; disk_number++)
            {
                int disk_start = disk_number * 0x800000;
                if (disk_start == 0) disk_start = 0x4000;
                for (int file_entry = disk_start; file_entry < disk_start + 0x4000; file_entry += 0x20)
                {
                    fileData[file_entry] = 0xe5; //empty entry

                    for (int i = 1; i < 0x0C; i++)
                        fileData[file_entry + i] = 0x20; //noname file
                }
            }
        }

        public void PreviousDisk()
        {
            if (disk_number > 0)
                disk_number--;

            UpdateDiskList();
        }

        public void NextDisk()
        {
            if (disk_number < disk_count - 1)
                disk_number++;

            UpdateDiskList();
        }


        public void UpdateDiskList()
        {
            disk_start = disk_number * 0x800000;
            if (disk_start == 0) disk_start = 0x4000;
            disk.LoadDisk(fileData, disk_start);
        }


        public void cmd_rmdir(string filename)
        {
            this.disk.DeleteFile(filename);

            foreach (FileEntry f in this.disk.GetFileEntries(filename))
            {
                Buffer.BlockCopy(f.GetDataEntry(), 0, fileData, disk_start + f._entry, 32);
            }

            disk.LoadDisk(fileData, disk_start);
        }

        public void cmd_mkbin(string str, byte[] data)
        {

            int num_blocks = FileEntry.CalcNumOfBlocks(data.Length);

            int num_entries = (int)Math.Ceiling((decimal)num_blocks / 8);


            List<short> reserve_entries = disk.GetFreeDirEntries().Take(num_entries).ToList();

            List<short> reserve_blocks = disk.GetFreeBlocks().Take(num_blocks).ToList();

            string newname = str;

            if (newname.ToArray().Where(p => p == '.').Count() <= 1
                && newname.IndexOf('\0') == -1
                && newname.IndexOf('\\') == -1
                && newname.IndexOf('/') == -1
                && newname.IndexOf(':') == -1
                && newname.IndexOf('*') == -1
                && newname.IndexOf('?') == -1
                && newname.IndexOf('\'') == -1
                && newname.IndexOf('\"') == -1
                && newname.IndexOf('<') == -1
                && newname.IndexOf('>') == -1
                && newname.IndexOf('|') == -1
                && newname.Trim().IndexOf(' ') == -1)
            {
                String name = newname.IndexOf('.') > -1 ? newname.Substring(0, newname.IndexOf('.')) : newname;
                String ext = newname.IndexOf('.') > -1 ? newname.Substring(newname.IndexOf('.') + 1) : "";

                name = name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 8)) : "";
                ext = ext.Length > 0 ? ext.Substring(0, Math.Min(ext.Length, 3)) : "";

                name = name.PadRight(8);
                ext = ext.PadRight(3);

                short ex = 0;
                int data_start = 0;

                foreach (short s in reserve_entries)
                {
                    List<short> current_blocks = reserve_blocks.Take(8).ToList();
                    bool isfull = reserve_blocks.Count > 8 && current_blocks.Count == 8;
                    int current_block_count = current_blocks.Count();

                    while (current_blocks.Count < 8)
                        current_blocks.Add(0);

                    int pos = 4 - ((current_block_count - 1) % 4);

                    if (num_entries > 1 || current_block_count > 4)
                        ex++;

                    FileEntry f = new FileEntry()
                    {
                        _entry = (s * 0x20),
                        _status = 0,

                        _name = name,
                        _ext = ext,
                        _ex = ex & 0b11111111,
                        _s1 = 0x0,
                        _s2 = (ex >> 8) & 0b11111111,
                        //_rc = isfull? 0x80 : (int)Math.Ceiling(((decimal)data.Length % 0x1000) / 0x80),
                        _rc = isfull ? 0x80 : (int)((((double)data.Length / 0x8000) - (current_block_count <= 4 ? 0 : 0.5)) * 0x100),
                        _entry_number = 0,
                        _num_records = 0,
                        _size = 0,
                        _start = reserve_blocks[0] * 0x1000,
                        _al = current_blocks.ToArray()
                    };

                    Buffer.BlockCopy(f.GetDataEntry(), 0, fileData, f._entry + disk_start, 0x20);

                    foreach (short al in f._al)
                    {
                        if (al > 0)
                        {
                            int block_size = (data.Length - data_start) > 0x1000 ? 0x1000 : (data.Length - data_start);
                            Buffer.BlockCopy(data, data_start, fileData, (al * 0x1000) + disk_start, block_size);

                            if (block_size < 0x1000)
                            {
                                int size_to_complete = 0x1000 - block_size;

                                //Buffer.BlockCopy(Enumerable.Repeat((byte)0x1A, size_to_complete).ToArray(), 0, fileData, (al * 0x1000) + disk_start + block_size, size_to_complete);
                                Buffer.BlockCopy(Enumerable.Repeat((byte)0x00, size_to_complete).ToArray(), 0, fileData, (al * 0x1000) + disk_start + block_size, size_to_complete);

                            }

                            data_start += 0x1000;
                        }
                    }

                    reserve_blocks.RemoveRange(0, Math.Min(8, reserve_blocks.Count));
                }

            }
            this.UpdateDiskList();
        }


        public bool cmd_rename(String filename, String newname)
        {
            if (newname.ToArray().Where(p => p == '.').Count() <= 1
                && newname.IndexOf('\0') == -1
                && newname.IndexOf('\\') == -1
                && newname.IndexOf('/') == -1
                && newname.IndexOf(':') == -1
                && newname.IndexOf('*') == -1
                && newname.IndexOf('?') == -1
                && newname.IndexOf('\'') == -1
                && newname.IndexOf('\"') == -1
                && newname.IndexOf('<') == -1
                && newname.IndexOf('>') == -1
                && newname.IndexOf('|') == -1
                && newname.IndexOf(',') == -1
                && newname.IndexOf(';') == -1
                && newname.IndexOf('=') == -1
                && newname.IndexOf('[') == -1
                && newname.IndexOf(']') == -1
                && newname.IndexOf('(') == -1
                && newname.IndexOf(')') == -1
                && newname.IndexOf('%') == -1
                && newname.Trim().IndexOf(' ') == -1)
            {
                String name = newname.IndexOf('.') > -1 ? newname.Substring(0, newname.IndexOf('.')) : newname;
                String ext = newname.IndexOf('.') > -1 ? newname.Substring(newname.IndexOf('.') + 1) : "";

                name = name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 8)) : "";
                ext = ext.Length > 0 ? ext.Substring(0, Math.Min(ext.Length, 3)) : "";

                name = name.PadRight(8);
                ext = ext.PadRight(3);

                disk.RenameFile(filename, name, ext);

                foreach (FileEntry f in disk.GetFileEntries(name + ext))
                {
                    Buffer.BlockCopy(f.GetDataEntry(), 0, fileData, disk_start + f._entry, 32);
                }

                disk.LoadDisk(fileData, disk_start);

                return true;
            }

            return false;
        }



        public bool ReadImageFile(String fileName)
        {
            if (File.Exists(fileName))
            {
                FileInfo fi = new FileInfo(fileName);

                fileData = new byte[fi.Length];

                using (BinaryReader b = new BinaryReader(
                File.Open(fileName, FileMode.Open)))
                {
                    // 2.
                    // Position and length variables.
                    int pos = 0;
                    // 2A.
                    // Use BaseStream.
                    int length = (int)b.BaseStream.Length;
                    while (pos < length)
                    {

                        fileData[pos] = b.ReadByte();
                        // 3.
                        // Read integer.
                        //int v = b.ReadInt32();
                        //Console.WriteLine(v);

                        // 4.
                        // Advance our position variable.
                        pos += sizeof(byte);
                    }
                }

                disk_count = (int)Math.Ceiling((decimal)fileData.Length / 0x800000);
                disk_start = disk_number * 0x800000;
                if (disk_start == 0) disk_start = 0x4000;

                this.UpdateDiskList();
                return true;
            }
            return false;
        }


        public bool SaveImageFile(string fileName)
        {
            using (BinaryWriter b = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                for (int i = 0; i < fileData.Length; i++)
                    b.Write(fileData[i]);
            }

            return true;
        }


        public void ReadRawDisk(int selectedVol)
        {
            using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
            {
                fileData = new byte[512 * clusters];
                fileData = disk.ReadClusters(0, clusters);
            }
        }

        public void WriteRawDisk(int selectedVol)
        {
            using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
            {
                disk.WriteClusters(fileData, 0);

            }
        }
    }
}
