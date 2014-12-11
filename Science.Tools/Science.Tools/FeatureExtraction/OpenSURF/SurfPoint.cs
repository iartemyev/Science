namespace Science.Tools.FeatureExtraction.OpenSURF
{
    public class SurfPoint
    {
        /// <summary>
        /// Default ctor
        /// </summary>
        public SurfPoint()
        {
            orientation = 0;
        }

        /// <summary>
        /// Coordinates of the detected interest point
        /// </summary>
        public float x;

        /// <summary>
        /// Coordinates of the detected interest point
        /// </summary>
        public float y;

        /// <summary>
        /// Detected scale
        /// </summary>
        public float scale;

        /// <summary>
        /// Response of the detected feature (strength)
        /// </summary>
        public float response;

        /// <summary>
        /// Orientation measured anti-clockwise from +ve x-axis
        /// </summary>
        public float orientation;

        /// <summary>
        /// Sign of laplacian for fast matching purposes
        /// </summary>
        public int laplacian;

        /// <summary>
        /// Descriptor vector
        /// </summary>
        public int descriptorLength;

        public float[] descriptor = null;

        public void SetDescriptorLength(int Size)
        {
            descriptorLength = Size;
            descriptor = new float[Size];
        }
    }
}
