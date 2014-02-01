using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
  public class T110_Client : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T110_Client().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe client stream from the handle passed on the commandline,
                 reads several lines of text and then exits";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      string inHandle = null, outHandle = null;
      foreach (string arg in arguments)
      {
        if (arg.StartsWith("/inHandle:")) {
          inHandle = arg.Substring("/inHandle:".Length);
        }
        if (arg.StartsWith("/outHandle:")) {
          outHandle = arg.Substring("/outHandle:".Length);
        }
      }
      if (inHandle == null || outHandle == null) {
        _log.Error("Usage: /inHandle:<pipehandle> /outHandle:<pipehandle>");
        _log.Error("  inHandle - handle generated by GetClientHandleAsString() which will be read from");
        _log.Error("  outHandle - handle generated by GetClientHandleAsString() which will be written to");
        return;
      }

      _log.Test("Setting up streams");
      _log.Info("Received in handle {0}", inHandle);
      _log.Info("Received out handle {0}", outHandle);

      using (AnonymousPipeClientStream pipeClientIn = new AnonymousPipeClientStream(PipeDirection.In, inHandle))
      using (AnonymousPipeClientStream pipeClientOut = new AnonymousPipeClientStream(PipeDirection.Out, outHandle)) 
      {
        _log.Test("Created out client");
        using (PipeWriter writer = new PipeWriter(pipeClientOut))
        using (PipeReader reader = new PipeReader(pipeClientIn)) 
        {
          Thread.Sleep(1000);
          _log.Test("Synchronizing with server");
          writer.WriteLine("PIPE CLIENT STARTED");
          _log.Test("Awaiting response from server");
          string result = reader.ReadLine();
          if (result != "PIPE SERVER STARTED") {
            _log.Error("Expected sync message from server of 'PIPE SERVER STARTED' but received '{0}'", result);
            return;
          }
          _log.Test("Synchronization with server completed with message '{0}'", result);
          _log.Test("Begin deisposing client");
        }
        _log.Test("Done dispose pipe tools");
      }
      _log.Test("Done disposing pipe streams");
    }
  }
}
