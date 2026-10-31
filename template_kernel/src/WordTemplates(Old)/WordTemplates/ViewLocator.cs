using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WordTemplates;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        Type vmType = data!.GetType();
        var name = vmType.FullName!
            .Replace("ViewModel", "View")
            .Replace("VM", "View");
        var viewType = Type.GetType(name);

        if (viewType != null)
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}

public class ViewModelBase : ObservableObject;