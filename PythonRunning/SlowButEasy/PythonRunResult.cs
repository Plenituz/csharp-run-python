using System.Collections.Generic;

namespace PythonRunning
{
    public struct PythonRunResult
    {
        public bool hasError;
        /// <summary>
        /// only valid value if hasError is true
        /// </summary>
        public string errorString;
        public Dictionary<string, object> returnedValues;
    }
}
