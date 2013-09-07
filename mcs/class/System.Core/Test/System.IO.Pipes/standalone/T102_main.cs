using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
  public class T102_ServerDispose_Main : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T102_ServerDispose_Main().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe server stream, dispoe client handles, then disposes the rest";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      _log.Info("Working directory {0}", Environment.CurrentDirectory);

      using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out))
      {
        _log.Info("Created pipe server stream");

        pipeServer.DisposeLocalCopyOfClientHandle();

        _log.Info("Local copy of client handle disposed");
      }
      _log.Info("Disposed pipe server stream");
    }
  }
}
