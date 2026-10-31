using Avalonia.Controls;
using TemplatesCore;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows.Input;

namespace TemplateSample.ViewModels
{
    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private string _sourcePath;
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                _sourcePath = value;
                OnPropertyChanged(nameof(SourcePath));
            }
        }

        private string _resultPath;
        public string ResultPath
        {
            get => _resultPath;
            set
            {
                _resultPath = value;
                OnPropertyChanged(nameof(ResultPath));
            }
        }

        public ICommand SelectDocumentCommand { get; }
        public ICommand SelectResultCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        public MainViewModel()
        {
            SelectDocumentCommand = new RelayCommand(async _ => await SelectDocument());
            SelectResultCommand = new RelayCommand(async _ => await SelectResultFolder());
            CreateCommand = new RelayCommand(_ => CreateDocument());
            LoadCommand = new RelayCommand(_ => LoadData());
            SaveCommand = new RelayCommand(_ => SaveData());
        }

        private async System.Threading.Tasks.Task SelectDocument()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = { new FileDialogFilter { Name = "DOCX files", Extensions = { "docx" } } }
            };

            var window = GetMainWindow();
            var result = await dialog.ShowAsync(window);
            if (result?.Length > 0)
                SourcePath = result[0];
        }

        private async System.Threading.Tasks.Task SelectResultFolder()
        {
            var dialog = new OpenFolderDialog();
            var window = GetMainWindow();
            var result = await dialog.ShowAsync(window);
            if (!string.IsNullOrWhiteSpace(result))
                ResultPath = result;
        }

        private void CreateDocument()
        {
            if (File.Exists(SourcePath) && Directory.Exists(ResultPath))
            {
                string destFile = Path.Combine(ResultPath, $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
                File.Copy(SourcePath, destFile, overwrite: true);
            }
        }

        private void LoadData()
        {
            // Ваша логика загрузки
        }

        private void SaveData()
        {
            // Ваша логика сохранения
        }

        private Window GetMainWindow()
        {
            return (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            Dispatcher.UIThread.Post(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (_ => true); // <<<<<< ВСЕГДА TRUE, если null
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
