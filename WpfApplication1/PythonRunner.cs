using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PythonRunnerNameSpace
{
    public class PythonRunner
    {
        /// <summary>
        /// for now this is defined by hand, it will one day be defined by not hand (by foot may be ?)
        /// </summary>
        private const string PYTHON_PATH = @"C:\Users\Plenituz\AppData\Local\Programs\Python\Python36-32\python.exe";
        /// <summary>
        /// this too, it should propably be user defined
        /// </summary>
        private const string SCRIPT_PATH = @"C:\Users\Plenituz\Desktop\tmp\test.py";
        private Process process;
        /// <summary>
        /// store only the stdout given by the process.
        /// We keep them separate to more easily detect the errors and tracebacks sent by python
        /// </summary>
        private string partialStdout = "";
        /// <summary>
        /// store only the stderr given by the process
        /// We keep them separate to more easily detect the errors and tracebacks sent by python
        /// </summary>
        private string partialStderr = "";
        /// <summary>
        /// store stdout and stderr given by the process, this
        /// is what the user should see
        /// </summary>
        private string _stdout = "";
        /// <summary>
        /// this is true when we reach the end of the stdout from the actual python program
        /// the user inputed. after that it's out cutom that's going to output the
        /// extracted values. 
        /// So when this is true we stop calling "onNewstdout", but keep storing values in _stdout
        /// the value of _stdout gets cleaned of our custom output when the process ends
        /// </summary>
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
            Task t = Task.Run(() =>
            {
                PythonRunResult result = RunProcess();
                //remove the custom output allowing us to extract variables from python
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
            //the next 2 lines tell the process to send stdout and stderr to 
            //"StdoutDataReceived" and "StderrDataReceived" as it comes
            //as exaplained this doesn't work for me, might for you so I kept it like that
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            //we wait for the process, that's why you should call this in another thread
            process.WaitForExit();
            process.Close();
            process.Dispose();
            process = null;
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
                //update storage for stdout only (not stderr) and the common stdout the user sees
                partialStdout += outLine.Data + "\n";
                _stdout += outLine.Data + "\n";
                //if our end of script print shows up, don't update the user on the next infos 
                //so we can clean it before showing it
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

        public void KillProcess()
        {
            try
            {
                process?.Kill();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("process was invalid when trying to kill it (PythonRunner)");
                process.Dispose();
                process = null;
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
            /*
             * prevPArt contains code that makes sure the variable we extract are defined (in the python script)
             * strPart is the first part of our print statement that extracts our values
             * it looks something like that in the end : %s=%s|%s=%s|%s=%s
             * varPart is the other half of that statement 
             * it looks like this : "outVal1",outVal1,"outVal2",outVal2
             * in the end the print statement looks like 
             * print("%s=%s|%s=%s" % ("outVal1", outVal1, "outVal2", outVal2))
             */
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

            pythonCode += "\nprint('ENDUSEROUTPUT')\n$\nprint($$$)"
                    .Replace("$$$", varStr).Replace("$", prevPart.ToString());
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
                //return run result with the traceback python gave us
                return new PythonRunResult()
                {
                    hasError = true,
                    errorString = error
                };
            }
            //if the result (stdout) is empty then something wrong happened
            //same if the result doesn't contain what our custom code printed
            if (string.IsNullOrWhiteSpace(result) || !result.Contains("ENDUSEROUTPUT"))
            {
                //no stdout that means the process was killed before finishing
                //or if there is no "ENDUSEROUTPUT" that means out custom code didn't have time to run
                return new PythonRunResult()
                {
                    hasError = true,
                    errorString = "the python process was killed"
                };
            }
            //from there on we are sure the process finished sucessfully
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
                    errorString = "You can't print \"ENDUSEROUTPUT\" or an unexpected case happened"
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

        /// <summary>
        /// tries to convert the string into an int or a float
        /// </summary>
        /// <param name="str"></param>
        /// <returns>the string as an int or a float, if prossible, otherwise you get the string back</returns>
        private object TryConvert(string str)
        {
            //only convert if necessary
            if (!convertValues)
                return str;
            //try parse int and float if it doesn't work, return the string
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
