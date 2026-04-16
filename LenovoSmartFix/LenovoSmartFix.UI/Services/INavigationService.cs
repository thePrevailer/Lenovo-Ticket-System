namespace LenovoSmartFix.UI.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void NavigateTo(Type pageType, object? parameter = null);
    void GoBack();
}
