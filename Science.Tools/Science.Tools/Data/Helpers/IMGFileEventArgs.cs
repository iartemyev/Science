using System;

namespace Science.Tools.Data.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public class IMGFileEventArgs : EventArgs
    {
        /// <summary>
        /// Общее количество итераций операции
        /// </summary>
        public int Count;
        /// <summary> 
        /// Значение текущей итерации 
        /// </summary>
        public int Value;
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message;
        /// <summary>
        /// Тип выполняемой операции
        /// </summary>
        public IMGFileTransactionType TransactionType;
        /// <summary>
        /// 
        /// </summary>
        public IMGFileEventArgs(int count, int value, IMGFileTransactionType type, string message) 
        {
            Count = count;
            Value = value;
            TransactionType = type;
            Message = message;
        }
    }
}
