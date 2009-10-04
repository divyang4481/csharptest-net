namespace CSharpTest.Net.SslTunnel.Server
{
	partial class SslServiceInstall
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.serviceInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.tunnelInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// serviceInstaller
			// 
			this.serviceInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
			this.serviceInstaller.Password = null;
			this.serviceInstaller.Username = null;
			// 
			// tunnelInstaller
			// 
			this.tunnelInstaller.Description = "An SSL tunneling service for forwarding insecure communications over a secure con" +
				"nection.";
			this.tunnelInstaller.DisplayName = "SSL Tunnel Service";
			this.tunnelInstaller.ServiceName = "SslTunnel";
			this.tunnelInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// SslServiceInstall
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstaller,
            this.tunnelInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller serviceInstaller;
		private System.ServiceProcess.ServiceInstaller tunnelInstaller;
	}
}