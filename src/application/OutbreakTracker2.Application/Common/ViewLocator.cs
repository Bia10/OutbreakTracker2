using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.Application.Common;

public sealed class ViewLocator(OutbreakTracker2Views views) : IDataTemplate
{
    // ConditionalWeakTable lets the GC collect both the ViewModel key and the Control value
    // when the ViewModel is no longer referenced — prevents leaks for transient VMs.
    private readonly ConditionalWeakTable<object, Control> _controlCache = new();

    public Control Build(object? param)
    {
        if (param is null)
            return CreateText("Data is null.");

        if (_controlCache.TryGetValue(param, out Control? control))
            return control;

        if (views.TryCreateView(param, out Control? view))
        {
            _controlCache.AddOrUpdate(param, view);
            return view;
        }

        return CreateText($"No View For {param.GetType().Name}.");
    }

    public bool Match(object? data) => data is ObservableObject;

    private static TextBlock CreateText(string text) => new() { Text = text };
}
