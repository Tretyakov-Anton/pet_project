    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.Json;

    using System.Reflection;
using System.Threading.Tasks;
    using Avalonia.Controls;
    using ReactiveUI;
    using System.Reactive;
    using TU_Template.Models;
using System.Xml.Linq;

namespace TU_Template.ViewModels;

    public class ApplicationGViewModel : ViewModelBase 
    {
        public ObservableCollection<DeviceGroupViewModel> DeviceGroups { get; }
        public ObservableCollection<string> AvailableDevices { get; }
        
        private readonly Dictionary<string, ObservableCollection<string>> _deviceTypesByName = new();

        private bool _deviceNamesLoaded;
        private string? _devicesJsonPath;
        
        public ReactiveCommand<Unit, Unit> AddDeviceCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowAddDeviceDialogCommand { get; }
        public ReactiveCommand<DeviceGroupViewModel, Unit> EditDeviceCommand { get; }
        public ReactiveCommand<DeviceGroupViewModel, Unit> RemoveDeviceCommand { get; }
        
        private readonly Window? _parentWindow;
        private readonly TemplateData _templateData;

        public ApplicationGViewModel(Window? parentWindow, TemplateData templateData)
        {
            _parentWindow = parentWindow;
            _templateData = templateData;

            DeviceGroups = new ObservableCollection<DeviceGroupViewModel>();
            AvailableDevices = new ObservableCollection<string>();

            // Загружаем данные о приборах лениво:
            // - имена (AvailableDevices) - только когда открывают диалог добавления
            // - типы/модели - только для выбранного имени

            RebuildDeviceGroupsFromModel();
            _templateData.DeviceGroups.CollectionChanged += (_, _) => RebuildDeviceGroupsFromModel();
            
            AddDeviceCommand = ReactiveCommand.Create(() =>
            {
                var newGroup = new DeviceGroup("Новая группа");
                _templateData.DeviceGroups.Add(newGroup);
            });
            
            ShowAddDeviceDialogCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_parentWindow == null) return;
                
                var dialog = new TU_Template.Views.AddDeviceDialog();
                await EnsureDeviceNamesLoadedAsync();
                dialog.SetDeviceNames(AvailableDevices);
                dialog.SetDeviceTypesLoaderAsync(LoadTypesForDeviceNameAsync);
                
                var result = await dialog.ShowDialog<bool?>(_parentWindow);
                if (result == true && !string.IsNullOrEmpty(dialog.SelectedDeviceName))
                {
                    var deviceName = dialog.SelectedDeviceName;
                    var deviceType = dialog.SelectedDeviceType ?? "";
                    
                    // Проверяем, есть ли уже прибор с таким наименованием
                    var existingVm = DeviceGroups.FirstOrDefault(g => g.Name == deviceName);
                    
                    if (existingVm != null)
                    {
                        // Если прибор уже существует, добавляем тип к существующему
                        var existingTypes = existingVm.TypeName.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToList();
                        
                        // Добавляем новый тип, если его еще нет
                        if (!string.IsNullOrWhiteSpace(deviceType) && !existingTypes.Contains(deviceType))
                        {
                            existingTypes.Add(deviceType);
                            existingVm.TypeName = string.Join(", ", existingTypes);
                        }
                    }
                    else
                    {
                        // Создаем новый прибор
                        var newGroup = new DeviceGroup(deviceName);
                        if (!string.IsNullOrWhiteSpace(deviceType))
                            newGroup.Types.Add(new DeviceType(deviceType));
                        _templateData.DeviceGroups.Add(newGroup);
                    }
                }
            });
            
            EditDeviceCommand = ReactiveCommand.CreateFromTask<DeviceGroupViewModel>(async (group) =>
            {
                if (_parentWindow == null || group == null) return;
                
                var dialog = new TU_Template.Views.AddDeviceDialog();
                await EnsureDeviceNamesLoadedAsync();
                dialog.SetDeviceNames(AvailableDevices);
                dialog.SetDeviceTypesLoaderAsync(LoadTypesForDeviceNameAsync);
                // Предзаполняем текущие значения
                dialog.SetSelectedValues(group.Name, group.TypeName);
                
                var result = await dialog.ShowDialog<bool?>(_parentWindow);
                if (result == true && !string.IsNullOrEmpty(dialog.SelectedDeviceName))
                {
                    // Сохраняем старое имя для поиска модели
                    var oldName = group.Name;
                    
                    // Обновляем данные в ViewModel
                    group.Name = dialog.SelectedDeviceName;
                    group.TypeName = dialog.SelectedDeviceType ?? "";
                    
                    // Обновляем модель
                    var modelGroup = _templateData.DeviceGroups.FirstOrDefault(g => g.Name == oldName);
                    if (modelGroup != null)
                    {
                        modelGroup.Name = dialog.SelectedDeviceName;
                    }
                }
            });
            RemoveDeviceCommand = ReactiveCommand.Create<DeviceGroupViewModel>(group =>
            {
                if (group != null && DeviceGroups.Contains(group))
                {
                    var modelToRemove = _templateData.DeviceGroups.FirstOrDefault(g => g.Name == group.Name);
                    if (modelToRemove != null)
                    {
                        _templateData.DeviceGroups.Remove(modelToRemove);
                    }
                }
            });
        }

        private void RebuildDeviceGroupsFromModel()
        {
            DeviceGroups.Clear();
            foreach (var modelGroup in _templateData.DeviceGroups)
            {
                DeviceGroups.Add(new DeviceGroupViewModel(modelGroup, this));
            }
        }

        private string? ResolveDevicesJsonPath()
        {
            if (!string.IsNullOrWhiteSpace(_devicesJsonPath))
                return _devicesJsonPath;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? baseDir;

            var possiblePaths = new[]
            {
                Path.Combine(assemblyDir, "Resources", "Devices", "devices.json"),
                Path.Combine(assemblyDir, "devices.json"),
                Path.Combine(baseDir, "Resources", "Devices", "devices.json"),
                Path.Combine(baseDir, "devices.json"),
                Path.Combine(baseDir, "Templates", "Resources", "Devices", "devices.json"),
                Path.Combine(baseDir, "Templates", "devices.json")
            };

            _devicesJsonPath = possiblePaths.FirstOrDefault(File.Exists);
            return _devicesJsonPath;
        }

        // Загружаем только список имен приборов (без всех типов/моделей).
        // Типы/модели подгружаются лениво только после выбора конкретного имени.
        private async Task EnsureDeviceNamesLoadedAsync()
        {
            if (_deviceNamesLoaded)
                return;

            var jsonPath = ResolveDevicesJsonPath();
            if (string.IsNullOrEmpty(jsonPath))
            {
                AddDistinct(AvailableDevices, "Ошибка загрузки списка");
                _deviceNamesLoaded = true;
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath);

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var deviceEl in doc.RootElement.EnumerateArray())
                {
                    // Новый формат: { "Name": "...", "Types": [...] }
                    if (deviceEl.ValueKind == JsonValueKind.Object &&
                        deviceEl.TryGetProperty("Name", out var nameProp))
                    {
                        var name = (nameProp.GetString() ?? "").Trim();
                        AddDistinct(AvailableDevices, name);
                        continue;
                    }

                    // Legacy формат: ["Имя1", "Имя2"]
                    if (deviceEl.ValueKind == JsonValueKind.String)
                    {
                        var name = (deviceEl.GetString() ?? "").Trim();
                        AddDistinct(AvailableDevices, name);
                    }
                }
            }
            catch (JsonException)
            {
                // Если вдруг JSON другой — ничего не делаем (список останется пустым).
            }

            _deviceNamesLoaded = true;
        }

        // Подгружаем список типов/моделей только для выбранного имени.
        private async Task<ObservableCollection<string>> LoadTypesForDeviceNameAsync(string deviceName)
        {
            deviceName = deviceName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(deviceName))
                return new ObservableCollection<string>();

            if (_deviceTypesByName.TryGetValue(deviceName, out var cached))
                return cached;

            var jsonPath = ResolveDevicesJsonPath();
            if (string.IsNullOrEmpty(jsonPath))
                return new ObservableCollection<string>();

            var json = await File.ReadAllTextAsync(jsonPath);
            var result = new ObservableCollection<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return result;

                foreach (var deviceEl in doc.RootElement.EnumerateArray())
                {
                    if (deviceEl.ValueKind != JsonValueKind.Object)
                        continue;

                    if (!deviceEl.TryGetProperty("Name", out var nameProp))
                        continue;

                    var name = (nameProp.GetString() ?? "").Trim();
                    if (!string.Equals(name, deviceName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Types может быть: ["Тип1","Тип2"] или [{ "Name":"Тип1"}, ...]
                    if (deviceEl.TryGetProperty("Types", out var typesProp) &&
                        typesProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var typeEl in typesProp.EnumerateArray())
                        {
                            if (typeEl.ValueKind == JsonValueKind.String)
                            {
                                AddDistinct(result, (typeEl.GetString() ?? "").Trim());
                            }
                            else if (typeEl.ValueKind == JsonValueKind.Object &&
                                     typeEl.TryGetProperty("Name", out var typeNameProp))
                            {
                                AddDistinct(result, (typeNameProp.GetString() ?? "").Trim());
                            }
                        }
                    }

                    break;
                }
            }
            catch (JsonException)
            {
                // Если JSON другой — вернём пустой список.
            }

            _deviceTypesByName[deviceName] = result;
            return result;
        }

        private void InitializePredefinedDeviceGroups()
        {
            if (_templateData == null)
            {
                return;
            }
            
            // Если группы уже загружены из template-data.json, не пересоздаём их,
            // чтобы не получить дубликаты при повторном открытии шаблона.
            if (_templateData.DeviceGroups != null && _templateData.DeviceGroups.Count > 0)
            {
                return;
            }

            if (_deviceTypesByName.Count == 0 && AvailableDevices.Count == 0)
            {
                return;
            }

            foreach (var kvp in _deviceTypesByName)
            {
                var deviceName = kvp.Key;
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    continue;
                }

                // Если группа с таким именем уже существует (на всякий случай), пропускаем.
                if (_templateData.DeviceGroups.Any(g => string.Equals(g.Name, deviceName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var group = new DeviceGroup(deviceName)
                {
                    IsPredefined = true
                };

                foreach (var typeName in kvp.Value.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    group.Types.Add(new DeviceType(typeName)
                    {
                        IsCustom = false
                    });
                }

                _templateData.DeviceGroups.Add(group);
            }
        }

        private void LoadDevicesFromExcel()
        {
            try
            {
                // Папка, где лежит сама сборка TU_Template (когда плагин загружается из Templates\)
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                  ?? AppDomain.CurrentDomain.BaseDirectory;

                // Базовая директория хост-приложения (Designer / отдельный запуск)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                var possiblePaths = new[]
                {
                    // 1. Рядом с dll плагина
                    Path.Combine(assemblyDir, "Resources", "Devices", "Oborudovanie_dlya_TU.xlsx"),
                    Path.Combine(assemblyDir, "Oborudovanie_dlya_TU.xlsx"),
                    // 2. В bin хоста
                    Path.Combine(baseDir, "Resources", "Devices", "Oborudovanie_dlya_TU.xlsx"),
                    Path.Combine(baseDir, "Oborudovanie_dlya_TU.xlsx"),
                    // 3. В Templates\ (куда выкладываются плагины)
                    Path.Combine(baseDir, "Templates", "Resources", "Devices", "Oborudovanie_dlya_TU.xlsx"),
                    Path.Combine(baseDir, "Templates", "Oborudovanie_dlya_TU.xlsx")
                };

                var excelPath = possiblePaths.FirstOrDefault(File.Exists);
                if (string.IsNullOrEmpty(excelPath))
                {
                    Console.WriteLine("Oborudovanie_dlya_TU.xlsx не найден ни по одному из известных путей.");
                    return;
                }

                using var archive = ZipFile.OpenRead(excelPath);

                // Читаем sharedStrings (таблица общих строк)
                var ns = (System.Xml.Linq.XNamespace)"http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var sharedStrings = new List<string>();

                var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (sharedEntry != null)
                {
                    using var sstStream = sharedEntry.Open();
                    var sstDoc = System.Xml.Linq.XDocument.Load(sstStream);
                    sharedStrings = sstDoc.Root?
                        .Elements(ns + "si")
                        .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => (string)t)))
                        .ToList() ?? new List<string>();
                }

                // Читаем первый лист: xl/worksheets/sheet1.xml
                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry == null)
                {
                    return;
                }

                using var sheetStream = sheetEntry.Open();
                var sheetDoc = System.Xml.Linq.XDocument.Load(sheetStream);
                var rows = sheetDoc
                    .Descendants(ns + "sheetData")
                    .SelectMany(sd => sd.Elements(ns + "row"));

                foreach (var row in rows)
                {
                    string GetCellString(string columnLetter)
                    {
                        var cell = row
                            .Elements(ns + "c")
                            .FirstOrDefault(c =>
                            {
                                var r = (string?)c.Attribute("r");
                                return r != null && r.StartsWith(columnLetter, StringComparison.OrdinalIgnoreCase);
                            });

                        if (cell == null)
                        {
                            return string.Empty;
                        }

                        var valueElement = cell.Element(ns + "v");
                        if (valueElement == null)
                        {
                            return string.Empty;
                        }

                        var raw = valueElement.Value;
                        var cellType = (string?)cell.Attribute("t");

                        if (cellType == "s" && int.TryParse(raw, out var sstIndex)
                            && sstIndex >= 0 && sstIndex < sharedStrings.Count)
                        {
                            return sharedStrings[sstIndex];
                        }

                        return raw;
                    }

                    // В xlsx считаем:
                    // B - Наименование оборудования (для левого списка модального окна)
                    // C - Конкретная модель оборудования (для правого списка модального окна)
                    var deviceName = GetCellString("B").Trim();
                    var deviceType = GetCellString("C").Trim();

                    // Пропускаем заголовок, если первый ряд содержит текст с признаками "тип"/"наименование"/"модель"
                    if (row.Attribute("r")?.Value == "1" && IsHeaderRow(deviceName, deviceType))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(deviceName))
                    {
                        AddDistinct(AvailableDevices, deviceName);

                        if (!string.IsNullOrWhiteSpace(deviceType))
                        {
                            if (!_deviceTypesByName.TryGetValue(deviceName, out var list))
                            {
                                list = new ObservableCollection<string>();
                                _deviceTypesByName[deviceName] = list;
                            }

                            AddDistinct(list, deviceType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки Oborudovanie_dlya_TU.xlsx: {ex.Message}");
            }
        }

        private void LoadDevicesFromJson()
        {
            try
            {
                // Ищем файл с описанием приборов в JSON.
                // Ожидаемый формат:
                // [
                //   { "Name": "Прибор 1", "Types": ["Тип1", "Тип2"] },
                //   { "Name": "Прибор 2", "Types": ["ТипA"] }
                // ]

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                  ?? baseDir;

                var possiblePaths = new[]
                {
                    Path.Combine(assemblyDir, "Resources", "Devices", "devices.json"),
                    Path.Combine(assemblyDir, "devices.json"),
                    Path.Combine(baseDir, "Resources", "Devices", "devices.json"),
                    Path.Combine(baseDir, "devices.json"),
                    Path.Combine(baseDir, "Templates", "Resources", "Devices", "devices.json"),
                    Path.Combine(baseDir, "Templates", "devices.json")
                };

                var jsonPath = possiblePaths.FirstOrDefault(File.Exists);
                if (string.IsNullOrEmpty(jsonPath))
                {
                    Console.WriteLine("devices.json не найден ни по одному из известных путей.");
                    return;
                }

                var json = File.ReadAllText(jsonPath);

                // Пытаемся сначала прочитать новый формат (объекты с Name/Types)
                try
                {
                    var devices = JsonSerializer.Deserialize<List<DeviceJsonModel>>(json);
                    if (devices != null)
                    {
                        foreach (var device in devices)
                        {
                            var deviceName = (device.Name ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(deviceName))
                            {
                                continue;
                            }

                            AddDistinct(AvailableDevices, deviceName);

                            if (device.Types != null && device.Types.Count > 0)
                            {
                                if (!_deviceTypesByName.TryGetValue(deviceName, out var list))
                                {
                                    list = new ObservableCollection<string>();
                                    _deviceTypesByName[deviceName] = list;
                                }

                                foreach (var typeName in device.Types
                                             .Select(t => (t ?? "").Trim())
                                             .Where(t => !string.IsNullOrWhiteSpace(t)))
                                {
                                    AddDistinct(list, typeName);
                                }
                            }
                        }

                        // Удалось прочитать новый формат — выходим
                        return;
                    }
                }
                catch (JsonException)
                {
                    // Падаем в запасной вариант ниже
                }

                // Fallback: старый формат — просто массив строк
                try
                {
                    var legacyDevices = JsonSerializer.Deserialize<string[]>(json);
                    if (legacyDevices != null)
                    {
                        foreach (var device in legacyDevices)
                        {
                            AddDistinct(AvailableDevices, (device ?? "").Trim());
                        }
                    }
                }
                catch (JsonException)
                {
                    // Если и это не получилось — отдадим ошибку общему catch ниже
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки devices.json: {ex.Message}");
                AddDistinct(AvailableDevices, "Ошибка загрузки списка");
            }
        }

        private sealed class DeviceJsonModel
        {
            public string? Name { get; set; }
            public List<string>? Types { get; set; }
        }

        private static bool IsHeaderRow(string deviceName, string deviceType)
        {
            var nameLower = deviceName.ToLowerInvariant();
            var typeLower = deviceType.ToLowerInvariant();
            return nameLower.Contains("тип") || nameLower.Contains("наимен") || nameLower.Contains("оборудование") ||
                   typeLower.Contains("модель") || typeLower.Contains("тип") || typeLower.Contains("наимен");
        }

        private static void AddDistinct(ObservableCollection<string> collection, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!collection.Contains(value))
            {
                collection.Add(value);
            }
        }
    }
