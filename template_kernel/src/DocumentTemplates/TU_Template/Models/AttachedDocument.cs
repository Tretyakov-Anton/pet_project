using CommunityToolkit.Mvvm.ComponentModel;

namespace TU_Template.Models;

public partial class AttachedDocument : ObservableObject
{
    [ObservableProperty] private string _documentCode = "";
    
    public AttachedDocument() { }
    
    public AttachedDocument(string documentCode)
    {
        _documentCode = documentCode;
    }
}

