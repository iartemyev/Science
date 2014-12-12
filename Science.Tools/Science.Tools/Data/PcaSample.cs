namespace Science.Tools.Data
{
    /// <summary>
    /// Объект обучающего множества, содержащий проекцию главных компонент исходного примера
    /// </summary>
    public class PcaSample : ITrainingSample<float[]>
    {
        /// <summary>
        /// Клас, к которому принадлежит пример
        /// </summary>
        public int Class { get; set; }

        /// <summary>
        /// Признаки примера
        /// </summary>
        public float[] Data { get; set; }
    }
}