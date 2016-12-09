using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace S3Uploader
{
    public partial class CredentialsForm : Form
    {
        public CredentialsForm()
        {
            InitializeComponent();
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            IAM = iamBox.Text;
            Key = keyBox.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CredentialsForm_Load(object sender, EventArgs e)
        {
            if (IAM != null && IAM.Length > 0)
                iamBox.Text = IAM;

            if (Key != null && Key.Length > 0)
                keyBox.Text = Key;
        }

        public string IAM { get; set; } = null;
        public string Key { get; set; } = null;
    }
}
