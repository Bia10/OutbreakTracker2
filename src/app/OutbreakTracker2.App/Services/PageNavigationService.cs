using OutbreakTracker2.App.Pages;
using System;

namespace OutbreakTracker2.App.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>() where T : PageBase
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}
