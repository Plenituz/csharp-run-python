
namespace PythonRunning
{
    public class PythonMethod
    {
        public bool hasReturn = false;
        public string name;
        public string[] args;

        public PythonMethod(bool hasReturn, string name, string[] args)
        {
            this.hasReturn = hasReturn;
            this.name = name;
            this.args = args;
        }
    }
}
