using System;
using System.Windows.Forms;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public partial class DuplicateReportForm : Form, IDuplicatesViewModel
    {
        private readonly ContextMenuStrip _emptyContextMenuStrip = new ContextMenuStrip();

        private Boolean _ignoreEventsFromControls = false;

        public DuplicateReportForm()
        {
            InitializeComponent();
        }

        public event Action<Boolean> ViewGroupsByHashRequested;
        public event Action<Boolean> ViewGroupsByFoldersRequested;

        public event Action<ItemToView[]> MoveToDuplicatesRequested;
        public event Action<ItemToView[]> DeleteDuplicatesRequested;

        #region Public methods

        public void SetTreeViewItems(ItemToView[] treeViewItems, Boolean resetForm)
        {
            treeView1.Nodes.Clear();
            Boolean viewFullPath = radioButton1.Checked; // TODO

            if (resetForm)
            {
                ResetFormControls();
            }

            foreach (ItemToView itemInReport in treeViewItems)
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
            Boolean hideIsolatedDuplicates = !checkBox1.Checked;

            if (radioButton1.Checked)
            {
                if (ViewGroupsByHashRequested != null)
                {
                    ViewGroupsByHashRequested.Invoke(hideIsolatedDuplicates);
                }
            }
            else
            {
                if (ViewGroupsByFoldersRequested != null)
                {
                    ViewGroupsByFoldersRequested.Invoke(hideIsolatedDuplicates);
                }
            }
        }

        private void ResetFormControls()
        {
            _ignoreEventsFromControls = true;

            radioButton1.Checked = true;
            checkBox1.Checked = false;

            _ignoreEventsFromControls = false;
        }

        #endregion

        #region Event handlers

        private void treeItemsViewParameters_Changed(object sender, EventArgs e)
        {
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
                ItemToView treeViewItem = GetTreeViewItem(treeView1.SelectedNode);
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
                ItemToView treeViewItem = GetTreeViewItem(treeView1.SelectedNode);
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

                ItemToView treeViewItem = e.Node.Tag as ItemToView;
                if (treeViewItem != null)
                {
                    moveToDuplicatesToolStripMenuItem.Enabled = treeViewItem.IsMoveable;
                    deleteToolStripMenuItem.Enabled = treeViewItem.IsDeletable;

                    e.Node.ContextMenuStrip = contextMenuStrip1;
                }
                else
                {
                    e.Node.ContextMenuStrip = _emptyContextMenuStrip;
                }
            }
            else
            {
                e.Node.ContextMenuStrip = _emptyContextMenuStrip;
            }

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

        private static TreeNode MakeTreeNodeWithChildren(ItemToView itemInReport)
        {
            TreeNode[] childNodes;

            ItemToView[] childItems = itemInReport.ChildItems;
            if (childItems != null && childItems.Length > 0)
            {
                childNodes = new TreeNode[childItems.Length];

                for (Int32 index = 0; index < childItems.Length; index++)
                {
                    ItemToView childItem = childItems[index];
                    TreeNode childNode = MakeTreeNodeWithChildren(childItem);
                    childNodes[index] = childNode;
                }
            }
            else
            {
                childNodes = new TreeNode[] { };
            }

            TreeNode treeNode = MakeTreeNode(itemInReport, childNodes);
            return treeNode;
        }

        private static TreeNode MakeTreeNode(ItemToView itemInReport, TreeNode[] children = null)
        {
            TreeNode treeNode;
            if (children != null)
            {
                treeNode = new TreeNode(itemInReport.Name, children);
            }
            else
            {
                treeNode = new TreeNode(itemInReport.Name);
            }

            treeNode.ForeColor = itemInReport.Color;
            treeNode.Tag = itemInReport;

            return treeNode;
        }

        private static ItemToView GetTreeViewItem(TreeNode treeNode)
        {
            Object nodeTag = treeNode.Tag;

            ItemToView treeViewItem;
            if (nodeTag is ItemToView)
            {
                treeViewItem = nodeTag as ItemToView;
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
