#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;

namespace CSharpTest.Net.CSBuild.Implementation
{
	partial class ProjectInfo
	{
		List<ProjectRef> _psedoDepends = new List<ProjectRef>();
		List<ProjectRef> _refCache = null;
		Dictionary<AssemblyName, bool> _allowed = new Dictionary<AssemblyName, bool>();

		public ProjectRef[] GetReferences()
		{
			if (_refCache == null)
			{
				_refCache = new List<ProjectRef>();

				//First we enum project-to-project references, I hate these, but alot of ID10Ts still use them ;)
				foreach (BuildItem bi in _project.GetEvaluatedItemsByName("ProjectReference").ToArray())
					_refCache.Add(new ProjectRef(bi, GetProjectPath));

				//Now we enum all the real Reference items...
				foreach (BuildItem bi in _project.GetEvaluatedItemsByName("Reference").ToArray())
					_refCache.Add(new ProjectRef(bi, GetProjectPath));
			}

			//sync cache data with allowed references.
			bool allow;
			foreach (ProjectRef r in _refCache)
				if (_allowed.TryGetValue(r.Assembly, out allow))
					r.Resolved = allow;

			return _refCache.ToArray();
		}

		public void AddDependencies(ProjectInfo[] depends)
		{
			foreach (ProjectInfo proj in depends)
			{
				BuildItem bi = new BuildItem("Reference", proj.AssemblyName);
				bi.SetMetadata("SpecificVersion", false.ToString());
				bi.SetMetadata("HintPath", proj.AbsoluteOutputPath);
				_psedoDepends.Add(new ProjectRef(bi, GetProjectPath));
			}
		}

        public void ResolveReference(ProjectRef reference, string hintPath, bool copyLocal, bool specificVersion)
		{
			_allowed[reference.Assembly] = true;
            BuildItem bi = FindByReference(reference);
            if (bi == null)
                Log.Error("Unable to locate reference: {0}", reference.Assembly);
            if (bi.Name != "Reference")
            {
                bi.Name = "Reference";
                bi.RemoveMetadata("Project");
            }
            if(specificVersion && File.Exists(hintPath))
                bi.Include = System.Reflection.AssemblyName.GetAssemblyName(hintPath).FullName;
            bi.SetMetadata("HintPath", MakeProjectRelativePath(hintPath));
            bi.SetMetadata("SpecificVersion", specificVersion.ToString());
            bi.SetMetadata("Private", copyLocal.ToString());
		}

		public bool RemoveReference(ProjectRef reference)
		{
			if (_refCache != null)
				_refCache.Remove(reference);

			Log.Verbose("Removing reference to: {0}", reference.Assembly);
            BuildItem bi = FindByReference(reference);
            if (bi != null)
            {
                _project.RemoveItem(bi);
                Log.Verbose("Removed reference to: {0}", reference.Assembly);
                return true;
            }

            Log.Warning("Don't know how to remove reference: {0}", reference.Assembly);
			return false;
		}

        private BuildItem FindByReference(ProjectRef reference)
        {
            if (reference.Guid.HasValue)
            {
                foreach (BuildItem bi in _project.GetEvaluatedItemsByName("ProjectReference").ToArray())
                {
                    if (reference.Guid == new Guid(bi.GetMetadata("Project")))
                        return bi;
                }
            }
            else if (reference.Assembly != null)
            {
                //Now we enum all the real Reference items...
                foreach (BuildItem bi in _project.GetEvaluatedItemsByName("Reference").ToArray())
                {
                    if (reference.Assembly.Name == new AssemblyName(bi.Include).Name)
                        return bi;
                }
            }
            return null;
        }

		BuildItemGroup FristReferenceGroup(string grpCondition)
		{
			foreach (BuildItemGroup grp in _project.ItemGroups)
			{
				if (grp.IsImported) continue;
				if (!ConditionsMatch(grpCondition, grp.Condition))
					continue;

				foreach (BuildItem bi in grp)
				{
					if (bi.Name == "Reference" || bi.Name == "ProjectReference")
						return grp;
				}
			}

			BuildItemGroup grpAdding = _project.AddNewItemGroup();
			if (!String.IsNullOrEmpty(grpCondition))
				grpAdding.Condition = grpCondition;
			return grpAdding;
		}

		public bool AddReference(string grpCondition, ProjectInfo project)
		{
			BuildItemGroup grpAdding = FristReferenceGroup(grpCondition);
			string file = this.MakeProjectRelativePath(project.FullName);

			BuildItem addedItem = grpAdding.AddNewItem("ProjectReference", file);
			addedItem.SetMetadata("Project", project.ProjectGuid.ToString().ToUpper());
			addedItem.SetMetadata("Name", project.AssemblyName);

			_refCache.Add(new ProjectRef(addedItem, GetProjectPath));
			return true;
		}

		public bool AddReference(string grpCondition, AssemblyName assembly, string condition, string hintPath, bool copyLocal, bool specificVersion)
		{
			BuildItemGroup grpAdding = FristReferenceGroup(grpCondition);

			if (!File.Exists(hintPath))
				throw new ApplicationException("Unable to locate path for: " + assembly.ToString());
			if (specificVersion || assembly.Version != null)
			{
				if (File.Exists(hintPath))
				{
					specificVersion = true;
					assembly = System.Reflection.AssemblyName.GetAssemblyName(hintPath);
				}
			}
			BuildItem addedItem = grpAdding.AddNewItem("Reference", !specificVersion ? assembly.Name : assembly.FullName);
			addedItem.Condition = condition;
			addedItem.SetMetadata("SpecificVersion", specificVersion.ToString());
			addedItem.SetMetadata("Private", copyLocal.ToString());
			if (!String.IsNullOrEmpty(hintPath))
				addedItem.SetMetadata("HintPath", MakeProjectRelativePath(hintPath));

			_refCache.Add(new ProjectRef(addedItem, GetProjectPath));
			return true;
		}
	}
}
