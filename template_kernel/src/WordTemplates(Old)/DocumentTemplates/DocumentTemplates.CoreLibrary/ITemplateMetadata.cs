namespace DocumentTemplates.CoreLibrary
{
    /// <summary>
    /// Содержит метаинформацию о шаблоне документа.
    /// Используется ядром для отображения информации пользователю.
    /// </summary>
    public interface ITemplateMetadata
    {
        /// <summary>
        /// Название шаблона.
        /// Например: "Договор аренды".
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Краткое описание шаблона.
        /// Например: "Шаблон для создания договора аренды недвижимости".
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Имя разработчика шаблона.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Версия шаблона (в формате SemVer или любой другой).
        /// Например: "1.0.0"
        /// </summary>
        string Version { get; }
    }
}
