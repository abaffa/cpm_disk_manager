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
using System.Globalization;

namespace cpm_disk_manager
{
    public partial class frmMain : Form
    {

        private ListViewColumnSorter lvwColumnSorter;

        private String current_filename = "";
        private bool isRenaming = false;
        private DiskImage diskImage = new DiskImage();


        private Dictionary<string, byte[]> clipboard = new Dictionary<string, byte[]>();

        // memory card
        DiskInfo selDisk;
        int selectedVol = -1;
        int selectedUser = -1;
        long ClustersToRead = 100;
        //

        public frmMain()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

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



            previousDiskToolStripMenuItem.Enabled = false;
            nextDiskToolStripMenuItem.Enabled = false;



            int diskimagesize = 64;
            try
            {
                diskimagesize = int.Parse(ini.IniReadValue("general", "disk_image_size"));

            }
            catch { }


            switch (diskimagesize)
            {
                case 68:
                    romWBWToolStripMenuItem_Click(this, null);
                    break;                    
                case 128:
                    mBToolStripMenuItem1_Click(this, null);
                    break;
                default:
                    mBToolStripMenuItem_Click(this, null);
                    break;
            }
                
        }




        void cmd_ls()
        {
            listView1.SelectedItems.Clear();
            listView1.Items.Clear();

            int itemCount = 0;

            foreach (FileEntry f in diskImage.Disk.cmd_ls())
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

                byte[] data = diskImage.Disk.GetFile((string)listView1.SelectedItems[0].Tag);

                frmEditFile frmedit = new frmEditFile();
                frmedit.setTitle("File: " + listView1.SelectedItems[0].Text);
                frmedit.Start_Address = 0;
                frmedit.setFilename(listView1.SelectedItems[0].Text);
                frmedit.setBinary(data);
                frmedit.ShowDialog(this);

                byte[] newdata = frmedit.getBinary();
                if (frmedit.getSaveKeyHit() || (!Utils.CompareByteArrays(data, newdata) && MessageBox.Show("Save file " + listView1.SelectedItems[0].Text + "?", "Confirm save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                {
                    diskImage.Disk.DeleteFile((string)listView1.SelectedItems[0].Tag);
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
            diskImage.cmd_rmdir(filename);
            cmd_ls();
        }


        void cmd_mktxt(string str, string txt)
        {

            byte[] data = System.Text.Encoding.ASCII.GetBytes(txt);

            cmd_mkbin(str, data);
        }


        void cmd_mkbin(string str, byte[] data)
        {
            diskImage.cmd_mkbin(str, data);

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
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    item.Selected = true;
                }
            }

            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.C && listView1.SelectedItems.Count > 0)
            {
                copyToolStripMenuItem_Click(null, null);
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.C && listView1.Items.Count > 0)
            {
                String clipboard = "";
                foreach (ListViewItem lvi in listView1.Items)
                {

                    clipboard += lvi.Text.PadRight(15);
                    clipboard += "U" + lvi.SubItems[1].Text.PadRight(5);
                    clipboard += lvi.SubItems[2].Text;
                    clipboard += "\r\n";
                }
                try
                {
                    Clipboard.SetText(clipboard);
                }
                catch { }
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V && clipboard.Count > 0)
            {
                pasteToolStripMenuItem_Click(null, null);
            }


        }


        private bool cmd_rename(String filename, String newname, LabelEditEventArgs e)
        {
            if (diskImage.cmd_rename(filename, newname))
            {
                String name = newname.IndexOf('.') > -1 ? newname.Substring(0, newname.IndexOf('.')) : newname;
                String ext = newname.IndexOf('.') > -1 ? newname.Substring(newname.IndexOf('.') + 1) : "";

                name = name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 8)) : "";
                ext = ext.Length > 0 ? ext.Substring(0, Math.Min(ext.Length, 3)) : "";

                name = name.PadRight(8);
                ext = ext.PadRight(3);

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

            editToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 1;
            renameToolStripMenuItem.Enabled = listView1.SelectedItems.Count == 1;
            deleteToolStripMenuItem.Enabled = listView1.SelectedItems.Count != 0;
            exportToolStripMenuItem.Enabled = listView1.Items.Count != 0;

