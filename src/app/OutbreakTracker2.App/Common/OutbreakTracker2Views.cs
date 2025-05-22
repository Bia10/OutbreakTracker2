using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OutbreakTracker2.App.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OutbreakTracker2.App.Common;

public class OutbreakTracker2Views
{
    private readonly Dictionary<Type, Type> _vmToViewMap = [];

    public OutbreakTracker2Views AddView<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(ServiceCollection services)
        where TView : ContentControl
        where TViewModel : ObservableObject
    {
        Type viewType = typeof(TView);
        Type viewModelType = typeof(TViewModel);

        _vmToViewMap.Add(viewModelType, viewType);

        if (viewModelType.IsAssignableTo(typeof(PageBase)))
            services.AddSingleton(typeof(PageBase), viewModelType);
        else
            services.AddSingleton(viewModelType);

        return this;
    }

    public bool TryCreateView(IServiceProvider? provider, Type viewModelType, [NotNullWhen(true)] out Control? view)
    {
        object viewModel = provider.GetRequiredService(viewModelType);

        return TryCreateView(viewModel, out view);
    }

    public bool TryCreateView(object? viewModel, [NotNullWhen(true)] out Control? view)
    {
        view = null;

        if (viewModel is null)
            return false;

        Type viewModelType = viewModel.GetType();

        if (_vmToViewMap.TryGetValue(viewModelType, out Type? viewType))
        {
            view = Activator.CreateInstance(viewType) as Control;

            if (view is not null)
                view.DataContext = viewModel;
        }

        return view is not null;
    }

    public Control CreateView<TViewModel>(IServiceProvider? provider) where TViewModel : ObservableObject
    {
        Type viewModelType = typeof(TViewModel);

        if (TryCreateView(provider, viewModelType, out Control? view))
            return view;

        throw new InvalidOperationException();
    }
}
