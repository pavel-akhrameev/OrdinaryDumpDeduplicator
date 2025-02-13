﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal partial class DuplicateReportForm : Form, IDuplicatesViewModel
    {
        private readonly ContextMenuStrip _emptyContextMenuStrip = new ContextMenuStrip();

        private Boolean _ignoreEventsFromControls = false;

        private Dictionary<ItemToView, TreeNode> _treeViewItems;
        private Dictionary<TreeNode, TreeNode> _parentNodes;

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
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetTreeViewItemsInternal(treeViewItems, resetForm)));
            }
            else
            {
                SetTreeViewItemsInternal(treeViewItems, resetForm);
            }
        }

        public void AddTreeViewItem(ItemToView treeViewItem, ItemToView parentItemToView)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddTreeViewItemInternal(treeViewItem, parentItemToView)));
            }
            else
            {
                AddTreeViewItemInternal(treeViewItem, parentItemToView);
            }
        }

        public void DeleteTreeViewItem(ItemToView treeViewItem)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => DeleteTreeViewItemInternal(treeViewItem)));
            }
            else
            {
                DeleteTreeViewItemInternal(treeViewItem);
            }
        }

        public void AddSessionMessage(String message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddSessionMessageInternal(message)));
            }
            else
            {
                AddSessionMessageInternal(message);
            }
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

        private void SetTreeViewItemsInternal(ItemToView[] treeViewItems, Boolean resetForm)
        {
            treeView1.Nodes.Clear();
            _treeViewItems = new Dictionary<ItemToView, TreeNode>();
            _parentNodes = new Dictionary<TreeNode, TreeNode>();

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

        private void AddTreeViewItemInternal(ItemToView treeViewItemToAdd, ItemToView parentItemToView = null)
        {
            TreeNode treeNode = MakeTreeNodeWithChildren(treeViewItemToAdd);

            if (parentItemToView != null)
            {
                if (_treeViewItems.TryGetValue(parentItemToView, out TreeNode parentTreeNode))
                {
                    parentTreeNode.Nodes.Add(treeNode);
                    _parentNodes.Add(treeNode, parentTreeNode);
                }
                else
                {
                    throw new Exception(""); // TODO
                }
            }
            else
            {
                treeView1.Nodes.Add(treeNode);
            }
        }

        private void DeleteTreeViewItemInternal(ItemToView existingTreeViewItem)
        {
            if (_treeViewItems.TryGetValue(existingTreeViewItem, out TreeNode treeNode))
            {
                TreeNode parentNode = _parentNodes[treeNode];
                parentNode.Nodes.Remove(treeNode);

                _treeViewItems.Remove(existingTreeViewItem);
            }
        }

        private TreeNode MakeTreeNodeWithChildren(ItemToView itemInReport)
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

        private TreeNode MakeTreeNode(ItemToView itemInReport, TreeNode[] children = null)
        {
            TreeNode treeNode;
            if (children != null)
            {
                treeNode = new TreeNode(itemInReport.Name, children);

                foreach (TreeNode childTreeNode in children)
                {
                    _parentNodes.Add(childTreeNode, treeNode);
                }
            }
            else
            {
                treeNode = new TreeNode(itemInReport.Name);
            }

            treeNode.ForeColor = itemInReport.Color;
            treeNode.Tag = itemInReport;

            _treeViewItems.Add(itemInReport, treeNode);
            return treeNode;
        }

        private void AddSessionMessageInternal(String message)
        {
            textBox1.Text = message;
        }

        #endregion

        #region Event handlers

        private void button1_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }

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
            ItemToView treeViewItem = nodeTag as ItemToView;
            if (treeViewItem != null && treeViewItem.Type == typeof(FileInfo))
            {
                FileInfo fileInfo = (FileInfo)treeViewItem.WrappedObject;
                textBox1.Text = fileInfo.File.Path;
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

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node != null)
                {

                    if (e.Node.Tag != null)
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
                        e.Node.ContextMenuStrip = contextMenuStrip2;
                    }
                }

                /*
                if (e.Node.Level == 0)
                {
                    e.Node.ContextMenuStrip = contextMenuStrip1;
                }
                */
            }
        }

        private void treeView1_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            if (e.Node != null)
            {
                Object nodeTag = e.Node?.Tag;
                ItemToView treeViewItem = nodeTag as ItemToView;
                if (treeViewItem != null && treeViewItem.Type == typeof(FileInfo))
                {
                    FileInfo fileInfo = (FileInfo)treeViewItem.WrappedObject;
                    FileInfo[] duplicates = fileInfo.SameContentFilesInfo.Duplicates
                        .Except(new[] { fileInfo })
                        .OrderBy(fInfo => fInfo.Sort)
                        .ToArray();

                    var duplicatesInfoStringBuilder = new StringBuilder();
                    if (duplicates.Length > 0)
                    {
                        duplicatesInfoStringBuilder.AppendLine("Duplicates:");
                        foreach (FileInfo duplicate in duplicates)
                        {
                            duplicatesInfoStringBuilder.AppendLine(duplicate.ToString());
                        }
                    }
                    else
                    {
                        duplicatesInfoStringBuilder.Append("Has no duplicates.");
                    }

                    e.Node.ToolTipText = duplicatesInfoStringBuilder.ToString();
                }
                else
                {
                    e.Node.ToolTipText = String.Empty;
                }
            }
        }

        private void DuplicateReportForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        #endregion

        #region Private static methods

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
