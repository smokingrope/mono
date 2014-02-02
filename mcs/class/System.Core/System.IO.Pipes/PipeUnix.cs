//
// PipeUnix.cs
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Mono.Unix.Native;

namespace System.IO.Pipes
{
	abstract class UnixAnonymousPipe : IPipe
	{
		protected UnixAnonymousPipe ()
		{
		}

		public abstract SafePipeHandle Handle { get; }

    public bool IsBroken() {
      if (IsReader() &&
          DoesReaderHandleHaveDataToRead()) {
        // if the reader pipe still has data to read then
        // we have to pretend the connection is active even
        // though it's not
        return false;
      }
      if (IsDisposalHandleBroken()) { return true; }

      return false;
    }
    public bool IsDisposalHandleBroken()
    {
      // check whether disposal handle has POLLERR / POLLHUP
      // and if so that means reader has closed the pipe
      MonoIO.PollFlags revents;
      int pollResult = MonoIO.PollFD(Handle.DisposalHandle.DangerousGetHandle().ToInt32(), 
                        MonoIO.PollFlags.POLLERR | MonoIO.PollFlags.POLLHUP,
                        out revents, 0);
      switch (pollResult) {
        case -1:
          // TODO: Better error reporting
          throw new IOException("Failure during wait for pipe drain");

        case 0:
          // no POLLERR or POLLHUP means client has not been disposed
          return false;

        default:
          // received POLLERR or POLLHUP, client has been disposed
          return true;
      }
      return false;
    }

    public bool DoesHandleHaveDataToRead(int handle)
    {
      MonoIO.PollFlags revents;
      int pollResult; 
        
      // Check whether read end of pipe has data remaining
      pollResult = MonoIO.PollFD(handle, MonoIO.PollFlags.POLLIN, out revents, 0);
      switch (pollResult) {
        case -1:
          // TODO: Better error reporting
          throw new IOException("Failure while checking pipe stream");

        case 0:
          // no POLLIN event means pipe buffer is empty
          return false;

        default:
          // received POLLIN event, more data to be read
          // defer to scheduler before continuing
          return true;
      }
    }

    public bool IsReader() {
      return Handle.DrainHandle == null;
    }
    public bool IsWriter() {
      return Handle.DrainHandle != null;
    }

    public bool DoesReaderHandleHaveDataToRead() {
      // only writer end has drain handle
      if (IsWriter()) {
        throw new NotSupportedException("cannot read from writer");
      }
      return DoesHandleHaveDataToRead(Handle.DangerousGetHandle().ToInt32());
    }
    public bool DoesDrainHandleHaveDataToRead() {
      if (IsReader()) {
        throw new NotSupportedException("cannot check drain handle from reader");
      }
      return DoesHandleHaveDataToRead(Handle.DrainHandle.DangerousGetHandle().ToInt32());
    }

		public virtual void WaitForPipeDrain ()
		{
      // only writer end has drain handle
      if (IsReader()) {
        throw new NotSupportedException("cannot wait from reader");
      }

      int brokenCount = 0;
      do {
        // if pipe is broken and pipe is drained, we prefer to return the 'pipe drain' event
        // as opposed to pipe broken event, so we require a double check of the drain handle
        // before throwing a disposed exception
        if (IsBroken() && ++brokenCount > 1) {
          throw new ObjectDisposedException("disposalHandle", "reader has closed the pipe");
        } else {
          brokenCount = 0;
        }
       
        if (!DoesDrainHandleHaveDataToRead()) { return; }
          
        // received POLLIN event, more data to be read
        // defer to scheduler before continuing
        System.Threading.Thread.Sleep(0);
      } while (true);
		}

    public abstract void Dispose();
	}

	class UnixAnonymousPipeClient : UnixAnonymousPipe, IAnonymousPipeClient
	{
		// AnonymousPipeClientStream owner;

