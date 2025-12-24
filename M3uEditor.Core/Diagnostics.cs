namespace M3uEditor.Core;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record TextSpan(int LineIndex, int Start, int Length);

public sealed record Diagnostic(DiagnosticSeverity Severity, string Code, string Message, TextSpan Span);
