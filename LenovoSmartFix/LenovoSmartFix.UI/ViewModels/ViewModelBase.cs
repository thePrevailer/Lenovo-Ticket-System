using CommunityToolkit.Mvvm.ComponentModel;

namespace LenovoSmartFix.UI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        protected set => SetProperty(ref _statusMessage, value);
    }
}
