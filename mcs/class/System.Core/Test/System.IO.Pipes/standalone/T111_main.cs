using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
  public class T111_Server_Main : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T111_Server_Main().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe server stream, 
                 invokes the client application specified on the commandline, 
                 synchronizes with the client,
                 and then expects the client to terminate the pipe,
                 generating a 'pipe broken' exception";
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
      using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out))
      using (AnonymousPipeServerStream syncServer = new AnonymousPipeServerStream(PipeDirection.In))
      {
        pipeClient.AddArgument("/inHandle:", pipeServer.GetClientHandleAsString());
        pipeClient.AddArgument("/outHandle:", syncServer.GetClientHandleAsString());
        pipeClient.Launch();

        Thread.Sleep(2000);
        pipeServer.DisposeLocalCopyOfClientHandle();
        syncServer.DisposeLocalCopyOfClientHandle();

        _log.Test("Setting up stream tools");
        using (PipeWriter writer = new PipeWriter(pipeServer)) 
        using (PipeReader reader = new PipeReader(syncServer))
        {
          _log.Test("Begin Synchronization with client");
          writer.WriteLine("PIPE SERVER STARTED");
          _log.Test("Awaiting response from client");
          string result = reader.ReadLine();
          _log.Test("Response received '{0}'", result);

          if (result != "PIPE CLIENT STARTED") {
            _log.Error("Expected sync message from server of 'PIPE CLIENT STARTED' but received '{0}'", result);
            return;
          }
          _log.Test("Sleeping for 1 second just to allow client to finish synchronization");
          Thread.Sleep(1);

          _log.Test("Synchronization with client completed with message '{0}'", result);
        }
        _log.Test("Finished disposing pipe tools");
        pipeServer.Dispose();
        syncServer.Dispose();
        pipeServer.Dispose();
        syncServer.Dispose();
        pipeServer.Dispose();
        syncServer.Dispose();
      }
      _log.Test("Finished disposing pipe streams");

      _log.Test("Awaiting pipe client exit");
      pipeClient.WaitForExit();
      _log.Test("Pipe client exited");
    }
  }
}
