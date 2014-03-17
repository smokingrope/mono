using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T111_Server_Client : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T111_Server_Client().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe client stream from the handle passed on the commandline, 
 					synchronizes with the server,
 					and then expects the server to terminate the pipe,
 					generating a 'pipe broken' exception when attempting to write to it afterwards";
			}
		}

		protected override void DoTest(string[] arguments)
		{
			InheritedContextSwitchTool();
			string inHandle = null, outHandle = null;
			foreach (string arg in arguments)
			{
				if (arg.StartsWith("/inHandle:")) {
					inHandle = arg.Substring("/inHandle:".Length);
				}
				if (arg.StartsWith("/outHandle:")) {
					outHandle = arg.Substring("/outHandle:".Length);
				}
			}
			if (inHandle == null || outHandle == null) {
				_log.Error("Usage: /inHandle:<pipehandle> /outHandle:<pipehandle>");
				_log.Error("  inHandle - handle generated by GetClientHandleAsString() which will be read from");
				_log.Error("  outHandle - handle generated by GetClientHandleAsString() which will be written to");
				return;
			}

			_log.Test("Setting up streams");
			_log.Info("Received in handle {0}", inHandle);
			_log.Info("Received out handle {0}", outHandle);

			using (AnonymousPipeClientStream pipeClientIn = new AnonymousPipeClientStream(PipeDirection.In, inHandle))
			using (AnonymousPipeClientStream pipeClientOut = new AnonymousPipeClientStream(PipeDirection.Out, outHandle))
			{
				_log.Test("Finished Creating Client");
				using (PipeWriter writer = new PipeWriter(pipeClientOut))
				using (PipeReader reader = new PipeReader(pipeClientIn))
				{
					_log.Test("Begin Synchronization with server");
					writer.WriteLine("PIPE CLIENT STARTED");
					ContextSwitch();
					_log.Test("Awaiting response from server");
					string result = reader.ReadLine();
					_log.Test("Response received '{0}'", result);

					if (result != "PIPE SERVER STARTED") {
						_log.Error("Expected sync message from server of 'PIPE SERVER STARTED' but received '{0}'", result);
						return;
					}
					_log.Test("Synchronization with server completed with message '{0}'", result);
	
					_log.Test("Waiting for server to dispose pipe stream");
					ContextSwitch(); 
 
					_log.Test("Attempting to send message on broken pipe");

					try 
					{
						writer.WriteLine("An exception should be thrown here");
						throw new InvalidOperationException("Expected an io exception before reaching this point");
					}
					catch (IOException eError) 
					{
						if (eError.Message != "The pipe is broken") {
							_log.Error("Expected specific io exception 'pipe broken' but got{0}{1}", Environment.NewLine, eError.ToString());
							return;
						}
						_log.Test("Got the expected 'pipe broken' exception");
					}
				}
				_log.Test("Finished disposing pipe tools");
			}
			_log.Test("Finished disposing pipe streams");

			_log.Test("Pipe client exiting");
		}
	}
}
