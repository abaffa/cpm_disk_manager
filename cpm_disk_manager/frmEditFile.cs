using cpm_disk_manager.tasm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cpm_disk_manager
{
    public partial class frmEditFile : Form
    {

        byte[] original_data = new byte[0];
        byte[] current_data = new byte[0];

        String filename = "";
        String hexadecimal_data = "";
        String ascii_data = "";

        int _org = 0;

        bool savekey = false;

        EditorType _filetype = EditorType.Binary;


        public int Start_Address
        {
            get { return _org; }
            set { _org = value; numericUpDown1.Value = value; }
        }

        public enum EditorType
        {
            Binary,
            Text
        }

        public EditorType FileType
        {
            get { return _filetype; }
            set
            {
                _filetype = value;
                toolStripComboBox1.SelectedIndex = (int)value;
            }
        }


        public frmEditFile()
        {
            InitializeComponent();

            FileType = EditorType.Binary;
            undoToolStripMenuItem.Enabled = false;
        }

        public bool getSaveKeyHit()
        {
            return savekey;
        }


        public void setTitle(String text)
        {
            this.Text = text;

        }

        public string getText()
        {
            if (_filetype == EditorType.Binary)
            {
                return hexadecimal_data;
            }
            else if (_filetype == EditorType.Text)
            {
                return ascii_data;
            }

            return "";
        }



        private void load_data()
        {
            ascii_data = System.Text.Encoding.Default.GetString(current_data);
            hexadecimal_data = Utils.ByteArrayToString(current_data);

            undoToolStripMenuItem.Enabled = !Utils.CompareByteArrays(original_data, current_data);
        }

        private void reload_data()
        {
            current_data = original_data;
            ascii_data = System.Text.Encoding.Default.GetString(current_data);
            hexadecimal_data = Utils.ByteArrayToString(current_data);
        }

        //Regex.Replace(textBox1.Text, "[^a-fA-F0-9]+", "", RegexOptions.Compiled)
        public void setBinary(byte[] data)
        {
            resetFile();
            original_data = data;
            reload_data();
            set_entry();
            calculate_checksum();
        }


        public void setFilename(String _filename)
        {
            filename = _filename;
        }


        public void newFile()
        {
            filename = "";
            resetFile();
        }
        private void resetFile()
        {
            original_data = new byte[0];
            current_data = new byte[0];
            ascii_data = "";
            hexadecimal_data = "";
        }

        public byte[] getBinary()
        {

            update_entry();

            return current_data;
        }

        private void frmEditFile_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyValue == (int)'s' || e.KeyValue == (int)'S'))
            {
                savekey = true;
                this.Close();
            }
        }


        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (toolStripComboBox1.SelectedIndex == 0)
            {
                FileType = EditorType.Binary;
            }
            else if (toolStripComboBox1.SelectedIndex == 1)
            {
                FileType = EditorType.Text;
            }

            set_entry();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reload_data();
            set_entry();
            undoToolStripMenuItem.Enabled = false;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private string do_disassembly(Byte[] _bytes)
        {

            //assembly = Regex.Replace(assembly, "[^a-fA-F0-9]+", "", RegexOptions.Compiled);
            //Byte[] _bytes = StringToByteArray(assembly);

            Start_Address = (int)numericUpDown1.Value;
            string disassembly = "";


            String[] disassembly_bytes = new string[_bytes.Length];

            int last_line = -1;
            string printable_chars = "";
            string hex_ops = "";
            for (int k = 0; k < disassembly_bytes.Length; k++)
            {
                if (disassembly_bytes[k] == null)
                {
                    char c = (char)_bytes[k];
                    bool isPrintable = !Char.IsControl(c) || Char.IsWhiteSpace(c);
                    if (c == 0x85 || c == '\t' || c == '\r' || c == '\n') c = '.';

                    if (last_line == -1 || (last_line / 0x10 != k / 0x10))
                    {

                        if (last_line > -1)
                        {
                            if (last_line % 0x10 > 0)
                            {
                                hex_ops = hex_ops.PadLeft(hex_ops.Length + ((last_line % 0x10) * 3));
                                printable_chars = printable_chars.PadLeft(printable_chars.Length + (last_line % 0x10));
                            }
                            disassembly_bytes[last_line] += hex_ops.PadRight(48) + "  " + printable_chars.PadRight(16) + "\r\n";
                        }

                        last_line = k;
                        disassembly_bytes[last_line] = (_org + k).ToString("X4") + ":";
                        hex_ops = "";
                        printable_chars = "";
                    }
                    hex_ops += " " + _bytes[k].ToString("X2");
                    printable_chars += (isPrintable ? c.ToString() : ".");


                }
            }

            if (printable_chars.Length > 0)
            {
                if (last_line % 0x10 > 0)
                {
                    hex_ops = hex_ops.PadLeft(hex_ops.Length + ((last_line % 0x10) * 3));
                    printable_chars = printable_chars.PadLeft(printable_chars.Length + (last_line % 0x10));
                }
                disassembly_bytes[last_line] += hex_ops.PadRight(48) + "  " + printable_chars.PadRight(16) + "\r\n";
            }

            disassembly = String.Join("", disassembly_bytes.Where(p => p != "").ToArray());

            calculate_checksum();

            return disassembly;
        }


        private void calculate_checksum()
        {

            int sum = 0;
            int byte_count = 0;
            foreach (char c in current_data)
            {
                sum += (int)c;
                byte_count++;
            }

            toolStripStatusLabel1.Text = "Checksum: " + (byte_count & 0b11111111).ToString("X2") + (sum & 0b11111111).ToString("X2");
        }


        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }






        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        using (BinaryReader b = new BinaryReader(File.Open(file, FileMode.Open)))
                        {

                            //string filename = Path.GetFileName(file);

                            current_data = Utils.ReadAllBytes(b);

                            if (original_data.LongLength == 0)
                                original_data = current_data;

                            set_entry();
                            calculate_checksum();

                        }

                    }

                }

            }
            catch
            {
                MessageBox.Show("An error occurred while reading the file.\nPlease check the file.", "Reading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void exportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Byte[] filearray = Utils.StringToByteArray(textBox1.Text);
                saveFileDialog1.FileName = filename;
                if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    string filename = saveFileDialog1.FileName;

                    File.WriteAllBytes(filename, filearray);
                    //MessageBox.Show("FileS, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }

            }
            catch
            {
                MessageBox.Show("An error occurred while writing the file.\nPlease check the file.", "Writing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Start_Address = (int)numericUpDown1.Value;
            textBox2.Text = do_disassembly(current_data);
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Visible = tabControl1.SelectedIndex == 1;
            numericUpDown1.Visible = tabControl1.SelectedIndex == 1;

            if (tabControl1.SelectedIndex == 1)
            {
                update_entry();
                textBox2.Text = do_disassembly(current_data);
            }
        }



        private void set_entry()
        {
            load_data();

            if (_filetype == EditorType.Binary)
            {
                textBox1.Text = hexadecimal_data;
            }
            else
            {
                textBox1.Text = ascii_data;
            }
        }

        private void update_entry()
        {
            string entry = textBox1.Text;

            if (_filetype == EditorType.Binary)
            {
                entry = Regex.Replace(entry, "[^a-fA-F0-9]+", "", RegexOptions.Compiled);
                current_data = Utils.StringToByteArray(entry);
            }
            else
            {
                current_data = Encoding.ASCII.GetBytes(entry);
            }

            load_data();
        }

        private void frmEditFile_FormClosing(object sender, FormClosingEventArgs e)
        {
            update_entry();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            /// verificar undo
            update_entry();
            load_data();
        }
    }
}
