using System;
using System.Collections;
using System.Windows;
using PythonRunnerNameSpace;

namespace PythonRunnerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PythonRunner runner;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string userScript = PYText.Text;
            runner = new PythonRunner();
            // runner.onNewstdout = UpdateStdout;
           
            //comment/uncomment the next line to switch between async/thread blocking
            //*/
            //Async example
            //by default the process will run in a background task
            runner.RunAndGetValues(userScript, new string[] { "outVal", "outPos", "oo" },
                (PythonRunner mRunner, PythonRunResult pythonResult) =>
                {
                  //  this will happen on the end of the process on the process's thread
                    DisplayResult(mRunner, pythonResult);
                });
            StdoutDisp.Text = "Python is running...";
            
            /*/
            //thread blocking example
            runner.runInBackground = false;
            //this next method is now thread blocking
            runner.RunAndGetValues(userScript, new string[] { "outVal", "outPos", "oo" },
                (PythonRunner mRunner, PythonRunResult pythonResult) =>
                {
                    //this will happen at the end of the run but on the main thread this time
                    DisplayResultSameThread(mRunner, pythonResult);
                });
            */
        }

        void UpdateStdout(PythonRunner runner)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.stdout; }));
        }

        void DisplayResultSameThread(PythonRunner runner, PythonRunResult result)
        {
            if (result.hasError)
            {
                StdoutDisp.Text = "\n" + result.errorString;
            }
            else
            {
                string tableStr = "";
                foreach (DictionaryEntry de in result.returnedValues)
                {
                    tableStr += de.Key + "=" + de.Value + "(" + de.Value.GetType() + ")\n";
                }
                StdoutDisp.Text = runner.stdout + "\n" + tableStr;
            }
        }

        void DisplayResult(PythonRunner runner, PythonRunResult result)
        {
            if (result.hasError)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = "\n" + result.errorString; }));
            }
            else
            {
                string tableStr = "";
                foreach (DictionaryEntry de in result.returnedValues)
                {
                    tableStr += de.Key + "=" + de.Value + "(" + de.Value.GetType() + ")\n";
                }
                Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.stdout + "\n" + tableStr; }));
            }
        }

        private void KillProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            runner?.KillProcess();
        }
    }
}
