using System;
using OpenCvSharp;

namespace Science.Tools.FeatureExtraction.PCA
{
    /// <summary>
    /// Основные свойства и методы работы алгоритма выделения главных компонент на основе OpenCV
    /// </summary>
    public class Pca : IPca
    {
        /// <summary>
        /// The output eigenvectors of covariation matrix (i.e. principal components); one vector per row.
        /// </summary>
        public CvMat Evecs { get; set; }

        /// <summary>
        /// The mean (average) vector, computed inside the function or provided by user
        /// </summary>
        public CvMat Avg { get; set; }

        /// <summary>
        /// Количество коэффициентов
        /// </summary>
        public int CoeffsCount { get; set; }

        /// <summary>
        /// Вычисление главных компонент (Evecs и Avg)
        /// </summary>
        /// <param name="dataSet">Множество исходных данных</param>
        public void Calculation(CvMat dataSet)
        {
            Evecs = new CvMat(dataSet.Width, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width * dataSet.Width]);
            var evalues = new CvMat(1, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width]);
            Avg = new CvMat(1, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width]);

            Cv.CalcPCA(dataSet, Avg, evalues, Evecs, PCAFlag.DataAsRow);
        }

        /// <summary>
        /// Вычисление главных компонент (Evecs и Avg)
        /// </summary>
        /// <param name="dataSet">Множество исходных данных</param>
        /// <param name="coeffsCount">Размеры получаемых проекций</param>
        /// <param name="projDataSet">Множество проекций исходных данных</param>
        public void Calculation(CvMat dataSet, int coeffsCount, out CvMat projDataSet)
        {
            CoeffsCount = coeffsCount;
            Evecs = new CvMat(dataSet.Width, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width * dataSet.Width]);
            var evalues = new CvMat(1, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width]);
            Avg = new CvMat(1, dataSet.Width, MatrixType.F32C1, new float[dataSet.Width]);

            Cv.CalcPCA(dataSet, Avg, evalues, Evecs, PCAFlag.DataAsRow);

            projDataSet = new CvMat(dataSet.Height, CoeffsCount, MatrixType.F32C1, new float[dataSet.Height * CoeffsCount]);

            Cv.ProjectPCA(dataSet, Avg, Evecs, projDataSet);
        }

        /// <summary>
        /// Проецирование
        /// </summary>
        /// <param name="src">Исходное изображение</param>
        /// <param name="dst">Проекция</param>
        public void Projection(CvMat src, CvMat dst)
        {
            Cv.ProjectPCA(src, Avg, Evecs, dst);
        }

        /// <summary>
        /// Проецирование
        /// </summary>
        /// <param name="src">Исходное изображение</param>
        public CvMat Projection(CvMat src)
        {
            var result = new CvMat(1, CoeffsCount, MatrixType.F32C1);
            Cv.ProjectPCA(src, Avg, Evecs, result);
            return result;
        }

        /// <summary>
        /// Проецирование
        /// </summary>
        /// <param name="src">Исходное изображение</param>
        public unsafe CvMat Projection(IplImage src)
        {
            using (var data = new CvMat(1, src.Width * src.Height, MatrixType.F32C1))
            {
                //List<float> l = new List<float>();
                //List<byte> b = new List<byte>();
                //////////////////////////////////////////////////////////////
                //выпрямление по строкам
                //float* ddata = data.DataSingle;
                ////int counter = 0;
                //for (int y = 0; y < src.Height; y++)
                //{
                //    byte* ptr = src.DataByte + (src.Step * y);
                //    for (int x = 0; x < src.Width; x++)
                //    {
                //        //float v = ptr[x];
                //        //l.Add(v);
                //        *ddata++ = (float)ptr[x];
                //        //counter++;
                //    }
                //}
                //////////////////////////////////////////////////////////////
                //выпрямление по столбцам
                float* ddata = data.DataSingle;
                byte* ptr = src.ImageDataPtr;
                int step = src.WidthStep;
                for (int x = 0; x < src.Width; x++)
                {
                    for (int y = 0; y < src.Height; y++)
                    {
                        *ddata++ = ptr[step * y + x] == 255 ? 1 : 0;
                    }
                }
                //////////////////////////////////////////////////////////////
                var result = new CvMat(1, CoeffsCount, MatrixType.F32C1);
                Cv.ProjectPCA(src, Avg, Evecs, result);
                return result;
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        /// <param name="filename">Полный путь к файлу</param>
        public void Load(string filename)
        {
            using (var fs = new CvFileStorage(filename, null, FileStorageMode.Read))
            {
                CoeffsCount = fs.ReadIntByName(null, "CoeffsCount");

                CvFileNode param = fs.GetFileNodeByName(null, "Evecs");

                Evecs = fs.Read<CvMat>(param);
                param = fs.GetFileNodeByName(null, "Avg");
                Avg = fs.Read<CvMat>(param);
            }
        }

        /// <summary>
        /// Сохранение данных
        /// </summary>
        /// <param name="filename">Полный путь к файлу</param>
        /// <param name="headerMessage">Сообщение о сохраняемом файле, добавляемое в заголовок</param>
        public void Save(string filename, string headerMessage = "")
        {
            string fileHeader = string.Format("Copyright (c) {1} All Rights Reserved{0}File contains data set for principal component analysis.{0}Creation time: {2}{0}Message:{3}", 
                Environment.NewLine, DateTime.Now.Year, DateTime.Now, headerMessage);
            using (var fs = new CvFileStorage(filename, null, FileStorageMode.Write))
            {
                fs.WriteString("FileHeader", fileHeader);
                fs.WriteInt("CoeffsCount", CoeffsCount);
                fs.Write("Evecs", Evecs);
                fs.Write("Avg", Avg);
            }
        }
    }
}