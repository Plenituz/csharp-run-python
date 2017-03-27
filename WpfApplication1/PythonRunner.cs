
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PythonRunnerNameSpace
{
    public class PythonRunner
    {
        private const string PYTHON_PATH = @"C:\Users\Plenituz\AppData\Local\Programs\Python\Python36-32\python.exe";
        private const string SCRIPT_PATH = @"C:\Users\Plenituz\Desktop\tmp\test.py";
        private Process process;
        private string partialStdout = "";
        private string partialStderr = "";

        public string stdout = "";
        public Action<PythonRunResult> onTaskEnd;
        public Action<PythonRunner> onNewstdout;
        

        private void RunTask()
        {
            Task.Run(() =>
            {
                PythonRunResult result = RunProcess();
                onTaskEnd?.Invoke(result);
            });
        }

        private PythonRunResult RunProcess()
        {
            process.Exited += new EventHandler((object sender, EventArgs args) =>
            {
                
                MessageBox.Show("nd");
            });
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
            return InterpretResult(partialStdout, partialStderr);
        }

        private void StdoutDataReceived(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //MessageBox.Show("received std " + outLine.Data);
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                partialStdout += outLine.Data + "\n";
                stdout += outLine.Data + "\n";
                onNewstdout?.Invoke(this);
            }
        }

        private void StderrDataReceived(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //MessageBox.Show("received err " + outLine.Data);
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                partialStderr += outLine.Data + "\n";
                stdout += outLine.Data + "\n";
                onNewstdout?.Invoke(this);
            }
        }

        public void RunAndGetValues(string pythonCode, string[] valueNames, Action<PythonRunResult> onTaskEnd)
        {
            this.onTaskEnd = onTaskEnd;
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
            RunTask();
        }

        private PythonRunResult InterpretResult(string result, string error)
        {
            string[] resultArr = result.Split(new string[] { "ENDUSEROUTPUT" }, StringSplitOptions.None);
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

            if (resultArr.Length != 2)
            {
                //return run result avec le message d'erreur custom 
                PythonRunResult pythonRunResult = new PythonRunResult()
                {
                    hasError = true,
                    errorString = "You can't print \"ENDUSEROUTPUT\""
                };
                return pythonRunResult;
            }
            else
            {
                //return runresult sans erreur et a hashmap remplie
                Hashtable table = new Hashtable();
                string[] vars = resultArr[1].Split('|');
                for(int i = 0; i < vars.Length; i++)
                {
                    string[] thisVar = vars[i].Split('=');
                    //thisVar[0] = varName
                    //thisVar[1] = varValue
                    table.Add(thisVar[0], thisVar[1]);
                }
                PythonRunResult pythonRunResult = new PythonRunResult()
                {
                    hasError = false,
                    returnedValues = table
                };
                return pythonRunResult;
            }
        }
    }
}
