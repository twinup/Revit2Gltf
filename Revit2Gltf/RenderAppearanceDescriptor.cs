#if REVIT2019
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    /// <summary>
    /// supplies dynamic custom type information for an Asset while it is displayed in PropertyGrid.
    /// </summary>
    internal class RenderAppearanceDescriptor : ICustomTypeDescriptor
    {
        #region Fields
        /// <summary>
        /// Reference to Asset
        /// </summary>
        Asset m_asset;

        /// <summary>
        /// Asset's property descriptors
        /// </summary>
        PropertyDescriptorCollection m_propertyDescriptors;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes Asset object
        /// </summary>
        /// <param name="asset">an Asset object</param>
        public RenderAppearanceDescriptor(Asset asset)
        {
            m_asset = asset;
            GetAssetProperties();
        }

        #endregion

        #region Methods
        #region ICustomTypeDescriptor Members

        /// <summary>
        /// Returns a collection of custom attributes for this instance of Asset.
        /// </summary>
        /// <returns>Asset's attributes</returns>
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(m_asset, false);
        }

        /// <summary>
        /// Returns the class name of this instance of Asset.
        /// </summary>
        /// <returns>Asset's class name</returns>
        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(m_asset, false);
        }

        /// <summary>
        /// Returns the name of this instance of Asset.
        /// </summary>
        /// <returns>The name of Asset</returns>
        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(m_asset, false);
        }

        /// <summary>
        /// Returns a type converter for this instance of Asset.
        /// </summary>
        /// <returns>The converter of the Asset</returns>
        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(m_asset, false);
        }

        /// <summary>
        /// Returns the default event for this instance of Asset.
        /// </summary>
        /// <returns>An EventDescriptor that represents the default event for this object, 
        /// or null if this object does not have events.</returns>
        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(m_asset, false);
        }

        /// <summary>
        /// Returns the default property for this instance of Asset.
        /// </summary>
        /// <returns>A PropertyDescriptor that represents the default property for this object, 
        /// or null if this object does not have properties.</returns>
        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(m_asset, false);
        }

        /// <summary>
        /// Returns an editor of the specified type for this instance of Asset.
        /// </summary>
        /// <param name="editorBaseType">A Type that represents the editor for this object. </param>
        /// <returns>An Object of the specified type that is the editor for this object, 
        /// or null if the editor cannot be found.</returns>
        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(m_asset, editorBaseType, false);
        }

        /// <summary>
        /// Returns the events for this instance of Asset using the specified attribute array as a filter.
        /// </summary>
        /// <param name="attributes">An array of type Attribute that is used as a filter. </param>
        /// <returns>An EventDescriptorCollection that represents the filtered events for this Asset instance.</returns>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(m_asset, attributes, false);
        }

        /// <summary>
        /// Returns the events for this instance of Asset.
        /// </summary>
        /// <returns>An EventDescriptorCollection that represents the events for this Asset instance.</returns>
        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(m_asset, false);
        }

        /// <summary>
        /// Returns the properties for this instance of Asset using the attribute array as a filter.
        /// </summary>
        /// <param name="attributes">An array of type Attribute that is used as a filter.</param>
        /// <returns>A PropertyDescriptorCollection that 
        /// represents the filtered properties for this Asset instance.</returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return m_propertyDescriptors;
        }

        /// <summary>
        /// Returns the properties for this instance of Asset.
        /// </summary>
        /// <returns>A PropertyDescriptorCollection that represents the properties 
        /// for this Asset instance.</returns>
        public PropertyDescriptorCollection GetProperties()
        {
            return m_propertyDescriptors;
        }

        /// <summary>
        /// Returns an object that contains the property described by the specified property descriptor.
        /// </summary>
        /// <param name="pd">A PropertyDescriptor that represents the property whose owner is to be found. </param>
        /// <returns>Asset object</returns>
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return m_asset;
        }
        #endregion

        /// <summary>
        /// Get Asset's property descriptors
        /// </summary>
        private void GetAssetProperties()
        {
            if (null == m_propertyDescriptors)
            {
                m_propertyDescriptors = new PropertyDescriptorCollection(new AssetPropertyPropertyDescriptor[0]);
            }
            else
            {
                return;
            }

            //For each AssetProperty in Asset, create an AssetPropertyPropertyDescriptor.
            //It means that each AssetProperty will be a property of Asset
            for (int index = 0; index < m_asset.Size; index++)
            {
                AssetProperty assetProperty = m_asset[index];
                if (null != assetProperty)
                {
                    AssetPropertyPropertyDescriptor assetPropertyPropertyDescriptor = new AssetPropertyPropertyDescriptor(assetProperty);
                    m_propertyDescriptors.Add(assetPropertyPropertyDescriptor);
                }
            }
        }
        #endregion
    }
}