		public UnixAnonymousPipeClient (AnonymousPipeClientStream owner, string handleAsString)
		{
      if (string.IsNullOrEmpty(handleAsString)) {
        throw new IOException("handleAsString - null or empty");
      }

      string[] fdstrings = handleAsString.Split(',');
      if (fdstrings.Length < 2 || fdstrings.Length > 3) {
        throw new IOException("handleAsString - invalid format");
      }

      int[] fds = new int[fdstrings.Length];
      IntPtr[] fdHandles = new IntPtr[fdstrings.Length];
      SafePipeHandle[] safeHandles = new SafePipeHandle[fdstrings.Length];
      for (int i = 0; i < fdstrings.Length; ++i) {
        if (!int.TryParse(fdstrings[i], out fds[i])) {
          throw new IOException("handlAsString - invalid handle");
        }
        // pre-allocate safe handles
        safeHandles[i] = new SafePipeHandle( (IntPtr)0, true);
      }

      MonoIO.GetPipeHandleFlag direction;
      switch (owner.direction) {
        case PipeDirection.In:
          if (fds.Length != 2) {
            throw new IOException("handleAsString - invalid handle format");
          }
          direction = MonoIO.GetPipeHandleFlag.GENERIC_READ;
          break;
        case PipeDirection.Out:
          if (fds.Length != 3) {
            throw new IOException("handleAsString - invalid handle format");
          }
          direction = MonoIO.GetPipeHandleFlag.GENERIC_WRITE;
          break;
        default:
          throw new IOException("Unknown pipe direction " + owner.direction);
      }
 
      MonoIOError error;
      for (int i = 0; i < fds.Length; ++i) {
        switch (i) {
          case 0:
          case 1:
            break;
          case 2:
            direction = MonoIO.GetPipeHandleFlag.GENERIC_READ;
            break;
          default:
            throw new InvalidOperationException ("this should never happen");
        }
        fdHandles[i] = MonoIO.GetPipeHandle(fds[i], direction, out error);

        if (error != MonoIOError.ERROR_SUCCESS) {
          throw new IOException ("Unable to load pipe with error code " + error);
        }

        safeHandles[i].SetHandle(fdHandles[i]);

        if (safeHandles[i].IsInvalid) {
          throw new IOException ("Invalid pipe handle (" + i + ")");
        }

        switch (i) {
          case 1:
            safeHandles[0].DisposalHandle = safeHandles[i];
            break;
          case 2:
            safeHandles[0].DrainHandle = safeHandles[i];
            break;
        }
      }

			this.handle = safeHandles[0];
		}

    public UnixAnonymousPipeClient (AnonymousPipeClientStream owner, SafePipeHandle safePipeHandle)
    {
      if (safePipeHandle == null) {
        throw new ArgumentNullException ("safePipeHandle");
      }
      if (safePipeHandle.IsInvalid) {
        throw new IOException("Invalid pipe");
      }
      if (safePipeHandle.DisposalHandle == null || safePipeHandle.DisposalHandle.IsInvalid) {
        throw new IOException ("Invalid disposal handle");
      }
      if (owner.direction == PipeDirection.Out &&
          (safePipeHandle.DrainHandle == null ||
           safePipeHandle.DrainHandle.IsInvalid)) {
        throw new IOException ("Invalid drain handle for out pipe");
      }

			this.handle = safePipeHandle;
      bool addRefSuccess = false;
      this.handle.DangerousAddRef(ref addRefSuccess);
      this.handle.DisposalHandle.DangerousAddRef(ref addRefSuccess);
      if (this.handle.DrainHandle != null) {
        this.handle.DrainHandle.DangerousAddRef(ref addRefSuccess);
      }
    }

		SafePipeHandle handle;

		public override SafePipeHandle Handle {
			get { return handle; }
		}

    public override void Dispose() {
      if (handle != null) {
        if (handle.DisposalHandle != null && !handle.DisposalHandle.IsClosed) {
          handle.DisposalHandle.DangerousRelease();
        }
        if (handle.DrainHandle != null && !handle.DrainHandle.IsClosed) {
          handle.DrainHandle.DangerousRelease();
        }
        if (!handle.IsClosed) {
          handle.DangerousRelease();
        }
        handle = null;
      }
    }
	}

	class UnixAnonymousPipeServer : UnixAnonymousPipe, IAnonymousPipeServer
	{
		// AnonymousPipeServerStream owner;

