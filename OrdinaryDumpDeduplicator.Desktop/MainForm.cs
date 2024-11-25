﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public partial class MainForm : Form, IMainViewModel
    {
        public MainForm()
        {
            InitializeComponent();

            SetDirectoryPath(@"\\VBOXSVR\files\Test data for deduplication");
        }

        public event Action<String> AddDataLocationRequested;
        public event Action<HierarchicalObject> RescanRequested;
        public event Action<IReadOnlyCollection<HierarchicalObject>> FindDuplicatesRequested;

        public event Action AboutFormRequested;
        public event Func<Boolean> ApplicationCloseRequested;

        #region Public methods

        public void SetListViewItems(IReadOnlyCollection<HierarchicalObject> hierarchicalObjects)
        {
            var listViewItemCollection = new List<ListViewItem>(hierarchicalObjects.Count);
            foreach (HierarchicalObject hierarchicalObject in hierarchicalObjects)
            {
                var listViewItem = MakeListViewItem(hierarchicalObject);
                listViewItemCollection.Add(listViewItem);
            }

            listView1.Items.Clear();
            listView1.Items.AddRange(listViewItemCollection.ToArray());

            // Select the item if single item is added.
            if (listViewItemCollection.Count == 1)
            {
                const Int32 singleItemIndex = 0;
                listView1.Items[singleItemIndex].Checked = true;
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

        /// <remarks>Add new data location.</remarks>
        private void button2_Click(object sender, EventArgs e)
        {
            String dataLocationPath = textBox1.Text;

            if (!String.IsNullOrWhiteSpace(dataLocationPath) && AddDataLocationRequested != null)
            {
                AddDataLocationRequested.Invoke(dataLocationPath);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (RescanRequested != null)
            {
                HierarchicalObject selectedObject = null;
                if (listView1.CheckedItems != null && listView1.CheckedItems.Count == 1)
                {
                    IReadOnlyCollection<HierarchicalObject> checkedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.CheckedItems));
                    if (checkedObjects != null && checkedObjects.Count == 1)
                    {
                        selectedObject = System.Linq.Enumerable.First(checkedObjects);
                    }
                    else
                    {
                        // Nothing to do here.
                    }
                }
                else if (listView1.CheckedItems != null && listView1.SelectedItems.Count > 0)
                {
                    IReadOnlyCollection<HierarchicalObject> selectedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.SelectedItems));
                    if (selectedObjects != null && selectedObjects.Count > 0)
                    {
                        selectedObject = System.Linq.Enumerable.First(selectedObjects);
                    }
                    else
                    {
                        throw new Exception(""); // TODO
                    }
                }
                else
                {
                    // Nothing to do here.
                }

                if (selectedObject != null)
                {
                    RescanRequested.Invoke(selectedObject);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listView1.CheckedItems != null && listView1.CheckedItems.Count > 0)
            {
                IReadOnlyCollection<HierarchicalObject> checkedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.CheckedItems));
                if (checkedObjects != null && checkedObjects.Count > 0 && FindDuplicatesRequested != null)
                {
                    FindDuplicatesRequested.Invoke(checkedObjects);
                }
            }
        }

        private void aboutOrdinaryDumpDeduplicatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AboutFormRequested != null)
            {
                AboutFormRequested.Invoke();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ApplicationCloseRequested != null)
            {
                Boolean allowedToClose = ApplicationCloseRequested.Invoke();
                e.Cancel = !allowedToClose;
            }
        }

        #endregion

        #region Private static methods

        private static IReadOnlyCollection<HierarchicalObject> GetObjectsFromListViewItems(IEnumerable<ListViewItem> listViewItems)
        {
            var objects = new List<HierarchicalObject>();
            foreach (ListViewItem listItem in listViewItems)
            {
                Object listItemTag = listItem.Tag;

                HierarchicalObject hierarchicalObject;
                if (listItemTag is HierarchicalObject)
                {
                    hierarchicalObject = listItemTag as HierarchicalObject;
                }
                else
                {
                    Type type = listItemTag.GetType();
                    throw new Exception($"{type.Name} is wrong type for listView1 item.");
                }

                objects.Add(hierarchicalObject);
            }

            return objects;
        }

        private static ListViewItem MakeListViewItem(HierarchicalObject hierarchicalObject)
        {
            String hierarchicalObjectSortString = Enum.GetName(typeof(ObjectSort), hierarchicalObject.Sort);
            var fields = new String[] { hierarchicalObject.Name, hierarchicalObjectSortString, String.Empty, String.Empty, hierarchicalObject.ToString() };
            var listViewItem = new ListViewItem(fields);
            listViewItem.Tag = hierarchicalObject;

            return listViewItem;
        }

        private static IEnumerable<ListViewItem> ToEnumerable(ListView.CheckedListViewItemCollection listViewItems)
        {
            foreach (ListViewItem listViewItem in listViewItems)
            {
                yield return listViewItem;
            }
        }

        private static IEnumerable<ListViewItem> ToEnumerable(ListView.SelectedListViewItemCollection listViewItems)
        {
            foreach (ListViewItem listViewItem in listViewItems)
            {
                yield return listViewItem;
            }
        }

        #endregion
    }
}
