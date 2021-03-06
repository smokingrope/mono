//
// AnonymousPipeClientStream.cs
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

#if !BOOTSTRAP_BASIC

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
	[MonoTODO ("Anonymous pipes are not working even on win32, due to some access authorization issue")]
	[HostProtection (SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public sealed class AnonymousPipeClientStream : PipeStream
	{
		private const int BUFFERLESS_SIZE = 1;

		IAnonymousPipeClient impl;

		public AnonymousPipeClientStream (string pipeHandleAsString)
			: this (PipeDirection.In, pipeHandleAsString)
		{
		}

		public AnonymousPipeClientStream (PipeDirection direction, string pipeHandleAsString)
			: base (direction, BUFFERLESS_SIZE)
		{
			if (IsWindows)
				impl = new Win32AnonymousPipeClient (this, pipeHandleAsString);
			else
				impl = new UnixAnonymousPipeClient (this, pipeHandleAsString);

			InitializeHandle (impl.Handle, false, false);
			IsConnected = true;
		}

		public AnonymousPipeClientStream (PipeDirection direction,SafePipeHandle safePipeHandle)
			: base (direction, BUFFERLESS_SIZE)
		{
			if (IsWindows)
				impl = new Win32AnonymousPipeClient (this, safePipeHandle);
			else
				impl = new UnixAnonymousPipeClient (this, safePipeHandle);

			InitializeHandle (impl.Handle, false, false);
			IsConnected = true;
		}

		internal override void WaitForPipeDrainInternal() {
			impl.WaitForPipeDrain();
		}

		~AnonymousPipeClientStream ()
		{
			// To be compatible with .net
		}
		protected override void Dispose (bool disposing)
		{
			base.Dispose(disposing);
			if (disposing) { impl.Dispose(); }
		}

		internal override bool IsBrokenInternal()
		{
			return impl.IsBroken();
		}

		public override PipeTransmissionMode ReadMode {
			set {
				if (value == PipeTransmissionMode.Message)
					throw new NotSupportedException ();
			}
		}

		public override PipeTransmissionMode TransmissionMode {
			get { return PipeTransmissionMode.Byte; }
		}
	}
}

#endif
