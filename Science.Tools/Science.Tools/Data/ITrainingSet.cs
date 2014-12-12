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
        DateTime Timestamp { get; }

        /// <summary>
        /// Тип данных выборки
        /// </summary>
        TrainingSamplesTypes Type { get; }

        /// <summary>
        /// Множество данных обучающей выборки
        /// </summary>
        IEnumerable<T> DataSet { get; set; }
    }
}