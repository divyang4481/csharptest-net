#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;

/// <summary>
/// This class merges the build output from MS-Build into our own logs.
/// </summary>
[System.Diagnostics.DebuggerNonUserCode]
class MSBuildLog
{
    public MSBuildLog(Engine engine)
    {
        ConsoleLogger trace = new Microsoft.Build.BuildEngine.ConsoleLogger(
                LoggerVerbosity.Normal, ConsoleWrite, ColorSetter, ColorResetter
            );
        trace.SkipProjectStartedText = false;
        trace.ShowSummary = false;
        engine.RegisterLogger(trace);
    }

    private void ConsoleWrite(string text)
    {
        if (!String.IsNullOrEmpty(text))
        {
            text = text.Trim();
            if (text.Length > 0)
                Log.Verbose(text.Trim());
        }
    }
    private void ColorSetter(ConsoleColor c) { }
    private void ColorResetter() { }
}
