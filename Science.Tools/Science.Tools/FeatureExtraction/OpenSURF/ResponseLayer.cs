namespace Science.Tools.FeatureExtraction.OpenSURF
{
    /// <summary>
    /// Reponse Layer 
    /// </summary>
    internal class ResponseLayer
    {
        public int width, height, step, filter;
        public float[] responses;
        public byte[] laplacian;

        public ResponseLayer(int width, int height, int step, int filter)
        {
            this.width = width;
            this.height = height;
            this.step = step;
            this.filter = filter;

            responses = new float[width * height];
            laplacian = new byte[width * height];
        }

        public byte getLaplacian(int row, int column)
        {
            return laplacian[row * width + column];
        }

        public byte getLaplacian(int row, int column, ResponseLayer src)
        {
            int scale = this.width / src.width;
            return laplacian[(scale * row) * width + (scale * column)];
        }

        public float getResponse(int row, int column)
        {
            return responses[row * width + column];
        }

        public float getResponse(int row, int column, ResponseLayer src)
        {
            int scale = this.width / src.width;
            return responses[(scale * row) * width + (scale * column)];
        }
    }
}