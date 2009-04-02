#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Xml;

namespace CSharpTest.Net.CoverageReport.Reader
{
	/// <summary>
	/// used to find and re-identify overloaded members
	/// </summary>
	class MethodOccurance
	{
		public readonly string Namespace;
		public readonly string Class;
		public readonly string Method;
		public int Counter;

		public MethodOccurance(string ns, string cls, string mthd)
		{
			Namespace = ns;
			Class = cls;
			Method = mthd;
			Counter = 0;
		}

		public override int GetHashCode()
		{
			return Namespace.GetHashCode() ^ Class.GetHashCode() ^ Method.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			MethodOccurance other = obj as MethodOccurance;
			if (other != null)
				return this.Namespace == other.Namespace && this.Class == other.Class && this.Method == other.Method;
			return false;
		}
	}

	class XmlData
	{
		public const string UNKOWN_NAME = "{UNKNOWN}";
		public const string WEB_APP_NAME = "App_Web_*";

		public coverageFile File;
		public moduleData Module;
		public methodData Method;
		public seqpntData Seqpnt;

		private XmlReader _xml;
		private Dictionary<string, string> _nametable;

		private XmlData(XmlReader rdr)
		{
			_xml = rdr;
			_nametable = new Dictionary<string, string>();
		}

		public static IEnumerable<XmlData> Read(XmlReader rdr)
		{
			try
			{
				Dictionary<MethodOccurance, MethodOccurance> members = new Dictionary<MethodOccurance, MethodOccurance>();
				XmlData instance = new XmlData(rdr);

				while (rdr.Read())
				{
					string locName = rdr.LocalName;
					if (StringComparer.Ordinal.Equals(locName, "coverage"))
						instance.File.Read(instance);
					else if (StringComparer.Ordinal.Equals(locName, "module"))
						instance.Module.Read(instance);
					else if (StringComparer.Ordinal.Equals(locName, "method"))
					{
						instance.Method.Read(instance);
						MethodOccurance foundmo, mo = new MethodOccurance(instance.Method.Namespace, instance.Method.Class, instance.Method.name);
						if (members.TryGetValue(mo, out foundmo))
						{
							instance.Method.name = instance.Name(
								String.Format("{0}(overload {1})", instance.Method.name, ++foundmo.Counter)
							);
						}
						else
							members.Add(mo, mo);
					}
					else if (StringComparer.Ordinal.Equals(locName, "seqpnt"))
					{
						instance.Seqpnt.Read(instance);
						yield return instance;
					}
				}
			}
			finally
			{
			}
		}

		#region Private Xml Reader API

		string ReadString(string atrName, string def)
		{
			string val = ReadString(atrName);
			if (val == null) return def;
			return val;
		}

		string ReadString(string atrName)
		{
			return _xml.GetAttribute(atrName);
		}

		string ReadName(string atrName, string def)
		{
			string val = ReadName(atrName);
			if (val == null) return def;
			return val;
		}

		string ReadName(string atrName)
		{
			return Name(_xml.GetAttribute(atrName));
		}

		string Name(string val)
		{
			if (val == null) return null;

			string newval;
			if (!_nametable.TryGetValue(val, out newval))
				_nametable.Add(val, newval = val);

			return newval;
		}

		DateTime ReadDate(string atrName)
		{
			string text = ReadString(atrName);
			if (String.IsNullOrEmpty(text))
				return DateTime.MinValue;

			return System.Xml.XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);
		}

		int ReadInt(string atrName, int def)
		{
			int val;
			if (!int.TryParse(ReadString(atrName), out val))
				return def;
			return val;
		}

		long ReadLong(string atrName, long def)
		{
			long val;
			if (!long.TryParse(ReadString(atrName), out val))
				return def;
			return val;
		}

		bool ReadBool(string atrName, bool def)
		{
			bool val;
			if (!bool.TryParse(ReadString(atrName), out val))
				return def;
			return val;
		}

		#endregion

