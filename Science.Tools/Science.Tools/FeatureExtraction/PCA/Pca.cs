using OpenCvSharp;

namespace Science.Tools.FeatureExtraction.PCA
{
    public class Pca : IPca
    {
        public CvArr Evecs { get; set; }
        public CvArr Avg { get; set; }
        public int CoeffsCount { get; set; }
        public void Calculation(CvArr data)
        {
            throw new System.NotImplementedException();
        }

        public void Projection(CvArr src, CvArr dst)
        {
            throw new System.NotImplementedException();
        }

        public void Load(string evecsFile, string avgFile)
        {
            throw new System.NotImplementedException();
        }

        public void Save(string evecsFile, string avgFile)
        {
            throw new System.NotImplementedException();
        }
    }
}