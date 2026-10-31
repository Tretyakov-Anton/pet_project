using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using WordTemplates.Models;
using WordTemplates.Services;
using WordTemplates.ViewModels;
using WordTemplates.Views;

namespace WordTemplates;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private readonly bool _runByAvaloniaRiderPlugin;

    public App()
    {
        var args = Environment.GetCommandLineArgs();
        _runByAvaloniaRiderPlugin = args[0].Contains("Avalonia.Designer.HostApp.dll");

        ServiceCollection services = new();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = mainWindowViewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        services.AddSingleton<IStorageProvider>(_ =>
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow!.StorageProvider);

        services.AddSingleton<TopLevel>(_ => TopLevel.GetTopLevel(
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow!)!);

        services.AddSingleton<IManagedNotificationManager>(p =>
            new WindowNotificationManager(p.GetRequiredService<TopLevel>())
            {
                MaxItems = 3, Position = NotificationPosition.BottomRight
            });

        services.AddSingleton<IDocumentProcessor, DocumentProcessor>();

        services.AddSingleton<TemplateData>(new TemplateData());

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<VariablesViewModel>();
        services.AddSingleton<ElementsViewModel>();
        services.AddSingleton<GroupsViewModel>();
    }
}

public class LazyService<T>(IServiceProvider serviceProvider)
    : Lazy<T>(serviceProvider.GetRequiredService<T>) where T : class;