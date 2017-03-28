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
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string userScript = PYText.Text;
            PythonRunner runner = new PythonRunner();
           // runner.onNewstdout = UpdateStdout;
            runner.RunAndGetValues(userScript, new string[] { "outVal", "outPos", "oo" },
                (PythonRunner mRunner, PythonRunResult pythonResult) =>
                {
                    DisplayResult(mRunner, pythonResult);
                });
            
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
    }
}
