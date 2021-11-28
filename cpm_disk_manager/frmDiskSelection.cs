using RawDiskLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cpm_disk_manager
{
    public partial class frmDiskSelection : Form
    {
        public frmDiskSelection()
        {
            InitializeComponent();
            list_drives();
        }

        DiskInfo _seldisk = new DiskInfo() { Id = -1, Name = "*** Cancel ***" };

        public DiskInfo SelDisk
        {
            get { return _seldisk; }
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        [DllImport("kernel32.dll")]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);


        public static char[] GetDriveLetters(string driveName)
        {
            List<char> results = new List<char>();

            StringBuilder sb = new StringBuilder(128);
            for (char ch = 'A'; ch < 'Z'; ch++)
            {
                uint result;
                do
                {
                    result = QueryDosDevice(ch + ":", sb, (uint)sb.Capacity);

                    if (result == 122)
                        sb.EnsureCapacity(sb.Capacity * 2);
                } while (result == 122);

                // Contains target?
                string[] drives = sb.ToString().Split('\0');

                if (drives.Any(s => s.Equals(driveName, StringComparison.InvariantCultureIgnoreCase)))
                    results.Add(ch);
            }

            return results.ToArray();
        }


        private void list_drives()
        {
            IEnumerable<char> volumeDrives = RawDiskLib.Utils.GetAllAvailableVolumes();
            IEnumerable<int> harddiskVolumes = RawDiskLib.Utils.GetAllAvailableDrives(DiskNumberType.Volume);

            //Console.WriteLine("You need to enter a volume on which to write and read. Note that this volume will be useless afterwards - do not chose anything by test volumes!");
            //Console.WriteLine("Select volume:");
            //List<int> options = new List<int>();

            listBox1.Items.Clear();

            listBox1.Items.Add(new DiskInfo() { Id = -1, Name = "*** Cancel ***" });

            foreach (int harddiskVolume in harddiskVolumes)
            {
                try
                {
                    using (RawDisk disk = new RawDisk(DiskNumberType.Volume, harddiskVolume))
                    {
                        if (disk.DiskInfo.MediaType == DeviceIOControlLib.Objects.Disk.MEDIA_TYPE.RemovableMedia)
                        {
                            string volume = disk.DosDeviceName.Remove(0, @"\\.\GLOBALROOT".Length);
                            char[] driveLetters = GetDriveLetters(volume).Where(volumeDrives.Contains).ToArray();

                            listBox1.Items.Add(new DiskInfo() { Id = harddiskVolume, Name = String.Format("[{1}] {0} {2} ", SizeSuffix(disk.SizeBytes), string.Join(", ", driveLetters), volume) });

                            //options.Add(harddiskVolume);

                        }
                    }
                }
                catch (Exception)
                {
                    // Don't write it
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((DiskInfo)listBox1.SelectedItem) != null)
            {
                if (((DiskInfo)listBox1.SelectedItem).Id == -1)
                {
                    Close();
                }
                else if (MessageBox.Show("Confirm Drive " + listBox1.SelectedItem.ToString() + "?", "Confirm Drive", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //WRITE(current_filename);

                    _seldisk = ((DiskInfo)listBox1.SelectedItem);
                    Close();
                }
            }
        }
    }
}
