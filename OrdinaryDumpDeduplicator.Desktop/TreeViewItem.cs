using System;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public sealed class TreeViewItem
    {
        private readonly HierarchicalObject _object;
        private readonly System.Drawing.Color _itemColor;
        private readonly Boolean _isHidden;

        private readonly TreeViewItem[] _childItems;

        public TreeViewItem(HierarchicalObject hierarchicalObject, System.Drawing.Color itemColor, TreeViewItem[] childItems, Boolean isHidden = false)
        {
            this._object = hierarchicalObject;
            this._itemColor = itemColor;
            this._childItems = childItems;
            this._isHidden = isHidden;
        }

        #region Public properties

        public HierarchicalObject HierarchicalObject => _object;

        public String Name => _object.Name;

        public System.Drawing.Color Color => _itemColor;

        public TreeViewItem[] ChildItems => _childItems;

        public Boolean IsHidden => _isHidden;

        #endregion
    }
}
