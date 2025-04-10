using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OpenShell.Dto;

public class BatchObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    public void AddRange(IEnumerable<T> items)
    {
        _suppressNotification = true;
        foreach (var item in items)
        {
            Add(item);
        }
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}
