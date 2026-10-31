using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TU_Template.Models;
using DocumentTemplates.CoreLibrary;
namespace TU_Template.ViewModels
{
    public partial class GroupsViewModel : ViewModelBase
    {
        private Window _parentWindow;
        public GroupsViewModel(Window parentWindow, TemplateData templateData)
        {
            _parentWindow = parentWindow;
            TemplateData = templateData;
        }
        public TemplateData TemplateData { get; }
        [RelayCommand]
        private void Add()
        {
            TemplateData.Groups.Add(new());
        }
        [RelayCommand]
        private void Remove(Group g)
        {
            TemplateData.Groups.Remove(g);
        }
    }
}
