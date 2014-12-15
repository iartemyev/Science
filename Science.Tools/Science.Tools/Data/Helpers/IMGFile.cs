using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;

namespace Science.Tools.Data.Helpers
{
    /// <summary>
    /// Предоставляет возможность работы с бинарным файлом кадров состава (*.img)
    /// </summary>
    [Serializable]
    public class IMGFile
    {
        public const string VERSION = "1.1";
        /// <summary>
        /// Событие начала процесса
        /// </summary>
        public event StartIMGFileEventHandler       Start;
        /// <summary>
        /// Событие новой итерации процесса
        /// </summary>
        public event ProcessIMGFileEventHandler     Processing;
        /// <summary>
        /// Событие завершения процесса
        /// </summary>
        public event CompleteIMGFileEventHandler    Complete;
        /// <summary>
        /// Событие ошибки
        /// </summary>
        public event ErrorIMGFileEventHandler       Error;
        /// <summary>
        /// Заголовок файла
        /// </summary>
        public TRAINIMAGEFILEHEADER                 Header;
        /// <summary>
        /// Данные
        /// </summary>
        TRAINIMAGEFILEDATA[]                        Data;
        /// <summary>
        /// Имя считанного файла
        /// </summary>
        public string                               FileName;
        /// <summary>
        /// Размер заголовка в байтах
        /// </summary>
        public int                                  SizeOfHeader;
        /// <summary>
        /// Для паузы, отмены ...
        /// </summary>
        object syncObj = new object();
        /// <summary>
        /// Флаг паузы
        /// </summary>
        bool _paused;
        /// <summary>
        /// Сигнал токену для отмены выполняемой длительной операции
        /// </summary>
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        /// <summary>
        /// Токен для отмены выполняемой длительной операции
        /// </summary>
        CancellationToken ct;
        /// <summary>
        /// Используется кодировка UTF-8
        /// </summary>
        public Encoding FileEncoding = new UTF8Encoding();
        /// <summary>
        /// Инициализация значениями по умолчанию для работы с новым файлом
        /// </summary>
        public IMGFile() 
        {

        }
        /// <summary>
        /// Инициализация работы с новым файлом
        /// </summary>
        public IMGFile(string fileName)
        {
            using (var br = new BinaryReader(System.IO.File.Open(fileName, FileMode.Open), FileEncoding))
            {
                ReadFileHeader(br);
                Data = new TRAINIMAGEFILEDATA[Header.F4_Count];
            } 
            FileName = fileName;
            SizeOfHeader = Header.GetSize();
        }
        /// <summary>
        /// Определяет, соответсвует ли формат указаного файл 
        /// </summary>
        public static bool Correspond(string fileName) 
        {
            var utf8 = new UTF8Encoding();
            using (var br = new BinaryReader(System.IO.File.Open(fileName, FileMode.Open), utf8))
            {
                DateTime createTime = DateTime.FromFileTime(br.ReadInt64());
                string val = br.ReadString();
                if (val.StartsWith("Состав"))
                {// старый формат
                    return false;
                }
                if (br.ReadString().StartsWith("Состав")) 
                {// соответствует
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Загрузка файла в память
        /// </summary>
        public void Load()
        {
            using (var br = new BinaryReader(System.IO.File.Open(FileName, FileMode.Open), FileEncoding))
            {   // считывание данных
                ReadFileHeader(br);
                ReadFileData(br);
            }
        }

        /// <summary>
        /// Инициализация работы с новым файлом
        /// </summary>
        public void ReadFile(string fileName) 
        {
            try
            {
                using (var br = new BinaryReader(System.IO.File.Open(fileName, FileMode.Open), FileEncoding))
                {
                    ReadFileHeader(br);
                    Data = new TRAINIMAGEFILEDATA[Header.F4_Count];
                }
                FileName = fileName;
                SizeOfHeader = Header.GetSize();
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.ReadFile, ex.Message));
            }
        }

        /// <summary>
        /// Запись в бинарный файл в формате *.img
        /// </summary>
        public bool WriteFile(string dstFolder)
        {
            try
            {
                // проверка наличия свободного места на диске
                var dInfo = new DirectoryInfo(dstFolder);
                var driveInfo = new DriveInfo(dInfo.Root.Name);
                var size = Header.F7_StreamPosition.Max();
                if (driveInfo.TotalFreeSpace < size + 1024)
                {
                    throw new Exception(string.Format("Недостаточно свободного места на диске {0}", dInfo.Root.Name));
                }
                // выгрузка кадров в файл
                FileName = string.Format("{0}/{1}.img", dstFolder, Header.F2_TrainName);
                using (var bw = new BinaryWriter(System.IO.File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.None), FileEncoding))
                {
                    WriteFile(bw);
                }
                return true;
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
            }
            return false;
        }

