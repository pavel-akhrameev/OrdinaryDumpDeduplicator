using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public partial class MainForm : Form, IMainViewModel
    {
        public MainForm()
        {
            InitializeComponent();

            SetDirectoryPath(@"\\VBOXSVR\files\MEGA");
        }

        public event Func<String, HierarchicalObject> AddDataLocationRequested;
        public event Action<IReadOnlyCollection<HierarchicalObject>> RescanRequested;
        public event Action FindDuplicatesRequested;

        public event Action<Boolean> ViewGroupsByHashRequested;
        public event Action ViewGroupsByFoldersRequested;

        public event Action<TreeViewItem[]> MoveToDuplicatesRequested;
        public event Action<TreeViewItem[]> DeleteDuplicatesRequested;

        #region Public methods

        public void SetTreeViewItems(TreeViewItem[] treeViewItems)
        {
            treeView1.Nodes.Clear();
            Boolean viewFullPath = radioButton1.Checked; // TODO

            foreach (TreeViewItem itemInReport in treeViewItems)
            {
                TreeNode treeNode = MakeTreeNodeWithChildren(itemInReport);
                treeView1.Nodes.Add(treeNode);
            }
        }

        public void AddSessionMessage(String message)
        {
            textBox2.Text = message;
        }

        #endregion

        #region Private methods

        private void SetDirectoryPath(String directoryPath)
        {
            textBox1.Text = directoryPath;
        }

        private void ViewGroupsOfDuplicates()
        {
            if (radioButton1.Checked)
            {
                if (ViewGroupsByHashRequested != null)
                {
                    Boolean hideIsolatedDuplicates = !checkBox1.Checked;
                    ViewGroupsByHashRequested.Invoke(hideIsolatedDuplicates);
                }
            }
            else
            {
                if (ViewGroupsByFoldersRequested != null)
                {
                    ViewGroupsByFoldersRequested.Invoke();
                }
            }
        }

        private static TreeNode MakeTreeNodeWithChildren(TreeViewItem itemInReport)
        {
            TreeViewItem[] childItems = itemInReport.ChildItems;
            TreeNode[] childNodes = new TreeNode[childItems.Length];
            if (childItems != null && childItems.Length > 0)
            {
                for (Int32 index = 0; index < childItems.Length; index++)
                {
                    TreeViewItem childItem = childItems[index];
                    TreeNode childNode = MakeTreeNodeWithChildren(childItem);
                    childNodes[index] = childNode;
                }
            }

            TreeNode treeNode = MakeTreeNode(itemInReport, childNodes);
            return treeNode;
        }

        private static TreeNode MakeTreeNode(TreeViewItem itemInReport, TreeNode[] children = null)
        {
            var treeNode = new TreeNode(itemInReport.Name, children);
            treeNode.ForeColor = itemInReport.Color;
            treeNode.Tag = itemInReport;

            return treeNode;
        }

        private static TreeViewItem GetTreeViewItem(TreeNode treeNode)
        {
            Object nodeTag = treeNode.Tag;

            TreeViewItem treeViewItem;
            if (nodeTag is TreeViewItem)
            {
                treeViewItem = nodeTag as TreeViewItem;
            }
            else
            {
                Type type = nodeTag.GetType();
                throw new Exception($"{type.Name} is wrong type for treeView1 node.");
            }

            return treeViewItem;
        }

        #endregion

        #region Event handlers

        private void button1_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    if (System.IO.Directory.Exists(folderBrowserDialog.SelectedPath))
                    {
                        SetDirectoryPath(folderBrowserDialog.SelectedPath);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String dataLocationPath = textBox1.Text;

            HierarchicalObject dataLocationObject = null;
            if (!String.IsNullOrWhiteSpace(dataLocationPath) && AddDataLocationRequested != null)
            {
                dataLocationObject = AddDataLocationRequested.Invoke(dataLocationPath);
            }

            if (dataLocationObject != null && RescanRequested != null)
            {
                RescanRequested.Invoke(new[] { dataLocationObject });
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            /*
            if (FindDuplicatesRequested != null)
            {
                FindDuplicatesRequested.Invoke();
            }
            */

            ViewGroupsOfDuplicates();
        }

        private void treeItemsViewParameters_Changed(object sender, EventArgs e)
        {
            checkBox1.Enabled = radioButton1.Checked;

            ViewGroupsOfDuplicates();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                TreeViewItem treeViewItem = GetTreeViewItem(treeView1.SelectedNode);
                if (treeViewItem != null)
                {
                    textBox2.Text = treeViewItem.HierarchicalObject.Name;
                }
                else
                {
                    textBox2.Text = String.Empty;
                }
            }
        }

        private void moveToDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                TreeViewItem treeViewItem = GetTreeViewItem(treeView1.SelectedNode);
                if (treeViewItem != null && MoveToDuplicatesRequested != null)
                {
                    MoveToDuplicatesRequested.Invoke(new[] { treeViewItem });
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                TreeViewItem treeViewItem = GetTreeViewItem(treeView1.SelectedNode);
                if (treeViewItem != null && DeleteDuplicatesRequested != null)
                {
                    DeleteDuplicatesRequested.Invoke(new[] { treeViewItem });
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Right))
            {
                treeView1.SelectedNode = e.Node;
            }

            e.Node.ContextMenuStrip = contextMenuStrip1;

            /*
            if (e.Node.Level == 0)
            {
                e.Node.ContextMenuStrip = contextMenuStrip1;
            }
            */
        }

        #endregion
    }
}
