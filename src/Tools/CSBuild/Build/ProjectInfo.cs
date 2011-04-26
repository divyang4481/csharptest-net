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
using System.IO;

namespace CSharpTest.Net.CSBuild.Build
{
	[System.Diagnostics.DebuggerDisplay("{AssemblyName} = {ProjectFile}")]
	class ProjectInfo
    {
        readonly Project _project;
        readonly PropertyList _properties;
        readonly ReferenceList _references;
        readonly List<String> _dependencies;

        public ProjectInfo(Project project)
        {
            _project = project;
            _properties = new ProjectPropertyList(_project);
            _references = new ReferenceList(_project);
            _dependencies = new List<string>();
        }

        internal Project MsProject { get { return _project; } }
        public PropertyList Properties { get { return _properties; } }
        public ReferenceList References { get { return _references; } }

        public string ProjectFile { get { return Path.GetFullPath(_project.FullFileName); } }
        public Guid ProjectGuid { get { return new Guid(Properties[MSProp.ProjectGuid]); } }
        public string AssemblyName { get { return Properties[MSProp.AssemblyName].Trim(); } }
        public string ProjectDir { get { return Properties[MSProp.ProjectDir]; } }
        public ProjectType ProjectType { get { return (ProjectType)Enum.Parse(typeof(ProjectType), Properties[MSProp.OutputType]); } }
        public string[] DefaultTargets { get { return _project.DefaultTargets.Split(';'); } }

        public string TargetFullName
        {
            get
            {
                String outDir = Properties[MSProp.OutDir];
                String targetFileName = Properties[MSProp.TargetFileName];

                if (!Path.IsPathRooted(outDir))
                    outDir = Path.Combine(ProjectDir, outDir);

                targetFileName = Path.Combine(outDir, targetFileName);
                return Path.GetFullPath(targetFileName);
            }
        }

        public String[] Dependencies
        {
            get { return _dependencies.ToArray(); }
            set
            {
                _dependencies.Clear();
                if( value != null )
                    _dependencies.AddRange(value);
            }
        }
    }
}
