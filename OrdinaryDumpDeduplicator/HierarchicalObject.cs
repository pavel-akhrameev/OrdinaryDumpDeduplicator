using System;

namespace OrdinaryDumpDeduplicator
{
    public sealed class HierarchicalObject
    {
        private readonly Object _object;
        private readonly Type _objectType;
        private readonly ObjectSort _objectSort;
        private readonly String _stringRepresentation;

        private HierarchicalObject[] _childObjects;

        #region Constructors and creators

        private HierarchicalObject(Object objectReference, Type objectType, ObjectSort objectSort, String representation, HierarchicalObject[] childObjects)
        {
            this._object = objectReference;
            this._objectType = objectType;
            this._objectSort = objectSort;
            this._stringRepresentation = representation;
            this._childObjects = childObjects;
        }

        public static HierarchicalObject Create(Object objectReference, ObjectSort objectSort, HierarchicalObject[] childObjects, String representation = null)
        {
            Type objectType = objectReference.GetType();
            if (representation == null)
            {
                representation = objectReference.ToString();
            }

            var hierarchicalObject = new HierarchicalObject(objectReference, objectType, objectSort, representation, childObjects);
            return hierarchicalObject;
        }

        #endregion

        #region Public properties

        public Object Object => _object;

        public HierarchicalObject[] ChildObjects => _childObjects;

        public String Name => _stringRepresentation;

        public Type Type => _objectType;

        public ObjectSort Sort => _objectSort;

        #endregion

        public void SetChildObjects(HierarchicalObject[] childObjects)
        {
            _childObjects = childObjects;
        }

        #region Overrides of object

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var other = obj as HierarchicalObject;
            if (other == null)
            {
                return false;
            }

            var isEqual = this._object.Equals(other._object);
            return isEqual;
        }

        public override Int32 GetHashCode()
        {
            var hashCode = this._object.GetHashCode();
            return hashCode;
        }

        public override String ToString()
        {
            return _object.ToString();
        }

        #endregion
    }
}
