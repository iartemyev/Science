using OpenCvSharp;

namespace Science.Tools.Data
{
    /// <summary>
    /// Объект обучающего множества, содержащий изображение исходного примера
    /// </summary>
    public class SrcTrainingSample : ITrainingSample<IplImage>
    {
        /// <summary>
        /// Клас, к которому принадлежит пример
        /// </summary>
        public int Class { get; set; }

        /// <summary>
        /// Информация о исходном примере (состав, камера, индекс кадра, ...)
        /// </summary>
        public string SampleInfo { get; set; }

        /// <summary>
        /// Признаки примера
        /// </summary>
        public IplImage Data { get; set; }
    }
}