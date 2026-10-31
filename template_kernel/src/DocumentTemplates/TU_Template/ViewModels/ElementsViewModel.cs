using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TU_Template.Models;

namespace TU_Template.ViewModels
{
    public partial class ElementsViewModel : ViewModelBase
    {
        private Window _parentWindow;
        public ElementsViewModel(Window parentWindow, TemplateData templateData)
        {
            _parentWindow = parentWindow;
            TemplateData = templateData;
        }
        public TemplateData TemplateData { get; }
        [RelayCommand]
        private void Add()
        {
            TemplateData.Elements.Add(new Element());
        }
        [RelayCommand]
        private void Remove(Element e)
        {
            TemplateData.Elements.Remove(e);
        }
    }
}
