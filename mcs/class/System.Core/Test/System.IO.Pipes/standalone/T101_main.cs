using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
	public class T101_ServerDispose_Main : PipeTestWrapper
	{
		public static int Main(string[] arguments) {
			return new T101_ServerDispose_Main().Execute(arguments);
		}

		public override string TestDescription {
			get {
				return @"Creates an anonymous pipe server stream and disposes it";
			}
		}

		protected override void DoTest(string[] arguments)
		{
			CreatedContextSwitchTool();
			_log.Test("Creating anonymous pipe server");

			using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out))
			{
				_log.Test("Created pipe server stream");
			}

			_log.Test("Disposed pipe server stream");
		}
	}
}
