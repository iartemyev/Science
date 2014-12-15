using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Science.Tools.Data.Helpers;
using Science.Tools.Data.Types;

namespace Science.Tools.Data
{
    /// <summary>
    /// 
    /// </summary>
    public static class TrainingSetExtensions
    {
        // todo формирование обучающей выборки в требуемом формате
        // todo чтение
        // todo запись
        // todo Определение типа выборки
        //

        /// <summary>
        /// Сбор исходных кадров вагонов. По 3 кадра на вагон
        /// </summary>
        public static void TrainDataCollection(string trainFolder, string trainName, string dstFolder, int camIndex)
        {
            int fr1, fr2, fr3;
            var fIdx = 0;
            var cIdx = 0;
            var flag = false;
            var tfx = TrainFileXML.FromFile(string.Format("{0}/{1}.xml", trainFolder, trainName));
            var img = new IMGFile(string.Format("{0}/IMG/{1}.img", trainFolder, trainName));

            var frames = new IplImage[3];

            if (!Directory.Exists(string.Format("{0}/{1}", dstFolder, trainName)))
                Directory.CreateDirectory(string.Format("{0}/{1}", dstFolder, trainName));

            for (var i = 1; i < tfx.Wagons.Count; i++)
            {
                fr1 = tfx.Wagons[i].StartIndx + (tfx.Wagons[i].EndIndx - tfx.Wagons[i].StartIndx)/4;
                fr2 = tfx.Wagons[i].StartIndx + (tfx.Wagons[i].EndIndx - tfx.Wagons[i].StartIndx) / 2;
                fr3 = fr2 + fr2 - fr1;

                // инициализация объектов кадров
                for (var f = 0; f < frames.Length; f++)
                {
                    if (frames[f] != null && frames[f].Width == 768 && frames[f].Height == 576)
                    {
                        frames[f].ReleaseData();
                    }
                    frames[f] = new IplImage();
                }

                fIdx = fr1;
                do
                { // получение 1 интересующего кадра вагона
                    flag = img.GetImage(ref frames[0], fIdx, ref cIdx);
                    fIdx++;
                } while (cIdx != camIndex && fIdx < fr2);
                fr1 = fIdx;
                if (!flag) continue;

                fIdx = fr2;
                do
                { // получение 2 интересующего кадра вагона
                    flag = img.GetImage(ref frames[1], fIdx, ref cIdx);
                    fIdx++;
                } while (cIdx != camIndex && fIdx < fr3);
                fr2 = fIdx;
                if (!flag) continue;

                fIdx = fr3;
                do
                { // получение 3 интересующего кадра вагона
                    flag = img.GetImage(ref frames[2], fIdx, ref cIdx);
                    fIdx++;
                } while (cIdx != camIndex && fIdx < tfx.Wagons[i].EndIndx);
                fr3 = fIdx;
                if (!flag) continue;

                // выгрузка кадров, если они все собраны
                if ((frames[0] != null && frames[0].Width > 0) && (frames[1] != null && frames[1].Width > 0) && (frames[2] != null && frames[2].Width > 0))
                {
                    using (var pano = new IplImage(frames[0].Width * 3, frames[0].Height, frames[0].Depth, frames[0].NChannels))
                    {
                        Cv.SetImageROI(pano, new CvRect(0, 0, frames[2].Width, frames[2].Height));
                        frames[2].Copy(pano);
                        Cv.ResetImageROI(pano);
                        Cv.SetImageROI(pano, new CvRect(frames[2].Width, 0, frames[1].Width, frames[1].Height));
                        frames[1].Copy(pano);
                        Cv.ResetImageROI(pano);
                        Cv.SetImageROI(pano, new CvRect(frames[2].Width + frames[1].Width, 0, frames[0].Width, frames[0].Height));
                        frames[0].Copy(pano);
                        Cv.ResetImageROI(pano);
                        Cv.SaveImage(string.Format("{0}/{1}/{1}_Камера{2}_{5}_{4}_{3}.jpg", dstFolder, trainName, camIndex, fr1, fr2, fr3), pano);
                    }
                }
            }
        }

        public static void MoveFilesFreq(string srcFolder, string dstFolder, int freq)
        {
            var di = new DirectoryInfo(srcFolder);
            var files = di.GetFiles("*.jpg");
            for (var i = 0; i < files.Length; i++)
            {
                if (i%freq == 0)
                {
                    File.Move(files[i].FullName, string.Format("{0}/{1}", dstFolder, files[i].Name));
                }
            }
        }

