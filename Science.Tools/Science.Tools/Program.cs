using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Science.Tools.FeatureExtraction.PCA;

namespace Science.Tools
{
    class Program
    {
        static void Main(string[] args)
        {

            string filename = "123.data";
            Pca pca = new Pca();
            pca.Avg= new CvMat(1, 10, MatrixType.F32C1, new float[]{1,2,3,4,5,6,7,8,9,0});
            pca.Evecs = new CvMat(10, 10, MatrixType.F32C1);
            pca.Evecs.Zero();
            pca.CoeffsCount = 10;

            pca.Save(filename, "hello world");
            pca = new Pca();
            pca.Load(filename);
            pca = new Pca();
        }
    }
}
