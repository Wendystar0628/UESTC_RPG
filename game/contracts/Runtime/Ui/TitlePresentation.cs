namespace Campus.Contracts;

/// <summary>Title-only scenery, switched beneath the application's loading screen.</summary>
public interface IAppTitlePresentation
{
    void SetVisible(bool visible);
}
