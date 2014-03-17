using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T107_Server_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T107_Server_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server stream, 
 					invokes the client application specified on the commandline, 
 					and receives several lines of text across the anonymous pipe stream";
			}
		}

		protected override void DoTest(string[] arguments)
		{
			CreatedContextSwitchTool();

			ProcessLauncher pipeClient = new ProcessLauncher(this, "/client:", arguments);
			if (pipeClient.ParseFailure) {
				_log.Error("Usage: /client:<clientexe>");
				_log.Error("  clientexe - exe that should be invoked with the anonymous pipe handle");
				return;
			}

			_log.Test("Creating anonymous pipe server stream");
			using (AnonymousPipeServerStream syncServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
			{
				_log.Test("Launching pipe client");
				pipeClient.AddArgument("/outHandle:", syncServer.GetClientHandleAsString());
				pipeClient.Launch();

				_log.Test("Disposing local copy of client handles");
				syncServer.DisposeLocalCopyOfClientHandle();

				ContextSwitch();

				_log.Test("Setting up stream tools");
				using (PipeReader reader = new PipeReader(syncServer))
				{
					_log.Test("Awaiting response from client");
					string result = reader.ReadLine();
					_log.Test("Response received");

					if (result != "PIPE CLIENT STARTED") {
						_log.Error("Expected sync message from server of 'PIPE CLIENT STARTED' but received '{0}'", result);
						return;
					} else {
						_log.Test("Received message '{0}'", result);
						_log.Test("Synchronization with client completed");
						ContextSwitch();

						result = reader.ReadLine();
						_log.Test("Received message from client '{0}'", result);
						
						result = reader.ReadLine();
						_log.Test("Received message from client '{0}'", result);

						result = reader.ReadLine();
						_log.Test("Received message from client '{0}'", result);
					}

					_log.Test("Message receiving completed");
				}
				_log.Test("Disposed anonymous pipe server stream");
	
				_log.Test("Awaiting pipe client exit");
				pipeClient.WaitForExit();
				_log.Test("Pipe client exited");
			}
		}
	}
}
