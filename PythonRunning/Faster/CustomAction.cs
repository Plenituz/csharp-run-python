using System;

namespace PythonRunning
{
    public class CustomAction
    {
        public object args;
        public Action<object> action;

        public CustomAction(Action<object> action, object args)
        {
            this.args = args;
            this.action = action;
        }

        public void Invoke()
        {
            action?.Invoke(args);
        }
    }
}
