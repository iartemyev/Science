namespace Science.Tools.Data
{
    /// <summary>
    /// Определяет основные свойства примера обучающей выборки
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITrainingSample<T>
    {
        /// <summary>
        /// Клас, к которому принадлежит пример
        /// </summary>
        int Class { get; set; }

        /// <summary>
        /// Признаки примера
        /// </summary>
        T Data { get; set; }
    }
}