using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace DontSleep
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);


            using (new Mutex(true, GetGuid(), out bool createNew))
            {
                if (createNew)
                {
                    TrayApp trayApp = new TrayApp();
                    trayApp.Run();
                }
            }
        }
        static string GetGuid()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Attribute attribute = Attribute.GetCustomAttribute(assembly, typeof(GuidAttribute));
            string guid = ((GuidAttribute)attribute).Value;
            return guid;
        }
    }
}