        /// <summary>
        /// Запись в бинарный файл в формате *.img
        /// </summary>
        public bool WriteFile(BinaryWriter bw)
        {
            try
            {
                WriteFileHeader(bw);
                WriteFileData(bw);
                return true;
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
            }
            return false;
        }

        public byte[] GetConsumingArray()
        {
            var lst = new List<byte>();
            using (var stream = new MemoryStream())
            using (var bw = new BinaryWriter(stream, this.FileEncoding))
            {
                bw.Write(Header.F0_CreateTime.ToFileTime());
                bw.Write(Header.F1_Version);
                bw.Write(Header.F2_TrainName);
                bw.Write(Header.F3_Reserved);
                bw.Write(Header.F4_Count);
                bw.Write(Header.F5_ActualCount);
                for (int i = 0; i < 4; i++)
                {
                    bw.Write(Header.F6_Resolution[i].F0_Width);
                    bw.Write(Header.F6_Resolution[i].F1_Heght);
                }
                var bArray = new byte[Header.F7_StreamPosition.Length*sizeof (long)];
                Buffer.BlockCopy(Header.F7_StreamPosition, 0, bArray, 0, bArray.Length);
                bw.Write(bArray);
                lst.AddRange(stream.ToArray());
            }
            for (int i = 0; i < Data.Length; i++)
            {
                //lock (syncObj)
                //{   // для паузы
                //    lst.AddRange(BitConverter.GetBytes(Data[i].F0_Index));
                //    lst.AddRange(BitConverter.GetBytes(Data[i].F1_Camera));
                //    lst.AddRange(BitConverter.GetBytes(Data[i].F2_Size));
                //    lst.AddRange(Data[i].F3_DataArray);
                //}
                lst.AddRange(Data[i].GetBytes());
            }
            byte[] array = lst.ToArray();
            lst.Clear();
            return array;
        }

