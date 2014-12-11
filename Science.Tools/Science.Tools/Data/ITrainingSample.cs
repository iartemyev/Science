using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITrainingSample<T>
    {
        /// <summary>
        /// 
        /// </summary>
        int Class { get; set; }

        /// <summary>
        /// 
        /// </summary>
        TrainingSampleTypes Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        T Data { get; set; }
    }
}