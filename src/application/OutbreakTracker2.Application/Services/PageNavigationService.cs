using OutbreakTracker2.Application.Pages;
using System;

namespace OutbreakTracker2.Application.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>() where T : PageBase
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}
