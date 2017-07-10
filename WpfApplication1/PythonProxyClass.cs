using System;
using System.ComponentModel;
using PythonRunning;

namespace PythonRunnerExample
{
    public class PythonProxyClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void UpdatePythonValue(string valueName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(valueName));
        }

        public int InvisibleProperty{ get; set; }

        //-------
        //these properties are accessible in python
        //-------

        string _stringProperty;
        [VisibleInPython(BackingField = nameof(_stringProperty))]
        public string StringProperty
        {
            get
            {
                return _stringProperty;
            }

            set
            {
                _stringProperty = value;
                UpdatePythonValue(nameof(StringProperty));
            }
        }

        int _intProperty;
        [VisibleInPython(BackingField = nameof(_intProperty))]
        public int IntProperty
        {
            get
            {
                return _intProperty;
            }

            set
            {
                _intProperty = value;
                UpdatePythonValue(nameof(IntProperty));
            }
        }

        float _floatProperty;
        [VisibleInPython(BackingField = nameof(_floatProperty))]
        public float FloatProperty
        {
            get
            {
                return _floatProperty;
            }

            set
            {
                _floatProperty = value;
                UpdatePythonValue(nameof(FloatProperty));
            }
        }

        //------
        //these method can be called in python
        //------

        public void InvisibleMethod()
        {

        }

        [VisibleInPython]
        public void SimpleMethod()
        {
            Console.WriteLine("simple method has been called");
        }

        [VisibleInPython]
        public void MethodWithArg(object arg)
        {
            Console.WriteLine("method with args called:" + arg);
        }

        [VisibleInPython]
        public int MethodWithReturn()
        {
            Console.WriteLine("method with return called, returning 6");
            return 6;
        }

        [VisibleInPython]
        public string MethodWithReturnAndArg(int arg)
        {
            string ret = "Method called with:" + arg;
            Console.WriteLine("method wth arg and return called, returning:'" + ret + "'");
            return ret;
        }

        [VisibleInPython]
        public float[] Method2(int arg, float arg2)
        {
            Console.WriteLine("called method2 with " + arg + "," + arg2);
            return new float[] { arg + arg2, arg - arg2 };
        }
    }
}
