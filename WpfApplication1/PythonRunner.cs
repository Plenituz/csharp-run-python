using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PythonRunnerNameSpace
{
    public class PythonRunner
    {
        private const string PYTHON_PATH = @"C:\Users\Plenituz\AppData\Local\Programs\Python\Python36-32\python.exe";
        private const string SCRIPT_PATH = @"C:\Users\Plenituz\Desktop\tmp\test.py";
        private Process process;
        private string partialStdout = "";
        private string partialStderr = "";
        private string _stdout = "";
        private bool stopStdout = false;

        public string stdout {
            get { return _stdout; }
        }
        /// <summary>
        /// if this is false the values extracted will all be string, but you might earn some ms
        /// </summary>
        public bool convertValues = true;
        /// <summary>
        /// called when the runner finished running
        /// </summary>
        public Action<PythonRunner, PythonRunResult> onTaskEnd;
        /// <summary>
        /// called every time the python code prints something (hopefully)
        /// </summary>
        public Action<PythonRunner> onNewstdout;
        

        /// <summary>
        /// run the python code in a task
        /// </summary>
        private void RunTask()
        {
            Task.Run(() =>
            {
                PythonRunResult result = RunProcess();
                CleanStdout();
                onTaskEnd?.Invoke(this, result);
            });
        }

        /// <summary>
        /// this function is used to remove the output created
        /// by the code we added to the python script ourself
        /// </summary>
        private void CleanStdout()
        {
            int index = stdout.IndexOf("ENDUSEROUTPUT");
            if(index != -1)
                _stdout = stdout.Substring(0, index);
        }

        /// <summary>
        /// Do the actual running of the python process
        /// This should be called in a Task
        /// </summary>
        /// <returns>A PythonRunResult object containing the result of the process</returns>
        private PythonRunResult RunProcess()
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
            return InterpretResult(partialStdout, partialStderr);
        }

        /// <summary>
        /// This gets called everytime the process sends data to stdout
        /// Python doesn't seem to work like that but other commands do
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private void StdoutDataReceived(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                partialStdout += outLine.Data + "\n";
                _stdout += outLine.Data + "\n";
                if (outLine.Data.Contains("ENDUSEROUTPUT"))
                    stopStdout = true;
                if(!stopStdout)
                    onNewstdout?.Invoke(this);
            }
        }

        /// <summary>
        /// This gets called everytime the process sends data to stderr
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private void StderrDataReceived(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //MessageBox.Show("received err " + outLine.Data);
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                partialStderr += outLine.Data + "\n";
                _stdout += outLine.Data + "\n";
                if (!stopStdout)
                    onNewstdout?.Invoke(this);
            }
        }

        /// <summary>
        /// Run the python code and extract values the code will produce
        /// The user just have to declare the values for you to get them back
        /// if he doesn't declare one of the values you will get it back as "None"
        /// </summary>
        /// <param name="pythonCode">The code to run in python</param>
        /// <param name="valueNames">The name of the variables to extract</param>
        /// <param name="onTaskEnd">an Action called when the python code is done running</param>
        public void RunAndGetValues(string pythonCode, string[] valueNames, Action<PythonRunner, PythonRunResult> onTaskEnd)
        {
            /*
             * The way this works is as follow : 
             * We create a file containing the python code provided bu add a little something at the end.
             * Basically we add code to the python file to make sure the values asked are declared
             * and than print out the values from the python script. That way here we can get the value
             * by analysing the stdout. To make sure we get the right values we first print "ENDUSEROUTPUT" 
             * and everything after that is sure to be our custom code talking.
             * That means the user can print "ENDUSEROUTPUT" (which would be weird honestly) otherwise you
             * will receive a PythonRunResult with an error string saying "you can't print "ENDUSEROUTOUT"
             * 
             * We do that to not have to use the technique IronPython uses
             * 
             * Using process.BeginOutputReadLine(); we sould be able to get the stdout as it is printed by
             * the user but for me it doesn't appear to be working, the stdout only get received at the end
             * of the python process. But if on your machine (or the user's machine) it works then you can 
             * subcribe to the "onNewStdout" delegate wich will give you the stdout as it comes in
             * 
             * By default at the end the runner will try to convert the values received from string to
             * int of float, you can disable that by setting convertValues to false
             */
            this.onTaskEnd = onTaskEnd;
            stopStdout = false;

            //add the custom code to the python file
            StringBuilder prevPart = new StringBuilder();
            StringBuilder strPart = new StringBuilder("\"");
            StringBuilder varPart = new StringBuilder("(");
            for(int i = 0; i < valueNames.Length; i++)
            {
                prevPart.Append("\ntry:\n\t" + valueNames[i] + "\nexcept NameError:\n\t" + valueNames[i] + "=None");
                strPart.Append("%s=%s" + (i != valueNames.Length-1 ? "|" : ""));
                varPart.Append("\"" + valueNames[i] + "\", " + valueNames[i] + 
                    (i != valueNames.Length-1 ? "," : ""));
            }
            strPart.Append("\"");
            varPart.Append(")");
            string varStr = strPart.ToString() + " % " + varPart.ToString();

            pythonCode +=
@"
print('ENDUSEROUTPUT')
$
print($$$)".Replace("$$$", varStr).Replace("$", prevPart.ToString());
            File.WriteAllText(SCRIPT_PATH, pythonCode);

            //prepare the process
            process = new Process();
            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = PYTHON_PATH;
            process.StartInfo.Arguments = SCRIPT_PATH;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += new DataReceivedEventHandler(StdoutDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(StderrDataReceived);
            //run the python code in another thread
            RunTask();
        }

        /// <summary>
        /// convert the string received from python to a PythonRunResult
        /// </summary>
        /// <param name="result">output of stdout</param>
        /// <param name="error">output of stderr</param>
        /// <returns></returns>
        private PythonRunResult InterpretResult(string result, string error)
        {
            //first check if there is and error if so stop
            if (!string.IsNullOrWhiteSpace(error))
            {
                //return run result avec le message d'erreur envoyé par python
                PythonRunResult pythonRunResult = new PythonRunResult()
                {
                    hasError = true,
                    errorString = error
                };
                return pythonRunResult;
            }

            //split where the python printed "ENDUSEROUTPUT", this should only have been printed once
            //therefore the string[] should only have 2 elements in it
            string[] resultArr = result.Split(new string[] { "ENDUSEROUTPUT" }, StringSplitOptions.None); 
            //if it doesn't have only 2 elements that means the user printed "ENDUSEROUTPUT" himself (weirdo)           
            if (resultArr.Length != 2)
            {
                //return run result with custom error message 
                PythonRunResult pythonRunResult = new PythonRunResult()
                {
                    hasError = true,
                    errorString = "You can't print \"ENDUSEROUTPUT\""
                };
                return pythonRunResult;
            }
            else
            {
                //return run result without and error and a hashtable filled with the extracted variables
                Hashtable table = new Hashtable();
                //the output will look something like this : "outVal1=5|outVal2=value"
                //each var is separated by a |
                string[] vars = resultArr[1].Split('|');
                for(int i = 0; i < vars.Length; i++)
                { 
                    string[] thisVar = vars[i].Split('=');
                    //thisVar[0] = varName
                    //thisVar[1] = varValue
                    table.Add(thisVar[0], TryConvert(thisVar[1]));
                }
                PythonRunResult pythonRunResult = new PythonRunResult()
                {
                    hasError = false,
                    returnedValues = table
                };
                return pythonRunResult;
            }
        }

        private object TryConvert(string str)
        {
            //only convert if necessary
            if (!convertValues)
                return str;
            int outInt;
            bool succeded = int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out outInt);
            if (succeded)
                return outInt;
            float outFloat;
            succeded = float.TryParse(str, NumberStyles.Float|NumberStyles.AllowThousands, 
                CultureInfo.InvariantCulture, out outFloat);
            if (succeded)
                return outFloat;
            return str;
        }
    }
}
