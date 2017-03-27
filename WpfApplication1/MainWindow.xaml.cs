using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            runner.onNewstdout = UpdateStdout;
            runner.RunAndGetValues(userScript, new string[] { "outVal", "outPos" },
                (PythonRunResult pythonResult) =>
                {
                    DisplayResult(pythonResult);
                });
        }

        void UpdateStdout(PythonRunner runner)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text = runner.stdout; }));
        }

        void DisplayResult(PythonRunResult result)
        {
            if (result.hasError)
            {
                //MessageBox.Show("error:\n" + result.errorString);
            }
            else
            {
                string tableStr = "";
                foreach (DictionaryEntry de in result.returnedValues)
                {
                    tableStr += de.Key + "=" + de.Value + "\n";
                }
                Application.Current.Dispatcher.Invoke(new Action(() => { StdoutDisp.Text += "\n" + tableStr; }));
            }
            //MessageBox.Show("ran in " + s.ElapsedMilliseconds + " ms");
        }
    }
}
