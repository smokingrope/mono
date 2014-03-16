using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace MonoTests.System.IO.Pipes
{
  public class T106_Client : PipeTestWrapper
  {
    public static int Main(string[] arguments) {
      return new T106_Client().Execute(arguments);
    }

    public override string TestDescription {
      get {
        return @"Creates an anonymous pipe client stream from the handle passed on the commandline,
                 reads several lines of text and then exits";
      }
    }

    protected override void DoTest(string[] arguments)
    {
      InheritedContextSwitchTool();

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

      using (AnonymousPipeClientStream pipeClientIn = new AnonymousPipeClientStream(PipeDirection.In, inHandle)) {
        _log.Test("Created in client");
      using (AnonymousPipeClientStream pipeClientOut = new AnonymousPipeClientStream(PipeDirection.Out, outHandle)) {
        _log.Test("Created out client");
      using (PipeWriter writer = new PipeWriter(pipeClientOut))
      using (PipeReader reader = new PipeReader(pipeClientIn)) 
      {
        _log.Test("Synchronizing with server");
        writer.WriteLine("PIPE CLIENT STARTED");
        _log.Test("Awaiting response from server");
        ContextSwitch();

        string result = reader.ReadLine();

        _log.Test("Response received '{0}'", result);

        if (result != "PIPE SERVER STARTED") {
          _log.Error("Expected sync message from server of 'PIPE SERVER STARTED' but received '{0}'", result);
          return;
        }
        ContextSwitch();
        ContextNatural();

        _log.Info("Begin Reading stream at {0:O}", DateTime.Now);
        result = reader.ReadLine();
        _log.Info("Receive completed at {0:O}", DateTime.Now);
        _log.Test("Received message 1: '{0}'", result);
        _log.Info("Start Reading stream at {0:O}", DateTime.Now);
        result = reader.ReadLine();
        _log.Info("Receive completed at {0:O}", DateTime.Now);
        _log.Test("Received message 2: '{0}'", result);
        _log.Info("Start Reading stream at {0:O}", DateTime.Now);
        result = reader.ReadLine();
        _log.Info("Receive completed at {0:O}", DateTime.Now);
        _log.Test("Received message 3: '{0}'", result);
        _log.Info("Start Reading stream at {0:O}", DateTime.Now);
        result = reader.ReadLine();
        _log.Info("Receive completed at {0:O}", DateTime.Now);
        _log.Test("Received message 4: '{0}'", result);
        _log.Info("Start Reading stream at {0:O}", DateTime.Now);
        result = reader.ReadLine();
        _log.Info("Receive completed at {0:O}", DateTime.Now);
        _log.Test("Received message 5: '{0}'", result);

         ContextEnroll();
         ContextSwitch();
        _log.Info("Sleeping for a while so that parent thread can cleanup first");
      }}}
    }
  }
}
