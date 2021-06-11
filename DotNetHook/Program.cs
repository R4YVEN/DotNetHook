using System;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNetHook.Extensions;
using DotNetHook.Hooks;
using DotNetHook.Models;

namespace DotNetHook
{
    internal class Program
    {
        #region Private Methods

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MessageBoxA(IntPtr hwnd, String text, String title, uint type);

        private static ManagedHook _managedHook;
        private static NativeHook _nativeHook;

        private static void Main(string[] args)
        {
            // Managed hook example.
            MethodBase writeLineMethod = "System.Console".GetMethod("WriteLine", new[] { typeof(string) }, BindingFlags.Static | BindingFlags.Public);
            MethodBase replacementMethod = "DotNetHook.Program".GetMethod("WriteLineReplacement", BindingFlags.Static | BindingFlags.NonPublic);
            _managedHook = new ManagedHook(writeLineMethod, replacementMethod);

            Console.WriteLine("Before Hook");

            // Apply the hook.
            _managedHook.Apply();

            //The Hook will also apply to other assemblies loaded while in runtime.
            AppDomain domain = AppDomain.CreateDomain("AnalyseMe");                     //Create a new AppDomain and load an external Assembly into it.
            Assembly assembly = domain.Load(File.ReadAllBytes("HookMe.exe"));           
            assembly.EntryPoint.Invoke(null, new object[] { new string[] { } });        //Invoke the EntryPoint

            // Remove the hook, alternatively you can use a "using" statement to dispose of the hook.
            _managedHook.Remove();

            Console.WriteLine("After Hook");

            Console.ReadLine();
        }

        private static void WriteLineReplacement(string str)
        {
            // We cannot call the original "Console.WriteLine" using traditional methods.
            // The below code will cause a stack overflow exception.
            // Console.WriteLine($"Hooked: {str}");

            // We must instead use the ManagedHook "Call" method.
            _managedHook.Call<object>(null, $"Hooked: {str}");
        }

        // Padding differs from release and debug mode sometimes. This below example is for x86.
        private delegate int MessageBoxDelegate(IntPtr hwnd, String text, String title, uint type);
        private static int MessageBoxReplacement(int pad1, int pad2, uint type, IntPtr titlePtr, IntPtr bodyPtr, IntPtr hwnd)
        {
            // This will not work in x64 config mode.

            string title = titlePtr.ReadASCIINullTerminatedString();
            string text = bodyPtr.ReadASCIINullTerminatedString();

            return (int)_nativeHook.Call<MessageBoxDelegate, object>(hwnd, "Hooked (original: " + text + ")", title, type); ;
        }
        #endregion
    }
}
