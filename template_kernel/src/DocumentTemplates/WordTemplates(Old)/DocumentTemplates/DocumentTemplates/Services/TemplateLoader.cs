using DocumentTemplates.CoreLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DocumentTemplates.Services
{
    public static class TemplateLoader
        {
            public static List<IDocumentTemplate> LoadTemplates(string path)
            {
                var templates = new List<IDocumentTemplate>();

                if (!Directory.Exists(path))
                    return templates;

                var dllFiles = Directory.GetFiles(path, "*.dll");

            foreach (var dll in dllFiles)
            {
                try
                {
                    var asm = Assembly.LoadFrom(dll);
                    var types = asm.GetTypes()
                        .Where(t => typeof(IDocumentTemplate).IsAssignableFrom(t) && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        if (Activator.CreateInstance(type) is IDocumentTemplate template)
                            templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки шаблона из {dll}: {ex.Message}");
                }
            }
            return templates;
        }
    }
}
