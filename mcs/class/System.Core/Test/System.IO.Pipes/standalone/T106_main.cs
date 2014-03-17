using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T106_Server_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T106_Server_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server stream, 
 					invokes the client application specified on the commandline, 
 					sends several lines of text across the anonymous pipe stream,
 					awaiting pipe drain between calls for synchronization,
 					then exits";
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
			using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
			using (AnonymousPipeServerStream syncServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
			{
				_log.Test("Launching pipe client");
				pipeClient.AddArgument("/inHandle:", pipeServer.GetClientHandleAsString());
				pipeClient.AddArgument("/outHandle:", syncServer.GetClientHandleAsString());
				pipeClient.Launch();
	
				_log.Test("Disposing local copy of client handles");
				pipeServer.DisposeLocalCopyOfClientHandle();
				syncServer.DisposeLocalCopyOfClientHandle();

				ContextSwitch();

				_log.Test("Setting up stream tools");
				using (PipeWriter writer = new PipeWriter(pipeServer)) 
				using (PipeReader reader = new PipeReader(syncServer))
				{
					_log.Test("Begin Synchronization with client");
					writer.WriteLine("PIPE SERVER STARTED");
					_log.Test("Awaiting response from client");
					ContextSwitch();
					string result = reader.ReadLine();
					_log.Test("Response received '{0}'", result);

					if (result != "PIPE CLIENT STARTED") {
						_log.Error("Expected sync message from server of 'PIPE CLIENT STARTED' but received '{0}'", result);
						return;
					}
					_log.Test("Synchronization with client completed");
					ContextNatural();

					_log.Test("Sending message 1");
					writer.WriteLine("Message 1 sent from anonymous pipe server stream");
					_log.Info("Wait begins at timestamp {0:O}", DateTime.Now);
					pipeServer.WaitForPipeDrain();
					_log.Test("Sending message 2");
					writer.WriteLine("Message 2 sent from anonymous pipe server stream");
					_log.Info("Wait begins at timestamp {0:O}", DateTime.Now);
					pipeServer.WaitForPipeDrain();
					_log.Test("Sending message 3");
					writer.WriteLine("Message 3 sent from anonymous pipe server stream");
					_log.Info("Wait begins at timestamp {0:O}", DateTime.Now);
					pipeServer.WaitForPipeDrain();
					_log.Test("Sending message 4");
					writer.WriteLine("Message 4 sent from anonymous pipe server stream");
					_log.Info("Wait begins at timestamp {0:O}", DateTime.Now);
					pipeServer.WaitForPipeDrain();
					_log.Test("Sending message 5");
					writer.WriteLine("Message 5 sent from anonymous pipe server stream");
					_log.Info("Wait begins at timestamp {0:O}", DateTime.Now);
					pipeServer.WaitForPipeDrain();
					_log.Test("Disposing stream writer");
 			
					ContextEnroll();
				}

				_log.Test("Message sending completed");
			}
			_log.Test("Disposed anonymous pipe server stream");

			_log.Test("Awaiting pipe client exit");
			ContextSwitch();
			pipeClient.WaitForExit();
			_log.Test("Pipe client exited");
		}
	}
}
