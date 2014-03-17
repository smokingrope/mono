using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T108_Server_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T108_Server_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server stream, 
 					invokes the client application specified on the commandline, 
 					sends a message across that pipe and waits for the client to consume it
 					before terminating.";
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
			using (AnonymousPipeServerStream syncServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
			{
				pipeClient.AddArgument("/inHandle:", syncServer.GetClientHandleAsString());
				pipeClient.Launch();

				syncServer.DisposeLocalCopyOfClientHandle();

				ContextSwitch();

				_log.Test("Setting up stream tools");
				using (var writer = new PipeWriter(syncServer))
				{
					_log.Test("Sending data to client");
					writer.WriteLine("Line 1");
					writer.WriteLine("Line 2");

					ContextNatural();

					_log.Test("Message sending completed, awaiting client drain completion");
					syncServer.WaitForPipeDrain();

					ContextEnroll();
					_log.Test("Pipe drain complete");
				}
				_log.Test("Disposed anonymous pipe server stream");

				_log.Test("Awaiting pipe client exit");
				pipeClient.WaitForExit();
				_log.Test("Pipe client exited");
			}
		}
	}
}
