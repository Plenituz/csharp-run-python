A hacky but light-weight way of running python code from c#.
This uses Process and some ninja action to extract values from the python code.

A full example is provided in MainWindow.xaml.cs (using wpf). The way this works is explained in PythonRunner.RunAndGetValues(..)


Usage : 
even thought PythonRunner is not thread blocking, it is not meant to be used to launch several python process at the same time.
If you want to run several python scripts at once you have to create several PythonRunner objects.
That being said you can reuse a PythonRunner once it's done to run a completely different script with different extracted values.

NOTE:
This is just a fun hack you should probably use IronPython if you want to actually run python from C#

NOTE2:
I added a new way of running python from c# in the "PythonRunning/Faster" directory, it's not fully tested/finished yet. You can find the older version in "PythonRunning/SlowButEasy" directory. Don't let the names fool you, they are both very slow methods.
