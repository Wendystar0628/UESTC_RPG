namespace Campus.Contracts.AppFlow;

/// <summary>
/// Main-thread delivery of an unrecoverable application failure to the production stage.
/// The receiver retains the original exception and owns shutdown. Callers finish their own
/// cleanup after reporting; they must not await application shutdown from their owned work.
/// </summary>
public interface IAppFailure
{
    void Report([FailureHandler("ENG-ARCH")] Exception error);
}
