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
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.CSBuild.Build;
using CSharpTest.Net.Utils;
using System.IO;
using System.Reflection;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	[Serializable]
	class EnforceReferences : BuildTask
	{
		class ReferenceWorkItem { public string FullPath; public ReferenceFolder FoundIn; }
		readonly List<ReferenceFolder> _folders = new List<ReferenceFolder>();
		readonly Dictionary<String, String> _failedReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		readonly Dictionary<String, ReferenceWorkItem> _assemblyNameToFile = new Dictionary<string, ReferenceWorkItem>(StringComparer.OrdinalIgnoreCase);
	    private readonly FrameworkVersions _framework;
	    readonly IDictionary<String, String> _namedValues;
		
		readonly bool _strict, _noStdLib, _noprojectrefs;
		public EnforceReferences(FrameworkVersions framework, IDictionary<String, String> namedValues, bool strictReferences, bool noStdLib, bool noProjectReferences)
		{
		    _framework = framework;
		    _namedValues = namedValues;
			_strict = strictReferences;
			_noStdLib = noStdLib;
			_noprojectrefs = noProjectReferences;
		}
					
		public void Add(IEnumerable<ReferenceFolder> folders)
		{
			_folders.AddRange(folders);
		}

		private void SetOptions(ProjectInfo project)
		{
			if (_noStdLib)
			{
				project.Properties[MSProp.NoStdLib] = true.ToString();
			}
			if (_strict)
			{
				List<string> resolveType = new List<string>();
				resolveType.Add("{HintPathFromItem}");
				if (!_noprojectrefs)
					resolveType.Add("{CandidateAssemblyFiles}");
                if (_framework == FrameworkVersions.v40)
                    resolveType.Add("{TargetFrameworkDirectory}"); //TODO: unable to avoid this for System.Core?

				project.Properties[MSProp.AssemblySearchPaths] = String.Join(";", resolveType.ToArray());
				//C:\Windows\Microsoft.NET\Framework\v2.0.50727\Microsoft.Common.targets: line 441
				//{CandidateAssemblyFiles};$(ReferencePath);{HintPathFromItem};{TargetFrameworkDirectory};
				//{Registry:$(FrameworkRegistryBase),$(TargetFrameworkVersion),$(AssemblyFoldersSuffix)$(AssemblyFoldersExConditions)};
				//{AssemblyFolders};{GAC};{RawFileName};$(OutputPath)
				// 3.5 uses $(OutDir) rather than $(OutputPath)
			}
		}

		protected override int Run(BuildEngine engine)
		{
			int errors = 0;
			engine.ProjectPreBuild += new ProjectPreBuildEventHandler(ProjectPreBuild);
			foreach (ReferenceFolder fld in _folders)
				ResolveFiles(fld);

			ReferenceWorkItem refItem;
			ProjectInfo refProj;

			AssemblyName mscorlib = new AssemblyName("mscorlib");

			foreach (ProjectInfo project in engine.Projects)
			{
				SetOptions(project);
				bool msCoreFound = false;
				foreach (ReferenceInfo reference in project.References)
				{
					msCoreFound |= StringComparer.OrdinalIgnoreCase.Equals(mscorlib.Name, reference.Assembly.Name);
					refItem = new ReferenceWorkItem();

                    if (engine.Projects.TryGetProject(reference, out refProj))
                        refItem.FullPath = refProj.TargetFullName;
                    else if (_assemblyNameToFile.TryGetValue(reference.Assembly.Name, out refItem))
                    { }
                    else if (!_strict)
                    { continue; } //ignore unknown reference
                    else
                    {
                        if (++errors > 0 && !_failedReferences.ContainsKey(reference.Assembly.Name))
                        {
                            Log.Error("Unable to locate reference in project {1}\r\n{2}", reference.Assembly, project.ProjectFile, reference.Details);
                            _failedReferences[reference.Assembly.Name] = reference.Assembly.Name;
                        }
                        continue; //failed to locate
                    }
					if (_noprojectrefs && reference.RefType == ReferenceType.ProjectReference)
					{
						if (refProj == null)
						{
							Log.Error("Unable to locate project referenced by {1}\r\n{2}", reference.ProjectFile, project.ProjectFile, reference.Details);
							errors++;
						}
						reference.MakeReference(new AssemblyName(refProj.AssemblyName), refItem.FullPath);
					}
					else
					{
						if ((_strict || !String.IsNullOrEmpty(reference.HintPath)) &&
							!StringComparer.OrdinalIgnoreCase.Equals(reference.HintPath, refItem.FullPath))
							reference.HintPath = refItem.FullPath;
					}

					if (reference.SpecificVersion == false && reference.Assembly.Version != null)
						reference.Assembly = new AssemblyName(reference.Assembly.Name);
				}

				if (!msCoreFound && _noStdLib)
				{
					if (!_strict)
						project.References.Add(mscorlib);
					else if (_assemblyNameToFile.TryGetValue(mscorlib.Name, out refItem))
						project.References.Add(mscorlib, refItem.FullPath);
					else
					{
						Log.Info("Unable to locate {0} in references.\r\n" +
						"When enabling no-standard-references and strict-references, you must specify\r\n" +
						"a reference path that contains the {0}.dll version to use.", mscorlib);
						throw new ApplicationException(String.Format("Aborted due to missing {0} library.", mscorlib));
					}
				}
			}
			return errors;
		}

		void ProjectPreBuild(BuildEngine engine, ProjectPreBuildEventArgs args)
		{
			foreach (ProjectInfo project in engine.Projects)
			{
				foreach (ReferenceInfo reference in project.References)
				{
					if (reference.SpecificVersion)
					{
						string filepath = reference.HintPath;
						if (File.Exists(filepath))
						{
							AssemblyName asmName = AssemblyName.GetAssemblyName(filepath);
							if (!StringComparer.OrdinalIgnoreCase.Equals(reference.Assembly.ToString(), asmName.ToString()))
								reference.Assembly = asmName;
						}
					}
				}
			}
		}

		void ResolveFiles(ReferenceFolder folder)
		{
			FileList found = new FileList();
			found.FileFound += new EventHandler<FileList.FileFoundEventArgs>(FileFound);
			found.RecurseFolders = folder.Recursive;
            found.Add(folder.AbsolutePath(_namedValues));

			ReferenceWorkItem item;
			
			foreach (FileInfo file in found)
			{
				string filenameonly = Path.GetFileNameWithoutExtension(file.Name);
				if(!_assemblyNameToFile.TryGetValue(filenameonly, out item))
					_assemblyNameToFile.Add(filenameonly, item = new ReferenceWorkItem());
				
				item.FullPath = file.FullName;
				item.FoundIn = folder;
			}
		}

		void FileFound(object sender, FileList.FileFoundEventArgs e)
		{
			e.Ignore = false == (
				StringComparer.OrdinalIgnoreCase.Equals(e.File.Extension, ".dll") ||
				StringComparer.OrdinalIgnoreCase.Equals(e.File.Extension, ".exe"));
		}
	}
}
