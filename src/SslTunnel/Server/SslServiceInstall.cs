using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;


namespace CSharpTest.Net.SslTunnel.Server
{
	[RunInstaller(true)]
	public partial class SslServiceInstall : Installer
	{
		public SslServiceInstall()
		{
			InitializeComponent();
		}
	}
}
