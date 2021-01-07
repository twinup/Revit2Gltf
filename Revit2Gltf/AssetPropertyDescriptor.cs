using Autodesk.Revit.DB;
using Autodesk.Revit.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    /// <summary>
    /// A description of a property consists of a name, its attributes and value
    /// here AssetPropertyPropertyDescriptor is used to wrap AssetProperty 
    /// to display its name and value in PropertyGrid
    /// </summary>
    internal class AssetPropertyPropertyDescriptor : PropertyDescriptor
    {
        #region Fields
        /// <summary>
        /// A reference to an AssetProperty
        /// </summary>
        private AssetProperty m_assetProperty;

        /// <summary>
        /// The type of AssetProperty's property "Value"
        /// </summary>m
        private Type m_valueType;

        /// <summary>
        /// The value of AssetProperty's property "Value"
        /// </summary>
        private Object m_value;
        #endregion

        #region Properties
        /// <summary>
        /// Property to get internal AssetProperty
        /// </summary>
        public AssetProperty AssetProperty
        {
            get { return m_assetProperty; }
        }
        #endregion

        #region override Properties
        /// <summary>
        /// Gets a value indicating whether this property is read-only
        /// </summary>
        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the type of the component this property is bound to. 
        /// </summary>
        public override Type ComponentType
        {
            get
            {
                return m_assetProperty.GetType();
            }
        }

        /// <summary>
        /// Gets the type of the property. 
        /// </summary>
        public override Type PropertyType
        {
            get
            {
                return m_valueType;
            }
        }
        #endregion

        /// <summary>
        /// Public class constructor
        /// </summary>
        /// <param name="assetProperty">the AssetProperty which a AssetPropertyPropertyDescriptor instance describes</param>
        public AssetPropertyPropertyDescriptor(AssetProperty assetProperty)
            : base(assetProperty.Name, new Attribute[0])
        {
            m_assetProperty = assetProperty;
        }

        #region override methods
        /// <summary>
        /// Compares this to another object to see if they are equivalent
        /// </summary>
        /// <param name="obj">The object to compare to this AssetPropertyPropertyDescriptor. </param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            AssetPropertyPropertyDescriptor other = obj as AssetPropertyPropertyDescriptor;
            return other != null && other.AssetProperty.Equals(m_assetProperty);
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// Here override the method "Equals", so it is necessary to override GetHashCode too.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_assetProperty.GetHashCode();
        }

        /// <summary>
        /// Resets the value for this property of the component to the default value. 
        /// </summary>
        /// <param name="component">The component with the property value that is to be reset to the default value.</param>
        public override void ResetValue(object component)
        {

        }

        /// <summary>
        /// Returns whether resetting an object changes its value. 
        /// </summary>
        /// <param name="component">The component to test for reset capability.</param>
        /// <returns>true if resetting the component changes its value; otherwise, false.</returns>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>G
        /// Determines a value indicating whether the value of this property needs to be persisted.
        /// </summary>
        /// <param name="component">The component with the property to be examined for persistence.</param>
        /// <returns>true if the property should be persisted; otherwise, false.</returns>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Gets the current value of the property on a component.
        /// </summary>
        /// <param name="component">The component with the property for which to retrieve the value.</param>
        /// <returns>The value of a property for a given component.</returns>
        public override object GetValue(object component)
        {
            Tuple<Type, Object> typeAndValue = GetTypeAndValue(m_assetProperty, 0);
            m_value = typeAndValue.Item2;
            m_valueType = typeAndValue.Item1;

            return m_value;
        }

        private static Tuple<Type, Object> GetTypeAndValue(AssetProperty assetProperty, int level)
        {
            Object theValue;
            Type valueType;
            //For each AssetProperty, it has different type and value
            //must deal with it separately
            try
            {
                if (assetProperty is AssetPropertyBoolean)
                {
                    AssetPropertyBoolean property = assetProperty as AssetPropertyBoolean;
                    valueType = typeof(AssetPropertyBoolean);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyDistance)
                {
                    AssetPropertyDistance property = assetProperty as AssetPropertyDistance;
                    valueType = typeof(AssetPropertyDistance);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyDouble)
                {
                    AssetPropertyDouble property = assetProperty as AssetPropertyDouble;
                    valueType = typeof(AssetPropertyDouble);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyDoubleArray2d)
                {
                    //Default, it is supported by PropertyGrid to display Double []
                    //Try to convert DoubleArray to Double []
                    AssetPropertyDoubleArray2d property = assetProperty as AssetPropertyDoubleArray2d;
                    valueType = typeof(AssetPropertyDoubleArray2d);
                    theValue = GetSystemArrayAsString(property.Value);
                }
                else if (assetProperty is AssetPropertyDoubleArray3d)
                {
                    AssetPropertyDoubleArray3d property = assetProperty as AssetPropertyDoubleArray3d;
                    valueType = typeof(AssetPropertyDoubleArray3d);
                    theValue = GetSystemArrayAsString(property.Value);
                }
                else if (assetProperty is AssetPropertyDoubleArray4d)
                {
                    AssetPropertyDoubleArray4d property = assetProperty as AssetPropertyDoubleArray4d;
                    valueType = typeof(AssetPropertyDoubleArray4d);
                    theValue = GetSystemArrayAsString(property.Value);
                }
                else if (assetProperty is AssetPropertyDoubleMatrix44)
                {
                    AssetPropertyDoubleMatrix44 property = assetProperty as AssetPropertyDoubleMatrix44;
                    valueType = typeof(AssetPropertyDoubleMatrix44);
                    theValue = GetSystemArrayAsString(property.Value);
                }
                else if (assetProperty is AssetPropertyEnum)
                {
                    AssetPropertyEnum property = assetProperty as AssetPropertyEnum;
                    valueType = typeof(AssetPropertyEnum);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyFloat)
                {
                    AssetPropertyFloat property = assetProperty as AssetPropertyFloat;
                    valueType = typeof(AssetPropertyFloat);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyInteger)
                {
                    AssetPropertyInteger property = assetProperty as AssetPropertyInteger;
                    valueType = typeof(AssetPropertyInteger);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyReference)
                {
                    AssetPropertyReference property = assetProperty as AssetPropertyReference;
                    valueType = typeof(AssetPropertyReference);
                    theValue = "REFERENCE"; //property.Type;
                }
                else if (assetProperty is AssetPropertyString)
                {
                    AssetPropertyString property = assetProperty as AssetPropertyString;
                    valueType = typeof(AssetPropertyString);
                    theValue = property.Value;
                }
                else if (assetProperty is AssetPropertyTime)
                {
                    AssetPropertyTime property = assetProperty as AssetPropertyTime;
                    valueType = typeof(AssetPropertyTime);
                    theValue = property.Value;
                }
                else
                {
                    valueType = typeof(String);
                    theValue = "Unprocessed asset type: " + assetProperty.GetType().Name;
                }

                if (assetProperty.NumberOfConnectedProperties > 0)
                {

                    String result = "";
                    result = theValue.ToString();

                    IList<AssetProperty> properties = assetProperty.GetAllConnectedProperties();

                    foreach (AssetProperty property in properties)
                    {
                        if (property is Asset)
                        {
                            // Nested?
                            Asset asset = property as Asset;
                            int size = asset.Size;
                            for (int i = 0; i < size; i++)
                            {
                                AssetProperty subproperty = asset[i];
                                Tuple<Type, Object> valueAndType = GetTypeAndValue(subproperty, level + 1);
                                String indent = "";
                                if (level > 0)
                                {
                                    for (int iLevel = 1; iLevel <= level; iLevel++)
                                        indent += "   ";
                                }
                                result += "\n " + indent + "- connected: name: " + subproperty.Name + " | type: " + valueAndType.Item1.Name +
                                  " | value: " + valueAndType.Item2.ToString();
                            }
                        }
                    }

                    theValue = result;
                }
            }
            catch
            {
                return null;
            }
            return new Tuple<Type, Object>(valueType, theValue);
        }

        /// <summary>
        /// Sets the value of the component to a different value.
        /// For AssetProperty, it is not allowed to set its value, so here just return.
        /// </summary>
        /// <param name="component">The component with the property value that is to be set. </param>
        /// <param name="value">The new value.</param>
        public override void SetValue(object component, object value)
        {
            return;
        }
        #endregion

        /// <summary>
        /// Convert Autodesk.Revit.DB.DoubleArray to Double [].
        /// For Double [] is supported by PropertyGrid.
        /// </summary>
        /// <param name="doubleArray">the original Autodesk.Revit.DB.DoubleArray </param>
        /// <returns>The converted Double []</returns>
        private static Double[] GetSystemArray(DoubleArray doubleArray)
        {
            double[] values = new double[doubleArray.Size];
            int index = 0;
            foreach (Double value in doubleArray)
            {
                values[index++] = value;
            }
            return values;
        }

        private static String GetSystemArrayAsString(DoubleArray doubleArray)
        {
            double[] values = GetSystemArray(doubleArray);

            String result = "";
            foreach (double d in values)
            {
                result += d;
                result += ",";
            }

            return result;
        }

        public override string ToString()
        {
            return base.Name;
        }
    }
}
