using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Template_TU.Services
{
    public static class TemplatesManager
    {
        // Получаем путь к папке Templates в корне решения
        public static readonly string _templatesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        public static List<string> FindTemplateDlls()
        {
            // Выводим текущий рабочий каталог для отладки
            System.Diagnostics.Debug.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            System.Diagnostics.Debug.WriteLine($"Searching in: {_templatesDirectory}");

            if (!Directory.Exists(_templatesDirectory))
            {
                System.Diagnostics.Debug.WriteLine("Templates directory does not exist.");
                return new List<string>(); // Возвращаем пустой список, если папка не существует
            }

            // Получаем все DLL-файлы в папке Templates
            var templateDlls = Directory.GetFiles(_templatesDirectory, "*.dll")
                                         .Select(Path.GetFileName) // Получаем только имена файлов
                                         .ToList();

            // Выводим найденные DLL для отладки
            System.Diagnostics.Debug.WriteLine($"Found DLLs: {string.Join(", ", templateDlls)}");

            return templateDlls;
        }
        public static ITemplateView LoadTemplateView(string dllName)
        {
            var dllPath = Path.Combine(_templatesDirectory, dllName);
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException("DLL file not found.", dllPath);
            }
            var assembly = Assembly.LoadFrom(dllPath);
            var types = assembly.GetTypes()
                                .Where(t => typeof(ITemplateView).IsAssignableFrom(t) && !t.IsInterface)
                                .ToList();
            // Здесь вы можете выбрать конкретный тип представления, например, первый
            if (types.Count > 0)
            {
                var instance = (ITemplateView)Activator.CreateInstance(types[0]);
                return instance;
            }
            throw new InvalidOperationException("No valid template view found in the DLL.");
        }
    }
    public interface ITemplateView
    {
        string GetTemplate();
    }
}
