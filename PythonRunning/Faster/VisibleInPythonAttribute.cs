using System;

namespace PythonRunning
{
    /// <summary>
    /// make the method or field visible in the python proxy object
    /// 
    /// when applied to a property, you must specify a backing field name
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class VisibleInPythonAttribute : Attribute
    {
        /// <summary>
        /// when applied to a property, you must specify a backing field name
        /// </summary>
        public string BackingField { get; set; }
        public VisibleInPythonAttribute()
        {
        }
    }
}
