using System;
using System.Windows;
using System.Collections.Generic;
using PythonRunning;

namespace PythonRunnerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PythonRunner2 runner;
        PythonProxyClass proxy = new PythonProxyClass();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            runner = new PythonRunner2(proxy);
            runner.OnStdOutUpdate += OnStdUpdate;
            runner.OnStdErrUpdate += OnStdUpdate;
            runner.OnDoneCompiling += Runner_OnDoneCompiling;
            runner.OnDoneRunning += Runner_OnDoneRunning;
            runner.OnValueUpdatedOnProxy += (name, prx) =>
            {
                Console.WriteLine(proxy.StringProperty);
            };            
        }

        private void Runner_OnDoneRunning()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                StdoutDisp.Text = runner.OverallOut;
            }));
        }

        private void Runner_OnDoneCompiling()
        {
            proxy.IntProperty = 60;
            proxy.FloatProperty = 50.34f;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                StdoutDisp.Text = runner.OverallOut;
            }));
        }

        /// <summary>
        /// this is not called in the main thread
        /// </summary>
        /// <param name="stdouts"></param>
        private void OnStdUpdate(string stdouts)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.OverallOut; }));
        }

        private void CompileBut_Click(object sender, RoutedEventArgs e)
        {
            runner.Compile(PYText.Text);
        }

        private void RunBut_Click(object sender, RoutedEventArgs e)
        {
            runner.Run();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //comment/uncomment the next line to switch between async/thread blocking
            /*
            //Async example
            //by default the process will run in a background task
            runner.RunAndGetValues(userScript,
                (PythonRunner mRunner, PythonRunResult pythonResult) =>
                {
                    //  this will happen on the end of the process on the process's thread
                    DisplayResult(mRunner, pythonResult);
                });
            StdoutDisp.Text = "Python is running...";
            
            
            //thread blocking example
            runner.runInBackground = false;
            //this next method is now thread blocking
            runner.RunAndGetValues(userScript,
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
                foreach (KeyValuePair<string, object> de in result.returnedValues)
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
                foreach (KeyValuePair<string, object> de in result.returnedValues)
                {
                    tableStr += de.Key + "=" + de.Value + "(" + de.Value.GetType() + ")\n";
                }
                Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.stdout + "\n" + tableStr; }));
            }
        }

        private void KillProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            //runner?.KillProcess();
        }
    }
}
