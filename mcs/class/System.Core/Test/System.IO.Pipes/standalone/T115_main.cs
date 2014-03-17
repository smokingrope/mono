using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T115_Server_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T115_Server_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server in stream, 
 					invokes the client application specified on the commandline, 
 					and then waits for the client to terminate";
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
			using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None))
			{
				pipeClient.AddArgument("/outHandle:", pipeServer.GetClientHandleAsString());
				pipeClient.Launch();

				_log.Test("Disposing local copy of client handles");
				pipeServer.DisposeLocalCopyOfClientHandle();

				ContextSwitch();

				_log.Test("Awaiting pipe client exit");
				pipeClient.WaitForExit();
			}
			_log.Test("Finished disposing pipe streams");

			_log.Test("Pipe client exited");
		}
	}
}
