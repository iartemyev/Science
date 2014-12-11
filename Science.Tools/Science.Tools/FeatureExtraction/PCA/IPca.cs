using OpenCvSharp;

namespace Science.Tools.FeatureExtraction.PCA
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPca
    {
        /// <summary>
        /// 
        /// </summary>
        CvArr Evecs { get; set; }

        /// <summary>
        /// 
        /// </summary>
        CvArr Avg { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int CoeffsCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        void Calculation(CvArr data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        void Projection(CvArr src, CvArr dst);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evecsFile"></param>
        /// <param name="avgFile"></param>
        void Load(string evecsFile, string avgFile);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evecsFile"></param>
        /// <param name="avgFile"></param>
        void Save(string evecsFile, string avgFile);
    }
}