namespace Science.Tools.Data.Types
{
    /// <summary>
    /// Типы объектов в обучающей выборке
    /// </summary>
    public enum TrainingSamplesTypes
    {
        /// <summary>
        /// Исходные изображения примеров
        /// </summary>
        Src,

        /// <summary>
        /// Проекции Pca
        /// </summary>
        Pca,

        /// <summary>
        /// Дескрипторы Surf
        /// </summary>
        Surf,

        /// <summary>
        /// Дескрипторы Orb
        /// </summary>
        Orb,

        /// <summary>
        /// Дескрипторы Freak
        /// </summary>
        Freak
    }
}