using System.Collections;
using System.Collections.Generic;

namespace PythonRunnerNameSpace
{
    public struct PythonRunResult
    {
        public bool hasError;
        public string errorString;
        public Dictionary<string, object> returnedValues;
    }
}
