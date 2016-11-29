//-----------------------------------------------------------------------
// <copyright file="ConsoleLogger.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Outputs messages to Console and log file.
    /// </summary>
    public static class ConsoleLogger
    {
        /// <summary>
        /// The log file
        /// </summary>
        private static string _logfile = "log.txt";

        /// <summary>
        /// The is initialized
        /// </summary>
        private static bool _isInitialized = false;

        /// <summary>
        /// The file locked
        /// </summary>
        private static ManualResetEvent _fileLocked = new ManualResetEvent(true);

        /// <summary>
        /// Initializes the logger.
        /// </summary>
        /// <param name="logfile">The log file.</param>
        public static void InitLogger(string logfile)
        {
            File.Delete(logfile);
            ConsoleLogger._logfile = logfile;
            _isInitialized = true;
        }

        /// <summary>
        /// Writes to log.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="toConsole">if set to <c>true</c> [to console].</param>
        public static void WriteToLog(string message, bool toConsole = false)
        {
            if (!_isInitialized)
            {
                Console.WriteLine(message);
                return;
            }

            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                _fileLocked.WaitOne();
                _fileLocked.Reset();
                ostrm = new FileStream(_logfile, FileMode.Append, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception)
            {
                ////Console.WriteLine("Cannot open " + _logfile + " for writing");
                ////Console.WriteLine(e.Message);
                return;
            }

            Console.SetOut(writer);
            Console.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "|" + message);
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
            if (toConsole)
            {
                Console.WriteLine(message);
            }
  
            _fileLocked.Set();
        }

        /// <summary>
        /// Writes the error to log.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="toConsole">if set to <c>true</c> [to console].</param>
        public static void WriteErrorToLog(string message, bool toConsole = false)
        {
            var stackTrace = new StackTrace(true);
            StackFrame[] f = stackTrace.GetFrames();

            WriteToLog("<STACKTRACE BEGIN>", toConsole);
            foreach (StackFrame r in f)
            { 
                string s = "[Filename: " + r.GetFileName() + " Method: " + r.GetMethod() + " Line: " + r.GetFileLineNumber() + " Column: " + r.GetFileColumnNumber() + "]";
                WriteToLog(s, toConsole);
            }

            WriteToLog("<STACKTRACE END> " + message, toConsole);
        }
    }
}
