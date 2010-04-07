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

namespace CSharpTest.Net.CSBuild.Implementation
{
	[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
	class ItemLookup : ICollection<ProjectInfo>
	{
		List<ItemInfo> _projects = new List<ItemInfo>();
		Dictionary<string, int> _byProject = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, int> _byAssembly = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, int> _byOutputFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Dictionary<Guid, int> _byProjectId = new Dictionary<Guid, int>();

		public ProjectInfo FindItem(ProjectRef reference)
		{
			int itemIx;
			if (reference.Guid.HasValue && _byProjectId.TryGetValue(reference.Guid.Value, out itemIx))
				return _projects[itemIx].BuildItem;
			if (reference.Project != null && _byProject.TryGetValue(reference.Project, out itemIx))
				return _projects[itemIx].BuildItem;
			if (reference.Output != null && _byOutputFile.TryGetValue(reference.Output, out itemIx))
				return _projects[itemIx].BuildItem;
			if (reference.Assembly != null && _byAssembly.TryGetValue(reference.Assembly.Name, out itemIx))
				return _projects[itemIx].BuildItem;
			return null;
		}

		public int Count 
		{
			get 
			{
				int count = 0;
				foreach (ItemInfo item in _projects)
					count += item.BuildItem == null ? 0 : 1;
				return count;		
			}
		}

		public void AddRange(IEnumerable<ProjectInfo> items)
		{
			foreach (ProjectInfo item in items)
				Add(item);
		}

		public void Add(ProjectInfo item)
		{
			int id = _projects.Count;
			ItemInfo info = new ItemInfo(item);
			
			_projects.Add(info);

			_byProject.Add(info.Project, id);
			_byOutputFile.Add(info.OutputFile, id);

			if (_byProjectId.ContainsKey(info.ProjectId))
				Log.Warning("Multiple projects with id {0}, using {1}", info.ProjectId, _projects[_byProjectId[info.ProjectId]].Project);
			else
				_byProjectId.Add(info.ProjectId, id);
			
			if (_byAssembly.ContainsKey(info.Assembly))
				Log.Warning("Multiple projects build assembly {0}, using {1}", info.Assembly, _projects[_byAssembly[info.Assembly]].Project);
			else
				_byAssembly.Add(info.Assembly, id);
		}

		public void ProjectChanged(object sender, ProjectPropetyChangedEventArgs args)
		{
			int ordinal;
			ProjectInfo item = Check.NotNull(args.Project);

			if (_byProject.TryGetValue(item.FullName, out ordinal))
			{
				_byOutputFile.Remove(_projects[ordinal].OutputFile);

				_projects[ordinal].OutputFile = item.AbsoluteOutputPath;
				_byOutputFile.Add(_projects[ordinal].OutputFile, ordinal);
			}
		}

		public bool Remove(ProjectInfo item)
		{
			int ordinal;
			if (_byProject.TryGetValue(item.FullName, out ordinal))
			{
				_projects[ordinal].BuildItem = null;
				return true;
			}
			return false;
		}


		public void Clear()
		{
			_projects.Clear();
			_byProject.Clear();
			_byAssembly.Clear();
			_byOutputFile.Clear();
			_byProjectId.Clear();
		}

		public bool Contains(ProjectInfo item)
		{
			int ix;
			return _byProject.TryGetValue(item.FullName, out ix) && _projects[ix].BuildItem != null;
		}

		public void CopyTo(ProjectInfo[] array, int arrayIndex)
		{
			ToArray().CopyTo(array, arrayIndex);
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public ProjectInfo[] ToArray()
		{
			List<ProjectInfo> copy = new List<ProjectInfo>();
			foreach (ItemInfo item in _projects)
			{
				if (item.BuildItem != null)
					copy.Add(item.BuildItem);
			}
			return copy.ToArray();
		}

		#region IEnumerable<BuildItem> Members

		public IEnumerator<ProjectInfo> GetEnumerator()
		{
			return new List<ProjectInfo>(ToArray()).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ToArray().GetEnumerator();
		}

		#endregion

		[System.Diagnostics.DebuggerDisplay("{Project}")]
		class ItemInfo
		{
			public ProjectInfo BuildItem;
			public readonly string Assembly;
			public readonly string Project;
			public string OutputFile;
			public readonly Guid ProjectId;

			public ItemInfo(ProjectInfo item)
			{
				BuildItem = item;
				Assembly = item.AssemblyName;
				Project = item.FullName;
				OutputFile = item.AbsoluteOutputPath;
				ProjectId = item.ProjectGuid;
			}
		}
	}
}