        public static void WriteTrainDataToBinForPca(string folder, string filename)
        {
            var resize = 3;
            var roi = new CvRect(100, 70, 560, 350);
            int count = (int)(roi.Width / resize) * 3 * (int)(roi.Height / resize); // целевое количество примеров в бинарнике

            // загрузка данных
            var srcSet = new SrcTrainingSet
            {
                 Timestamp = DateTime.Now,
                 Type = TrainingSamplesTypes.Src,
                 DataSet = new List<SrcTrainingSample>()
            };
            var fInfo = new DirectoryInfo(folder);
            DirectoryInfo[] classsFolder = fInfo.GetDirectories();
            for (var i = 0; i < classsFolder.Length; i++)
            {
                FileInfo[] file = classsFolder[i].GetFiles("*.jpg");
                for (var f = 0; f < file.Length; f++)
                {
                    using (var srcPano = new IplImage(file[f].FullName, LoadMode.GrayScale))
                    using (var frame1 = new IplImage(srcPano.Width / 3, srcPano.Height, srcPano.Depth, srcPano.NChannels))
                    using (var frame2 = new IplImage(srcPano.Width / 3, srcPano.Height, srcPano.Depth, srcPano.NChannels))
                    using (var frame3 = new IplImage(srcPano.Width / 3, srcPano.Height, srcPano.Depth, srcPano.NChannels))
                    using (var roiF1 = new IplImage(roi.Size, srcPano.Depth, srcPano.NChannels))
                    using (var roiF2 = new IplImage(roi.Size, srcPano.Depth, srcPano.NChannels))
                    using (var roiF3 = new IplImage(roi.Size, srcPano.Depth, srcPano.NChannels))
                    using (var smallF1 = new IplImage(roi.Width / resize, roi.Height / resize, srcPano.Depth, srcPano.NChannels))
                    using (var smallF2 = new IplImage(roi.Width / resize, roi.Height / resize, srcPano.Depth, srcPano.NChannels))
                    using (var smallF3 = new IplImage(roi.Width / resize, roi.Height / resize, srcPano.Depth, srcPano.NChannels))
                    using (var dstPano = new IplImage(smallF1.Width * 3, smallF1.Height, srcPano.Depth, srcPano.NChannels))
                    {
                        // copy
                        Cv.SetImageROI(srcPano, new CvRect(0, 0, srcPano.Width / 3, srcPano.Height));
                        srcPano.Copy(frame1);
                        Cv.ResetImageROI(srcPano);

                        Cv.SetImageROI(srcPano, new CvRect(srcPano.Width / 3, 0, srcPano.Width / 3, srcPano.Height));
                        srcPano.Copy(frame2);
                        Cv.ResetImageROI(srcPano);

                        Cv.SetImageROI(srcPano, new CvRect(frame3.Width * 2, 0, srcPano.Width / 3, srcPano.Height));
                        srcPano.Copy(frame3);
                        Cv.ResetImageROI(srcPano);

                        // roi
                        Cv.SetImageROI(frame1, roi);
                        frame1.Copy(roiF1);
                        Cv.ResetImageROI(frame1);

                        Cv.SetImageROI(frame2, roi);
                        frame2.Copy(roiF2);
                        Cv.ResetImageROI(frame2);

                        Cv.SetImageROI(frame3, roi);
                        frame3.Copy(roiF3);
                        Cv.ResetImageROI(frame3);

                        // resize
                        Cv.Resize(roiF1, smallF1, Interpolation.Linear);
                        Cv.Resize(roiF2, smallF2, Interpolation.Linear);
                        Cv.Resize(roiF3, smallF3, Interpolation.Linear);

                        // invert
                        smallF1.Not(smallF1);
                        smallF2.Not(smallF2);
                        smallF3.Not(smallF3);

                        // binarize
                        Binarizer.SauvolaFast(smallF1, smallF1, 15, 0.1, 128);
                        Binarizer.SauvolaFast(smallF2, smallF2, 15, 0.1, 128);
                        Binarizer.SauvolaFast(smallF3, smallF3, 15, 0.1, 128);

                        // pano
                        Cv.SetImageROI(dstPano, new CvRect(0, 0, smallF1.Width, dstPano.Height));
                        smallF1.Copy(dstPano);
                        Cv.ResetImageROI(dstPano);

                        Cv.SetImageROI(dstPano, new CvRect(smallF1.Width, 0, smallF1.Width, dstPano.Height));
                        smallF2.Copy(dstPano);
                        Cv.ResetImageROI(dstPano);

                        Cv.SetImageROI(dstPano, new CvRect(smallF1.Width * 2, 0, smallF1.Width, dstPano.Height));
                        smallF3.Copy(dstPano);
                        Cv.ResetImageROI(dstPano);

                        //// debug
                        //Cv.SaveImage("dstPano.Jpg", dstPano);

                        // add to list
                        srcSet.DataSet.Add(new SrcTrainingSample { Class = i, Data = dstPano.Clone(), SampleInfo = file[f].Name});
                    }
                }
            }

            // запись данных в бинарник

            using (var bw = new BinaryWriter(new FileStream(filename, FileMode.Create), new UTF8Encoding()))
            {
                long c = 0;
                do
                {
                    for (int j = 0; j < srcSet.DataSet.Count; j++)
                    {
                        c++;
                        bw.Write(srcSet.DataSet[j].Class);
                        bw.Write(srcSet.DataSet[j].Data.ConvertToBytes());
                    }
                } while (c < count);
            }

        }

        /// <summary>
        /// Конвертация изображения в массив байт (со значениями 1 и 0)
        /// </summary>
        /// <param name="image">Используемый объект изображения</param>
        /// <returns>Возвращает массив байт</returns>
        private static unsafe byte[] ConvertToBytes(this IplImage image)
        {
            var bytes = new byte[image.Width * image.Height];
            byte* ptr = image.ImageDataPtr;
            int offset = 0;
            fixed (byte* bPtr = bytes)
            {
                byte* _bPtr = bPtr;
                for (int y = 0; y < image.Height; y++)
                {
                    offset = image.WidthStep*y;
                    for (int x = 0; x < image.Width; x++)
                    {
                        *_bPtr = (byte)(ptr[offset + x] == 0 ? 0 : 1);
                        _bPtr++;
                    }
                }
            }
            return bytes;
        }


    }
}