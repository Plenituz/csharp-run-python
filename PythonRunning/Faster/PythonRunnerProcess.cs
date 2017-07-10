using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace PythonRunning
{
    internal class PythonRunnerProcess
    {
        /// <summary>
        /// for now this is defined by hand, it will one day be defined by not hand (by foot may be ?)
        /// </summary>
        private static string PYTHON_PATH = @"python";
        /// <summary>
        /// this too, it should propably be user defined
        /// </summary>
        private static string SCRIPT_PATH = @"C:\_CODING\C#\WpfApplication1\PythonProcess.py";

        private const string COMPILE_CODE = "COMPILE_CODE";
        private const string END_COMPILE_CODE = "END_COMPILE_CODE";
        private const string DONE_COMPILE_CODE = "DONE_COMPILE_CODE";
        private const string RUN_CODE = "RUN_CODE";
        private const string END_RUN_CODE = "END_RUN_CODE";
        private const string UPDATE_PROXY_VALUE = "UPDATE_PROXY_VALUE:";
        private const string CALL_METHOD = "CALL_METHOD:";

        private const int STATE_WAITING = 0;
        private const int STATE_COMPILING = 1;
        private const int STATE_RUNNING = 2;

        Process pythonProcess;
        internal object proxy;
        public event Action<string> OnStdOut;
        public event Action<string> OnStdErr;
        public event Action OnDoneCompiling;
        public event Action OnStateWaiting;
        public event Action OnDoneRunning;
        public event Action OnCompileError;
        public event Action OnRunError;
        public event Action<string> OnBadValue;
        /// <summary>
        /// args are value name and the proxy object
        /// </summary>
        public event Action<string, object> OnValueUpdatedOnProxy;
        public bool hasCompileError = false;
        public bool hasRunError = false;

        int _state = STATE_WAITING;
        int State
        {
            get { return _state; }
            set
            {
                _state = value;
                if (value == STATE_WAITING)
                    OnStateWaiting?.Invoke();
            }
        }

        public PythonRunnerProcess()
        {
            InitProcess();
        }

        private void InitProcess()
        {
            Console.WriteLine(SCRIPT_PATH);
            pythonProcess = new Process();
            pythonProcess.Exited += PythonProcess_Exited;
            pythonProcess.ErrorDataReceived += PythonProcess_ErrorDataReceived;
            pythonProcess.OutputDataReceived += PythonProcess_OutputDataReceived;
            pythonProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = PYTHON_PATH,
                Arguments = SCRIPT_PATH,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            pythonProcess.Start();
            pythonProcess.BeginErrorReadLine();
            pythonProcess.BeginOutputReadLine();
        }

        private void PythonProcess_Exited(object sender, EventArgs e)
        {
            pythonProcess.Close();
            pythonProcess.Dispose();
            State = STATE_WAITING;
            InitProcess();
        }

        private void PythonProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            string data = e.Data;
            if (e.Data.Contains("STDERR:"))
            {
                data = e.Data.Split(new char[] { ':' }, 2)[1];
            }
            Console.WriteLine("stderr[" + State + "]:" + e.Data);

            switch (State)
            {
                case STATE_RUNNING:
                    {
                        OnStdErr?.Invoke(data);
                        hasRunError = true;
                    }
                    break;
                case STATE_COMPILING:
                    {
                        OnStdErr?.Invoke(data);
                        hasCompileError = true;
                    }
                    break;
            }
        }

        private void PythonProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            if (e.Data.Contains("STDERR:"))
            {
                PythonProcess_ErrorDataReceived(sender, e);
                return;
            }
            Console.WriteLine("stdout[" + State + "]:" + e.Data);

            switch (State)
            {
                case STATE_RUNNING:
                    {

                        if (e.Data.Equals(END_RUN_CODE))
                        {
                            State = STATE_WAITING;
                            if (hasRunError)
                                OnRunError?.Invoke();
                        }
                        else if (e.Data.Contains(UPDATE_PROXY_VALUE))
                        {
                            string[] split = e.Data.Split(new char[] { ':' }, 3);
                            string name = split[1];
                            string value = split[2];
                            UpdateProxyProperty(name, value);
                        }
                        else if (e.Data.Contains(CALL_METHOD))
                        {
                            string[] split = e.Data.Split(new char[] { ':' }, 3);
                            string methodName = split[1];
                            JArray decoded = (JArray)JsonConvert.DeserializeObject(split[2]);
                            object ans = CallProxyMethod(methodName, decoded);
                            string jsonAns = "";
                            if(ans is Exception)
                            {
                                jsonAns = JsonConvert.SerializeObject(new Hashtable() { { "exception", ((Exception)ans).Message } });
                            }
                            else
                            {
                                jsonAns = JsonConvert.SerializeObject(new Hashtable() { { "answer", ans } });
                            }
                            pythonProcess.StandardInput.WriteLine(jsonAns);
                        }
                        else
                        {
                            OnStdOut?.Invoke(e.Data);
                            OnDoneRunning?.Invoke();
                        }
                    }
                    break;
                case STATE_COMPILING:
                    {
                        if (e.Data.Equals(DONE_COMPILE_CODE))
                        {
                            State = STATE_WAITING;
                            OnDoneCompiling?.Invoke();
                            if(hasCompileError)
                                OnCompileError?.Invoke();
                        }
                    }
                    break;
            }
        }

        private object CallProxyMethod(string methodName, JArray decoded)
        {
            Type proxyType = proxy.GetType();
            MethodInfo method = proxyType.GetMethod(methodName);
            ParameterInfo[] parameters = method.GetParameters();
            List<object> args = new List<object>();
            for(int i = 0; i < decoded.Count; i++)
            {
                try
                {
                    args.Add(Convert.ChangeType(decoded[i], parameters[i].ParameterType));
                }
                catch (Exception e)
                {
                    string message = e.Message + "\nhappened at parameter " + i + " of " + methodName;
                    return new Exception(message);
                }
            }
            object ans = method.Invoke(proxy, args.ToArray());
            return ans;
        }

        /// <summary>
        /// if there is an error in this function, you didn't setup the VisibleInPython attribute with a backing field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void UpdateProxyProperty(string name, string value)
        {
            Type proxyType = proxy.GetType();
            PropertyInfo prop = proxyType.GetProperty(name);
            VisibleInPythonAttribute attr = (VisibleInPythonAttribute)prop.GetCustomAttribute(typeof(VisibleInPythonAttribute));
            FieldInfo field = proxyType.GetField(attr.BackingField, BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                object castedValue = Convert.ChangeType(value, field.FieldType);
                field.SetValue(proxy, castedValue);
                OnValueUpdatedOnProxy?.Invoke(name, proxy);
            }
            catch (Exception)
            {
                string err = "Python provided a bad value (" + value + ") for property " + name;
                OnStdErr?.Invoke(err);
                OnBadValue?.Invoke(err);
            }
        }

        public void UpdatePythonProperty(string name, object value)
        {
            pythonProcess.StandardInput.WriteLine(UPDATE_PROXY_VALUE + name + ":" + value);
        }

        //TODO enlever l'histoire de key ca sert plus a rien 
        public bool CompileCode(string json)
        {
            if (State != STATE_WAITING)
                return false;
            hasCompileError = false;
            hasRunError = false;

            State = STATE_COMPILING;
            pythonProcess.StandardInput.WriteLine(COMPILE_CODE);
            using (StringReader reader = new StringReader(json))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    pythonProcess.StandardInput.WriteLine(line);
                }
            }
            pythonProcess.StandardInput.WriteLine(END_COMPILE_CODE);
            return true;
        }

        public bool RunCode()
        {
            if (State != STATE_WAITING)
                return false;
            if (hasCompileError)
                throw new Exception("tried to run code with a compile error");

            hasRunError = false;
            State = STATE_RUNNING;
            pythonProcess.StandardInput.WriteLine(RUN_CODE);
            return true;
        }

        public void Stop()
        {
            //this might not always stop the process (if it's reading compile code for exemple)
            //so we close the process manually without waiting
            pythonProcess.StandardInput.WriteLine("STOP");
            pythonProcess.Close();
            pythonProcess.Dispose();
            pythonProcess = null;
        }
    }
}
