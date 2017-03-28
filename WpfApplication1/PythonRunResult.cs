using System.Collections;

namespace PythonRunnerNameSpace
{
    public struct PythonRunResult
    {
        public bool hasError;
        public string errorString;
        public Hashtable returnedValues;
    }
}