		public UnixAnonymousPipeServer (AnonymousPipeServerStream owner, PipeDirection direction, HandleInheritability inheritability, int bufferSize)
		{
			// this.owner = owner;
      
      IntPtr[] l_comHandle = new IntPtr[2];
      IntPtr[] l_disposeHandle = new IntPtr[2];

      // create pipes for tracking dispose
      if (!MonoIO.CreatePipe(out l_disposeHandle[0], out l_disposeHandle[1])) {
        throw new IOException ("Error creating diposable anonymous pipe");
      }
      
      // create real pipes
      if (!MonoIO.CreatePipe (out l_comHandle[0], out l_comHandle[1])) {
        throw new IOException ("Error creating anonymous pipe");
      }

      // select pipe mode
      int sidx, cidx;
      bool clientNeedsDrain;
      switch (owner.direction) {
        case PipeDirection.In:
          sidx = 0; cidx= 1;
          clientNeedsDrain = true;
          break;
        case PipeDirection.Out:
          sidx = 1; cidx = 0;
          clientNeedsDrain = false;
          break;
        default:
          throw new NotSupportedException("pipe direction " + owner.direction);
      }

      this.server_handle = new SafePipeHandle (l_comHandle[sidx], true);
      this.server_handle.DisposalHandle = new SafePipeHandle(l_disposeHandle[sidx], true);
      this.client_handle = new SafePipeHandle (l_comHandle[cidx], true);
      this.client_handle.DisposalHandle = new SafePipeHandle(l_disposeHandle[cidx], true);
      
      if (clientNeedsDrain) {
        this.drain_handle = this.client_handle.DrainHandle = this.server_handle;
      } else {
        this.drain_handle = this.server_handle.DrainHandle = this.client_handle;
      }
 
      var errorMessage = new StringBuilder();
      var delimiter = string.Empty;
      Action<SafePipeHandle,string> checkHandle = (handle,name)=> {
        if (handle.IsInvalid) {
          errorMessage.Append(delimiter);
          errorMessage.Append(name);
          delimiter = ",";
        }
      };
      checkHandle(this.server_handle, "server com handle");
      checkHandle(this.server_handle.DisposalHandle, "server dispose handle");
      checkHandle(this.client_handle, "client com handle");
      checkHandle(this.client_handle.DisposalHandle, "client dispose handle");
      
      if (errorMessage.Length != 0) {
        this.server_handle.Dispose();
        this.client_handle.Dispose();
        throw new IOException ("invalid handles " + errorMessage.ToString());
      }
		}

		public UnixAnonymousPipeServer (AnonymousPipeServerStream owner, SafePipeHandle serverHandle, SafePipeHandle clientHandle)
		{
      if (serverHandle == null) {
        throw new IOException("serverHandle is null");
      }
      if (clientHandle == null) {
        throw new IOException("clientHandle is null");
      }
      if (serverHandle.IsInvalid) {
        throw new IOException("serverHandle is invalid");
      }
      if (clientHandle.IsInvalid) {
        throw new IOException("clientHandle is invalid");
      }
      if (serverHandle.DisposalHandle == null || serverHandle.DisposalHandle.IsInvalid) {
        throw new IOException ("server doesnt posess valid dispose handle");
      }
      if (clientHandle.DisposalHandle == null || clientHandle.DisposalHandle.IsInvalid) {
        throw new IOException ("client doesnt posess valid dispose handle");
      }
      if ((clientHandle.DrainHandle == null || clientHandle.DrainHandle.IsInvalid || clientHandle.DrainHandle != serverHandle) &&
          (serverHandle.DrainHandle == null || serverHandle.DrainHandle.IsInvalid || serverHandle.DrainHandle != clientHandle)) {
        throw new IOException ("drain handle not valid");
      }

      bool addRefSuccess = false;
			this.server_handle = serverHandle;
      this.server_handle.DangerousAddRef(ref addRefSuccess);
      this.server_handle.DisposalHandle.DangerousAddRef(ref addRefSuccess);
			this.client_handle = clientHandle;
      this.client_handle.DangerousAddRef(ref addRefSuccess);
      this.client_handle.DisposalHandle.DangerousAddRef(ref addRefSuccess);

      if (clientHandle.DrainHandle != null) {
        drain_handle = clientHandle.DrainHandle;
      } else {
        drain_handle = serverHandle.DrainHandle;
      }
		}

