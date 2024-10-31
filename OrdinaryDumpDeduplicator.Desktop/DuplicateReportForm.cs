using System;
using System.Windows.Forms;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public partial class DuplicateReportForm : Form, IDuplicatesViewModel
    {
        private Boolean _ignoreEventsFromControls = false;

        public DuplicateReportForm()
        {
            InitializeComponent();
        }

        public event Action<Boolean> ViewGroupsByHashRequested;
        public event Action ViewGroupsByFoldersRequested;

        public event Action<TreeViewItem[]> MoveToDuplicatesRequested;
        public event Action<TreeViewItem[]> DeleteDuplicatesRequested;

        #region Public methods

        public void SetTreeViewItems(TreeViewItem[] treeViewItems, Boolean resetForm)
        {
            treeView1.Nodes.Clear();
            Boolean viewFullPath = radioButton1.Checked; // TODO

            if (resetForm)
            {
                ResetFormControls();
            }

            foreach (TreeViewItem itemInReport in treeViewItems)
            {
                TreeNode treeNode = MakeTreeNodeWithChildren(itemInReport);
                treeView1.Nodes.Add(treeNode);
            }
        }

        public void AddSessionMessage(String message)
        {
            textBox1.Text = message;
        }

        #endregion

        #region Private methods

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

        private void ResetFormControls()
        {
            _ignoreEventsFromControls = true;

            checkBox1.Checked = false;

            _ignoreEventsFromControls = false;
        }

        #endregion

        #region Event handlers

        private void treeItemsViewParameters_Changed(object sender, EventArgs e)
        {
            if (!radioButton1.Checked)
            {
                checkBox1.Enabled = false;
            }

            if (!_ignoreEventsFromControls)
            {
                ViewGroupsOfDuplicates();
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Object nodeTag = treeView1.SelectedNode?.Tag;
            var file = nodeTag as File;
            if (file != null)
            {
                textBox1.Text = file.Path;
            }
            else
            {
                textBox1.Text = String.Empty;
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
            if (e.Button == MouseButtons.Right)
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

        private void DuplicateReportForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        #endregion

        #region Private static methods

        private static TreeNode MakeTreeNodeWithChildren(TreeViewItem itemInReport)
        {
            TreeNode treeNode;

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

            treeNode = MakeTreeNode(itemInReport, childNodes);
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
    }
}