        /// <summary>
        /// Упаковка буфера кадров в памяти в формате *.img
        /// </summary>
        public bool PackageFileInMemory(FRAMEINFO[] frames)
        {
            try
            {
                Data = new TRAINIMAGEFILEDATA[frames.Length];
                int pc = Data.Length / 100;
                for (int i = 0; i < Data.Length; i++)
                {
                    lock (syncObj)
                    {// для паузы
                        Data[i].F0_Index = frames[i].F0_Index;
                        Data[i].F1_Camera = frames[i].F1_Camera;
                        if (frames[i].F2_Image != null)
                        {
                            Data[i].F2_Size = frames[i].F2_Image.Cols;
                            Data[i].F3_DataArray = new byte[Data[i].F2_Size];
                            unsafe
                            {
                                byte* ptr = frames[i].F2_Image.DataByte;
                                for (int c = 0; c < frames[i].F2_Image.Cols; c++)
                                {
                                    Data[i].F3_DataArray[c] = ptr[c];
                                }
                            }
                        }
                        else
                        {
                            Data[i].F2_Size = 0;
                            Data[i].F3_DataArray = new byte[Data[i].F2_Size];
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
            }
            return false;
        }

        /// <summary>
        /// Упаковка в бинарный файл буфера кадров
        /// </summary>
        public bool PackageFile(FRAMEINFO[] frames, string dstFolder) 
        {
            try
            {
                // проверка наличия свободного места на диске
                var dInfo = new DirectoryInfo(dstFolder);
                var driveInfo = new DriveInfo(dInfo.Root.Name);
                var size = Header.F7_StreamPosition.Max();
                if (driveInfo.TotalFreeSpace < size + 1024)
                {
                    throw new Exception(string.Format("Недостаточно свободного места на диске {0}", dInfo.Root.Name));
                }
                // выгрузка кадров в файл
                FileName = string.Format("{0}/{1}.img", dstFolder, Header.F2_TrainName);
                using (var bw = new BinaryWriter(System.IO.File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.None), FileEncoding))
                {
                    WriteFileHeader(bw);
                    WriteFileData(bw, frames);
                    frames = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
            }
            return false;
        }
        /// <summary>
        /// Упаковка папки IMG в бинарный файл
        /// </summary>
        public bool PackageFile(string srcFolder, string dstFolder)
        {
            try
            {
                // чтение данных и формирование заголовка
                DirectoryInfo imgFolder = new DirectoryInfo(srcFolder);
                DirectoryInfo[] camFolders = imgFolder.GetDirectories();
                if (camFolders.Length != 4) return false;
                List<FRAMEINFO> frames = new List<FRAMEINFO>();
                Header.F0_CreateTime = DateTime.Now;
                Header.F1_Version = VERSION;
                Header.F2_TrainName = imgFolder.Parent.Name;
                Header.F3_Reserved = string.Empty;
                Header.F6_Resolution = new CAMERARESOLUTION[4];
                FileInfo[] files;
                int index = 0;
                if (Start != null)
                {
                    Start(this, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, "Старт упаковки " + Header.F2_TrainName));
                }
                int pc = 0;
                for (int i = 0; i < camFolders.Length; i++)
                {
                    files = camFolders[i].GetFiles("*.jpg");
                    pc = files.Length / 100;
                    for (int j = 0; j < files.Length; j++)
                    {
                        index = System.Convert.ToInt32(files[j].Name.Split('.')[0]);
                        lock (syncObj)
                        {// для паузы
                            using (IplImage img = Cv.LoadImage(files[j].FullName, LoadMode.GrayScale))
                            {
                                if (img != null)
                                {
                                    frames.Add(new FRAMEINFO()
                                    {
                                        F0_Index = index,
                                        F1_Camera = i + 1,
                                        F2_Image = Cv.EncodeImage(".jpg", img)
                                    });
                                    Header.F6_Resolution[i].F0_Width = img.Width;
                                    Header.F6_Resolution[i].F1_Heght = img.Height;
                                }
                            }
                        }
                        if (Processing != null && j % pc == 0)
                        {
                            int value = i * (100 / camFolders.Length) + j / camFolders.Length / pc;
                            if (value <= 100)
                            {
                                Processing(this, new IMGFileEventArgs(100, value, IMGFileTransactionType.PackageFile, string.Format("Считывание кадров {0}. Камера{1}", Header.F2_TrainName, i + 1)));
                            }
                        }
                    }
                }
                if (frames.Count == 0)
                {
                    return false;
                }
                frames.Sort(new ComparerFramesByIndex());
                Header.F5_ActualCount = frames.Count;
                Header.F4_Count = frames.Last().F0_Index + 1;
                Header.F7_StreamPosition = new long[Header.F4_Count];
                SizeOfHeader = Header.GetSize();
                long position = SizeOfHeader;
                for (int i = 0; i < frames.Count; i++)
                {// определение позиций кадров в бинарнике для быстрой навигации
                    Header.F7_StreamPosition[frames[i].F0_Index] = position;
                    position += frames[i].GetSize();
                }
                //// запись данных и формирование массива данных
                FileName = string.Format("{0}/{1}.img", dstFolder == string.Empty ? srcFolder : dstFolder, Header.F2_TrainName);
                using (BinaryWriter bw = new BinaryWriter(System.IO.File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.None), FileEncoding))
                {
                    WriteFileHeader(bw);
                    WriteFileData(bw, frames.ToArray());
                }
                frames.Clear();
                if (Complete != null)
                {
                    Complete(this, new IMGFileEventArgs(100, 100, IMGFileTransactionType.PackageFile, "Завершение упаковки " + Header.F2_TrainName));
                }
            }
            catch (Exception ex) 
            { 
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
            }
            return true;
        }
        /// <summary>
        /// Распаковка бинарного файла в указанную папку
        /// </summary>
        public bool UnpackageFile(string dstFolder) 
        {
            bool flag = false;
            try
            {
                if (Start != null)
                {
                    Start(this, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, "Старт распаковки " + Header.F2_TrainName));
                }
                // формирование директорий
                DirectoryInfo imgFolder = new DirectoryInfo(dstFolder);
                for (int i = 0; i < 4; i++)
                {
                    if (!Directory.Exists(string.Format("{0}/Камера{1}", dstFolder, i + 1)))
                    {
                        Directory.CreateDirectory(string.Format("{0}/Камера{1}", dstFolder, i + 1));
                    }
                }
                using (StreamWriter sw = new StreamWriter(string.Format("{0}/{1}.txt", dstFolder, Header.F2_TrainName), true))
                {
                    sw.WriteLine(Header.ToString());
                }
                // распаковка
                int pc = Header.F7_StreamPosition.Length / 100;
                IplImage img = new IplImage();
                int camera = 0;
                for (int i = 0; i < Header.F7_StreamPosition.Length; i++)
                {
                    lock (syncObj)
                    {// для паузы
                        if (GetImage(ref img, i, ref camera))
                        {
                            Cv.SaveImage(string.Format("{0}/Камера{1}/{2}.jpg", dstFolder, Data[i].F1_Camera, Data[i].F0_Index), img);
                        }
                        if (Processing != null && i % pc == 0)
                        {
                            Processing(this, new IMGFileEventArgs(Header.F7_StreamPosition.Length, i, IMGFileTransactionType.PackageFile, string.Format("Распаковка кадров кадров. {0}", Header.F2_TrainName)));
                        }
                    }
                }
                img.ReleaseData();
                if (Complete != null)
                {
                    Complete(this, new IMGFileEventArgs(100, 100, IMGFileTransactionType.PackageFile, "Завершение распаковки " + Header.F2_TrainName));
                }
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, ex.Message));
                flag = false;
            }
            return flag;
        }
        /// <summary>
        /// Конвертация в новый формат
        /// </summary>
        public unsafe bool Convert(string srcFile) 
        {
            try
            {
                if (!IMGFile.Correspond(srcFile))
                {
                    if (Start != null)
                    {
                        Start(this, new IMGFileEventArgs(0, 0, IMGFileTransactionType.PackageFile, string.Format("Старт конвертации файла {0}", srcFile)));
                    }

                    if (Complete != null)
                    {
                        Complete(this, new IMGFileEventArgs(100, 100, IMGFileTransactionType.Convert, string.Format("{0}. Конвертация успешно завершена", Header.F2_TrainName)));
                    }
                }
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.Convert, ex.Message));
            }
            return false;
        }

        //void tif_Processing(object sender, TrainImageFileEventArgs e)
        //{
        //    if (Processing != null)
        //    {
        //        Processing(this, new IMGFileEventArgs(e.Count, e.Value, IMGFileTransactionType.Convert, string.Format("{0}. Считывание данных.", (sender as TrainImageFile).F0_Header.F1_TrainName)));
        //    }
        //}

        //void tif_Error(object sender, TrainImageFileEventArgs e)
        //{
        //    if (Error != null) Error(this, new IMGFileEventArgs(e.Count, e.Value, IMGFileTransactionType.Convert, e.ErrorMes));
        //}
        /// <summary>
        /// Запрос изображения по индексу
        /// </summary>
        public unsafe bool GetImage(ref IplImage img, int index, ref int camera) 
        {
            try
            {
                if (Header.F7_StreamPosition != null && Header.F7_StreamPosition[index] != 0)
                {
                    if (Data[index].F3_DataArray == null)
                    {// кадр еще не был считан
                        using (var br = new BinaryReader(System.IO.File.Open(FileName, FileMode.Open), FileEncoding))
                        {
                            long pos = Header.F7_StreamPosition[index];
                            if (pos <= 0)
                            {// в случае, если поучено отрицательное значение позиции, записанное по причине переполнения int-величины
                                long val = int.MinValue;
                                val = Math.Abs(val); 
                                pos += val;
                                pos += int.MaxValue;
                                pos += 1;
                            }
                            br.BaseStream.Position = pos;
                            Data[index].F0_Index = br.ReadInt32();
                            Data[index].F1_Camera = br.ReadInt32();
                            Data[index].F2_Size = br.ReadInt32();
                            Data[index].F3_DataArray = br.ReadBytes(Data[index].F2_Size);
                            //
                            camera = Data[index].F1_Camera;
                            using (var m = new CvMat(1, Data[index].F2_Size, MatrixType.U8C1, Data[index].F3_DataArray))
                            {
                                img = Cv.DecodeImage(m, LoadMode.GrayScale);
                            }
                            //
                            Data[index].F3_DataArray = null;
                            //
                            return true;
                        }
                    }
                    // кадр уже есть в кэше
                    camera = Data[index].F1_Camera;
                    using (var m = new CvMat(1, Data[index].F2_Size, MatrixType.U8C1, Data[index].F3_DataArray))
                    {
                        img = Cv.DecodeImage(m, LoadMode.GrayScale);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.Other, ex.Message));
            }
            return false;
        }
        /// <summary>
        /// Прореживание данных с заданной частотой
        /// </summary>
        public unsafe bool Reduction(int frequency) 
        {
            try
            {
                if (FileName == null || Header.F7_StreamPosition == null)
                {
                    throw new Exception("Файл не был считан.");
                }
                if (Start != null)
                {
                    Start(this, new IMGFileEventArgs(0, 0, IMGFileTransactionType.Other, string.Format("{0}. Старт прореживания данных с заданной частотой (каждый {1})", Header.F2_TrainName, frequency)));
                }
                using (BinaryReader br = new BinaryReader(System.IO.File.Open(FileName, FileMode.Open), FileEncoding))
                {// считывание данных
                    ReadFileHeader(br);
                    if (Header.F5_ActualCount < 100)
                    {// осталось минимальное кол-во кадров
                        if (Complete != null)
                        {
                            Complete(this, new IMGFileEventArgs(100, 100, IMGFileTransactionType.Other, string.Format("{0}. В файле осталось минимальное количество кадров.", Header.F2_TrainName)));
                        }
                        return true;
                    }
                    Data = new TRAINIMAGEFILEDATA[Header.F5_ActualCount];
                    ReadFileData(br);
                }
                // прореживание
                if (Processing != null)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, Data.Length, IMGFileTransactionType.Other, string.Format("{0}. Прореживание кадров", Header.F2_TrainName)));
                }
                Data = Data.Where((value, index) => index % frequency == 0)
                    .ToArray();
                // перезаполнение заголовка
                if (Processing != null)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, Data.Length, IMGFileTransactionType.Other, string.Format("{0}. Перезаполнение заголовка", Header.F2_TrainName)));
                }
                Header.F5_ActualCount = Data.Length;
                Header.F7_StreamPosition = new long[Header.F4_Count];
                long position = SizeOfHeader;
                int idx = 0;
                fixed (long* ptr = &Header.F7_StreamPosition[0])
                {
                    for (int i = 0; i < Data.Length; i++)
                    {
                        idx = Data[i].F0_Index;
                        ptr[idx] = position;
                        position += Data[i].GetSize();
                    }
                }
                // выгрузка во временный файл
                if (Processing != null)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, Data.Length, IMGFileTransactionType.Other, string.Format("{0}. Выгрузка в файл", Header.F2_TrainName)));
                }
                string filename = FileName.Replace(".img", ".bin");
                using (BinaryWriter bw = new BinaryWriter(System.IO.File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None), FileEncoding))
                {
                    WriteFileHeader(bw);
                    WriteFileData(bw);
                }
                // замена старого файла
                if (Processing != null)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, Data.Length, IMGFileTransactionType.Other, string.Format("{0}. Замена старого файла", Header.F2_TrainName)));
                }
                System.IO.File.Delete(FileName);
                System.IO.File.Move(filename, FileName);
                if (Complete != null)
                {
                    Complete(this, new IMGFileEventArgs(100, 100, IMGFileTransactionType.Other, string.Format("{0}. Прореживание данных с частотой (каждый {1}) завершено", Header.F2_TrainName, frequency)));
                }
            }
            catch (Exception ex)
            {
                Catcher(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, new IMGFileEventArgs(0, 0, IMGFileTransactionType.Other, ex.Message));
            }
            return false;
        }
        /// <summary>
        /// Пауза
        /// </summary>
        public void Pause()
        {
            if (_paused == false)
            {
                Monitor.Enter(syncObj);
                _paused = true;
            }
        }
        /// <summary>
        /// Возобновление
        /// </summary>
        public void Resume()
        {
            if (_paused)
            {
                _paused = false;
                Monitor.Exit(syncObj);
            }
        }
        /// <summary>
        /// Отмена
        /// </summary>
        public void Cancel() 
        {
            tokenSource.Cancel();
        }
        /// <summary>
        /// Считывание заголовка файла
        /// </summary>
        void ReadFileHeader(BinaryReader br) 
        {
            Header.F0_CreateTime = DateTime.FromFileTime(br.ReadInt64());
            Header.F1_Version = br.ReadString();
            Header.F2_TrainName = br.ReadString();
            Header.F3_Reserved = br.ReadString();
            Header.F4_Count = br.ReadInt32();
            Header.F5_ActualCount = br.ReadInt32();
            Header.F6_Resolution = new CAMERARESOLUTION[4];
            for (int i = 0; i < 4; i++)
            {
                Header.F6_Resolution[i].F0_Width = br.ReadInt32();
                Header.F6_Resolution[i].F1_Heght = br.ReadInt32();
            }
            byte[] bArr = br.ReadBytes(Header.F4_Count * sizeof(long));
            Header.F7_StreamPosition = new long[Header.F4_Count];
            Buffer.BlockCopy(bArr, 0, Header.F7_StreamPosition, 0, bArr.Length);
            SizeOfHeader = (int)br.BaseStream.Position;
        }
        /// <summary>
        /// Считывание данных файла
        /// </summary>
        void ReadFileData(BinaryReader br)
        {
            int pc = Data.Length / 100;
            for (int i = 0; i < Data.Length; i++)
            {
                lock (syncObj)
                {   // для паузы
                    if (br.BaseStream.Length - br.BaseStream.Position > 12)
                    {
                        Data[i].F0_Index = br.ReadInt32();
                        Data[i].F1_Camera = br.ReadInt32();
                        Data[i].F2_Size = br.ReadInt32();
                        if (br.BaseStream.Length - br.BaseStream.Position >= Data[i].F2_Size)
                            Data[i].F3_DataArray = br.ReadBytes(Data[i].F2_Size);
                    }
                }
                if (Processing != null && i % pc == 0)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, i, IMGFileTransactionType.ReadFile, string.Format("{0}. Считывание кадров", Header.F2_TrainName)));
                }
            }
        }
        /// <summary>
        /// Запись заголовка файла
        /// </summary>
        void WriteFileHeader(BinaryWriter bw) 
        {
            bw.Write(Header.F0_CreateTime.ToFileTime());
            bw.Write(Header.F1_Version);
            bw.Write(Header.F2_TrainName);
            bw.Write(Header.F3_Reserved);
            bw.Write(Header.F4_Count);
            bw.Write(Header.F5_ActualCount);
            for (int i = 0; i < 4; i++)
            {
                bw.Write(Header.F6_Resolution[i].F0_Width);
                bw.Write(Header.F6_Resolution[i].F1_Heght);
            }
            var bArray = new byte[Header.F7_StreamPosition.Length * sizeof(long)];
            Buffer.BlockCopy(Header.F7_StreamPosition, 0, bArray, 0, bArray.Length);
            bw.Write(bArray);
        }
        /// <summary>
        /// Запись данных - кадров состава из буффера
        /// </summary>
        void WriteFileData(BinaryWriter bw, FRAMEINFO[] frames) 
        {
            Data = new TRAINIMAGEFILEDATA[frames.Length];
            int pc = Data.Length / 100;
            for (int i = 0; i < Data.Length; i++)
            {
                lock (syncObj)
                {// для паузы
                    Data[i].F0_Index = frames[i].F0_Index;
                    Data[i].F1_Camera = frames[i].F1_Camera;
                    if (frames[i].F2_Image != null)
                    {
                        Data[i].F2_Size = frames[i].F2_Image.Cols;
                        Data[i].F3_DataArray = new byte[Data[i].F2_Size];
                        unsafe
                        {
                            byte* ptr = frames[i].F2_Image.DataByte;
                            for (int c = 0; c < frames[i].F2_Image.Cols; c++)
                            {
                                Data[i].F3_DataArray[c] = ptr[c];
                            }
                        }
                    }
                    else
                    {
                        Data[i].F2_Size = 0;
                        Data[i].F3_DataArray = new byte[Data[i].F2_Size];
                    }
                }
                if (Processing != null && i % pc == 0)
                {
                    Processing(this, new IMGFileEventArgs(Data.Length, i, IMGFileTransactionType.PackageFile, string.Format("{0}. Выгрузка кадров в файл", Header.F2_TrainName)));
                }
            }
            WriteFileData(bw);
        }
        /// <summary>
        /// Запись данных - кадров состава
        /// </summary>
        void WriteFileData(BinaryWriter bw) 
        {
            for (int i = 0; i < Data.Length; i++)
            {
                lock (syncObj)
                {   // для паузы
                    bw.Write(Data[i].F0_Index);
                    bw.Write(Data[i].F1_Camera);
                    bw.Write(Data[i].F2_Size);
                    bw.Write(Data[i].F3_DataArray);
                }
            }
        }
        /// <summary>
        /// Обработчик исключительных ситуаций
        /// </summary>
        void Catcher(Exception ex, string method, IMGFileEventArgs e)
        {
            if (Error != null) Error(this, e);
        }
        /// <summary>
        /// Представляет механизм для сортировки буфера кадров по индексу
        /// </summary>
        public class ComparerFramesByIndex : IComparer<FRAMEINFO>
        {
            int IComparer<FRAMEINFO>.Compare(FRAMEINFO x, FRAMEINFO y)
            {
                return (int)(x.F0_Index - y.F0_Index);
            }
        }
        /// <summary>
        /// Инкапсуляция полей заголовка файла
        /// </summary
        [Serializable]
        public struct TRAINIMAGEFILEHEADER
        {
            /// <summary>
            /// Время создания файла
            /// </summary>
            public DateTime         F0_CreateTime;
            /// <summary>
            /// Версия файла
            /// </summary>
            public string           F1_Version;
            /// <summary>
            /// Имя состава, которому принадлежат изображения
            /// </summary>
            public string           F2_TrainName;
            /// <summary>
            /// Зарезервированное поле
            /// </summary>
            public string           F3_Reserved;
            /// <summary>
            /// Исходное количество изображений
            /// </summary>
            public int              F4_Count;
            /// <summary>
            /// Количество изображений оставшихся в файле
            /// </summary>
            public int              F5_ActualCount;
            /// <summary>
            /// Массив разрешений камер
            /// </summary>
            public CAMERARESOLUTION[] F6_Resolution;
            /// <summary>
            /// Массив позиций структур кадров в бинарном потоке
            /// </summary>
            public long[]           F7_StreamPosition;

            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var item in typeof(TRAINIMAGEFILEHEADER).GetFields()) 
                {
                    sb.Append(string.Format("{0}:{1};\r\n", item.Name, item.GetValue(this)));
                }
                return sb.ToString();
            }
            /// <summary>
            /// Получает размер заголовка
            /// </summary>
            public int GetSize()
            {
                UTF8Encoding utf8 = new UTF8Encoding();
                int size = 
                    sizeof(long) +
                    (CountByte7BitEncodedInt(utf8.GetBytes(F1_Version).Length) + utf8.GetBytes(F1_Version).Length) +
                    (CountByte7BitEncodedInt(utf8.GetBytes(F2_TrainName).Length) + utf8.GetBytes(F2_TrainName).Length) +
                    (CountByte7BitEncodedInt(utf8.GetBytes(F3_Reserved).Length) + utf8.GetBytes(F3_Reserved).Length) + 
                    sizeof(int) + 
                    sizeof(int) + 
                    4 * sizeof(int) * 2 + 
                    F4_Count * sizeof(long);
                return size;
            }
            /// <summary>
            /// Получает количество байт, в которых кодируется длина строки с помощью BinaryWriter
            /// </summary>
            int CountByte7BitEncodedInt(int value)
            {
                int bCount = 0;
                uint num = (uint)value;
                while (num >= 128U)
                {
                    num >>= 7;
                    bCount++;
                }
                bCount++;
                return bCount;
            }

            /// <summary>
            /// 
            /// </summary>
            public byte[] ToBytes()
            {
                var bytes = new Byte[GetSize()];
                GCHandle pinStructure = GCHandle.Alloc(this, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(pinStructure.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    return bytes;
                }
                finally
                {
                    pinStructure.Free();
                }
            }
        }

        /// <summary>
        /// Инкапсуляция полей данных файла
        /// </summary>
        [Serializable]
        public struct TRAINIMAGEFILEDATA
        {
            /// <summary>
            /// Индекс кадра
            /// </summary>
            public int F0_Index;

            /// <summary>
            /// Номер камеры
            /// </summary>
            public int F1_Camera;

            /// <summary>
            /// Количество байт, оставшихся после сжатия в JPEG
            /// </summary>
            public int F2_Size;

            /// <summary>
            /// Массив значений сжатого в JPEG кадра
            /// </summary>
            public byte[] F3_DataArray;
            
            /// <summary>
            /// Получает размер структуры
            /// </summary>
            public int GetSize()
            {
                int sum = F3_DataArray != null ? F3_DataArray.Length : 0;
                return 12 + sum;
            }

            /// <summary>
            /// 
            /// </summary>
            public byte[] ToBytes()
            {
                var bytes = new Byte[GetSize()];
                GCHandle pinStructure = GCHandle.Alloc(this, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(pinStructure.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    return bytes;
                }
                finally
                {
                    pinStructure.Free();
                }
            }

            public byte[] ToByteArray()
            {
                byte[] arr = null;
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    
                    Int16 size = (Int16)Marshal.SizeOf(this);
                    arr = new byte[size];
                    ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(this, ptr, true);
                    Marshal.Copy(ptr, arr, 0, size);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                return arr;
            }

            public byte[] GetBytes()
            {
                List<byte> lst = new List<byte>();
                lst.AddRange(BitConverter.GetBytes(F0_Index));
                lst.AddRange(BitConverter.GetBytes(F1_Camera));
                lst.AddRange(BitConverter.GetBytes(F2_Size));
                byte[] arr = new byte[lst.Count + F3_DataArray.Length];
                lst.ToArray().CopyTo(arr, 0);
                F3_DataArray.CopyTo(arr, 12);
                return arr;
            }
        }

        /// <summary>
        /// Представляет набор исходных данных, необходимых для записи в файл
        /// </summary>
        public struct FRAMEINFO
        {
            /// <summary>
            /// Индекс кадра
            /// </summary>
            public int F0_Index;

            /// <summary>
            /// Номер камеры
            /// </summary>
            public int F1_Camera;

            /// <summary>
            /// Матрица значений сжатого в JPEG изображения
            /// </summary>
            public CvMat F2_Image;

            /// <summary>
            /// Получает размер заголовка
            /// </summary>
            public int GetSize()
            {
                int sum = F2_Image != null ? F2_Image.Cols : 0;
                return 12 + sum;
            }
        }
    }

    public delegate void StartIMGFileEventHandler(object sender, IMGFileEventArgs e);

    public delegate void ProcessIMGFileEventHandler(object sender, IMGFileEventArgs e);

    public delegate void CompleteIMGFileEventHandler(object sender, IMGFileEventArgs e);

    public delegate void ErrorIMGFileEventHandler(object sender, IMGFileEventArgs e);

    /// <summary>
    /// Предоставляет перечисление типов выполняемых операций
    /// </summary>
    public enum IMGFileTransactionType
    {
        /// <summary>
        /// Упаковка в бинарный файл
        /// </summary>
        PackageFile,

        /// <summary>
        /// Распаковка бинарного файла
        /// </summary>
        UnpackageFile,

        /// <summary>
        /// Инициализация работы с новым файлом
        /// </summary>
        ReadFile,

        /// <summary>
        /// Конвертация в новый формат
        /// </summary>
        Convert,

        /// <summary>
        /// 
        /// </summary>
        Other
    }

    /// <summary>
    /// Инкапсуляция значений расширения камеры
    /// </summary>
    [Serializable]
    public struct CAMERARESOLUTION
    {
        /// <summary> 
        /// Ширина кадра
        /// </summary>
        public int F0_Width;
        /// <summary>
        /// Высота кадра
        /// </summary>
        public int F1_Heght;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("F0_Width:{0}; F1_Heght:{1}", F0_Width, F1_Heght));
            return sb.ToString();
        }
    }
}
