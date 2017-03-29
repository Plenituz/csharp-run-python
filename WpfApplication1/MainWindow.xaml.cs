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
            runner.RunAndGetValues(userScript, new string[] { "outVal", "outPos", "oo" },
                (PythonRunner mRunner, PythonRunResult pythonResult) =>
                {
                    DisplayResult(mRunner, pythonResult);
                });
            StdoutDisp.Text = "Python is running...";
        }

        void UpdateStdout(PythonRunner runner)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.stdout; }));
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
