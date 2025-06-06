using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace OutbreakTracker2.Application.Common;

public class ViewLocator(OutbreakTracker2Views views) : IDataTemplate
{
    private readonly Dictionary<object, Control> _controlCache = [];

    public Control Build(object? param)
    {
        if (param is null)
            return CreateText("Data is null.");


        if (_controlCache.TryGetValue(param, out Control? control))
            return control;


        if (views.TryCreateView(param, out Control? view))
        {
            _controlCache.Add(param, view);

            return view;
        }

        return CreateText($"No View For {param.GetType().Name}.");
    }

    public bool Match(object? data) => data is ObservableObject;

    private static TextBlock CreateText(string text) => new()
        { Text = text };
}
