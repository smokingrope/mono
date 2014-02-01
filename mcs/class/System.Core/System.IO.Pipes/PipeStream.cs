//
// PipeStream.cs
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

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace System.IO.Pipes
{
	[PermissionSet (SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[HostProtection (SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public abstract class PipeStream : Stream
	{
    protected const int DefaultBufferSize = 0x400;
		internal static bool IsWindows {
			get { return Win32Marshal.IsWindows; }
		}

		internal Exception ThrowACLException ()
		{
			return new NotImplementedException ("ACL is not supported in Mono");
		}

		internal static PipeAccessRights ToAccessRights (PipeDirection direction)
		{
			switch (direction) {
			case PipeDirection.In:
				return PipeAccessRights.ReadData;
			case PipeDirection.Out:
				return PipeAccessRights.WriteData;
			case PipeDirection.InOut:
				return PipeAccessRights.ReadData | PipeAccessRights.WriteData;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		internal static PipeDirection ToDirection (PipeAccessRights rights)
		{
			bool r = (rights & PipeAccessRights.ReadData) != 0;
			bool w = (rights & PipeAccessRights.WriteData) != 0;
			if (r) {
				if (w)
					return PipeDirection.InOut;
				else
					return PipeDirection.In;
			} else {
				if (w)
					return PipeDirection.Out;
				else
					throw new ArgumentOutOfRangeException ();
			}
		}

		protected PipeStream (PipeDirection direction, int bufferSize)
			: this (direction, PipeTransmissionMode.Byte, bufferSize)
		{
		}

		protected PipeStream (PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
		{
			this.direction = direction;
			this.transmission_mode = transmissionMode;
			read_trans_mode = transmissionMode;
			if (outBufferSize <= 0)
				throw new ArgumentOutOfRangeException ("bufferSize must be greater than 0");
			buffer_size = outBufferSize;
		}

		internal PipeDirection direction;
		PipeTransmissionMode transmission_mode, read_trans_mode;
		int buffer_size;
		SafePipeHandle handle;

		public override bool CanRead {
			get { return !IsClosedInternal() && (direction & PipeDirection.In) != 0; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return !IsClosedInternal() && (direction & PipeDirection.Out) != 0; }
		}

		public virtual int InBufferSize {
			get 
      {
        // undocumented throws ObjectDisposedException
        // undocumented throws InvalidOperationException - handle has not been set
        CheckPipePropertyOperations();

        if (!CanRead) {
          throw new NotSupportedException("The stream is unreadable");
        }

        return buffer_size; 
      }
		}

		public bool IsAsync { get; private set; }

		public bool IsConnected {
      get {
        return !IsWaitingToConnectInternal() &&
               !IsDisconnectedInternal() &&
               !IsClosedInternal() &&
               !IsBrokenInternal() &&
               IsConnectedInternal();
      }
      protected set {
        _isConnected = value;
      }
    }

		protected bool IsHandleExposed { get; private set; }

		[MonoTODO]
		public bool IsMessageComplete
    {
      get {
        // undocumented IOException - the pipe is broken
        CheckPipePropertyOperations();

        if (this.ReadMode != PipeTransmissionMode.Message) {
          throw new InvalidOperationException("Transmission mode is not Message");
        }

        return IsMessageCompleteInternal();
      }
    }
    internal virtual bool IsMessageCompleteInternal() {
      return false;
    }

		[MonoTODO]
		public virtual int OutBufferSize {
      get {
        // undocumented throws ObjectDisposedException
        // undocumented throws InvalidOperationException - handle has not been set
        CheckPipePropertyOperations();

        if (!this.CanWrite) {
          throw new NotSupportedException("The stream is unwriteable");
        }

        return buffer_size;
      }
		}

		public virtual PipeTransmissionMode ReadMode {
			get {
        // undocumented throws ObjectDisposedException

				CheckPipePropertyOperations ();

				return read_trans_mode;
			}
			set {
				CheckPipePropertyOperations ();
        switch (value) {
          case PipeTransmissionMode.Byte:
          case PipeTransmissionMode.Message:
            break;
          default:
            throw new ArgumentOutOfRangeException ("The supplied value is not a valid PipeTransmissionMode value.");
        }
				read_trans_mode = value;
			}
		}

		public SafePipeHandle SafePipeHandle {
			get {
        if (IsClosedInternal()) {
          throw new ObjectDisposedException ("The pipe is closed");
        }
        if (!IsHandleSetInternal()) {
          throw new InvalidOperationException ("The handle has not been set");
        }

				return handle;
			}
		}

		public virtual PipeTransmissionMode TransmissionMode {
			get {
				CheckPipePropertyOperations ();

				return transmission_mode;
			}
		}

		// initialize/dispose/state check

    // documentation terms to be interpreted
    internal virtual bool IsWaitingToConnectInternal() {
      return !_isConnected && !_isClosed;
    }
    void ThrowIfWaitingToConnect() {
      if (IsWaitingToConnectInternal()) {
        throw new InvalidOperationException ("The pipe is waiting to connect");
      }
    }

    // connected means client / server connection has been established?
    private bool _isConnected;
    internal virtual bool IsConnectedInternal() {
      return _isConnected && !_isClosed;
    }

    // Disconnected means link between client / server was open, but has since been terminated
    internal virtual bool IsDisconnectedInternal() {
      return !_isConnected && _isClosed;
    }
    void ThrowIfDisconnected() {
      if (IsDisconnectedInternal()) {
        throw new InvalidOperationException ("The pipe is disconnected");
      }
    }

    // closed pipe means "Disposed"
    private bool _isClosed;
    internal virtual bool IsClosedInternal() {
      return _isClosed;
    }
    void ThrowIfClosed() {
      if (IsClosedInternal()) {
        throw new ObjectDisposedException ("The pipe is closed");
      }
    }

    // broken means pipe has become unusable due to error?
    internal virtual bool IsBrokenInternal() {
      return false;
    }
    void ThrowIfBroken() {
      if (IsBrokenInternal()) {
        throw new IOException ("The pipe is broken");
      }
    }

    // handle has not been initialized
    internal virtual bool IsHandleSetInternal() {
      return this.handle != null; 
    }
    void ThrowIfHandleNotSet() {
      if (!IsHandleSetInternal()) {
        throw new InvalidOperationException ("The handle has not been set");
      }
    }

    // default to disallow message mode?
    internal virtual bool AllowMessageModeInternal() {
      return false;
    }

		protected internal virtual void CheckPipePropertyOperations ()
		{
      ThrowIfClosed();
      ThrowIfHandleNotSet();
      ThrowIfBroken();
      ThrowIfWaitingToConnect();
		}

		protected internal void CheckReadOperations ()
		{
      ThrowIfClosed();
      ThrowIfHandleNotSet();
      ThrowIfBroken();
      ThrowIfWaitingToConnect();
      ThrowIfDisconnected();

			if (!CanRead) {
				throw new NotSupportedException ("The pipe stream does not support read operations");
      }
		}

		protected internal void CheckWriteOperations ()
		{
      ThrowIfClosed();
      ThrowIfHandleNotSet();
      ThrowIfBroken();
      ThrowIfWaitingToConnect();
      ThrowIfDisconnected();

			if (!CanWrite) {
				throw new NotSupportedException ("The pipe stream does not support write operations");
      }
		}

		protected void InitializeHandle (SafePipeHandle handle, bool isExposed, bool isAsync)
		{
			this.handle = handle;
			this.IsHandleExposed = isExposed;
			this.IsAsync = isAsync;
		}

		protected override void Dispose (bool disposing)
		{
			if (handle != null && disposing) {
				handle.Dispose ();
        handle = null;
      }

      _isClosed = false;
		}

		// not supported

		public override long Length {
			get { throw new NotSupportedException (); }
		}

		public override long Position {
			get { return 0; }
			set { throw new NotSupportedException (); }
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
		}

		public PipeSecurity GetAccessControl ()
		{
			return new PipeSecurity (SafePipeHandle,
						 AccessControlSections.Owner |
						 AccessControlSections.Group |
						 AccessControlSections.Access);
		}

		public void SetAccessControl (PipeSecurity pipeSecurity)
		{
			if (pipeSecurity == null)
				throw new ArgumentNullException ("pipeSecurity");
				
			pipeSecurity.Persist (SafePipeHandle);
		}

		// pipe I/O

		public void WaitForPipeDrain ()
		{
      // undocumented InvalidOperationException - waiting to connect
      // undocumented InvalidOperationException - handle has not been set
      // undocumented InvalidOperationException - the pipe is disconnected
      CheckWriteOperations();

      WaitForPipeDrainInternal();
		}
    internal virtual void WaitForPipeDrainInternal()
    {
    }

		public override int Read ([In] byte [] buffer, int offset, int count)
		{
			CheckReadOperations ();

      MonoIOError error;
      int amount = MonoIO.Read (handle.DangerousGetHandle(), buffer, offset, count, out error);
      if (error != MonoIOError.ERROR_SUCCESS) {
        throw MonoIO.GetException ("<PIPE>", error);
      }
      return amount;
		}

		public override int ReadByte ()
		{
      byte []result = new byte[1];
      if (Read(result, 0, 1)>0) {
        return result[0];
      } else {
        return -1;
      }
		}

		public override void Write (byte [] buffer, int offset, int count)
		{
			CheckWriteOperations ();

      MonoIOError error;
      int amount = MonoIO.Write (handle.DangerousGetHandle(), buffer, offset, count, out error);
      if (error != MonoIOError.ERROR_SUCCESS) {
        throw MonoIO.GetException("<PIPE>", error);
      }
		}

		public override void WriteByte (byte value)
		{
      byte[] output = new byte[1];
      output[0] = value;
      Write(output, 0, 1);
		}

		public override void Flush ()
		{
			CheckWriteOperations ();

      // completely unbuffered so we don't need to do anything
		}

		// async

		Func<byte [],int,int,int> read_delegate;

		[HostProtection (SecurityAction.LinkDemand, ExternalThreading = true)]
		public override IAsyncResult BeginRead (byte [] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (read_delegate == null)
				read_delegate = new Func<byte[],int,int,int> (Read);
			return read_delegate.BeginInvoke (buffer, offset, count, callback, state);
		}

		Action<byte[],int,int> write_delegate;

		[HostProtection (SecurityAction.LinkDemand, ExternalThreading = true)]
		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (write_delegate == null)
				write_delegate = new Action<byte[],int,int> (Write);
			return write_delegate.BeginInvoke (buffer, offset, count, callback, state);
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			return read_delegate.EndInvoke (asyncResult);
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			write_delegate.EndInvoke (asyncResult);
		}
	}
}

#endif
