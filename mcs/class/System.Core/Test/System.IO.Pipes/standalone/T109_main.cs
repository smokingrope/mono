using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
  public class T109_Server_Main : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T109_Server_Main().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe server stream, 
                 invokes the client application specified on the commandline, 
                 synchronizes with the client,
                 reads several messages from the client and exits";
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
      using (AnonymousPipeServerStream syncServer = new AnonymousPipeServerStream(PipeDirection.Out))
      using (AnonymousPipeServerStream readServer = new AnonymousPipeServerStream(PipeDirection.In))
      {
        pipeClient.AddArgument("/inHandle:", syncServer.GetClientHandleAsString());
        pipeClient.AddArgument("/outHandle:", readServer.GetClientHandleAsString());
        pipeClient.Launch();

        syncServer.DisposeLocalCopyOfClientHandle();
        readServer.DisposeLocalCopyOfClientHandle();

        _log.Test("Setting up stream tools");

        using (var reader = new PipeReader(readServer))
        using (var writer = new PipeWriter(syncServer))
        {
          _log.Test("Begin synchronization with client");
          writer.WriteLine("SERVER STARTED");
          string result = reader.ReadLine();
          if (result != "CLIENT STARTED") {
            _log.Error("UNEXPECTED MESSAGE WHILE STARTING PIPE CLIENT '{0}'", result);
            return;
          }
          _log.Test("Synchronization with client completed with message '{0}'", result);

          for (int i = 0; i < 4; ++i) {
            _log.Info("Begin awaiting message {0} at {1:O}", i, DateTime.Now);
            Thread.Sleep(200);
            result = reader.ReadLine();
            _log.Info("Awaiting message {0} completed at {1:O}", i, DateTime.Now);
            _log.Test("Received message {0} from client", result);
          }

          _log.Test("Receiving completed, client begin shutdown");
        }
        _log.Test("Disposed stream utilities");
      }
      _log.Test("Disposed anonymous pipe server stream");

      pipeClient.WaitForExit();
      _log.Test("Pipe client exited");
    }
  }
}
