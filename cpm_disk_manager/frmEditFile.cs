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
        bool savekey = false;
        int oldcombo = -1;


        byte[] originaldata = new byte[0];
        string originaldata_hex = "";
        string originalfile = "";
        string originaltextfile = "";

        string assembly = "";
        string ascii_bytes = "";
        string dissassembly = "";


        int _org = 0;

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
        EditorType _filetype = EditorType.Binary;
        public EditorType FileType
        {
            get { return _filetype; }
            set
            {
                _filetype = value;
                toolStripComboBox1.SelectedIndex = (int)value;

                disassemblyToolStripMenuItem.Visible = _filetype == EditorType.Binary;
            }
        }

        public bool ShowUndo
        {
            get { return undoToolStripMenuItem.Visible; }
            set { undoToolStripMenuItem.Visible = value; }
        }

        public bool ShowEditorType
        {
            get { return toolStripComboBox1.Visible; }
            set { toolStripComboBox1.Visible = value; }
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
                return Regex.Replace(textBox1.Text, "[^a-fA-F0-9]+", "", RegexOptions.Compiled);
            }
            else if (_filetype == EditorType.Text)
            {
                return textBox1.Text;
            }

            return "";
        }
        
        public void setBinary(byte[] data)
        {
            newFile();
            originaldata = data;
            originaldata_hex = Utils.ByteArrayToString(data);
            textBox1.Text = originaldata_hex;

            
        }


        public void newFile()
        {
            originaldata = new byte[0];
            originaldata_hex = "";
            originalfile = "";
            originaltextfile = "";

            assembly = "";
            ascii_bytes = "";
            dissassembly = "";

            textBox1.ReadOnly = false;
        }

        public byte[] getBinary()
        {
            return StringToByteArray(textBox1.Text);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyValue == (int)'s' || e.KeyValue == (int)'S'))
            {
                savekey = true;
                this.Close();
            }


            if (disassemblyToolStripMenuItem.Text == "Disassembly")
            {
                if (_filetype == EditorType.Binary)
                {
                    undoToolStripMenuItem.Enabled = (textBox1.Text != originalfile);
                }
                else if (_filetype == EditorType.Text)
                {
                    undoToolStripMenuItem.Enabled = (textBox1.Text != originaltextfile);
                }
            }

        }

        private string stringByteToText(String bytetext)
        {
            Byte[] filearray = StringToByteArray(bytetext);

            string file = "";
            foreach (Byte b in filearray)
            {
                file += Convert.ToChar(b);
            }

            file = file.Replace("\r", "");
            file = file.Replace("\n", "\r\n");

            return file;
        }

        private string textToStringByte(String _text)
        {
            string file = "";
            foreach (char a in _text.ToCharArray())
            {
                file += Convert.ToByte(a).ToString("X2");
            }

            return file;
        }

        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (toolStripComboBox1.SelectedIndex == 0)
            {
                FileType = EditorType.Binary;
                disassemblyToolStripMenuItem.Visible = true;
                if (oldcombo == 1)
                {
                    try
                    {
                        if (textBox1.Text != ascii_bytes)
                        {
                            assembly = textToStringByte(textBox1.Text);
                            textBox1.Text = assembly;
                        }
                        else
                            textBox1.Text = assembly;


                    }
                    catch
                    {
                        toolStripComboBox1.SelectedIndex = 1;
                    }
                }


                oldcombo = toolStripComboBox1.SelectedIndex;
            }
            else if (toolStripComboBox1.SelectedIndex == 1)
            {
                FileType = EditorType.Text;
                disassemblyToolStripMenuItem.Visible = false;

                if (oldcombo == 0)
                {
                    if (textBox1.Text != assembly || ascii_bytes == "")
                    {
                        ascii_bytes = stringByteToText(textBox1.Text);
                        textBox1.Text = ascii_bytes;
                    }
                    else
                        textBox1.Text = ascii_bytes;


                }
                oldcombo = toolStripComboBox1.SelectedIndex;
            }
            else
            {
                toolStripComboBox1.SelectedIndex = 0;
                disassemblyToolStripMenuItem.Visible = true;
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            assembly = originalfile;
            ascii_bytes = originaltextfile;

            if (_filetype == EditorType.Binary)
            {
                textBox1.Text = assembly;
            }
            else if (_filetype == EditorType.Text)
            {
                textBox1.Text = ascii_bytes;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        public string format_memo_name(string tmp_instr_name)
        {
            tmp_instr_name = tmp_instr_name.Trim();
            tmp_instr_name = tmp_instr_name.Replace(", ", ",");
            tmp_instr_name = tmp_instr_name.Replace(" + ", "+");
            tmp_instr_name = tmp_instr_name.Replace(" - ", "-");
            tmp_instr_name = tmp_instr_name.Replace(" * ", "*");
            tmp_instr_name = tmp_instr_name.Replace(" ^ ", "^");
            tmp_instr_name = tmp_instr_name.Replace(" / ", "/");

            tmp_instr_name = tmp_instr_name.Replace(" \\ ", "\\");
            tmp_instr_name = tmp_instr_name.Replace(" | ", "|");

            tmp_instr_name = tmp_instr_name.Replace("\t", " ");
            tmp_instr_name = tmp_instr_name.Replace("  ", " ");

            //tmp_instr_name = tmp_instr_name.Replace("/", "\\");
            //tmp_instr_name = tmp_instr_name.Replace("-", "|");

            tmp_instr_name = tmp_instr_name.Replace(",", ", ");
            tmp_instr_name = tmp_instr_name.Replace("+", " + ");
            tmp_instr_name = tmp_instr_name.Replace("-", " - ");
            tmp_instr_name = tmp_instr_name.Replace("*", " * ");
            tmp_instr_name = tmp_instr_name.Replace("/", " / ");
            tmp_instr_name = tmp_instr_name.Replace("^", " ^ ");
            tmp_instr_name = tmp_instr_name.Replace("|", " | ");
            return tmp_instr_name;
        }

        private int get_address_param(Tasm_Opcode op, byte[] _bytes, int i, int param_size)
        {
            bool escape = op.opcode.Length == 4;
            int size = Math.Min(op.size, param_size);
            String hex = "";
            for (int j = op.size - (escape ? 2 : 1); j > 0 && _bytes.Length > (i + j + 1); j--)
            {
                string _byte = _bytes[i + j].ToString("X2");
                hex += _byte;
            }

            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }


        private string process_dissassembly(Tasm_Opcode op, byte[] _bytes, int i)
        {
            string dissassembly = "";
            String _params = "";

            String current = op.opcode;
            bool escape = current.Length == 4;

            if (escape)
            {
                current = current.Substring(2) + " " + current.Substring(0, 2);
                i++;
            }

            String _params_hex = "";
            for (int j = op.size - (escape ? 2 : 1); j > 0 && _bytes.Length > (i + j + 1); j--)
            {
                string _byte = _bytes[i + j].ToString("X2");
                _params += _byte;
                _params_hex = " " + _byte.ToUpper() + _params_hex;
            }

            dissassembly += (_org + i).ToString("X4") + ": " + (current + _params_hex).PadRight(18);


            if (_params != "")
            {
                String code = format_memo_name(op.desc);

                code = code.Replace("I8", "U8");
                code = code.Replace("I16", "U16");

                int iparams = 0;
                while (code.IndexOf("U8") > -1 || code.IndexOf("U16") > -1)
                {

                    if (code.IndexOf("U8") > -1 && code.IndexOf("U16") > -1)
                    {
                        if (code.IndexOf("U8") > code.IndexOf("U16"))
                        {
                            code = code.Substring(0, code.IndexOf("U16")) + "$" + _params.Substring(iparams, 4) + code.Substring(code.IndexOf("U16") + 3);
                            iparams += 4;
                        }
                        else
                        {
                            code = code.Substring(0, code.IndexOf("U8")) + "$" + _params.Substring(iparams, 2) + code.Substring(code.IndexOf("U8") + 2);
                            iparams += 2;
                        }
                    }
                    else if (code.IndexOf("U16") > -1)
                    {
                        code = code.Substring(0, code.IndexOf("U16")) + "$" + _params.Substring(iparams, 4) + code.Substring(code.IndexOf("U16") + 3);
                        iparams += 4;
                    }
                    else if (code.IndexOf("U8") > -1)
                    {
                        code = code.Substring(0, code.IndexOf("U8")) + "$" + _params.Substring(iparams, 2) + code.Substring(code.IndexOf("U8") + 2);
                        iparams += 2;
                    }
                }

                dissassembly += code;
                dissassembly += "\r\n";
            }
            else
            {
                dissassembly += format_memo_name(op.desc);
                dissassembly += "\r\n";
            }

            /*
            if (op.desc.IndexOf("@") > -1)
            {
                if (_params != "")
                {
                    dissassembly += format_memo_name(op.desc).Replace("@", "$" + _params);
                    dissassembly += "\r\n";
                }
                else
                {
                    dissassembly += format_memo_name(op.desc);
                    dissassembly += " = $" + _params;
                    dissassembly += "\r\n";
                }
            }
            else
            {
                dissassembly += format_memo_name(op.desc);
                dissassembly += "\r\n";
            }
            */

            return dissassembly;
        }
        private void do_disassembly(string assembly)
        {

            Start_Address = (int)numericUpDown1.Value;

            assembly = Regex.Replace(assembly, "[^a-fA-F0-9]+", "", RegexOptions.Compiled);
            dissassembly = "";

            Byte[] _bytes = StringToByteArray(assembly);

            String[] dissassembly_bytes = new string[_bytes.Length];

            Dictionary<String, Tasm_Opcode> opcode_list = Tasm_Opcode.load();

            Stack<int> calls = new Stack<int>();
            Stack<int> syscalls = new Stack<int>();
            Stack<bool> sys_code = new Stack<bool>();

            //for (int i = 0; i < _bytes.Length;)
            int i = 0x0000;

            bool read_opcode = chk_program.Checked;

            if (!chkhex.Checked)
            {
                while (true)
                {


                    if (!read_opcode)
                    {
                        if (i + 1 >= _bytes.Length) break;

                        if (dissassembly_bytes[i] != null) break;

                        String hex = _bytes[i + 1].ToString("X2") + _bytes[i].ToString("X2");

                        dissassembly_bytes[i] = (_org + i).ToString("X4") + ": " + _bytes[i].ToString("X2") + " " + _bytes[i + 1].ToString("X2") + "\r\n";
                        dissassembly_bytes[i + 1] = "";

                        int next_index = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                        int next_i = next_index - _org;

                        if (next_i >= _bytes.Length || next_i < 0) break;

                        if (next_i < _bytes.Length - 1 && dissassembly_bytes[next_i] != null && dissassembly_bytes[next_i + 1] == "")
                            i += 2;
                        else if (next_i < _bytes.Length - 1 && dissassembly_bytes[next_i] != null && dissassembly_bytes[next_i + 1] == null)
                            break;
                        else
                        {
                            syscalls.Push(i + 2);
                            i = next_index - _org;

                            sys_code.Push(read_opcode);
                            read_opcode = true;
                        }
                    }
                    else
                    {
                        String current = _bytes[i].ToString("X2");
                        String opcode = current.ToUpper();
                        bool escape = current == "FD";

                        if (escape)
                        {
                            current += " " + _bytes[i + 1].ToString("X2");
                            opcode = _bytes[i + 1].ToString("X2").ToUpper() + "FD";
                        }

                        current = current.ToUpper();

                        if (opcode_list.ContainsKey(opcode))
                        {
                            Tasm_Opcode op = opcode_list[opcode];

                            dissassembly_bytes[i] = process_dissassembly(op, _bytes, i);
                            for (int x = i + 1; (x < i + op.size && x < _bytes.Length); x++)
                                if (dissassembly_bytes[x] == null)
                                    dissassembly_bytes[x] = "";
                                else
                                {
                                    //  throw new Exception();
                                }

                            if (op.opcode == "07" || op.opcode == "C6" || op.opcode == "C7" || op.opcode == "C8" || op.opcode == "C9"
                                 || op.opcode == "CA" || op.opcode == "CB" || op.opcode == "CC" || op.opcode == "CD" || op.opcode == "CE" || op.opcode == "CF"
                                 || op.opcode == "D0" || op.opcode == "D1")
                            {

                                int next_index = get_address_param(op, _bytes, i, 2);
                                if (next_index >= _org && (next_index - _org) < _bytes.Length && !calls.Contains(i + op.size))
                                {

                                    calls.Push(i + op.size);
                                    i = next_index - _org;
                                }
                                else
                                    i += op.size;

                            }
                            else if (op.opcode == "06")
                            {
                                if (syscalls.Count == 0)
                                    break;

                                i = syscalls.Pop();
                                read_opcode = sys_code.Pop();
                            }

                            else if (op.opcode == "09")
                            {
                                if (calls.Count == 0)
                                {
                                    if (syscalls.Count > 0)
                                    {
                                        i = syscalls.Pop();
                                        read_opcode = sys_code.Pop();
                                    }
                                    else
                                        break;
                                }
                                else
                                    i = calls.Pop();
                            }
                            else if (op.opcode == "0A")
                            {
                                int next_index = get_address_param(op, _bytes, i, 2);
                                if (next_index >= _org && (next_index - _org) < _bytes.Length)
                                {

                                    if (dissassembly_bytes[next_index - _org] != null)
                                    {
                                            i += op.size;
                                    }
                                    else
                                        i = next_index - _org;
                                }
                                else
                                    i += op.size;
                            }
                            else
                                i += op.size;


                        }
                        else
                        {
                            //if (dissassembly_bytes[i] != null) throw new Exception();
                            // dissassembly_bytes[i] = current + "; Unknown opcode: \"" + current + "\"\r\n";
                            i++;
                        }
                    }

                    if (i >= _bytes.Length)
                        break;
                }
            }

            int last_line = -1;
            string printable_chars = "";
            string hex_ops = "";
            for (int k = 0; k < dissassembly_bytes.Length; k++)
            {
                if (dissassembly_bytes[k] == null)
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
                            dissassembly_bytes[last_line] += hex_ops.PadRight(48) + "  " + printable_chars.PadRight(16) + "\r\n";
                        }

                        last_line = k;
                        dissassembly_bytes[last_line] = (_org + k).ToString("X4") + ":";
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
                dissassembly_bytes[last_line] += hex_ops.PadRight(48) + "  " + printable_chars.PadRight(16) + "\r\n";
            }

            dissassembly = String.Join("", dissassembly_bytes.Where(p => p != "").ToArray());
            textBox1.Text = dissassembly;

            int sum = 0;
            int byte_count = 0;
            foreach (char c in _bytes)
            {
                sum += (int)c;
                byte_count++;
            }

            toolStripStatusLabel1.Text = "Checksum: " + (byte_count & 0b11111111).ToString("X2") + (sum & 0b11111111).ToString("X2");

        }
        private void disassemblyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filetype == EditorType.Binary)
            {
                if (disassemblyToolStripMenuItem.Text == "Disassembly")
                {
                    assembly = textBox1.Text;
                    do_disassembly(assembly);
                    textBox1.ReadOnly = true;
                    toolStripComboBox1.Enabled = false;
                    disassemblyToolStripMenuItem.Text = "Assembly";

                    undoToolStripMenuItem.Enabled = false;
                }
                else
                {
                    toolStripComboBox1.Enabled = true;
                    textBox1.Text = assembly;
                    textBox1.ReadOnly = false;
                    disassemblyToolStripMenuItem.Text = "Disassembly";


                    if (disassemblyToolStripMenuItem.Text == "Disassembly")
                    {
                        if (_filetype == EditorType.Binary)
                        {
                            undoToolStripMenuItem.Enabled = (textBox1.Text != originalfile);
                        }
                        else if (_filetype == EditorType.Text)
                        {
                            undoToolStripMenuItem.Enabled = (textBox1.Text != originaltextfile);
                        }
                    }
                }
            }
        }

        private void frmEditFile_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (disassemblyToolStripMenuItem.Text != "Disassembly")
            {
                disassemblyToolStripMenuItem_Click(null, null);
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
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


        string Convert2Str(byte[] data)
        {
            char[] characters = data.Select(b => (char)b).ToArray();
            return new string(characters);
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            toolStripStatusLabel1.Text = "Checksum: ";
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    using (BinaryReader b = new BinaryReader(File.Open(file, FileMode.Open)))
                    {

                        string filedata = "";
                        string filename = Path.GetFileName(file);

                        Byte[] data = ReadAllBytes(b);

                        if (_filetype == EditorType.Binary)
                        {
                            int index = 0;
                            int size = data.Length;

                            while (index < size)
                            {
                                filedata += data[index].ToString("X2");
                                index++;
                            }
                            textBox1.Text = filedata;
                        }
                        else if (_filetype == EditorType.Text)
                        {

                            textBox1.Text = Convert2Str(data);
                        }



                        int sum = 0;
                        int byte_count = 0;
                        foreach (char c in data)
                        {
                            sum += (int)c;
                            byte_count++;
                        }

                        toolStripStatusLabel1.Text = "Checksum: " + (byte_count & 0b11111111).ToString("X2") + (sum & 0b11111111).ToString("X2");
                    }

                }

            }

        }

        private void exportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Byte[] filearray = StringToByteArray(textBox1.Text);
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;

                File.WriteAllBytes(filename, filearray);
                //MessageBox.Show("FileS, "Save Media", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Start_Address = (int)numericUpDown1.Value;
            if (_filetype == EditorType.Binary)
            {
                if (disassemblyToolStripMenuItem.Text == "Assembly")
                {
                    do_disassembly(assembly);
                }
            }
        }

        private void chkBreakJmp_CheckedChanged(object sender, EventArgs e)
        {
            if (_filetype == EditorType.Binary)
            {
                if (disassemblyToolStripMenuItem.Text == "Assembly")
                {
                    do_disassembly(assembly);
                }
            }
        }

        private void chk_program_CheckedChanged(object sender, EventArgs e)
        {
            if (_filetype == EditorType.Binary)
            {
                if (disassemblyToolStripMenuItem.Text == "Assembly")
                {
                    do_disassembly(assembly);
                }
            }
        }

        private void chkhex_CheckedChanged(object sender, EventArgs e)
        {
            if (_filetype == EditorType.Binary)
            {
                if (disassemblyToolStripMenuItem.Text == "Assembly")
                {
                    do_disassembly(assembly);
                }
            }
        }
    }
}
