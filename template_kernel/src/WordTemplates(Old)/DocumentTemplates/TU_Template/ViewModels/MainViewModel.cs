using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using TU_Template.Models;

namespace TU_Template.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public TemplateData TemplateData { get; set; }
        public ReactiveCommand<Unit, Unit> ShowFileSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowElementsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowGroupsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowApplicationG { get; }

        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public MainViewModel(Window parentWindow)
        {
            TemplateData = new TemplateData();

            ShowFileSettingsCommand = ReactiveCommand.Create(() =>
            {
                CurrentView = new FileSettingsViewModel(parentWindow);
            });
            ShowElementsCommand = ReactiveCommand.Create(() =>
            {
                CurrentView = new ElementsViewModel(parentWindow, TemplateData);
            });
            ShowGroupsCommand = ReactiveCommand.Create(() =>
            {
                CurrentView = new GroupsViewModel(parentWindow, TemplateData);
            });
            ShowApplicationG = ReactiveCommand.Create(() =>
            {
                CurrentView = new ApplicationGViewModel(parentWindow, TemplateData);

            });

            CurrentView = new FileSettingsViewModel(parentWindow); // Стартовая страница
        }
    }
}
