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
        /// <summary>
        /// Папка модулей, доступная пользователю на запись. Нужна, когда приложение
        /// установлено в Program Files: туда обычный пользователь писать не может.
        /// Сюда же копирует файл кнопка «Добавить модуль шаблона (.dll)».
        /// </summary>
        public static string UserTemplatesDirectory => Path.Combine(AppDataRoot, "Templates");

        /// <summary>Корень служебных данных приложения в профиле пользователя.</summary>
        private static string AppDataRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocumentTemplates");

        /// <summary>
        /// Загружает модули шаблонов из папки рядом с приложением и из пользовательской папки.
        /// </summary>
        /// <param name="path">Папка модулей относительно каталога приложения (обычно "Templates").</param>
        public static List<IDocumentTemplate> LoadTemplates(string path)
        {
            var templates = new List<IDocumentTemplate>();

            var searchDirectories = GetSearchDirectories(path);
            if (searchDirectories.Count == 0)
                return templates;

            // Теневая копия нужна, чтобы не держать файлы модулей заблокированными
            // (иначе их нельзя пересобрать при запущенном приложении).
            //
            // ВАЖНО: копируем в профиль пользователя, а НЕ внутрь папки приложения.
            // Раньше теневая копия создавалась в <папка приложения>\Templates\_shadow, и после
            // установки в Program Files обычный пользователь не мог туда писать: создание папки
            // и копирование падали в пустые catch, затем Assembly.LoadFrom не находил файл —
            // список шаблонов оказывался пустым, хотя файлы лежали на месте. Выглядело это так,
            // будто приложение «не видит модули» без запуска от администратора.
            var shadowDir = CreateShadowDirectory();

            foreach (var directory in searchDirectories)
            {
                LoadFromDirectory(directory, shadowDir, templates);
            }

            return templates;
        }

        /// <summary>Папки, в которых ищем модули: рядом с приложением и в профиле пользователя.</summary>
        private static List<string> GetSearchDirectories(string path)
        {
            var result = new List<string>();

            if (!string.IsNullOrWhiteSpace(path))
            {
                // Всегда относительно папки exe (иначе из ярлыка/Program Files CWD может быть не папка приложения).
                var appDirectory = Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(AppContext.BaseDirectory, path);

                if (Directory.Exists(appDirectory))
                    result.Add(appDirectory);
            }

            var userDirectory = UserTemplatesDirectory;
            if (Directory.Exists(userDirectory) &&
                !result.Any(d => string.Equals(d, userDirectory, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(userDirectory);
            }

            return result;
        }

        /// <summary>
        /// Создаёт свежую папку для теневых копий и подчищает папки прошлых запусков.
        /// Папка уникальна на каждый вызов: Assembly.LoadFrom кеширует сборки по пути,
        /// иначе после замены модуля подхватилась бы старая версия.
        /// </summary>
        private static string? CreateShadowDirectory()
        {
            try
            {
                var shadowRoot = Path.Combine(AppDataRoot, "Modules");
                Directory.CreateDirectory(shadowRoot);

                CleanupOldShadowDirectories(shadowRoot);

                var shadowDir = Path.Combine(shadowRoot, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(shadowDir);
                return shadowDir;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось создать папку для теневых копий модулей: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Удаляет теневые копии прошлых запусков. Папки текущего сеанса удалить не получится —
        /// их файлы заблокированы загруженными сборками, такие ошибки просто пропускаем.
        /// </summary>
        private static void CleanupOldShadowDirectories(string shadowRoot)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(shadowRoot))
                {
                    try { Directory.Delete(directory, recursive: true); }
                    catch { /* занята текущим или другим запущенным экземпляром */ }
                }
            }
            catch
            {
                // Уборка необязательна, на загрузку модулей не влияет.
            }
        }

        private static void LoadFromDirectory(string directory, string? shadowDir, List<IDocumentTemplate> templates)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.dll");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось прочитать папку модулей {directory}: {ex.Message}");
                return;
            }

            foreach (var dll in files)
            {
                var fileName = Path.GetFileName(dll);

                // Ядро уже загружено в процесс — повторно грузить нельзя.
                if (string.Equals(fileName, "DocumentTemplates.CoreLibrary.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                var loadPath = PrepareForLoad(dll, shadowDir);

                try
                {
                    var asm = Assembly.LoadFrom(loadPath);
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
                    // Сюда же попадают служебные библиотеки (Xceed, System.*) — у них просто нет нужных типов.
                    Console.WriteLine($"Модуль не загружен ({fileName}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Копирует модуль и файлы рядом с ним в теневую папку и возвращает путь для загрузки.
        /// Если скопировать не вышло, грузим файл на месте — приложение всё равно должно работать.
        /// </summary>
        private static string PrepareForLoad(string dll, string? shadowDir)
        {
            if (shadowDir == null) return dll;

            try
            {
                var sourceDirectory = Path.GetDirectoryName(dll);
                if (sourceDirectory == null) return dll;

                // Копируем всю папку модулей: модулю рядом с собой нужны не только сборки,
                // но и справочники (devices.json, GCh_D2_List.json, xlsx).
                CopyDirectory(sourceDirectory, shadowDir);

                var candidate = Path.Combine(shadowDir, Path.GetFileName(dll));
                return File.Exists(candidate) ? candidate : dll;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Теневая копия не создана, загружаем на месте: {ex.Message}");
                return dll;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(file));
                if (File.Exists(target)) continue;

                try { File.Copy(file, target, overwrite: true); }
                catch { /* отдельный файл скопировать не удалось — не прерываем остальные */ }
            }

            foreach (var sub in Directory.GetDirectories(source))
            {
                // Папки теневых копий прошлых версий внутрь не тянем.
                if (string.Equals(Path.GetFileName(sub), "_shadow", StringComparison.OrdinalIgnoreCase))
                    continue;

                CopyDirectory(sub, Path.Combine(destination, Path.GetFileName(sub)));
            }
        }
    }
}
