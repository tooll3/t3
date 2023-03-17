using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace T3.Player
{
    public partial class StartDialog : Form
    {
        public StartDialog()
        {
            InitializeComponent();
        }

        //string reswidth = "1920";
        //string resheight = "1080";
        //string resolution = "";
        //string windowed = "";
        //string vsync = "";
        //string looped = "";

        string _cmd = "player.exe";

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //foreach (ListViewItem item in _resolutionListbox.Items)
            //{

            //}
        }

        private void Label2_Click(object sender, EventArgs e)
        {
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            //+" --width " + reswidth + " --height " + resheight

            var arguments = $"--width {_txtboxwidth.Text} --height {_txtboxheight.Text}";

            if (_loopCheckBox.Checked)
                arguments += " --loop true";

            if (_vsyncCheckbox.Checked)
                arguments += " --novsync true";

            if (_windowedCheckbox.Checked)
                arguments += " --windowed true";

            _resultTestLabel.Text = arguments;

            // Process process = new Process();
            // process.StartInfo.FileName = "cmd.exe";
            // process.StartInfo.UseShellExecute = false;
            // process.StartInfo.RedirectStandardInput = true;
            // process.StartInfo.RedirectStandardOutput = true;
            // process.StartInfo.RedirectStandardError = true;
            // process.Start();
            // process.StandardInput.WriteLine(arguments);
            // Task.Delay(3000).Wait();
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
        }
    }
}