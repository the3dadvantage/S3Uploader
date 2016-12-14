using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Security.Cryptography;
using System.IO;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace S3Uploader
{
    public partial class UploadForm : Form
    {
        TransferUtility transUtil;
        AmazonS3Client client;
        List<string> selectedFiles = new List<string>();
        string bucketName = "";
        
        public UploadForm()
        {
            InitializeComponent();
        }

        private void UploadForm_Load(object sender, EventArgs e)
        {
            LoadCredentials(Application.LocalUserAppDataPath + "/S3Uploader.keys");
        }

        bool LoadCredentials(string filename)
        {
            try
            {
                byte[] protectedBytes = ProtectedData.Unprotect(File.ReadAllBytes(filename), null, DataProtectionScope.LocalMachine);
                string data = System.Text.Encoding.UTF8.GetString(protectedBytes);

                string[] lines = data.Split(new char[] { ',' });

                Init(lines[0], lines[1]);

                return true;
            }
            catch (Exception) { }

            return false;
        }

        void SaveCredentials(string filename, string iam, string key)
        {
            string data = iam + "," + key;

            byte[] protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(filename, protectedBytes);
        }

        void Init(string iam, string key)
        {
            client = new AmazonS3Client(iam, key, RegionEndpoint.USEast1);

            ListBucketsRequest req = new ListBucketsRequest();
            ListBucketsResponse res = client.ListBuckets(req);

            bucketCombo.Items.Clear();
            foreach (S3Bucket b in res.Buckets)
            {
                bucketCombo.Items.Add(b.BucketName);
            }

            if (bucketCombo.Items.Count > 0)
                bucketCombo.SelectedIndex = 0;
        }

        private void addFileBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if(ofd.ShowDialog() == DialogResult.OK)
            {
                foreach(string s in ofd.FileNames)
                {
                    if (!selectedFiles.Contains(s))
                    {
                        selectedFiles.Add(s);
                        fileListView.Items.Add(s);
                    }
                }

                if(selectedFiles.Count > 0)
                {
                    uploadBtn.Enabled = true;
                }
            }
        }

        private void uploadBtn_Click(object sender, EventArgs e)
        {
            if(selectedFiles.Count == 0)
            {
                MessageBox.Show("There are no files selected");
                return;
            }

            if(client == null)
            {
                MessageBox.Show("You must login first");
                return;
            }

            Thread t = new Thread(new ThreadStart(() => {
                transUtil = new TransferUtility(client);
                
                while (selectedFiles.Count > 0)
                {
                    TransferUtilityUploadRequest uploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        FilePath = selectedFiles[0]
                    };
                    
                    if(subDirBox.Text.Length > 0)
                    {
                        string s = subDirBox.Text.Trim(new char[] { '/', '\\' });
                        uploadRequest.Key = s + "/" + selectedFiles[0].Split(new char[] { '\\', '/' }).Last();
                    }

                    uploadRequest.UploadProgressEvent += UploadRequest_UploadProgressEvent;

                    transUtil.Upload(uploadRequest);
                    Invoke((MethodInvoker)delegate
                    {
                        uploadProgress.Value = 100;
                        fileListView.Items.RemoveAt(0);
                    });

                    selectedFiles.RemoveAt(0);
                }

                Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show("Upload complete");
                });
            }));

            t.Start();
        }

        private void UploadRequest_UploadProgressEvent(object sender, UploadProgressArgs e)
        {
            if(InvokeRequired)
            {
                Invoke((MethodInvoker)delegate {
                    uploadProgress.Value = (int)(((float)e.TransferredBytes / (float)e.TotalBytes) * 100.0f);
                });
            }
        }

        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CredentialsForm cf = new CredentialsForm();

            if(cf.ShowDialog() == DialogResult.OK)
            {
                Init(cf.IAM, cf.Key);
                SaveCredentials(Application.LocalUserAppDataPath + "/S3Uploader.keys", cf.IAM, cf.Key);
            }
        }

        private void bucketCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            bucketName = (string)bucketCombo.Items[bucketCombo.SelectedIndex];
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedFiles.RemoveAt(fileListView.SelectedIndices[0]);
            fileListView.Items.RemoveAt(fileListView.SelectedIndices[0]);
        }

        private void fileListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                filesMenuStrip.Show(Cursor.Position);
            }
        }
    }
}
