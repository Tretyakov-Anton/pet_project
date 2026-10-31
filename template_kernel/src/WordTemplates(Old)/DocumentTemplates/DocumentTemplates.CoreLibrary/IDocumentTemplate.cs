using Avalonia.Controls;

namespace DocumentTemplates.CoreLibrary
{
    /// <summary>
    /// Основной интерфейс для всех шаблонов.
    /// Ядро будет загружать реализации этого интерфейса из DLL-файлов.
    /// </summary>
    public interface IDocumentTemplate
    {
        /// <summary>
        /// Метаданные шаблона, отображаемые в интерфейсе пользователя.
        /// </summary>
        ITemplateMetadata Metadata { get; }

        /// <summary>
        /// Возвращает визуальный элемент Avalonia, который отображается пользователю
        /// для ввода данных, необходимых для генерации документа.
        /// </summary>
        /// <returns>Контрол Avalonia.</returns>
        Control GetEditorControl(Window parentWindow);
    }
}
