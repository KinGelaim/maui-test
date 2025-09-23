using System.Diagnostics.CodeAnalysis;
using MauiXaml.Models;

namespace MauiXaml.Utilities
{
    /// <summary>
    /// Расширения модели проекта
    /// </summary>
    public static class ProjectExtentions
    {
        /// <summary>
        /// Проверяет является ли проект нулевым или новым
        /// </summary>
        /// <param name="project">Проект</param>
        /// <returns>Возвращает true, если проект null или идентификатор равен 0</returns>
        public static bool IsNullOrNew([NotNullWhen(false)] this Project? project)
        {
            return project is null || project.ID == 0;
        }
    }
}