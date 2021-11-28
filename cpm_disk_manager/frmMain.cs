using RawDiskLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;

namespace cpm_disk_manager
{
    public partial class frmMain : Form
    {

        private ListViewColumnSorter lvwColumnSorter;

        public frmMain()
        {
            InitializeComponent();

            contextMenuStrip1_Opening(null, null);



            openFileToolStripMenuItem.Enabled = false;
            deleteToolStripMenuItem1.Enabled = false;

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");

            listView1.Columns.Add("Title");
            listView1.Columns.Add("User");
            /*
            listView1.Columns.Add("s1");
            listView1.Columns.Add("s2");
            listView1.Columns.Add("ex");
            listView1.Columns.Add("rc");
            */
            listView1.Columns.Add("Size");
            listView1.Columns.Add("Extension");
            /*
            listView1.Columns.Add("_size");
            listView1.Columns.Add("_start");
            

            listView1.Columns.Add("_entry_number");
            listView1.Columns.Add("_num_records");
            */
            lvwColumnSorter = new ListViewColumnSorter();
            this.listView1.ListViewItemSorter = lvwColumnSorter;

            try
            {
                listView1.Columns[0].Width = int.Parse(ini.IniReadValue("general", "col_0"));
                listView1.Columns[1].Width = int.Parse(ini.IniReadValue("general", "col_1"));
                listView1.Columns[2].Width = int.Parse(ini.IniReadValue("general", "col_2"));
                listView1.Columns[3].Width = int.Parse(ini.IniReadValue("general", "col_3"));


                lvwColumnSorter.SortColumn = int.Parse(ini.IniReadValue("general", "sort_column"));
                lvwColumnSorter.Order = (SortOrder)int.Parse(ini.IniReadValue("general", "column_order"));

                refresh_sort_dir();

            }
            catch { }



            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }


            readDiskToolStripMenuItem.Enabled = isElevated;
            writeDiskToolStripMenuItem.Enabled = isElevated;
            selectedVol = -1;



            previousDiskToolStripMenuItem.Enabled = false;
            nextDiskToolStripMenuItem.Enabled = false;


            openRecentToolStripMenuItem_Click(null, null);
        }

        int selectedVol = -1;
        DiskInfo selDisk;

        String current_filename = "";
        bool isRenaming = false;

        const int CF_CARD_LBA_SIZE = 0x800;         // temporary small size

        const int DISK_SIZE = 0x800000;

        //int parent_dir_LBA = 0x00;
        Stack<int> parentStack = new Stack<int>();

        Byte[] fileData;

        Disk disk = new Disk();
        List<FileEntry> FAT = new List<FileEntry>();
        int disk_start = 0;
        int disk_number = 0;
        int disk_count = 0;

        Byte[] getByteArray(byte[] fileBytes2, int start, int max)
        {
            Byte[] ret = new byte[max];
            for (int i = 0; i < max; i++)
                ret[i] = fileBytes2[start + i];

            return ret;
        }


