using System;
using System.Collections.Generic;
using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    /// <summary>
    /// Определяет основные свойства множества обучающей выборки
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITrainingSet<T>
    {
        /// <summary>
        /// Метка времени
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Тип данных выборки
        /// </summary>
        TrainingSamplesTypes Type { get; set; }

        /// <summary>
        /// Множество данных обучающей выборки
        /// </summary>
        List<T> DataSet { get; set; }
    }
}