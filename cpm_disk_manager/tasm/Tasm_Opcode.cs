using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager.tasm
{
    public class Tasm_Opcode
    {
        public string opcode { get; set; }
        public string desc { get; set; }
        public int size { get; set; }


        public Tasm_Opcode(string opcode, string desc, int size)
        {
            this.opcode = opcode.Trim();
            this.desc = desc.Trim();
            this.size = size;

            if (this.desc.IndexOf(" \"\"") > -1)
                this.desc = this.desc.Replace(" \"\"", "").Trim();

        }



        public static Dictionary<String, Tasm_Opcode> load()
        {
            String line;
            Dictionary<String, Tasm_Opcode> opcode_list = new Dictionary<string, Tasm_Opcode>();
            if (!Directory.Exists(System.Environment.CurrentDirectory + "\\tasm"))
                Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\tasm");

            try
            {
                StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + "\\tasm\\tasm80.tab");
                //Read the first line of text
                line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {

                    String[] cols = line.Split('\t');
                    int icol = 0;

                    string opcode = "";
                    string desc = "";
                    int size = 0;
                    foreach (string c in cols)
                    {
                        if (c.Trim() != "")
                        {
                            switch (icol)
                            {
                                case 0:
                                    desc = c.Trim();
                                    break;
                                case 1:
                                    opcode = c.Trim();
                                    break;
                                case 2:
                                    size = int.Parse(c.Trim());
                                    break;
                            }

                            icol++;
                        }
                    }

                    if(opcode != "" && !opcode_list.ContainsKey(opcode))
                    {
                        opcode_list.Add(opcode, new Tasm_Opcode(opcode, desc, size));
                    }

                    line = sr.ReadLine();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }

            return opcode_list;
        }
    }
}
