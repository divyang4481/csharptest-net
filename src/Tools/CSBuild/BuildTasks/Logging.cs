using System;
using System.Collections.Generic;
using System.Text;
using CSharpTest.Net.CSBuild.Build;
using Microsoft.Build.Framework;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
    class ConsoleOutput : BuildTask
    {
        readonly LoggerVerbosity Level;
        public ConsoleOutput(LoggerVerbosity level) { this.Level = level; }

        protected override int Run(BuildEngine engine)
        {
            engine.SetConsoleLevel(Level);
            return 0;
        }
    }
    [Serializable]
    class LogFileOutput : BuildTask
    {
        readonly string AbsolutePath;
        readonly LoggerVerbosity Level;
        public LogFileOutput(string path, LoggerVerbosity level) { this.AbsolutePath = path; this.Level = level; }

        protected override int Run(BuildEngine engine)
        {
            engine.SetTextLogFile(Environment.CurrentDirectory, AbsolutePath, Level);
            return 0;
        }
    }
    [Serializable]
    class XmlFileOutput : BuildTask
    {
        readonly string AbsolutePath;
        readonly LoggerVerbosity Level;
        public XmlFileOutput(string path, LoggerVerbosity level) { this.AbsolutePath = path; this.Level = level; }

        protected override int Run(BuildEngine engine)
        {
            engine.SetXmlLogFile(Environment.CurrentDirectory, AbsolutePath, Level);
            return 0;
        }
    }
}
