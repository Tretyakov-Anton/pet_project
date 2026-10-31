using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using DocumentTemplates.ViewModels;
using DocumentTemplates.Theme;
using Avalonia.Styling;

namespace DocumentTemplates.Views
{
    public sealed class MessageDialog
    {
        private readonly Window _owner;
        private readonly string _title;
        private readonly string _message;

        public MessageDialog(Window owner, string title, string message)
        {
            _owner = owner;
            _title = title;
            _message = message;
        }

        public async System.Threading.Tasks.Task ShowAsync()
        {
            var dialog = new Window
            {
                Title = _title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = _message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(0,12,0,0) }
                    }
                }
            };
            if (_owner is not null)
                dialog.Icon = _owner.Icon;

            // Close on OK
            if (dialog.Content is StackPanel sp && sp.Children.LastOrDefault() is Button btn)
            {
                btn.Click += (_, _) => dialog.Close();
            }
            await dialog.ShowDialog(_owner!);
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateThemeToggleButtonContent();
        }

        private void UpdateThemeToggleButtonContent()
        {
            if (Application.Current?.RequestedThemeVariant == ThemeVariant.Light)
            {
                ThemeToggleButton.Content = "Тёмная тема";
                if (ThemeIconToggleButton != null)
                    ToolTip.SetTip(ThemeIconToggleButton, "Переключить на тёмную тему");
            }
            else
            {
                ThemeToggleButton.Content = "Светлая тема";
                if (ThemeIconToggleButton != null)
                    ToolTip.SetTip(ThemeIconToggleButton, "Переключить на светлую тему");
            }
        }

        private void OnToggleThemeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var toLight = Application.Current?.RequestedThemeVariant != ThemeVariant.Light;

            if (toLight)
            {
                Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
                ThemeManager.ApplyLightTheme();
            }
            else
            {
                Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
                ThemeManager.ApplyDarkTheme();
            }

            UpdateThemeToggleButtonContent();
        }

        private async void OnAddModuleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var options = new FilePickerOpenOptions
                {
                    Title = "Выберите модуль (.dll)",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Библиотеки .NET") { Patterns = new[] { "*.dll" } }
                    }
                };

                var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
                if (result == null || result.Count == 0) return;

                var sourcePath = result[0].Path.LocalPath;
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return;

                // Кладём в пользовательскую папку, а не рядом с приложением: после установки
                // в Program Files у обычного пользователя нет прав на запись туда.
                var templatesDir = Services.TemplateLoader.UserTemplatesDirectory;
                Directory.CreateDirectory(templatesDir);

                var destPath = Path.Combine(templatesDir, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destPath, overwrite: true);

                // Обновить список модулей
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ReloadTemplates();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(this, "Ошибка добавления модуля", ex.Message).ShowAsync();
            }
        }
    } 
}



