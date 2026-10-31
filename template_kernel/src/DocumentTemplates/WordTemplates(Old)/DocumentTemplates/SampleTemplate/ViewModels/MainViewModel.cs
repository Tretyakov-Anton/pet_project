using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace SampleTemplate.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> ShowFileSettingsCommand { get; }

        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public MainViewModel(Window parentWindow)
        {
            ShowFileSettingsCommand = ReactiveCommand.Create(() =>
            {
                CurrentView = new FileSettingsViewModel(parentWindow);
            });

            CurrentView = new FileSettingsViewModel(parentWindow); // Стартовая страница
        }
    }
}
