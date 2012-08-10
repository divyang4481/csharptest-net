#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
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