		#region Structures
		//<coverage profilerVersion="1.5.7 Beta" driverVersion="1.5.7.0" startTime="2009-02-10T17:24:09.6875-06:00" measureTime="2009-02-10T17:24:15.984375-06:00">
		public struct coverageFile
		{
			public string profilerVersion;//="1.5.7 Beta" 
			public string driverVersion;//="1.5.7.0" 
			public DateTime startTime;//="2009-02-10T17:24:09.6875-06:00" 
			public DateTime measureTime;//="2009-02-10T17:24:15.984375-06:00">

			public void Read(XmlData rdr)
			{
				profilerVersion = rdr.ReadString("profilerVersion");
				driverVersion = rdr.ReadString("driverVersion");
				startTime = rdr.ReadDate("startTime");
				measureTime = rdr.ReadDate("measureTime");
			}
		}

		//<module moduleId="18" name="C:\Projects\svault\bin\SVault.Core.dll" assembly="SVault.Core" assemblyIdentity="SVault.Core, Version=1.5.0.0, Culture=neutral, PublicKeyToken=null, processorArchitecture=x86">
		public struct moduleData
		{
			//ignored: public string moduleId;//="18" 
			public string name;//="C:\Projects\svault\bin\SVault.Core.dll" 
			public string filename;//="C:\Projects\svault\bin\SVault.Core.dll" 
			public string assembly;//="SVault.Core" 
			public string assemblyIdentity;//="SVault.Core, Version=1.5.0.0, Culture=neutral, PublicKeyToken=null, processorArchitecture=x86">

			public void Read(XmlData rdr)
			{
				int pos;
				if (null != (name = filename = rdr.ReadName("name")))
					try { name = rdr.Name(System.IO.Path.GetFileName(name)); } catch { }
				
				assemblyIdentity = rdr.ReadName("assemblyIdentity");
				if (null == (assembly = rdr.ReadName("assembly")))
				{
					if (null != (assembly = assemblyIdentity) && (pos = assembly.IndexOf(',')) > 0)
						assembly = rdr.Name(assembly.Substring(0, pos));
				}
				if (null == assembly)
					assembly = UNKOWN_NAME;

				if (assembly.StartsWith("App_Web_", StringComparison.Ordinal))
					assembly = WEB_APP_NAME;
			}
		}

		//<method name=".ctor" excluded="false" instrumented="true" class="SVault.ApplicationProfileAttribute">
		public struct methodData
		{
			public string name;//=".ctor" 
			public bool excluded;//="false" 
			public bool instrumented;//="true" 
			//string _class;//="SVault.ApplicationProfileAttribute">
			public string Namespace;
			public string Class;

			public void Read(XmlData rdr)
			{
				name = rdr.ReadName("name", UNKOWN_NAME);
				excluded = rdr.ReadBool("excluded", false);
				instrumented = rdr.ReadBool("instrumented", false);

				int pos;
				string cls = rdr.ReadName("class", UNKOWN_NAME);
				
				pos = cls.IndexOf("+<");
				if (pos > 0)
					pos = cls.LastIndexOf('.', pos);
				else
					pos = cls.LastIndexOf('.');

				if (pos > 0)
				{
					Namespace = rdr.Name(cls.Substring(0, pos));
					cls = cls.Substring(pos + 1);
				}
				else Namespace = "-";

				Class = rdr.Name(cls);
			}
		}

		//<seqpnt visitcount="8" line="42" column="9" endline="42" endcolumn="105" excluded="false" document="c:\Projects\svault\src\core\SVault.Core\Services\ServiceAttributes.cs" />
		public struct seqpntData
		{
			public long visitcount;//="8" 
			public int line;//="42" 
			public int column;//="9" 
			public int endline;//="42" 
			public int endcolumn;//="105" 
			public bool excluded;//="false" 
			public string document;//="c:\Projects\svault\src\core\SVault.Core\Services\ServiceAttributes.cs" />

			public void Read(XmlData rdr)
			{
				visitcount = rdr.ReadLong("visitcount", 0);
				line = rdr.ReadInt("line", 0);
				column = rdr.ReadInt("column", 0);
				endline = rdr.ReadInt("endline", line);
				endcolumn = rdr.ReadInt("endcolumn", column);
				excluded = rdr.ReadBool("excluded", false);
				document = rdr.ReadName("document");
			}
		}
		#endregion
	}
}
