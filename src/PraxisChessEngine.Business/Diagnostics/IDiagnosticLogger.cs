using System;

namespace PraxisChessEngine.Business.Diagnostics;

public interface IDiagnosticLogger
{
    void Log(string message);

    void Log(Exception exception, string message);
}

public sealed class NullDiagnosticLogger : IDiagnosticLogger
{
    public void Log(string message)
    {
    }

    public void Log(Exception exception, string message)
    {
    }
}
