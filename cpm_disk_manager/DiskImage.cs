using RawDiskLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cpm_disk_manager
{
    public enum DiskImageFormat
    {
        _64MB_Searle,
        _128MB_Searle,
        __ROMWBW
    }
    public class DiskImage
    {
        //const int CF_CARD_LBA_SIZE = 0x800;         // temporary small size


        Byte[] fileData;


        Disk disk;
        List<FileEntry> fileEntryList = new List<FileEntry>();
        int disk_start = 0;
        int disk_number = 0;
        int disk_count = 0;

        public Disk Disk { get { return this.disk; } }

        public int DiskNumber { get { return this.disk_number; } }
        public int DiskCount { get { return this.disk_count; } }

        public DiskImageFormat DiskImageFormat { get; set; }

        int clusters_4mb = 0x2000;       //   4MB Image
        int clusters_64mb = 0x20000;    //  64MB Image
        int clusters_68mb = 0x20900;    //  68MB Image      //68288512 bytes
        int clusters_128mb = 0x40000;   // 128MB Image


        int default_disksize = 0x800000;
        int default_firstdisksize = 0x800000;
        int default_lastdisksize = 0x800000;
        int default_firstdiskstart = 0x4000;

        public DiskImage()
        {
            DiskImageFormat = DiskImageFormat._64MB_Searle;
        }

        public DiskImage(DiskImageFormat _imageSize)
        {
            DiskImageFormat = _imageSize;
        }


        private int UpdateCurrentDiskStart()
        {

            disk_count = (int)Math.Floor((decimal)fileData.Length / default_disksize);

            int current_disksize = default_disksize;

            if (disk_count == 0)
            {
                disk_start = default_firstdiskstart;
                current_disksize = default_firstdisksize;
            }
            else if (disk_number == disk_count - 1)
            {
                if(DiskImageFormat == DiskImageFormat.__ROMWBW)
                    disk_start = default_firstdiskstart + (disk_number * current_disksize);
                else
                    disk_start = (disk_number * current_disksize);
                current_disksize = default_lastdisksize;
            }
            else
            {
                if (DiskImageFormat == DiskImageFormat.__ROMWBW)
                    disk_start = default_firstdiskstart + (disk_number * current_disksize);
                else
                    disk_start = (disk_number * current_disksize);
                current_disksize = default_disksize;
            }

                

            return current_disksize;
        }


        public void SetFormat(DiskImageFormat _diskImageSize)
        {
            int clusters = clusters_4mb;
            disk = new Disk(_diskImageSize);

            DiskImageFormat = _diskImageSize;

            if (DiskImageFormat == DiskImageFormat._64MB_Searle)
            {
                clusters = clusters_64mb;
                default_disksize = 0x800000;
                default_firstdiskstart = 0x4000;
                default_firstdisksize = default_disksize - default_firstdiskstart;
                default_lastdisksize = default_disksize;
            }
            else if (DiskImageFormat == DiskImageFormat.__ROMWBW)
            {
                clusters = clusters_68mb;
                default_disksize = 0x820000; //0x800000;
                default_firstdiskstart = 0x20000; //0x4000;

                default_firstdisksize = default_disksize;
                default_lastdisksize = default_disksize;
            }
            else if (DiskImageFormat == DiskImageFormat._128MB_Searle)
            {
                clusters = clusters_128mb;
                default_disksize = 0x800000;
                default_firstdiskstart = 0x4000;

                default_firstdisksize = default_disksize - default_firstdiskstart;
                default_lastdisksize = default_disksize;
            }

            fileData = new byte[512 * clusters];

            disk_number = 0;

            UpdateCurrentDiskStart();


            createEmptyFileEntries();

            UpdateDiskList();
        }


        public void NewImage(DiskImageFormat _diskImageSize)
        {
            SetFormat(_diskImageSize);
        }


        private void createEmptyFileEntries()
        {
            for (int disk_number = 0; disk_number < disk_count; disk_number++)
            {
                UpdateCurrentDiskStart();

                for (int file_entry = disk_start; file_entry < disk_start + default_firstdiskstart; file_entry += 0x20)
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
            UpdateCurrentDiskStart();
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
                        _rc = isfull ? 0x80 : (int)Math.Ceiling(((((double)data.Length / 0x8000) - (current_block_count <= 4 ? 0 : 0.5)) * 0x100)),
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



        public void cmd_chuser(String filename, int newuser)
        {
            disk.SetUser(filename, newuser);

            foreach (FileEntry f in disk.GetFileEntries(filename))
            {
                Buffer.BlockCopy(f.GetDataEntry(), 0, fileData, disk_start + f._entry, 32);
            }

            disk.LoadDisk(fileData, disk_start);

        }



        public bool ReadImageFile(String fileName, ToolStripProgressBar progressBar = null)
        {
            try
            {
                if (File.Exists(fileName))
                {

                    FileInfo fi = new FileInfo(fileName);

                    DiskImageFormat =
                        (
                        fi.Length >= 100000000 ? DiskImageFormat._128MB_Searle :
                        (fi.Length >= 68000000 ? DiskImageFormat.__ROMWBW : DiskImageFormat._64MB_Searle)
                        );

                    SetFormat(DiskImageFormat);
                    disk = new Disk(DiskImageFormat);


                    //fileData = new byte[fi.Length];

                    if (progressBar != null)
                    {
                        progressBar.Minimum = 0;
                        progressBar.Maximum = (int)fi.Length;
                        progressBar.Visible = true;
                    }


                    using (BinaryReader b = new BinaryReader(
                    File.Open(fileName, FileMode.Open)))
                    {
                        // 2.
                        // Position and length variables.
                        int pos = 0;
                        // 2A.
                        // Use BaseStream.
                        int length = (int)b.BaseStream.Length;
                        while (pos < length && pos < fileData.Length)
                        {

                            fileData[pos] = b.ReadByte();
                            // 3.
                            // Read integer.
                            //int v = b.ReadInt32();
                            //Console.WriteLine(v);

                            // 4.
                            // Advance our position variable.

                            if (progressBar != null)
                            {
                                if (pos % 1000000 == 0)
                                {
                                    progressBar.Value = pos;
                                    Application.DoEvents();
                                }
                            }

                            pos += sizeof(byte);
                        }
                    }

                    if (progressBar != null)
                        progressBar.Visible = false;

                    UpdateCurrentDiskStart();

                    this.UpdateDiskList();
                    return true;
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("An error occurred while reading the image file.\nPlease check the file.", "Reading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (progressBar != null)
                progressBar.Visible = false;

            return false;
        }


        public bool SaveImageFile(string fileName, ToolStripProgressBar progressBar = null)
        {
            try
            {
                if (progressBar != null)
                {
                    progressBar.Minimum = 0;
                    progressBar.Maximum = (int)fileData.Length;
                    progressBar.Visible = true;
                }


                using (BinaryWriter b = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    for (int pos = 0; pos < fileData.Length; pos++)
                    {
                        b.Write(fileData[pos]);
                        if (progressBar != null)
                        {
                            if (pos % 1000000 == 0)
                            {
                                progressBar.Value = pos;
                                Application.DoEvents();
                            }
                        }
                    }
                }


                if (progressBar != null)
                    progressBar.Visible = false;

                return true;

            }
            catch
            {
                MessageBox.Show("An error occurred while writing the image file.\nPlease check the file.", "Writing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (progressBar != null)
                progressBar.Visible = false;

            return false;
        }


        public void ReadRawDisk(int selectedVol)
        {
            try
            {
                int clusters = DiskImageFormat == DiskImageFormat._128MB_Searle ? clusters_128mb :
                    (DiskImageFormat == DiskImageFormat.__ROMWBW ? clusters_68mb : clusters_64mb);


                using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                {
                    fileData = new byte[512 * clusters];
                    fileData = disk.ReadClusters(0, clusters);
                }

                UpdateCurrentDiskStart();

                this.UpdateDiskList();

            }
            catch
            {
                MessageBox.Show("An error occurred while reading disk.", "Reading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void WriteRawDisk(int selectedVol)
        {
            try
            {
                using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                {
                    disk.WriteClusters(fileData, 0);
                }
            }
            catch
            {
                MessageBox.Show("An error occurred while writing the disk.", "Writing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
