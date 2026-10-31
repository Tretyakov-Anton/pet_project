using ReactiveUI;
using System;
using System.Diagnostics;

namespace DocumentTemplates.CoreLibrary
{
    public abstract class ViewModelBase : ReactiveObject
    {
        protected virtual void Log(string message) => 
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{GetType().Name}] {message}");

        protected virtual void LogError(string message, Exception? ex = null) =>
            Debug.WriteLine($"[ERROR] [{DateTime.Now:HH:mm:ss}] [{GetType().Name}] {message} {ex?.Message}");
    }
}