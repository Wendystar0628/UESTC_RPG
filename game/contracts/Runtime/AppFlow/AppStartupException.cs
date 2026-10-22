namespace Campus.Contracts.AppFlow;

/// <summary>A terminal preparation failure retaining its original diagnostic cause.</summary>
public sealed class AppStartupException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
