//
// SafePipeHandle.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// Copyright (C) 2009 Novell, Inc.  http://www.novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.IO.Pipes;

namespace Microsoft.Win32.SafeHandles
{
	[HostProtection (SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	[SecurityPermission (SecurityAction.LinkDemand, UnmanagedCode = true)]
	public sealed class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafePipeHandle (IntPtr preexistingHandle, bool ownsHandle)
			: base (ownsHandle)
		{
			SetHandle (preexistingHandle);
		}
 
		internal void SetHandle (IntPtr handle)
		{
			base.SetHandle (handle);
		}
 
		/* required for unix tracking of pipe disconnects
		   and must be present here to support constructor accepting PipeHandle args */
		internal SafePipeHandle DisposalHandle = null;

		/* Required for unix tracking of pipe drain in anonymous pipes 
		   and must be present here to support constructor accepting PipeHandle args */
		internal SafePipeHandle DrainHandle = null;

		protected override bool ReleaseHandle()
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
					// TODO: close win32 pipe handle
					return true;
				default:
					MonoIOError result;
					return MonoIO.Close(handle, out result);
			}
		}
	}
}