        public byte[] ReadAllBytes(BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }

        }

        void cmd_ls()
        {


            listView1.Items.Clear();

            if (parentStack.Count > 0)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.ImageIndex = 0;
                lvi.Name = "Title";
                lvi.Text = "..";

                lvi.Tag = "-1";

                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "Type", Text = "Directory" });

                listView1.Items.Add(lvi);
            }

            int itemCount = 0;

            //while (index < FST_FILES_PER_DIR)//cmd_ls_L1:
            foreach (FileEntry f in disk.cmd_ls())
            {
                int num = f._size;
                String size = "";


                if (num > 1000)
                    size = (num / 1000.0).ToString("N3") + " KB";
                else
                    size = num.ToString("N0") + " B";

                ListViewItem lvi = new ListViewItem();
                lvi.ImageIndex = 2;
                lvi.Text = f._name.Trim() + "." + f._ext.Trim();
                lvi.Tag = f._name + f._ext;
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "User", Text = f._status.ToString("X2") });
                /*
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "s1", Text = f._s1.ToString("X2") });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "s2", Text = f._s2.ToString("X2") });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "ex", Text = f._ex.ToString("X2") });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "rc", Text = f._rc.ToString("X2") });
                */
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "Size", Text = size });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "Extension", Text = f._ext.Trim() });
                
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "_size", Text = num.ToString() });
                /*
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "_start", Text = f._start.ToString("X4") });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "_entry_number", Text = f._entry_number.ToString("X4") });
                lvi.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "_num_records", Text = f._num_records.ToString("X4") });
                */
                listView1.Items.Add(lvi);

                itemCount++;


            }

            if (itemCount == 1)
                statusCount.Text = CurrentDriveLabel() + " " + itemCount.ToString() + " item";

            else
                statusCount.Text = CurrentDriveLabel() + " " + itemCount.ToString() + " items";
        }







        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {

                byte[] data = disk.GetFile((string)listView1.SelectedItems[0].Tag);

                int start_address = 0;
                try
                {
                    IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                    String hex_start = ini.IniReadValue("address_start", "disk_buffer");
                    start_address = Convert.ToInt32(hex_start, 16);
                }
                catch
                { }

                frmEditFile frmedit = new frmEditFile();
                frmedit.setTitle("File: " + listView1.SelectedItems[0].Text);
                frmedit.Start_Address = start_address;
                frmedit.setBinary(data);
                frmedit.ShowDialog(this);

                byte[] newdata = frmedit.getBinary();
                if (!Utils.CompareByteArrays(data, newdata))
                {
                    disk.DeleteFile((string)listView1.SelectedItems[0].Tag);
                    cmd_mkbin((string)listView1.SelectedItems[0].Text, frmedit.getBinary());
                }
            }
        }



        void refreshIconView()
        {

            Bitmap[] i = {
                global::cpm_disk_manager.Properties.Resources.icoLargeIcon,
                global::cpm_disk_manager.Properties.Resources.icoDetails,
            //    global::cpm_disk_manager.Properties.Resources.ico62999,
                global::cpm_disk_manager.Properties.Resources.icoSmallIcon,

                global::cpm_disk_manager.Properties.Resources.icoList,
                global::cpm_disk_manager.Properties.Resources.icoTile };
            toolStripSplitButton1.Image = i[(int)listView1.View];
        }



        Byte ConvertIntHexToByte(int d)
        {
            return Convert.ToByte(d.ToString(), 16);

        }


        private static DialogResult ShowInputDialog(ref string input, String Title, Form owner)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 70);
            Form inputBox = new Form();
            inputBox.StartPosition = FormStartPosition.CenterParent;

            inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = Title;

            System.Windows.Forms.TextBox textBox = new TextBox();
            textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
            textBox.Location = new System.Drawing.Point(5, 5);
            textBox.Text = input;
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog(owner);
            input = textBox.Text;
            return result;
        }

        /*
        int find_file(string title, int lba)
        {
            return find_file(title, getByteArray(fileData, lba * 512, 512));
        }


        int find_file(string title, Byte[] diskbuffer)
        {
            int d = 0;

            int index = 0;

            while (Utils.getStringFromByteArray(diskbuffer, d, 0x20) != title && index < FST_FILES_PER_DIR)
            {
                d = d + 0x20;
                index++;
                if (index == FST_FILES_PER_DIR)
                    return -1;
            }

            return d;
        }
        */

        void cmd_rmdir(string filename)
        {
            disk.DeleteFile(filename);

            foreach (FileEntry f in disk.GetFileEntries(filename))
            {
                Buffer.BlockCopy(f.GetDataEntry(), 0, fileData, disk_start + f._entry, 32);
            }

            disk.LoadDisk(fileData, disk_start);
            cmd_ls();
        }


        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        void cmd_mktxt(string str, string txt)
        {

            byte[] data = System.Text.Encoding.ASCII.GetBytes(txt);

            cmd_mkbin(str, data);
        }


        void cmd_mkbin(string str, byte[] data)
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

            update_disk_list();
        }


        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isRenaming) return;

            if (e.KeyCode == Keys.Delete)
            {
                deleteToolStripMenuItem_Click(sender, e);
            }

            else if (e.KeyCode == Keys.F2)
            {
                if (listView1.SelectedItems.Count == 1)
                {
                    isRenaming = true;
                    listView1.SelectedItems[0].BeginEdit();
                }
            }

            else if (e.KeyCode == Keys.Enter)
            {
                listView1_DoubleClick(null, null);
            }
        }


        private bool cmd_rename(String filename, String newname, LabelEditEventArgs e)
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

                listView1.Items[e.Item].Text = name.Trim() + "." + ext.Trim();
                listView1.Items[e.Item].Tag = name + ext;
                return true;
            }

            return false;
        }


        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label != null)
            {
                e.CancelEdit = !cmd_rename((string)listView1.Items[e.Item].Tag, e.Label, e);
                e.CancelEdit = true;
            }

            isRenaming = false;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            newFileToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 0;

            renameToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 1;
            deleteToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 1;
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                listView1.SelectedItems[0].BeginEdit();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                DialogResult dialogResult = MessageBox.Show("Confirm Delete \"" + listView1.SelectedItems[0].Text + "\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                    cmd_rmdir((string)listView1.SelectedItems[0].Tag);
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            int itemCount = listView1.SelectedItems.Count;
            int itemSize = 0;
            String itemSizeText = "";

            if (itemCount != 0)
            {
                foreach (ListViewItem item in listView1.SelectedItems)
                {
                    itemSize += int.Parse(item.SubItems["_size"].Text);
                }
            }

            if (itemSize > 1000000)
            {
                itemSizeText = (itemSize / 1000000.0).ToString("N2") + " MB";
            }
            else if (itemSize > 1000)
            {
                itemSizeText = (itemSize / 1000.0).ToString("N2") + " KB";
            }
            else
                itemSizeText = itemSize + " B";


            if (itemCount == 1)
            {
                statusSelectedCount.Text = itemCount.ToString() + " item selected";
                statusSelectedSize.Text = itemSizeText;
            }
            else if (itemCount > 1)
            {
                statusSelectedCount.Text = itemCount.ToString() + " items selected";
                statusSelectedSize.Text = itemSizeText;
            }
            else
                statusSelectedCount.Text = "";


            newFileToolStripMenuItem1.Enabled = listView1.SelectedItems.Count == 0;
            openFileToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 1;
            deleteToolStripMenuItem1.Enabled = listView1.SelectedItems.Count == 1;

        }


        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            if (listView1.View == View.Tile)
                listView1.View = 0;
            else
                listView1.View++;

            refreshIconView();

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "view", ((int)listView1.View).ToString());
        }

        private void largeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.LargeIcon;
            refreshIconView();
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            refreshIconView();
        }

        private void smallIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.SmallIcon;
            refreshIconView();
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.List;
            refreshIconView();
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Tile;
            refreshIconView();

        }

        bool READ(String fileName)
        {
            if (File.Exists(fileName))
            {

                current_filename = fileName;

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
                update_disk_list();
                return true;
            }
            return false;
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedVol = -1;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog1.FileName;

                IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                ini.IniWriteValue("general", "last_open", filename);

                if (READ(filename))
                {
                    this.Text = filename;
                    READ(filename);

                }
            }
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (current_filename != "")
            {
                if (MessageBox.Show("Save file?", "Confirm save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    WRITE(current_filename);
                }
            };

        }

        bool WRITE(string fileName)
        {
            using (BinaryWriter b = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                for (int i = 0; i < fileData.Length; i++)
                    b.Write(fileData[i]);
            }

            return true;
        }


        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            deleteToolStripMenuItem_Click(sender, e);
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String str = "";

            if (ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
                if (str.Trim() != "")
                {
                    int start_address = 0;
                    try
                    {
                        IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                        String hex_start = ini.IniReadValue("address_start", "disk_buffer");
                        start_address = Convert.ToInt32(hex_start, 16);
                    }
                    catch
                    { }

                    frmEditFile frmedit = new frmEditFile();
                    frmedit.setTitle("New Text File: " + str);
                    frmedit.Start_Address = start_address;
                    frmedit.ShowUndo = false;
                    frmedit.ShowEditorType = false;
                    frmedit.FileType = frmEditFile.EditorType.Text;
                    frmedit.newFile();
                    frmedit.ShowDialog(this);

                    DialogResult dialogResult = MessageBox.Show("Save New File \"" + str + "\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        cmd_mktxt(str, frmedit.getText());
                        cmd_ls();

                    }

                }
        }

        private void binaryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String str = "";

            if (ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
                if (str.Trim() != "")
                {

                    int start_address = 0;
                    try
                    {
                        IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                        String hex_start = ini.IniReadValue("address_start", "disk_buffer");
                        start_address = Convert.ToInt32(hex_start, 16);
                    }
                    catch
                    { }

                    frmEditFile frmedit = new frmEditFile();
                    frmedit.setTitle("New Binary File: " + str);
                    frmedit.Start_Address = start_address;
                    frmedit.ShowUndo = false;
                    frmedit.ShowEditorType = false;
                    frmedit.FileType = frmEditFile.EditorType.Binary;
                    frmedit.newFile();
                    frmedit.ShowDialog(this);

                    DialogResult dialogResult = MessageBox.Show("Save New File \"" + str + "\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Byte[] data = frmedit.getBinary();
                        cmd_mkbin(str, data);
                        cmd_ls();

                    }

                }
        }

        private void textToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String str = "";

            if (ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
                if (str.Trim() != "")
                {

                    int start_address = 0;
                    try
                    {
                        IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                        String hex_start = ini.IniReadValue("address_start", "disk_buffer");
                        start_address = Convert.ToInt32(hex_start, 16);
                    }
                    catch
                    { }

                    frmEditFile frmedit = new frmEditFile();
                    frmedit.setTitle("New Text File: " + str);
                    frmedit.Start_Address = start_address;
                    frmedit.ShowUndo = false;
                    frmedit.ShowEditorType = false;
                    frmedit.FileType = frmEditFile.EditorType.Text;
                    frmedit.newFile();
                    frmedit.ShowDialog(this);

                    DialogResult dialogResult = MessageBox.Show("Save New File \"" + str + "\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        cmd_mktxt(str, frmedit.getText());
                        cmd_ls();

                    }

                }
        }

        private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String str = "";

            if (ShowInputDialog(ref str, "File Binary Name", this) == DialogResult.OK)
                if (str.Trim() != "")
                {
                    int start_address = 0;
                    try
                    {
                        IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                        String hex_start = ini.IniReadValue("address_start", "disk_buffer");
                        start_address = Convert.ToInt32(hex_start, 16);
                    }
                    catch
                    { }

                    frmEditFile frmedit = new frmEditFile();
                    frmedit.setTitle("New File: " + str);
                    frmedit.Start_Address = start_address;
                    frmedit.ShowUndo = false;
                    frmedit.ShowEditorType = false;
                    frmedit.FileType = frmEditFile.EditorType.Binary;
                    frmedit.newFile();
                    frmedit.ShowDialog(this);

                    DialogResult dialogResult = MessageBox.Show("Save New File \"" + str + "\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        cmd_mkbin(str, frmedit.getBinary());
                        cmd_ls();

                    }

                }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            bool newfile = false;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    using (BinaryReader b = new BinaryReader(File.Open(file, FileMode.Open)))
                    {
                        string filename = Path.GetFileName(file);
                        Byte[] data = ReadAllBytes(b);
                        cmd_mkbin(filename, data);

                    }
                    newfile = true;
                }

            }
            if (newfile)
                cmd_ls();
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openRecentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedVol = -1;
            string filename = "";
            string viewstyle = "";

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            filename = ini.IniReadValue("general", "last_open");
            viewstyle = ini.IniReadValue("general", "view");
            int result = -1;
            if (int.TryParse(viewstyle, out result))
            {
                if (result >= 0 && result <= 4)
                {
                    listView1.View = (View)result;
                    refreshIconView();
                }
            }

            if (filename.Trim() != "")
            {
                this.Text = filename;
                READ(filename);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                if (WRITE(filename))
                    this.Text = filename;
            }

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmAbout about = new FrmAbout();
            about.Show(this);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1_DoubleClick(null, null);
        }


        private void editBootToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Byte[] data = fileData.Take(0x200).ToArray();

            int start_address = 0;
            try
            {
                IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                String hex_start = ini.IniReadValue("address_start", "boot_origin");
                start_address = Convert.ToInt32(hex_start, 16);
            }
            catch
            { }

            frmEditFile frmedit = new frmEditFile();
            frmedit.setTitle("Edit Boot");
            frmedit.Start_Address = start_address;
            frmedit.setBinary(data);
            frmedit.ShowEditorType = false;
            frmedit.ShowDialog(this);

            byte[] newdata = frmedit.getBinary();
            if (!Utils.CompareByteArrays(data, newdata))
            {
                DialogResult dialogResult = MessageBox.Show("Confirm Edit of the \"Boot Sector\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    if (frmedit.FileType == frmEditFile.EditorType.Binary)
                    {
                        Byte[] filearray = StringToByteArray(frmedit.getText());

                        int i = 0;
                        for (; i < filearray.Length && i < 0x200; i++)
                            fileData[i] = filearray[i];

                        for (; i < 0x200; i++)
                            fileData[i] = 0x00;
                    }
                }
            }
        }

        private void editKernelToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Byte[] data = fileData.Skip(0x200).Take(0x4000 - 0x200).ToArray();

            int start_address = 0;
            try
            {
                IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                String hex_start = ini.IniReadValue("address_start", "kernel_origin");
                start_address = Convert.ToInt32(hex_start, 16);
            }
            catch
            { }

            frmEditFile frmedit = new frmEditFile();
            frmedit.setTitle("Edit Kernel");
            frmedit.Start_Address = start_address;
            frmedit.setBinary(data);
            frmedit.ShowEditorType = false;
            frmedit.ShowDialog(this);

            byte[] newdata = frmedit.getBinary();
            if (!Utils.CompareByteArrays(data, newdata))
            {
                DialogResult dialogResult = MessageBox.Show("Confirm Edit of the \"Kernel Sector\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    if (frmedit.FileType == frmEditFile.EditorType.Binary)
                    {

                        Byte[] filearray = StringToByteArray(frmedit.getText());

                        int i = 0;
                        for (; i < filearray.Length && (i + 0x200) < 0x4000; i++)
                            fileData[i + 0x200] = filearray[i];

                        for (; i < 0x4000; i++)
                            fileData[i + 0x200] = 0x00;
                    }
                }
            }
        }

        private void listView1_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "col_" + e.ColumnIndex.ToString(), listView1.Columns[e.ColumnIndex].Width.ToString());
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1_DoubleClick(null, null);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmd_ls();
        }

        private void newImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedVol = -1;

            int clusters = 8192;
            fileData = new byte[512 * clusters];
            cmd_ls();
        }




        private void readDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmDiskSelection frmDisk = new frmDiskSelection();
            frmDisk.ShowDialog(this);

            selDisk = frmDisk.SelDisk;

            if (selDisk.Id != -1)
            {
                DialogResult dialogResult = MessageBox.Show("Load Disk \"" + selDisk.Name + "\"?", "Load Media", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    selectedVol = selDisk.Id;
                    using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                    {
                        int clusters = 8192;
                        fileData = new byte[512 * clusters];
                        fileData = disk.ReadClusters(0, clusters);
                    }

                    cmd_ls();
                }
            }
            else
                selectedVol = -1;

        }

        private void writeDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (selectedVol == -1)
            {
                frmDiskSelection frmDisk = new frmDiskSelection();
                frmDisk.ShowDialog(this);

                selDisk = frmDisk.SelDisk;

                if (selDisk.Id != -1)
                {
                    DialogResult dialogResult = MessageBox.Show("Save Disk \"" + selDisk.Name + "\"?", "Save Media", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        int selectedVol = selDisk.Id;
                        using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                        {
                            try
                            {
                                disk.WriteClusters(fileData, 0);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }

            }
            else
            {
                DialogResult dialogResult = MessageBox.Show("Save Disk \"" + selDisk.Name + "\"?", "Save Media", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    int selectedVol = selDisk.Id;
                    using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                    {
                        try
                        {
                            disk.WriteClusters(fileData, 0);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }


        }

        long ClustersToRead = 100;
        public void PresentResult(RawDisk disk)
        {



            byte[] data = disk.ReadClusters(0, (int)Math.Min(disk.ClusterCount, ClustersToRead));

            string fatType = Encoding.ASCII.GetString(data, 82, 8);     // Extended FAT parameters have a display name here.
            bool isFat = fatType.StartsWith("FAT");
            bool isNTFS = Encoding.ASCII.GetString(data, 3, 4) == "NTFS";

            // Optimization, if it's a known FS, we know it's not all zeroes.
            bool allZero = (!isNTFS || !isFat) && data.All(s => s == 0);

            Console.WriteLine("Size in bytes : {0:N0}", disk.SizeBytes);
            Console.WriteLine("Sectors       : {0:N0}", disk.ClusterCount);
            Console.WriteLine("SectorSize    : {0:N0}", disk.SectorSize);
            Console.WriteLine("ClusterCount  : {0:N0}", disk.ClusterCount);
            Console.WriteLine("ClusterSize   : {0:N0}", disk.ClusterSize);
            Console.WriteLine("Is NTFS       : {0}", isNTFS);
            Console.WriteLine("Is FAT        : {0}", isFat ? fatType : "False");

            Console.WriteLine("All bytes zero: {0}", allZero);
        }

        private void editorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmEditFile frmedit = new frmEditFile();
            frmedit.setTitle("Editor");
            frmedit.Start_Address = 0;
            frmedit.newFile();
            frmedit.ShowEditorType = false;
            frmedit.ShowDialog(this);
        }

        private void previousDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (disk_number > 0)
                disk_number--;

            update_disk_list();
        }

        private void nextDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (disk_number < disk_count - 1)
                disk_number++;

            update_disk_list();
        }

        void update_disk_list()
        {

            previousDiskToolStripMenuItem.Enabled = (disk_number > 0);
            nextDiskToolStripMenuItem.Enabled = (disk_number < disk_count - 1);

            disk_start = disk_number * 0x800000;
            disk.LoadDisk(fileData, disk_start);
            cmd_ls();
        }

        string CurrentDriveLabel()
        {
            return (char)(65 + disk_number) + ":";
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }


            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "sort_column", lvwColumnSorter.SortColumn.ToString());
            ini.IniWriteValue("general", "column_order", ((int)lvwColumnSorter.Order).ToString());
            
            refresh_sort_dir();

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }


        private void refresh_sort_dir()
        {
            foreach(ColumnHeader c in listView1.Columns)
                SetSortArrow(c, SortOrder.None);

            SetSortArrow(listView1.Columns[lvwColumnSorter.SortColumn], lvwColumnSorter.Order);
        }
        private void SetSortArrow(ColumnHeader head, SortOrder order)
        {
            const string ascArrow = " ▲";
            const string descArrow = " ▼";

            // remove arrow
            if (head.Text.EndsWith(ascArrow) || head.Text.EndsWith(descArrow))
                head.Text = head.Text.Substring(0, head.Text.Length - 2);

            // add arrow
            switch (order)
            {
                case SortOrder.Ascending: head.Text += ascArrow; break;
                case SortOrder.Descending: head.Text += descArrow; break;
            }
        }
    }
}

