﻿using System;
using System.Collections.Generic;
using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    /// <summary>
    /// Множество обучающей выборки, состоящее из проекций главных компонент исходных данных
    /// </summary>
    public class PcaTrainingSet : ITrainingSet<PcaSample>
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
        public List<PcaSample> DataSet { get; set; }
    }
}