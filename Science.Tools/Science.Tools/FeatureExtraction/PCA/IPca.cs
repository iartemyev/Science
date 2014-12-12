using OpenCvSharp;

namespace Science.Tools.FeatureExtraction.PCA
{
    /// <summary>
    /// Определяет основные свойства и методы работы алгоритма выделения главных компонент (PCA)
    /// </summary>
    public interface IPca
    {
        /// <summary>
        /// The output eigenvectors of covariation matrix (i.e. principal components); one vector per row.
        /// </summary>
        CvMat Evecs { get; set; }

        /// <summary>
        /// The mean (average) vector, computed inside the function or provided by user
        /// </summary>
        CvMat Avg { get; set; }

        /// <summary>
        /// Количество коэффициентов
        /// </summary>
        int CoeffsCount { get; set; }

        /// <summary>
        /// Вычисление главных компонент (Evecs и Avg)
        /// </summary>
        /// <param name="dataSet">Множество исходных данных</param>
        void Calculation(CvMat dataSet);

        /// <summary>
        /// Проецирование
        /// </summary>
        /// <param name="src">Исходное изображение</param>
        /// <param name="dst">Проекция</param>
        void Projection(CvMat src, CvMat dst);

        /// <summary>
        /// Загрузка данных
        /// </summary>
        /// <param name="filename"></param>
        void Load(string filename);

        /// <summary>
        /// Сохранение данных
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="headerMessage"></param>
        void Save(string filename, string headerMessage = "");
    }
}