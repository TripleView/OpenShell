using System.ComponentModel;

namespace OpenShell.Dto;

public class BaseBinding : INotifyPropertyChanged
{
    private bool IsNotify = true;
    public void StopNotify()
    {
        this.IsNotify = false;
    }
    public void StartNotify()
    {
        this.IsNotify = true;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        if (IsNotify)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}