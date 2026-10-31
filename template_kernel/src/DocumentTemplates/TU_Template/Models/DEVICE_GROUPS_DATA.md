# Структура данных приборов (DeviceGroups)

## Обзор

Данные о приборах хранятся в классе `TemplateData` в свойстве `DeviceGroups`.

## Структура данных

### TemplateData
```csharp
public partial class TemplateData : ObservableObject
{
    public ObservableCollection<DeviceGroup> DeviceGroups { get; set; }
    // ... другие свойства
}
```

### DeviceGroup
```csharp
public partial class DeviceGroup : ObservableObject
{
    public string Name { get; set; }  // Наименование прибора (например, "Анализатор спектра")
    public ObservableCollection<DeviceType> Types { get; set; }  // Коллекция типов/моделей
    public bool IsPredefined { get; set; }  // Флаг предопределенного прибора
}
```

### DeviceType
```csharp
public class DeviceType : ObservableObject
{
    public string Name { get; set; }  // Название типа/модели (например, "СК4М", "Тип1")
    public bool IsCustom { get; set; }
}
```

## Пример данных

```json
{
  "DeviceGroups": [
    {
      "Name": "Анализатор спектра",
      "Types": [
        { "Name": "СК4М" },
        { "Name": "СК5М" }
      ],
      "IsPredefined": false
    },
    {
      "Name": "Источники питания",
      "Types": [
        { "Name": "Тип1" }
      ],
      "IsPredefined": false
    }
  ]
}
```

## Сериализация в JSON

Данные автоматически сериализуются через `TemplateDataJsonModel`:

```csharp
// Сохранение
var jsonModel = templateData.ToJsonModel();
var json = JsonSerializer.Serialize(jsonModel, new JsonSerializerOptions
{
    WriteIndented = true
});
File.WriteAllText("template-data.json", json);

// Загрузка
var json = File.ReadAllText("template-data.json");
var model = JsonSerializer.Deserialize<TemplateDataJsonModel>(json);
var templateData = model.ToTemplateData();
```

## Использование данных

### Получение всех приборов
```csharp
foreach (var deviceGroup in templateData.DeviceGroups)
{
    Console.WriteLine($"Прибор: {deviceGroup.Name}");
    foreach (var type in deviceGroup.Types)
    {
        Console.WriteLine($"  - {type.Name}");
    }
}
```

### Поиск прибора по наименованию
```csharp
var analyzer = templateData.DeviceGroups
    .FirstOrDefault(dg => dg.Name == "Анализатор спектра");

if (analyzer != null)
{
    var types = analyzer.Types.Select(t => t.Name).ToList();
    // types = ["СК4М", "СК5М"]
}
```

### Получение всех типов прибора
```csharp
var deviceTypes = templateData.DeviceGroups
    .Where(dg => dg.Name == "Анализатор спектра")
    .SelectMany(dg => dg.Types)
    .Select(t => t.Name)
    .ToList();
```

## Важные замечания

1. **Синхронизация**: В `DeviceGroupViewModel` используется свойство `TypeName` (строка вида "Тип1, Тип2"), которое автоматически синхронизируется с коллекцией `Types` в модели `DeviceGroup`.

2. **Объединение типов**: При добавлении прибора с уже существующим наименованием, типы автоматически объединяются через запятую в одном элементе списка.

3. **Сохранение**: Данные сохраняются в JSON через `FileSettingsViewModel.SaveSettingsAsync()` в файл `template-data.json`.

4. **Загрузка**: Данные загружаются через `FileSettingsViewModel.LoadSettingsAsync()` из файла `template-data.json`.

## Расположение файлов

- **Модель данных**: `src/DocumentTemplates/TU_Template/Models/TemplateData.cs`
- **Модель прибора**: `src/DocumentTemplates/TU_Template/Models/DeviceGroup.cs`
- **Модель типа**: `src/DocumentTemplates/TU_Template/Models/DeviceType.cs`
- **ViewModel**: `src/DocumentTemplates/TU_Template/ViewModels/DeviceGroupViewModel.cs`
- **Сохранение/загрузка**: `src/DocumentTemplates/TU_Template/ViewModels/FileSettingsViewModel.cs`

