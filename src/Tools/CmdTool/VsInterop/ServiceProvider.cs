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
using System.Diagnostics;
using System.Runtime.InteropServices;
using CSharpTest.Net.CustomTool.Interfaces;

namespace CSharpTest.Net.CustomTool.VsInterop
{
	class ServiceProvider : IServiceProvider, IObjectWithSite
	{

		private static Guid IID_IUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");

		private IOleServiceProvider _serviceProvider;
		public ServiceProvider(IOleServiceProvider sp)
		{
			_serviceProvider = sp;
		}

		public virtual void Dispose()
		{
			if (_serviceProvider != null)
				_serviceProvider = null;
		}

		static bool Failed(int hr) { return (hr < 0); }
		static bool Succeeded(int hr) { return (hr >= 0); }

		public virtual object GetService(Type serviceClass)
		{
			if (serviceClass == null)
				return null;

			return GetService(serviceClass.GUID, serviceClass);
		}

		public virtual object GetService(Guid guid)
		{
			return GetService(guid, null);
		}

		private object GetService(Guid guid, Type serviceClass)
		{
			if (_serviceProvider == null)
				return null;

			object service = null;

			if (guid.Equals(Guid.Empty))
				return null;

			if (guid.Equals(typeof(IOleServiceProvider).GUID))
				return _serviceProvider;
			if (guid.Equals(typeof(IObjectWithSite).GUID))
				return (IObjectWithSite)this;

			IntPtr pUnk;
			int hr = _serviceProvider.QueryService(ref guid, ref IID_IUnknown, out pUnk);

			if (Succeeded(hr) && (pUnk != IntPtr.Zero))
			{
				service = Marshal.GetObjectForIUnknown(pUnk);
				Marshal.Release(pUnk);
			}

			return service;
		}

		void IObjectWithSite.GetSite(ref Guid riid, object[] ppvSite)
		{
			ppvSite[0] = GetService(riid);
		}

		void IObjectWithSite.SetSite(object pUnkSite)
		{
			if (pUnkSite is IOleServiceProvider)
				_serviceProvider = (IOleServiceProvider)pUnkSite;
		}
	}
}

