using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace DriveExtractor
{
    public partial class Form1 : Form
    {


        private List<Node> listNodes = new List<Node>();
        private BackgroundWorker worker;
        private const int STATUS_INDEX = 3;
        private const int DST_DIR_INDEX = 4;



        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDrives();

            tbDestination.Text = Properties.Settings.Default["lastDestinationRootDir"].ToString();
            tbSubDirPrefix.Text = Properties.Settings.Default["lastPrefixDir"].ToString();
        }

        private void LoadDrives()
        {
            cbSource.Items.Clear();

            foreach (DriveInfo f in DriveInfo.GetDrives())
                cbSource.Items.Add(f);

            if (cbSource.Items.Count > 0)
                cbSource.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFoldersAndFiles();
        }

        private void LoadFoldersAndFiles()
        {

            listNodes.Clear();
            listView1.Items.Clear();

            string subDir = GetSubDirectory();
            try
            {
                string[] folders = Directory.GetDirectories(cbSource.Text, "*", System.IO.SearchOption.TopDirectoryOnly);

                foreach (string folder in folders)
                {
                    Node n = new Node(folder);
                    n.PathDst = Path.Combine(tbDestination.Text, subDir, n.Name);

                    if (n.IsValid())
                    {
                        ListViewItem lvi = MakeListViewItemFromNode(n);
                        listView1.Items.Add(lvi);
                        listNodes.Add(n);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            try
            {
                string[] files = Directory.GetFiles(cbSource.Text, "*", System.IO.SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    Node n = new Node(file);
                    n.PathDst = Path.Combine(tbDestination.Text, subDir, n.Name);

                    if (n.IsValid())
                    {
                        ListViewItem lvi = MakeListViewItemFromNode(n);
                        listView1.Items.Add(lvi);
                        listNodes.Add(n);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            progressBar1.Value = 0;


  
        }

        private ListViewItem MakeListViewItemFromNode(Node n)
        {
            ListViewItem lvi = new ListViewItem();

            lvi.Text = n.PathSrc;
            lvi.SubItems.Add(n.Type.ToString());
            lvi.SubItems.Add(n.FriendySize);
            lvi.SubItems.Add(n.Status.ToString());
            lvi.SubItems.Add(n.PathDst);
            n.ListViewItemIndex = listView1.Items.Count;
            lvi.Tag = n;
            lvi.Checked = true;
            return lvi;
        }

        

        private void btnImport_Click(object sender, EventArgs e)
        {

            progressBar1.Value = 0;
            btnImport.Enabled = false;
            btnCancel.Visible = true;

            List<Node> copyNodes = new List<Node>();
            foreach(ListViewItem lvi in listView1.Items)
            {
                Node n = lvi.Tag as Node;
                if (lvi.Checked)
                    copyNodes.Add(n);
                else
                {
                    n.Status = Node.NodeStatus.SKIPPED;
                    lvi.SubItems[STATUS_INDEX].Text = n.Status.ToString();
                }
                    
            }


            progressBar1.Maximum = copyNodes.Count;

            UpdateSubDirectory();

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync(copyNodes);

            
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnImport.Enabled = true;
            btnCancel.Visible = false;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Node n = e.UserState as Node;
            ListViewItem lvi = listView1.Items[n.ListViewItemIndex];
            Node.NodeStatus status = n.Status;

            if (status != Node.NodeStatus.COPYING)
            {
                if (progressBar1.Value < progressBar1.Maximum)
                    progressBar1.Value++;
            }
                

            lvi.SubItems[STATUS_INDEX].Text = n.Status.ToString();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Worker_DoWork");
            List<Node> nodes = e.Argument as List<Node>;

            foreach (Node n in nodes)
            {
                if (worker.CancellationPending)
                {
                    n.Status = Node.NodeStatus.CANCELLED;
                    worker.ReportProgress(100, n);
                } else
                {
                    if (n.IsCopy)
                    {
                        worker.ReportProgress(0, n);
                        n.CopyOperation();
                        worker.ReportProgress(100, n);
                    }
                }
                
                
  
            }
            e.Result = nodes;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            // Show the FolderBrowserDialog.  
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                tbDestination.Text = folderDlg.SelectedPath;
                Properties.Settings.Default["lastDestinationRootDir"] = tbDestination.Text;
                Properties.Settings.Default.Save(); // Saves settings in application configuration file
            }
            LoadFoldersAndFiles();
        }

        private void tbSubDirPrefix_TextChanged(object sender, EventArgs e)
        {
            UpdateSubDirectory();
            Properties.Settings.Default["lastPrefixDir"] = tbSubDirPrefix.Text;
            Properties.Settings.Default.Save(); // Saves settings in application configuration file
        }

        private string GetSubDirectory()
        {
            string subDir = "";

            try
            {
                //get subdir if applicable
                if (tbSubDirPrefix.Text.Length > 0)
                {
                    //get last subdir and increment by one
                    string[] folders = Directory.GetDirectories(tbDestination.Text, tbSubDirPrefix.Text + "*", System.IO.SearchOption.TopDirectoryOnly);
                    subDir = String.Format("{0}_{1:D3}", tbSubDirPrefix.Text, folders.Length + 1);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                subDir = String.Format("{0}_001", tbSubDirPrefix.Text);
            }

            

            return subDir;
        }

        private void UpdateSubDirectory()
        {
            string subDir = GetSubDirectory();

            if (subDir.Length >= 0)
            {
                foreach (Node n in listNodes)
                {
                    n.PathDst = Path.Combine(tbDestination.Text, subDir, n.Name);
                    listView1.Items[n.ListViewItemIndex].SubItems[DST_DIR_INDEX].Text = n.PathDst;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            worker.CancelAsync();
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ListViewItem lvi = e.Item as ListViewItem;
            Node n = lvi.Tag as Node;
            n.IsCopy = lvi.Checked;
        }

        private void openSourcePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = listView1.SelectedItems[0];

            if (lvi != null)
            {
                Node n = lvi.Tag as Node;
                string dirName = Path.GetDirectoryName(n.PathSrc);

                if (Directory.Exists(dirName))
                {
                    System.Diagnostics.Process.Start("explorer.exe", dirName);
                }
            }
        }

        private void openDestinationPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = listView1.SelectedItems[0];

            if (lvi != null)
            {
                Node n = lvi.Tag as Node;
                string dirName = Path.GetDirectoryName(n.PathDst);

                if (Directory.Exists(dirName))
                {
                    System.Diagnostics.Process.Start("explorer.exe", dirName);
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadFoldersAndFiles();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CopyDirectory(cbSource.Text, Path.Combine(tbDestination.Text, tbSubDirPrefix.Text), true);
            //FileSystem.CopyDirectory(cbSource.Text, , UIOption.AllDialogs);
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                FileSystem.CopyFile(file.FullName, targetFilePath, UIOption.AllDialogs);
                //file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private void btnUncheckAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listView1.Items)
                lvi.Checked = false;
        }

        private void btnCheckAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listView1.Items)
                lvi.Checked = true;
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = listView1.SelectedItems[0];

            if (lvi != null)
            {
                Node n = lvi.Tag as Node;
                Clipboard.SetText(n.PathDst);
            }
        }

        private void tbDestination_TextChanged(object sender, EventArgs e)
        {
            UpdateSubDirectory();
        }
    }
}


