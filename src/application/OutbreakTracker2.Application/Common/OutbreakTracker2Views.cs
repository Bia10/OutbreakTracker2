using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OutbreakTracker2.Application.Pages;

namespace OutbreakTracker2.Application.Common;

public class OutbreakTracker2Views
{
    // Stores a compile-time factory per ViewModel type — no Activator.CreateInstance needed.
    // The static () => new TView() lambda is a pure newobj IL instruction, fully AOT-safe.
    private readonly Dictionary<Type, Func<Control>> _vmToViewFactoryMap = [];

    public OutbreakTracker2Views AddView<
        TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel
    >(ServiceCollection services)
        where TView : ContentControl, new()
        where TViewModel : ObservableObject
    {
        Type viewModelType = typeof(TViewModel);

        _vmToViewFactoryMap[viewModelType] = static () => new TView();

        if (viewModelType.IsAssignableTo(typeof(PageBase)))
            services.AddSingleton(typeof(PageBase), viewModelType);
        else
            services.AddSingleton(viewModelType);

        return this;
    }

    public bool TryCreateView(IServiceProvider? provider, Type viewModelType, [NotNullWhen(true)] out Control? view)
    {
        view = null;

        if (provider is null)
            return false;

        object viewModel = provider.GetRequiredService(viewModelType);
        return TryCreateView(viewModel, out view);
    }

    public bool TryCreateView(object? viewModel, [NotNullWhen(true)] out Control? view)
    {
        view = null;

        if (viewModel is null)
            return false;

        Type viewModelType = viewModel.GetType();

        if (_vmToViewFactoryMap.TryGetValue(viewModelType, out Func<Control>? factory))
        {
            view = factory();
            view.DataContext = viewModel;
        }

        return view is not null;
    }

    public Control CreateView<TViewModel>(IServiceProvider? provider)
        where TViewModel : ObservableObject
    {
        Type viewModelType = typeof(TViewModel);

        if (TryCreateView(provider, viewModelType, out Control? view))
            return view;

        throw new InvalidOperationException();
    }
}
