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

            SetDirectoryPath(@"\\VBOXSVR\files\Test data for deduplication");
        }

        public event Action<String> AddDataLocationRequested;
        public event Action<ItemToView> RescanRequested;
        public event Action<IReadOnlyCollection<ItemToView>> FindDuplicatesRequested;

        public event Action AboutFormRequested;
        public event Func<Boolean> ApplicationCloseRequested;

        #region Public methods

        public void SetListViewItems(IReadOnlyCollection<ItemToView> items)
        {
            var listViewItemCollection = new List<ListViewItem>(items.Count);
            foreach (ItemToView itemToView in items)
            {
                var listViewItem = MakeListViewItem(itemToView);
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
                ItemToView selectedObject = null;
                if (listView1.CheckedItems != null && listView1.CheckedItems.Count == 1)
                {
                    IReadOnlyCollection<ItemToView> checkedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.CheckedItems));
                    if (checkedObjects != null && checkedObjects.Count == 1)
                    {
                        selectedObject = System.Linq.Enumerable.First(checkedObjects);
                    }
                    else
                    {
                        // Nothing to do here.
                    }
                }
                else if (listView1.SelectedItems != null && listView1.SelectedItems.Count == 1)
                {
                    IReadOnlyCollection<ItemToView> selectedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.SelectedItems));
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
                IReadOnlyCollection<ItemToView> checkedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.CheckedItems));
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

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            Boolean isPossibleToRescanDataLocation = false;
            Boolean isPossibleToSearchDuplicates = false;

            if (listView1.CheckedItems != null && listView1.CheckedItems.Count >= 1)
            {
                isPossibleToSearchDuplicates = true;

                IReadOnlyCollection<ItemToView> checkedObjects = GetObjectsFromListViewItems(ToEnumerable(listView1.CheckedItems));
                if (checkedObjects.Count == 1)
                {
                    isPossibleToRescanDataLocation = true;
                }
            }

            if (!isPossibleToRescanDataLocation)
            {
                isPossibleToRescanDataLocation = listView1.SelectedItems != null && listView1.SelectedItems.Count == 1;
            }

            button3.Enabled = isPossibleToRescanDataLocation;
            button4.Enabled = isPossibleToSearchDuplicates;
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Boolean isPossibleToRescanDataLocation = listView1.SelectedItems != null && listView1.SelectedItems.Count == 1;
            if (!isPossibleToRescanDataLocation)
            {
                isPossibleToRescanDataLocation = listView1.CheckedItems != null && listView1.CheckedItems.Count == 1;
            }

            button3.Enabled = isPossibleToRescanDataLocation;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Boolean isPossibleToAddDataLocation = !String.IsNullOrWhiteSpace(textBox1.Text);
            button2.Enabled = isPossibleToAddDataLocation;
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

        private static IReadOnlyCollection<ItemToView> GetObjectsFromListViewItems(IEnumerable<ListViewItem> listViewItems)
        {
            var objects = new List<ItemToView>();
            foreach (ListViewItem listItem in listViewItems)
            {
                Object listItemTag = listItem.Tag;

                ItemToView itemToView;
                if (listItemTag is ItemToView)
                {
                    itemToView = listItemTag as ItemToView;
                }
                else
                {
                    Type type = listItemTag.GetType();
                    throw new Exception($"{type.Name} is wrong type for listView1 item.");
                }

                objects.Add(itemToView);
            }

            return objects;
        }

        private static ListViewItem MakeListViewItem(ItemToView itemToView)
        {
            //String ItemToViewSortString = Enum.GetName(typeof(ObjectSort), ItemToView.Sort);
            String lastRescanDate = "None";
            var fields = new String[] { itemToView.Name, lastRescanDate, String.Empty, String.Empty, itemToView.ToString() };
            var listViewItem = new ListViewItem(fields);
            listViewItem.Tag = itemToView;

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
