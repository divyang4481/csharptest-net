#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Reflection;
using Microsoft.Build.BuildEngine;
using CSharpTest.Net.Utils;
using System.IO;

namespace CSharpTest.Net.CSBuild.Build
{
	[System.Diagnostics.DebuggerDisplay("{Assembly}")]
	class ReferenceInfo
	{
        private readonly Project _project;
        private readonly BuildItem _item;
        private ReferenceType _refType = ReferenceType.Undefined;
        private AssemblyName _assembly = null;

		public ReferenceInfo(Project project, BuildItem item)
		{
            _project = project;
            _item = item;
            _refType = (ReferenceType)Enum.Parse(typeof(ReferenceType), item.Name);

			if (_refType == ReferenceType.ProjectReference)
            {
				_assembly = new AssemblyName();
            }
            else if (RefType == ReferenceType.Reference)
            {
				string name = _item.Include;
				if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					name = name.Substring(0, name.Length - 4);
				_assembly = new AssemblyName(name);
            }
            else
                throw new ApplicationException("Unkown reference type " + item.Name);
		}

		internal BuildItem BuildItem { get { return _item; } }
		public ReferenceType RefType { get { return _refType; } }

		public void MakeReference(AssemblyName asmName, string fullPath)
		{
			_refType = ReferenceType.Reference;
			_item.Name = _refType.ToString();
			this.Assembly = asmName;
			this.HintPath = fullPath;
			_item.RemoveMetadata("Name");
			_item.RemoveMetadata("Project");
			_item.RemoveMetadata("Package");
		}

		public AssemblyName Assembly
		{
			get { return _assembly; }
			set
			{
				ProcessorArchitecture oldPA = _assembly.ProcessorArchitecture;
				System.Globalization.CultureInfo oldCulture = _assembly.CultureInfo;
				if (_refType != ReferenceType.Reference) throw new ApplicationException("Unable to change assembly on a project reference.");
				_assembly = (AssemblyName)value.Clone();

				if (_assembly.CultureInfo == null)
					_assembly.CultureInfo = oldCulture;
				if (_assembly.ProcessorArchitecture == ProcessorArchitecture.None)
					_assembly.ProcessorArchitecture = oldPA;
				
				_item.Include = value.ToString();
			}
		}

        public string ProjectFile
        {
            get { return RefType == ReferenceType.ProjectReference ? ProjectPathToFullPath(_item.Include) : null; }
            set { if(RefType == ReferenceType.ProjectReference) _item.Include = FullPathToProjectPath(value); }
        }
        public string Condition
        {
            get { return _item.Condition; }
            set { _item.Condition = value; }
        }
        public string HintPath
        {
            get 
			{
                string fullPath = ProjectPathToFullPath(_item.GetMetadata("HintPath"));
                //Doesn't build with strict-references...
                //if (!String.IsNullOrEmpty(ExecutableExtension))
                //    fullPath = Path.ChangeExtension(fullPath, ExecutableExtension);
				return fullPath;
			}
            set 
			{
				ExecutableExtension = null;
				if (value == null) _item.RemoveMetadata("HintPath"); 
				else
				{
					string filepath = FullPathToProjectPath(value);
                    //Doesn't build with strict-references...
                    //string ext = Path.GetExtension(filepath);
                    //if (!StringComparer.OrdinalIgnoreCase.Equals(".dll", ext))
                    //{
                    //    ExecutableExtension = ext;
                    //    filepath = Path.ChangeExtension(filepath, ".dll");
                    //}
					_item.SetMetadata("HintPath", filepath);
				}
			}
        }
		public string ExecutableExtension
        {
            get { return _item.GetMetadata("ExecutableExtension"); }
            set { if (value != null) _item.SetMetadata("ExecutableExtension", value); else _item.RemoveMetadata("ExecutableExtension"); }
        }
        public Guid? ProjectGuid
        {
            get
            {
                if (String.IsNullOrEmpty(_item.GetMetadata("Project")))
                    return null;
                else
                    return new Guid(_item.GetMetadata("Project"));
            }
            set { if (value != null) _item.SetMetadata("Project", value.ToString()); else _item.RemoveMetadata("Project"); }
        }
        public FrameworkVersions? RequiresVersion
        {
            get 
            {
                if (String.IsNullOrEmpty(_item.GetMetadata("RequiredTargetFramework")))
                    return null;
                else 
                    return (FrameworkVersions)Enum.Parse(typeof(FrameworkVersions), "v" + _item.GetMetadata("RequiredTargetFramework").Replace(".", "")); 
            }
            set { if (value != null) _item.SetMetadata("RequiredTargetFramework", value.ToString().TrimStart('v').Insert(1, ".")); else _item.RemoveMetadata("RequiredTargetFramework"); }
        }
        public bool CopyLocal
        {
            get { return _item.GetMetadata("Private") == "True"; }
            set { _item.SetMetadata("Private", value.ToString()); }
        }
        public bool SpecificVersion
        {
			get
			{
				if (!_item.HasMetadata("SpecificVersion") && Assembly.Version != null)
					return true;
				return _item.GetMetadata("SpecificVersion") == "True";
			}
            set 
			{
				if (!value)
				{
					if(_assembly.Version != null)
						_assembly = new AssemblyName(Assembly.Name);
					if(_item.HasMetadata("SpecificVersion"))
						_item.SetMetadata("SpecificVersion", value.ToString());
				}
				else
					_item.SetMetadata("SpecificVersion", value.ToString()); 
			}
        }

		public string Details
		{
			get
			{
				StringWriter sw = new StringWriter();
				string format = "{0,15}: {1}";
				sw.WriteLine(format, "ReferenceType", this.RefType);
				sw.WriteLine(format, "AssemblyName", this.Assembly);
				if(this.ProjectFile != null)
					sw.WriteLine(format, "ProjectFile", this.ProjectFile);
				if (this.ProjectGuid != null)
					sw.WriteLine(format, "ProjectGuid", this.ProjectGuid);
				if(!String.IsNullOrEmpty(Condition))
					sw.WriteLine(format, "Condition", this.Condition);
				if (!String.IsNullOrEmpty(HintPath))
					sw.WriteLine(format, "HintPath", this.HintPath);
				if (RequiresVersion != null)
					sw.WriteLine(format, "RequiresVersion", this.RequiresVersion);
				if (_item.HasMetadata("Private"))
					sw.WriteLine(format, "CopyLocal", this.CopyLocal);
				if (_item.HasMetadata("SpecificVersion"))
					sw.WriteLine(format, "SpecificVersion", this.SpecificVersion);
				return sw.ToString();
			}
		}

        private string FullPathToProjectPath(string fullPath)
        {
            if (String.IsNullOrEmpty(fullPath)) return null;
            return FileUtils.MakeRelativePath(_project.FullFileName, fullPath);
        }

        private string ProjectPathToFullPath(string relativePath)
        {
            if (String.IsNullOrEmpty(relativePath)) return null;
            if (!Path.IsPathRooted(relativePath))
                relativePath = Path.Combine(Path.GetDirectoryName(_project.FullFileName), relativePath);

            relativePath = Path.GetFullPath(relativePath);
            return relativePath;
        }
	}
}