            copyToolStripMenuItem.Enabled = listView1.SelectedItems.Count != 0;
            userToolStripMenuItem.Enabled = listView1.SelectedItems.Count != 0;

            pasteToolStripMenuItem.Enabled = clipboard.Count != 0;


            renToUppercaseToolStripMenuItem.Enabled = listView1.SelectedItems.Count != 0;
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
            else if (listView1.SelectedItems.Count > 1)
            {

                DialogResult dialogResult = MessageBox.Show("Confirm Delete all \"" + listView1.SelectedItems.Count.ToString() + "\" files?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {

                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        cmd_rmdir((string)item.Tag);
                        item.Selected = false;
                    }

                    listView1_ItemSelectionChanged(null, null);
                }
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            int itemCount = listView1.SelectedItems.Count;
            int itemSize = 0;
            String itemSizeText = "";

            selectedUser = -1;

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

                List<FileEntry> fileEntries = diskImage.Disk.GetFileEntry((string)listView1.SelectedItems[0].Tag);

                if (fileEntries != null)
                {
                    if (fileEntries[0]._status < toolStripComboBox1.Items.Count)
                    {
                        toolStripComboBox1.SelectedIndex = fileEntries[0]._status;
                        selectedUser = fileEntries[0]._status;
                    }
                }

            }
            else if (itemCount > 1)
            {
                statusSelectedCount.Text = itemCount.ToString() + " items selected";
                statusSelectedSize.Text = itemSizeText;

                int files_users = -1;
                bool many_users = false;

                foreach (ListViewItem item in listView1.SelectedItems)
                {
                    List<FileEntry> fileEntries = diskImage.Disk.GetFileEntry((string)item.Tag);

                    if (fileEntries != null)
                    {
                        if (fileEntries[0]._status < toolStripComboBox1.Items.Count)
                        {

                            if (files_users == -1)
                            {
                                selectedUser = fileEntries[0]._status;
                                files_users = fileEntries[0]._status;


                            }
                            else if (files_users != fileEntries[0]._status)
                            {
                                many_users = true;
                            }
                        }
                    }
                }

                if (many_users)
                    toolStripComboBox1.SelectedIndex = -1;
                else
                    toolStripComboBox1.SelectedIndex = files_users;
            }
            else
            {
                statusSelectedCount.Text = "";
                statusSelectedSize.Text = "";
            }


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

        private bool readImageFile(String fileName)
        {
            if (diskImage.ReadImageFile(fileName, toolStripProgressBar1))
            {

                if (diskImage.DiskImageFormat == DiskImageFormat._128MB_Searle)
                    mBToolStripMenuItem1_Click(this, null);
                else if (diskImage.DiskImageFormat == DiskImageFormat.__ROMWBW)
                    romWBWToolStripMenuItem_Click(this, null);  
                else
                    mBToolStripMenuItem_Click(this, null);


                    selectedVol = -1;
                current_filename = fileName;

                update_disk_list();
                return true;
            }
            return false;
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = current_filename;
            openFileDialog1.AddExtension = true;
            openFileDialog1.DefaultExt = "dsk";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Disk Images (*.dsk, *.img)|*.dsk; *.img|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog1.FileName;

                IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
                ini.IniWriteValue("general", "last_open", filename);

                if (readImageFile(filename))
                {
                    this.Text = "CP/M Disk Manager " + filename;
                }
                else
                {
                    current_filename = "";
                    this.Text = "CP/M Disk Manager (No File)";
                }

            }
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (current_filename != "")
            {
                if (MessageBox.Show("Save file?", "Confirm save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {

                    if (saveImageFile(current_filename))
                        this.Text = "CP/M Disk Manager " + current_filename;
                    else
                    {
                        current_filename = "";
                        //this.Text = "CP/M Disk Manager (No File)";
                    }
                }
            }

        }

        private bool saveImageFile(string fileName)
        {
            return diskImage.SaveImageFile(fileName, toolStripProgressBar1);
        }


        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            deleteToolStripMenuItem_Click(sender, e);
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String str = "";

            if (Utils.ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
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

            if (Utils.ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
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

            if (Utils.ShowInputDialog(ref str, "File Name", this) == DialogResult.OK)
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

            if (Utils.ShowInputDialog(ref str, "File Binary Name", this) == DialogResult.OK)
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
                        Byte[] data = Utils.ReadAllBytes(b);
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
                if (readImageFile(filename))
                {
                    this.Text = "CP/M Disk Manager " + filename;
                }
                else
                {
                    current_filename = "";
                    this.Text = "CP/M Disk Manager (No File)";
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            saveFileDialog1.FileName = current_filename;
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = "dsk";
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.Filter = "Disk Images (*.dsk, *.img)|*.dsk; *.img|All Files (*.*)|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                if (saveImageFile(filename))
                {
                    this.Text = "CP/M Disk Manager " + filename;
                }
                else
                {
                    current_filename = "";
                    //this.Text = "CP/M Disk Manager (No File)";
                }
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

            //Byte[] data = fileData.Take(0x200).ToArray();

            //int start_address = 0;
            //try
            //{
            //    IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            //    String hex_start = ini.IniReadValue("address_start", "boot_origin");
            //    start_address = Convert.ToInt32(hex_start, 16);
            //}
            //catch
            //{ }

            //frmEditFile frmedit = new frmEditFile();
            //frmedit.setTitle("Edit Boot");
            //frmedit.Start_Address = start_address;
            //frmedit.setBinary(data);
            //frmedit.ShowEditorType = false;
            //frmedit.ShowDialog(this);

            //byte[] newdata = frmedit.getBinary();
            //if (!Utils.CompareByteArrays(data, newdata))
            //{
            //    DialogResult dialogResult = MessageBox.Show("Confirm Edit of the \"Boot Sector\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //    if (dialogResult == DialogResult.Yes)
            //    {
            //        if (frmedit.FileType == frmEditFile.EditorType.Binary)
            //        {
            //            Byte[] filearray = Utils.StringToByteArray(frmedit.getText());

            //            int i = 0;
            //            for (; i < filearray.Length && i < 0x200; i++)
            //                fileData[i] = filearray[i];

            //            for (; i < 0x200; i++)
            //                fileData[i] = 0x00;
            //        }
            //    }
            //}
        }

        private void editKernelToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //Byte[] data = fileData.Skip(0x200).Take(0x4000 - 0x200).ToArray();

            //int start_address = 0;
            //try
            //{
            //    IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            //    String hex_start = ini.IniReadValue("address_start", "kernel_origin");
            //    start_address = Convert.ToInt32(hex_start, 16);
            //}
            //catch
            //{ }

            //frmEditFile frmedit = new frmEditFile();
            //frmedit.setTitle("Edit Kernel");
            //frmedit.Start_Address = start_address;
            //frmedit.setBinary(data);
            //frmedit.ShowEditorType = false;
            //frmedit.ShowDialog(this);

            //byte[] newdata = frmedit.getBinary();
            //if (!Utils.CompareByteArrays(data, newdata))
            //{
            //    DialogResult dialogResult = MessageBox.Show("Confirm Edit of the \"Kernel Sector\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //    if (dialogResult == DialogResult.Yes)
            //    {
            //        if (frmedit.FileType == frmEditFile.EditorType.Binary)
            //        {

            //            Byte[] filearray = Utils.StringToByteArray(frmedit.getText());

            //            int i = 0;
            //            for (; i < filearray.Length && (i + 0x200) < 0x4000; i++)
            //                fileData[i + 0x200] = filearray[i];

            //            for (; i < 0x4000; i++)
            //                fileData[i + 0x200] = 0x00;
            //        }
            //    }
            //}
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
            diskImage.NewImage(diskImage.DiskImageFormat);
            cmd_ls();


            if (diskImage.DiskImageFormat == DiskImageFormat.__ROMWBW)
                this.Text = "CP/M Disk Manager (New ROMWBW image)";
            else if (diskImage.DiskImageFormat == DiskImageFormat._128MB_Searle)
                this.Text = "CP/M Disk Manager (New 128MB image)";
            else
                this.Text = "CP/M Disk Manager (New 64MB image)";
        }



        private void editorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmEditFile frmedit = new frmEditFile();
            frmedit.setTitle("Editor");
            frmedit.Start_Address = 0;
            frmedit.newFile();
            frmedit.ShowDialog(this);
        }

        private void previousDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            diskImage.PreviousDisk();
            update_disk_list();
        }

        private void nextDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            diskImage.NextDisk();
            update_disk_list();
        }

        void update_disk_list()
        {

            previousDiskToolStripMenuItem.Enabled = (diskImage.DiskNumber > 0);
            nextDiskToolStripMenuItem.Enabled = (diskImage.DiskNumber < diskImage.DiskCount - 1);

            cmd_ls();
        }

        public string CurrentDriveLabel()
        {
            return (char)(65 + diskImage.DiskNumber) + ":";
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
            foreach (ColumnHeader c in listView1.Columns)
                Utils.SetSortArrow(c, SortOrder.None);

            Utils.SetSortArrow(listView1.Columns[lvwColumnSorter.SortColumn], lvwColumnSorter.Order);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        /// CF CARD

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
                    diskImage.ReadRawDisk(selectedVol);

                    update_disk_list();
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

                        try
                        {
                            diskImage.WriteRawDisk(selectedVol);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    try
                    {
                        diskImage.WriteRawDisk(selectedVol);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }


        }


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


        private void mBToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            mBToolStripMenuItem1.Checked = true;
            mBToolStripMenuItem.Checked = false;
            romWBWToolStripMenuItem.Checked = false;
            diskImage.DiskImageFormat = DiskImageFormat._128MB_Searle;

            newImageToolStripMenuItem.Text = "New Image(128MB)";

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "disk_image_size", "128");
        }

        private void mBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mBToolStripMenuItem1.Checked = false;
            mBToolStripMenuItem.Checked = true;
            romWBWToolStripMenuItem.Checked = false;
            diskImage.DiskImageFormat = DiskImageFormat._64MB_Searle;

            newImageToolStripMenuItem.Text = "New Image(64MB)";

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "disk_image_size", "64");
        }

        private void romWBWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mBToolStripMenuItem1.Checked = false;
            mBToolStripMenuItem.Checked = false;
            romWBWToolStripMenuItem.Checked = true;
            diskImage.DiskImageFormat = DiskImageFormat.__ROMWBW;

            newImageToolStripMenuItem.Text = "New Image(ROMWBW)";

            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\" + "config.ini");
            ini.IniWriteValue("general", "disk_image_size", "68");
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedUser > -1 && toolStripComboBox1.SelectedIndex != -1)
            {


                if (listView1.SelectedItems.Count == 1)
                {

                    diskImage.cmd_chuser((string)listView1.SelectedItems[0].Tag, toolStripComboBox1.SelectedIndex);

                    cmd_ls();

                    contextMenuStrip1.Hide();
                }
                else if (listView1.SelectedItems.Count > 1)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        diskImage.cmd_chuser((string)item.Tag, toolStripComboBox1.SelectedIndex);
                    }

                    cmd_ls();

                    contextMenuStrip1.Hide();
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clipboard.Clear();


            foreach (ListViewItem item in listView1.SelectedItems)
            {
                String filename = (string)item.Tag;
                filename = filename.PadRight(11);
                filename = filename.Substring(0, 8).Trim() + "." + filename.Substring(8, 3).Trim();

                clipboard.Add(filename, diskImage.Disk.GetFile((string)item.Tag));
            }

        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {

            HashSet<String> files = new HashSet<string>();
            foreach (FileEntry f in diskImage.Disk.cmd_ls())
            {
                String filename = f._name.Trim() + "." + f._ext.Trim();
                if (!files.Contains(filename))
                    files.Add(filename);
            }

            foreach (String filename in clipboard.Keys)
            {

                if (files.Contains(filename))
                {
                    DialogResult dialogResult = MessageBox.Show("Overwrite File \"" + filename + "\"?", "Overwrite", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        diskImage.Disk.DeleteFile(filename);
                        Byte[] data = clipboard[filename];
                        cmd_mkbin(filename, data);
                    }
                    else if (dialogResult == DialogResult.Cancel)
                    {
                        break;
                    }
                }
                else
                {
                    Byte[] data = clipboard[filename];
                    cmd_mkbin(filename, data);
                }
            }
            cmd_ls();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            openRecentToolStripMenuItem_Click(null, null);
        }

        private void renToUppercaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                String filename = (string)item.Tag.ToString().ToUpper();
                filename = filename.PadRight(11);
                filename = filename.Substring(0, 8).Trim() + "." + filename.Substring(8, 3).Trim();

                diskImage.cmd_rename(item.Tag.ToString(), filename);
            }
            cmd_ls();
        }

        private void filesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                byte[] data = diskImage.Disk.GetFile((string)listView1.SelectedItems[0].Tag);

                String filename = listView1.SelectedItems[0].Text;

                saveFileDialog1.FileName = filename;
                if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    filename = saveFileDialog1.FileName;

                    File.WriteAllBytes(filename, data);
                    //MessageBox.Show("FileS, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
            else
            {


                if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
                {

                    List<ListViewItem> col = new List<ListViewItem>();
                    if (listView1.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem lvi in listView1.SelectedItems)
                        {
                            col.Add(lvi);
                        }
                    }
                    else
                    {
                        foreach (ListViewItem lvi in listView1.Items)
                        {
                            col.Add(lvi);
                        }
                    }

                    foreach (ListViewItem lvi in col)
                    {
                        byte[] data = diskImage.Disk.GetFile((string)lvi.Tag);

                        String filename = lvi.Text;

                        File.WriteAllBytes(folderBrowserDialog1.SelectedPath + "\\" + filename, data);
                        //MessageBox.Show("FileS, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void filesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.AddExtension = true;
            openFileDialog1.DefaultExt = "*.*";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bool newfile = false;
                string[] files = openFileDialog1.FileNames;
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        using (BinaryReader b = new BinaryReader(File.Open(file, FileMode.Open)))
                        {
                            string filename = Path.GetFileName(file);
                            Byte[] data = Utils.ReadAllBytes(b);
                            cmd_mkbin(filename, data);

                        }
                        newfile = true;
                    }

                }
                if (newfile)
                    cmd_ls();

            }
        }

        private void pKGToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = "pkg";
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.Filter = "Package File (*.pkg)|*.pkg|All Files (*.*)|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string pkgfilename = saveFileDialog1.FileName;


                if (File.Exists(pkgfilename))
                    File.Delete(pkgfilename);

                using (TextWriter text_writer = File.CreateText(pkgfilename))
                {

                    List<ListViewItem> col = new List<ListViewItem>();
                    if (listView1.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem lvi in listView1.SelectedItems)
                        {
                            col.Add(lvi);
                        }
                    }
                    else
                    {
                        foreach (ListViewItem lvi in listView1.Items)
                        {
                            col.Add(lvi);
                        }
                    }

                    foreach (ListViewItem lvi in col)
                    {
                        byte[] data = diskImage.Disk.GetFile((string)lvi.Tag);

                        string filename = lvi.Text;
                        string hexdata = Utils.ByteArrayToHexString(data);
                        string checksum = Utils.CalculateChecksum(data);

                        string pkgdata = "";
                        pkgdata += "A:DOWNLOAD " + filename + "\r\n";
                        pkgdata += "U0\r\n";
                        pkgdata += ":" + hexdata + ">" + checksum + "\r\n";
                        text_writer.Write(pkgdata);
                    }
                }
            }
        }

        private void pKGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.AddExtension = true;
            openFileDialog1.DefaultExt = "*.pkg";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Package File (*.pkg)|*.pkg|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String[] lines = File.ReadAllLines(openFileDialog1.FileName);
                bool newfile = false;
                int i = 0;

                string current_filename = "";
                int user = 0;

                while (i < lines.Length)
                {
                    if (lines[i].IndexOf(":DOWNLOAD ") > 0)
                        current_filename = lines[i].Substring(11);

                    if (lines[i].IndexOf("U") == 0)
                    {
                        int.TryParse(lines[i].Substring(1), out user);
                    }


                    if (current_filename != "" && lines[i].IndexOf(":") == 0 && lines[i].IndexOf(">") > 0)
                    {
                        string hexdata = lines[i].Substring(1, lines[i].IndexOf(">") - 1);
                        string checksum = lines[i].Substring(lines[i].IndexOf(">") + 1);
                        byte[] data = Utils.HexStringToByteArray(hexdata);

                        if (checksum == Utils.CalculateChecksum(data))
                        {
                            cmd_mkbin(current_filename, data);
                        }
                        current_filename = "";
                        user = 0;
                    }

                    i++;
                }
                if (newfile)
                    cmd_ls();
            }
        }

        
    }
}

