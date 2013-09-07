using System;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace MonoTests.System.IO.Pipes
{
  public abstract class PipeTestWrapper
  {
    private TimeSpan _timeoutDuration;
    protected readonly TestLogger _log;
    private bool _executionError = false;
    private string[] _executionArgs = null;
    private StreamWriter _outFile = null;

    /// <summary>
    /// Instantiate pipe wrapper with timeout duration of 1 minute (the default)
    /// </summary>
    public PipeTestWrapper() :
      this (new TimeSpan(0, 0, 1, 0, 0))
    {
    }
    /// <summary>
    /// Instantiate pipe test wrapper with configurable timeout duration
    /// </summary>
    public PipeTestWrapper(TimeSpan timeoutDuration) {
      _timeoutDuration = timeoutDuration;
      _log = new TestLogger(this);
    }

    public class TestLogger {
      private PipeTestWrapper _parent;
      public TestLogger(PipeTestWrapper parent) {
        _parent = parent;
      }
      public void Error(string msg) {
        Error(null, msg);
      }
      public void Error(string msg, params object[] format)
      {
        this.Error(string.Format(msg, format));
      }
      public void Error(Exception exc, string msg, params object[] format)
      {
        Error(exc, string.Format(msg, format));
      }
      public void Error(Exception exc, string msg)
      {
        WriteError(exc, msg);
        _parent._executionError = true;
      }
      public void Info(string msg) {
        WriteInfo(msg);
      }
        
      public void Info(string msg, params object[] format) {
        Info(string.Format(msg, format));
      }

      private const string INDENT = "    ";
      private string GetHeader(string type)
      {
        return string.Format("{2}[{0}]:{1}:", _parent.TestName, DateTime.Now, type);
      }
      private string Format(string msg) {
        return msg.Replace(Environment.NewLine, Environment.NewLine + INDENT);
      }
      private void Write(string msg) {
        Console.WriteLine(msg);

        if (_parent._outFile != null) {
          _parent._outFile.WriteLine(msg);
        }
      }
      private void WriteError(Exception exc, string msg)
      {
        lock (this) {
          //Console.ForegroundColor = ConsoleColor.Red;
          bool indent = false;
          if (msg != null) {
            Write(GetHeader("ERROR") + Format(msg));
            indent = true;
          }
          if (exc != null) {
            var excMsg = Format(exc.ToString());
            if (indent) {
              excMsg = INDENT + excMsg;
            } else {
              excMsg = GetHeader("ERROR") + excMsg;
            }
            Write(excMsg);
          }
          //Console.ResetColor();
        }
      }
      private void WriteInfo(string msg)
      {
        lock (this) {
          //Console.ForegroundColor = ConsoleColor.White;
          Write(GetHeader("INFO") + Format(msg));
          //Console.ResetColor();
        }
      }
    }

    protected abstract void DoTest(string[] arguments);

    private void ExecuteInternal()
    {
      try
      {
        DoTest(_executionArgs);
      }
      catch (Exception eError)
      {
        _log.Error(eError, "Test failed with unhandled exception");
      }
    }

    public virtual string TestName
    {
      get 
      {
        return this.GetType().Name;
      }
    }
    public abstract string TestDescription
    {
      get;
    }

    public string FormatArguments(params string[] clientArgs) {
      var result = string.Join(" ", clientArgs);

      _log.Info("Generated argument list:{0}{1}", Environment.NewLine, result);

      return result;
    }

    public int Execute(string[] arguments)
    {
      try 
      {
        _log.Info("Test {0} started at {1}", this.TestName, DateTime.Now);

        string outputFilename = null;
        foreach (string arg in arguments) {
          if (arg.StartsWith("/outfile:")) {
            outputFilename = arg.Substring("/outfile:".Length);
          }
        }
        FileStream fileStream = null;
        try {
        if (outputFilename != null) {
            _log.Info("Opening output file '{0}'", outputFilename);
            fileStream = new FileStream(outputFilename, FileMode.Create);
            _outFile = new StreamWriter(fileStream);
            _outFile.AutoFlush = true;
          } else { 
            _log.Info("No output file is being generated, use /outfile:<filename> to generate output");
          }

          this._executionArgs = arguments;
          Stopwatch runTimer = Stopwatch.StartNew();

          _log.Info("Starting testing thread");

          Thread testThread = new Thread(new ThreadStart(this.ExecuteInternal));
          testThread.Start();

          while (testThread.IsAlive)
          {
            if (runTimer.Elapsed > this._timeoutDuration) {
              _log.Error("Test timeout '{0}' has been exceeded, terminating", this._timeoutDuration);
              return 2;
            }

            Thread.Sleep(0);
          }
          runTimer.Stop();

          if (this._executionError) {
            _log.Error("Test failed in {0}", runTimer.Elapsed);
            return 1;
          }

          _log.Info("Test Succeeded in {0}", runTimer.Elapsed);
          return 0;
        }
        finally {
          if (_outFile != null) {
            _outFile.Dispose();
            _outFile = null;
          }
          if (fileStream != null) {
            fileStream.Dispose();
          }
        }
      }
      catch (Exception eError)
      {
        _log.Error(eError, "Test runner failed");
        return 3;
      }
    }
  }
}
