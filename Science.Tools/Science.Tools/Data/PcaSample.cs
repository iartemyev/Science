using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    public class PcaSample : ITrainingSample<float[]>
    {
        public int Class { get; set; }
        public TrainingSampleTypes Type { get; set; }
        public float[] Data { get; set; }
    }
}