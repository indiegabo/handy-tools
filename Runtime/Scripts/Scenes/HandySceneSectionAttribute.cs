using System;

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Provides stable metadata for one SceneExtender section type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class HandySceneSectionAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Initializes section metadata with one stable section identifier.
        /// </summary>
        /// <param name="sectionId">
        /// Stable identifier used to persist the section across refactors.
        /// </param>
        public HandySceneSectionAttribute(string sectionId)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
            {
                throw new ArgumentException(
                    "Section identifier cannot be null or whitespace.",
                    nameof(sectionId));
            }

            SectionId = sectionId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable identifier used to persist the section.
        /// </summary>
        public string SectionId { get; }

        /// <summary>
        /// Gets or sets the optional display name shown in the inspector.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the relative order used when sorting sections.
        /// </summary>
        public int Order { get; set; }

        #endregion
    }
}