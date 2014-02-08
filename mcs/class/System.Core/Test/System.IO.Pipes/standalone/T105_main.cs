using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
  public class T105_Server_Main : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T105_Server_Main().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe server stream, 
                 invokes the client application specified on the commandline, 
                 sends several lines of text across the anonymous pipe stream,
                 exits";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      ProcessLauncher pipeClient = new ProcessLauncher(this, "/client:", arguments);
      if (pipeClient.ParseFailure) {
        _log.Error("Usage: /client:<clientexe>");
        _log.Error("  clientexe - exe that should be invoked with the anonymous pipe handle");
        return;
      }

      _log.Test("Creating anonymous pipe server stream");
      using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
      {
        pipeClient.AddArgument("/handle:", pipeServer.GetClientHandleAsString());
        pipeClient.Launch();

        pipeServer.DisposeLocalCopyOfClientHandle();

        _log.Test("Sending message");
        using (PipeWriter writer = new PipeWriter(pipeServer)) 
        {
          writer.WriteLine("Message 1 sent from anonymous pipe server stream");
          writer.WriteLine("Message 2 sent from anonymous pipe server stream");
          writer.WriteLine("Message 3 sent from anonymous pipe server stream");
          writer.WriteLine("Message 4 sent from anonymous pipe server stream");
          writer.WriteLine("Message 5 sent from anonymous pipe server stream");
        }

        _log.Test("Message sending completed");
      }
      _log.Test("Disposed anonymous pipe server stream");

      _log.Test("Awaiting pipe client exit");
      pipeClient.WaitForExit();
      _log.Test("Pipe client exited");
    }
  }
}
