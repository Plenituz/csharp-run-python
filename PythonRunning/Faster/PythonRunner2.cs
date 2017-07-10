using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace PythonRunning
{
    public class PythonRunner2
    {
        /// <summary>
        /// this is stdout and stderr in the same string
        /// </summary>
        public string OverallOut { get; private set; }

        private string _stdout;
        /// <summary>
        /// this is the content of stdout from the python script
        /// </summary>
        public string Stdout
        {
            get { return _stdout; }
            set
            {
                _stdout = value;
                OnStdOutUpdate?.Invoke(_stdout);
            }
        }
        private string _stderr;
        /// <summary>
        /// this is the content of stdout from the python script
        /// </summary>
        public string Stderr
        {
            get { return _stderr; }
            set
            {
                _stderr = value;
                OnStdErrUpdate?.Invoke(_stderr);
            }
        }

        PythonRunnerProcess process;
        Queue<CustomAction> schedule = new Queue<CustomAction>();
        /// <summary>
        /// this event and all the others will NOT get called by the main thread
        /// 
        /// theses event are just proxy
        /// </summary>
        public event Action<string> OnStdOutUpdate;
        public event Action<string> OnStdErrUpdate;
        public event Action OnDoneCompiling
        {
            add { process.OnDoneCompiling += value; }
            remove { process.OnDoneCompiling -= value; }
        }
        public event Action OnStateWaiting
        {
            add { process.OnStateWaiting += value; }
            remove { process.OnStateWaiting -= value; }
        }
        public event Action OnDoneRunning
        {
            add { process.OnDoneRunning += value; }
            remove { process.OnDoneRunning -= value; }
        }
        public event Action OnCompileError
        {
            add { process.OnCompileError += value; }
            remove { process.OnCompileError -= value; }
        }
        public event Action OnRunError
        {
            add { process.OnRunError += value; }
            remove { process.OnRunError -= value; }
        }
        /// <summary>
        /// args are valueName and the proxy object
        /// </summary>
        public event Action<string, object> OnValueUpdatedOnProxy
        {
            add { process.OnValueUpdatedOnProxy += value; }
            remove { process.OnValueUpdatedOnProxy -= value; }
        }
        public bool HasCompileError => process.hasCompileError;
        public bool HasRunError => process.hasRunError;
        public object Proxy => process.proxy;

        /// <summary>
        /// the name might get displayed to the user if the python code has errors
        /// </summary>
        /// <param name="name"></param>
        public PythonRunner2(INotifyPropertyChanged proxy)
        {
            process = new PythonRunnerProcess();
            proxy.PropertyChanged += Proxy_PropertyChanged;
            process.proxy = proxy;
            process.OnStdErr += OnStdErr;
            process.OnStdOut += OnStdOut;
            OnStateWaiting += ContinueSchedule;
        }

        private void Proxy_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            object value = Proxy.GetType().GetProperty(e.PropertyName).GetValue(Proxy);
            process.UpdatePythonProperty(e.PropertyName, value);
        }

        void ContinueSchedule()
        {
            if(schedule.Count != 0)
                schedule.Dequeue()?.Invoke();
        }

        /// <summary>
        /// Compile the given code to be executed, this needs to be called before calling Run
        /// 
        /// if the python process is occupied, this will do nothing, to make sur the action
        /// happens, call the same function without the "_" prefix
        /// </summary>
        public bool _Compile(string code)
        {
            List<string> visibleInPythonProperties =
                Proxy.GetType().GetProperties()
                .Where(prop => prop.GetCustomAttributes(false)
                                .OfType<VisibleInPythonAttribute>()
                                .Count() != 0)
                                .Select(prop => prop.Name)
                                .ToList();

            List<PythonMethod> visibleInPythonMethods =
                Proxy.GetType().GetMethods()
                .Where(meth => meth.GetCustomAttributes(false)
                                .OfType<VisibleInPythonAttribute>()
                                .Count() != 0)
                                .Select(meth =>
                                    new PythonMethod(meth.ReturnType != typeof(void),
                                    meth.Name,
                                    meth.GetParameters().Select(param => param.Name).ToArray()))
                                .ToList();

            string json = JsonConvert.SerializeObject(new Hashtable()
            {
                {"userCode", code },
                {"properties", visibleInPythonProperties },
                {"functions", visibleInPythonMethods }
            });
            return process.CompileCode(json); ;             
        }

        public void Compile(string code)
        {
            if (!_Compile(code))
            {
                schedule.Enqueue(new CustomAction(obj => {
                        _Compile((string)obj);
                    }, 
                    code));
            }
        }

        /// <summary>
        /// Run the compiled program, python will crash if you didn't call compile before
        /// 
        /// if the python process is occupied, this will do nothing, to make sur the action
        /// happens, call the same function without the "_" prefix
        /// </summary>
        public bool _Run()
        {
            return process.RunCode();
        }

        /// <summary>
        /// Run the compiled program, python will crash if you didn't call compile before
        /// 
        /// call this if you want to make sure the action happens
        /// if it doesn't happen immediatly it will be added to a queue of event and 
        /// be executed as soon as the python process can 
        /// </summary>
        public void Run()
        {
            if (!_Run())
            {
                schedule.Enqueue(new CustomAction(obj => _Run(), null));
            }
        }

        void OnStdOut(string line)
        {
            OverallOut += line + "\n";
            Stdout += line + "\n";
        }

        void OnStdErr(string line)
        {
            OverallOut += line + "\n";
            Stderr += line + "\n";
        }

        /// <summary>
        /// tries to convert the string into an int or a float
        /// </summary>
        /// <param name="str"></param>
        /// <returns>the string as an int or a float, if prossible, otherwise you get the string back</returns>
        private object TryConvert(string str)
        {
            //try parse int and float if it doesn't work, return the string
            int outInt;
            bool succeded = int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out outInt);
            if (succeded)
                return outInt;
            float outFloat;
            succeded = float.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out outFloat);
            if (succeded)
                return outFloat;
            return str;
        }
    }
}
