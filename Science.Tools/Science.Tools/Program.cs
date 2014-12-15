using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Science.Tools.Data;
using Science.Tools.FeatureExtraction.PCA;

namespace Science.Tools
{
    class Program
    {
        static void Main(string[] args)
        {

            TrainingSetExtensions.WriteTrainDataToBinForPca(@"D:\usarnv\RailOCR\Данные\TrainDataSet(classes-4,count-100)", "data.bin");
            return;
            TrainingSetExtensions.MoveFilesFreq(
                @"D:\usarnv\RailOCR\Данные\TrainDataSet(classes-4,count-100)\4 Крытый вагон 4",
                @"D:\usarnv\RailOCR\Данные\TestDataSet(classes-8,count-100)\4 Крытый вагон 4", 2);
            return;
            string yearPath = @"D:\usarnv\RailOCR\Данные\2014";
            string moth = "Октябрь";
            Console.WriteLine(moth);
            string dataset = string.Format(@"D:\usarnv\RailOCR\Данные\DataSet\{0}", moth);
            Console.WriteLine(dataset);
            Directory.CreateDirectory(dataset);
            var di = new DirectoryInfo(string.Format("{0}/{1}", yearPath, moth));
            DirectoryInfo[] trains = di.GetDirectories();
            Console.WriteLine(trains.Length);
            for (int i = (int)(trains.Length * 0.77); i < trains.Length; i++)
            {
                try
                {
                    var train = trains[i];
                    Console.WriteLine(train.Name);
                    string trainFolder = train.FullName,
                        trainName = train.Name,
                        dstFolder = dataset;
                    int camIndex = 1;
                    TrainingSetExtensions.TrainDataCollection(trainFolder, trainName, dstFolder, camIndex);
                    Console.WriteLine((double)i / (double)trains.Length * 100.0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    try
                    {
                        Directory.Delete(string.Format("{0}/{1}", dataset, trains[i].Name), true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            Console.ReadLine();
            //string trainFolder = @"D:\usarnv\RailOCR\Данные\2014\Октябрь\Состав_2014_10_07-17_44_29",
            //    trainName = "Состав_2014_10_07-17_44_29",
            //    dstFolder = AppDomain.CurrentDomain.BaseDirectory;
            //int camIndex = 1;
            //TrainingSetExtensions.TrainDataCollection(trainFolder, trainName, dstFolder, camIndex);
            //string filename = "123.data";
        }
    }
}
