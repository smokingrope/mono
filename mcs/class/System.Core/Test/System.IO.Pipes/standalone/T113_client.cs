using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
	public class T113_Client : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T113_Client().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous out pipe client stream from the handle passed on the commandline,
 					and generate an error if the provided handle actually exists.";
			}
		}

		protected override void DoTest(string[] arguments)
		{
			InheritedContextSwitchTool();
			string outHandle = null;
			foreach (string arg in arguments)
			{
				if (arg.StartsWith("/outHandle:")) {
					outHandle = arg.Substring("/outHandle:".Length);
				}
			}
			if (outHandle == null) {
				_log.Error("Usage: /outHandle:<pipehandle>");
				_log.Error("  outHandle - handle generated by GetClientHandleAsString() which will be read from");
				return;
			}

			_log.Test("Setting up streams");
			_log.Info("Received in handle {0}", outHandle);

			try {
				using (AnonymousPipeClientStream pipeClientOut = new AnonymousPipeClientStream(PipeDirection.Out, outHandle))
				{
					_log.Error("Never should reach this point");
				}
			} catch (IOException eError) {
				if (eError.Message != "Unable to load pipe with error code ERROR_INVALID_HANDLE") {
					_log.Error(eError, "Unexpected io exception");
				} else {
					_log.Test("Received expect exception");
				}
			}
			_log.Test("Done disposing pipe streams");
		}
	}
}
