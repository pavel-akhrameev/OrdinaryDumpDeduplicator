using System;

namespace OrdinaryDumpDeduplicator.Desktop
{
    public sealed class ItemToView
    {
        #region Private fields

        private readonly Object _object;
        private readonly Type _objectType;
        private readonly ItemToView[] _childItems;

        private readonly String _stringRepresentation;
        private readonly System.Drawing.Color _itemColor;

        private readonly Boolean _isMoveable;
        private readonly Boolean _isDeletable;
        private readonly Boolean _isHidden;

        #endregion

        public ItemToView(
            Object objectReference,
            Type objectType,
            ItemToView[] childItems,
            String representation,
            System.Drawing.Color itemColor,
            Boolean isMoveable,
            Boolean isDeletable,
            Boolean isHidden = false)
        {
            this._object = objectReference;
            this._objectType = objectType;
            this._childItems = childItems;

            this._stringRepresentation = representation;
            this._itemColor = itemColor;
            this._childItems = childItems;
            this._isMoveable = isMoveable;
            this._isDeletable = isDeletable;
            this._isHidden = isHidden;
        }

        #region Public properties

        public Object WrappedObject => _object;

        public Type Type => _objectType;

        public String Name => _stringRepresentation;

        public ItemToView[] ChildItems => _childItems;

        public System.Drawing.Color Color => _itemColor;

        public Boolean IsMoveable => _isMoveable;

        public Boolean IsDeletable => _isDeletable;

        public Boolean IsHidden => _isHidden;

        #endregion
    }
}
