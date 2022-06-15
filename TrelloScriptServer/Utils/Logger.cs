using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TrelloScriptServer
{
    class Logger
    {
        string logs = Directory.GetCurrentDirectory() + "/logs";
        int LogNum = 0;
        StreamWriter ActualLog;
        static bool PrintToCout = false;
        static Logger instance = null;

        private Logger()
        {
            if (!Directory.Exists(logs))
            {
                Directory.CreateDirectory(logs);
            }
            else
            {
                LogNum = Directory.GetFiles(logs).Length;
            }
            ActualLog = new StreamWriter(logs + "//" + "log" + LogNum + ".txt");
            ActualLog.WriteLine("Start of log number " + LogNum);
            ActualLog.Flush();
        }

        public static void WriteLine(string s) 
        {
            if(instance == null)
            {
                instance = new Logger();
            }
            if(PrintToCout)
            {
                Console.WriteLine(s);
            }
            instance.ActualLog.WriteLine(s);
            instance.ActualLog.Flush();
        }

        public static void setPrintToCout(bool value)
        {
            PrintToCout = value;
        }

        ~Logger()
        {
            ActualLog.Flush();
            ActualLog.Close();
        }
    }
}
