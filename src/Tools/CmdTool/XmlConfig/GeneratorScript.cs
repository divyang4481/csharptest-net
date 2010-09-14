using System.Xml.Serialization;
using CSharpTest.Net.Processes;

namespace CSharpTest.Net.CustomTool.XmlConfig
{
    /// <summary> Defines an executable script </summary>
    public class GeneratorScript
    {
        /// <summary> The type of the script content </summary>
        [XmlAttribute("type")] 
        public ScriptEngine.Language Type;

		/// <summary> Includes a script file by prepending it's contents to the enclosed script block </summary>
		[XmlAttribute("src")]
    	public string Include;

        /// <summary> The script content </summary>
        [XmlText]
        public string Text;
    }

	/// <summary> Defines an executable script </summary>
	public class GeneratorExecute : GeneratorScript
	{
		public GeneratorExecute()
		{
			Type = ScriptEngine.Language.Exe;
		}

		/// <summary> The type of the script content </summary>
		[XmlAttribute("exe")]
		public string Exe
		{
			get { return Text; }
			set { Text = (value ?? string.Empty).Trim(); }
		}
	}
}