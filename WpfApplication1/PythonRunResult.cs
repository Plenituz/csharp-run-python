using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonRunnerNameSpace
{
    public struct PythonRunResult
    {
        public bool hasError;
        public string errorString;
        public Hashtable returnedValues;
    }
}
