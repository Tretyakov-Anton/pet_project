using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WordMultiTemplates.Views
{
    public partial class MainWindow : Window
    {
        private ComboBox _templatesComboBox;
        private Button _createButton;
        private Button _refreshButton;

        private List<Tuple<string, Type, Assembly>> _moduleWindows = new List<Tuple<string, Type, Assembly>>();

        public MainWindow()
        {
            InitializeComponent();

            _templatesComboBox = this.FindControl<ComboBox>("TemplatesComboBox");
            _createButton = this.FindControl<Button>("CreateButton");
            _refreshButton = this.FindControl<Button>("RefreshButton");

            LoadModuleWindows();
            LoadTemplatesToUI();

            _createButton.Click += CreateButton_Click;
            _refreshButton.Click += (s, e) =>
            {
                LoadModuleWindows();
                LoadTemplatesToUI();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadModuleWindows()
        {
            string modulesPath = Path.Combine(AppContext.BaseDirectory, "TemplateModules");
            if (!Directory.Exists(modulesPath))
            {
                Directory.CreateDirectory(modulesPath);
            }

            _moduleWindows.Clear(); // Очистка списка перед загрузкой

            foreach (var dllFile in Directory.GetFiles(modulesPath, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    var windowTypes = assembly.GetTypes()
                        .Where(t => t.IsPublic && !t.IsAbstract && typeof(Window).IsAssignableFrom(t));

                    foreach (var type in windowTypes)
                    {
                        string moduleName = $"{type.Name} ({Path.GetFileName(dllFile)})";
                        _moduleWindows.Add(new Tuple<string, Type, Assembly>(moduleName, type, assembly));
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки сборки {dllFile}: {ex.Message}");
                }
            }
        }

        private void LoadTemplatesToUI()
        {
            _templatesComboBox.ItemsSource = _moduleWindows.Select(mw => mw.Item1).ToList();
            if (_templatesComboBox.ItemsSource != null && _moduleWindows.Count > 0)
            {
                _templatesComboBox.SelectedIndex = 0;
            }
        }

        private void CreateButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var selectedIndex = _templatesComboBox.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _moduleWindows.Count)
            {
                ShowError("Пожалуйста, выберите шаблон.");
                return; // Ничего не выбрано
            }

            var selectedModule = _moduleWindows[selectedIndex];

            try
            {
                var window = (Window)Activator.CreateInstance(selectedModule.Item2);
                window.Show();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка создания окна модуля: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            // Здесь можно использовать MessageBox или другой способ уведомления
            Console.WriteLine(message); // Для примера, просто выводим в консоль
        }

    }
}
