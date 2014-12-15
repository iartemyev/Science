using System;
using System.Collections.Generic;
using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    /// <summary>
    /// Исходная обучающая выборка
    /// </summary>
    public class SrcTrainingSet : ITrainingSet<SrcTrainingSample>
    {
        /// <summary>
        /// Метка времени
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Тип данных выборки
        /// </summary>
        public TrainingSamplesTypes Type { get; set; }

        /// <summary>
        /// Множество данных обучающей выборки
        /// </summary>
        public List<SrcTrainingSample> DataSet { get; set; }
    }
}