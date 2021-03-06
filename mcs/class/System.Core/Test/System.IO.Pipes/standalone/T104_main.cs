using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
	public class T104_ServerDispose_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T104_ServerDispose_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server stream, dispoe client handles, sets up a stream writer, writes to it, then disposes everything. This was able to reproduce an issue with a double dispose of pipe handles in an unsafe manner _wapi_handle_unref_full. The error was caused by the stream underlying the AnonymousPipeServerStream capturing the actual file descriptor instead of a safe handle to the file descriptor, and then trying to dispose it during finalization (which only happens after it is written to)";
			}
		}

		protected override void DoTest(string[] arguments)
		{
			CreatedContextSwitchTool();
			using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out))
			{
				_log.Test("Created pipe server stream");

				pipeServer.DisposeLocalCopyOfClientHandle();

				_log.Test("Local copy of client handle disposed");

				using (PipeWriter writer = new PipeWriter(pipeServer)) {
					try {
						_log.Test("Ready to attempt write to pipe");
						writer.WriteLine("TEST");         
						throw new InvalidOperationException("Expected a broken pipe exception to be generated before reaching this point");
					} catch (IOException eError) {
						if (eError.Message == "The pipe is broken") {
							_log.Test("Received expected IO Exception because no clients are listening for messages.");
						} else {
							_log.Error("Exception message: {0}", eError.Message);
							throw;
						}
					}
				}

				_log.Test("Disposed stream writer");
			}

			_log.Test("Disposed pipe server stream");
		}
	}
}