    SafePipeHandle drain_handle;
		SafePipeHandle server_handle, client_handle;

		public override SafePipeHandle Handle {
			get { return server_handle; }
		}

		public SafePipeHandle ClientHandle {
			get { return client_handle; }
		}

    public string GetClientHandleAsString() {
      StringBuilder result = new StringBuilder();
      result.Append(this.ClientHandle.DangerousGetHandle().ToInt64().ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
      result.Append(",");
      result.Append(this.ClientHandle.DisposalHandle.DangerousGetHandle().ToInt64().ToString(System.Globalization.NumberFormatInfo.InvariantInfo));

      // If client is writer, it needs a handle to the reader to synchronize via WaitForPipeDrain()
      if (this.ClientHandle.DrainHandle != null) {
        result.Append(",");
        result.Append(this.ClientHandle.DrainHandle.DangerousGetHandle().ToInt64().ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
      }
      return result.ToString();
    }

    bool disposedClientHandle = false;
		public void DisposeLocalCopyOfClientHandle ()
		{
      // make some low-cost attempt to compel the operating system 
      // to run whatever 'client process' is supposed to be
      // opening the client handle before disposing it
      System.Threading.Thread.Sleep(10);

      client_handle.DisposalHandle.DangerousRelease();

      // if server is not writer, we don't need to maintain a reference to main client handle
      if (this.drain_handle != this.client_handle) {
        client_handle.DangerousRelease();
      }
      disposedClientHandle = true;
		}

    public override void Dispose() {
      if (server_handle != null) {
        if (server_handle.DisposalHandle != null && !server_handle.DisposalHandle.IsClosed) {
          server_handle.DisposalHandle.DangerousRelease();
        }
        if (!server_handle.IsClosed) {
          server_handle.DangerousRelease();
        }
      }
      if (client_handle != null) {
        if (!disposedClientHandle && client_handle.DisposalHandle != null && !client_handle.DisposalHandle.IsClosed) {
          client_handle.DisposalHandle.DangerousRelease();
          disposedClientHandle = true;
        }
        if (!client_handle.IsClosed) {
          client_handle.DangerousRelease();
        }
      }
      drain_handle = null;
      server_handle = null;
      client_handle = null;
    }
	}

	abstract class UnixNamedPipe : IPipe
	{
		public abstract SafePipeHandle Handle { get; }

		public void WaitForPipeDrain ()
		{
			throw new NotImplementedException ();
		}
  
    public bool IsBroken() {
      return false;
    }
    public abstract void Dispose();

		public void EnsureTargetFile (string name)
		{
			if (!File.Exists (name)) {
				var error = Syscall.mknod (name, FilePermissions.S_IFIFO | FilePermissions.ALLPERMS, 0);
				if (error != 0)
					throw new IOException (String.Format ("Error on creating named pipe: error code {0}", error));
			}
		}

		protected void ValidateOptions (PipeOptions options, PipeTransmissionMode mode)
		{
			if ((options & PipeOptions.WriteThrough) != 0)
				throw new NotImplementedException ("WriteThrough is not supported");

			if ((mode & PipeTransmissionMode.Message) != 0)
				throw new NotImplementedException ("Message transmission mode is not supported");
			if ((options & PipeOptions.Asynchronous) != 0) // FIXME: use O_NONBLOCK?
				throw new NotImplementedException ("Asynchronous pipe mode is not supported");
		}
		
		protected string RightsToAccess (PipeAccessRights rights)
		{
			string access = null;
			if ((rights & PipeAccessRights.ReadData) != 0) {
				if ((rights & PipeAccessRights.WriteData) != 0)
					access = "r+";
				else
					access = "r";
			}
			else if ((rights & PipeAccessRights.WriteData) != 0)
				access = "w";
			else
				throw new InvalidOperationException ("The pipe must be opened to either read or write");

			return access;
		}
		
		protected FileAccess RightsToFileAccess (PipeAccessRights rights)
		{
			if ((rights & PipeAccessRights.ReadData) != 0) {
				if ((rights & PipeAccessRights.WriteData) != 0)
					return FileAccess.ReadWrite;
				else
					return FileAccess.Read;
			}
			else if ((rights & PipeAccessRights.WriteData) != 0)
				return FileAccess.Write;
			else
				throw new InvalidOperationException ("The pipe must be opened to either read or write");
		}
	}

	class UnixNamedPipeClient : UnixNamedPipe, INamedPipeClient
	{
		// .ctor with existing handle
		public UnixNamedPipeClient (NamedPipeClientStream owner, SafePipeHandle safePipeHandle)
		{
			this.owner = owner;
			this.handle = safePipeHandle;
			// FIXME: dunno how is_async could be filled.
		}

		// .ctor without handle - create new
		public UnixNamedPipeClient (NamedPipeClientStream owner, string serverName, string pipeName,
		                             PipeAccessRights desiredAccessRights, PipeOptions options, HandleInheritability inheritability)
		{
			this.owner = owner;

			if (serverName != "." && !Dns.GetHostEntry (serverName).AddressList.Contains (IPAddress.Loopback))
				throw new NotImplementedException ("Unix fifo does not support remote server connection");
			var name = Path.Combine ("/var/tmp/", pipeName);
			EnsureTargetFile (name);
			
			RightsToAccess (desiredAccessRights);
			
			ValidateOptions (options, owner.TransmissionMode);
			
			// FIXME: handle inheritability

			opener = delegate {
				var fs = new FileStream (name, FileMode.Open, RightsToFileAccess (desiredAccessRights), FileShare.ReadWrite);
				//owner.Stream = fs;
				handle = new SafePipeHandle (fs.Handle, false);
			};
		}

		NamedPipeClientStream owner;
		SafePipeHandle handle;
		Action opener;

		public override SafePipeHandle Handle {
			get { return handle; }
		}

		public void Connect ()
		{
			if (owner.IsConnected)
				throw new InvalidOperationException ("The named pipe is already connected");

			opener ();
		}

		public void Connect (int timeout)
		{
			AutoResetEvent waitHandle = new AutoResetEvent (false);
			opener.BeginInvoke (delegate (IAsyncResult result) {
				opener.EndInvoke (result);
				waitHandle.Set ();
				}, null);
			if (!waitHandle.WaitOne (TimeSpan.FromMilliseconds (timeout)))
				throw new TimeoutException ();
		}

		public bool IsAsync {
			get { return false; }
		}

		public int NumberOfServerInstances {
			get { throw new NotImplementedException (); }
		}

    public override void Dispose() {
      if (handle != null) {
        handle.Dispose();
        handle = null;
      }
    }
	}

	class UnixNamedPipeServer : UnixNamedPipe, INamedPipeServer
	{
		//NamedPipeServerStream owner;

		// .ctor with existing handle
		public UnixNamedPipeServer (NamedPipeServerStream owner, SafePipeHandle safePipeHandle)
		{
			this.handle = safePipeHandle;
			//this.owner = owner;
		}

		// .ctor without handle - create new
		public UnixNamedPipeServer (NamedPipeServerStream owner, string pipeName, int maxNumberOfServerInstances,
		                             PipeTransmissionMode transmissionMode, PipeAccessRights rights, PipeOptions options,
		                            int inBufferSize, int outBufferSize, HandleInheritability inheritability)
		{
			string name = Path.Combine ("/var/tmp/", pipeName);
			EnsureTargetFile (name);

			RightsToAccess (rights);

			ValidateOptions (options, owner.TransmissionMode);

			// FIXME: maxNumberOfServerInstances, modes, sizes, handle inheritability
			
			var fs = new FileStream (name, FileMode.Open, RightsToFileAccess (rights), FileShare.ReadWrite);
			handle = new SafePipeHandle (fs.Handle, false);
			//owner.Stream = fs;
			should_close_handle = true;
		}

		SafePipeHandle handle;
		bool should_close_handle;

		public override SafePipeHandle Handle {
			get { return handle; }
		}

		public void Disconnect ()
		{
			if (should_close_handle)
				Syscall.fclose (handle.DangerousGetHandle ());
		}

		public void WaitForConnection ()
		{
			// FIXME: what can I do here?
		}
    
    public override void Dispose() {
      if (handle != null) {
        handle.Dispose();
        handle = null;
      }
    }
	}
}

#endif
