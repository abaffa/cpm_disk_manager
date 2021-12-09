using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cpm_disk_manager
{
    public static class Utils
    {

        public static DialogResult ShowInputDialog(ref string input, String Title, Form owner)
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

        public static void SetSortArrow(ColumnHeader head, SortOrder order)
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

        /////////////////////////////////////////
        public static String getStringFromByteArray(byte[] fileBytes2, int start, int max)
        {
            String ret = "";
            for (int i = 0; i < max && fileBytes2[start + i] != 0x00; i++)
                ret += Convert.ToChar(fileBytes2[start + i]);

            return ret.Trim('\0');
        }




        public static string stringByteToText(String bytetext)
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

        public static string textToStringByte(String _text)
        {
            string file = "";
            foreach (char a in _text.ToCharArray())
            {
                file += Convert.ToByte(a).ToString("X2");
            }

            return file;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            try
            {
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            catch { }
            return bytes;
        }

        public static string Convert2Str(byte[] data)
        {
            char[] characters = data.Select(b => (char)b).ToArray();
            return new string(characters);
        }


        public static String ByteArrayToString(byte[] data)
        {
            string file = "";
            int index = 0;
            int size = data.Length;
            while (index < size)
            {
                file += data[index].ToString("X2");
                index++;
            }

            return file;
        }

        public static bool CompareByteArrays(byte[] data1 ,byte[] data2)
        {
            return data1.SequenceEqual(data1) && data1.LongLength == data2.LongLength;
        }



        public static Byte[] getByteArray(byte[] fileBytes2, int start, int max)
        {
            Byte[] ret = new byte[max];
            for (int i = 0; i < max; i++)
                ret[i] = fileBytes2[start + i];

            return ret;
        }


        public static byte[] ReadAllBytes(BinaryReader reader)
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

        public static Byte ConvertIntHexToByte(int d)
        {
            return Convert.ToByte(d.ToString(), 16);

        }


    }
}
