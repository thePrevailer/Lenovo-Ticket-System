using Microsoft.UI.Xaml.Controls;

namespace LenovoSmartFix.UI.Services;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame) => _frame = frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        _frame?.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
