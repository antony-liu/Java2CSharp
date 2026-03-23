using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Java2CSharp
{
    public class ConsoleManager
    {

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        internal static extern bool AllocConsole();

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        internal static extern bool FreeConsole();

        /// <summary>
        /// 在程序启动时，执行一次即可
        /// </summary>
        public static void OpenConsole()
        {
            try
            {
                var consoleTitle = "App Runtime Log";
                AllocConsole();
                Console.BackgroundColor = ConsoleColor.Black;

                Console.CursorVisible = false;
                Console.Title = consoleTitle;
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// 该方法只在退出程序时，调用
        /// </summary>
        public static void CloseConsole()
        {
            FreeConsole();
        }

        public static void WriteLine(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void WriteLineError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(msg);
        }

        public static void WriteLineError(Exception ex)
        {
            WriteLineError(ex.Message);
        }

        public static void WriteLineInfo(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteLine(msg);
        }

    }
}
