using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Science.Tools.Data.Helpers
{
    [XmlRoot("Train")]
    public class TrainFileXML
    {
        [XmlAttribute]
        public string Name { get; set; }

        public List<Wagon> Wagons { get; set; }

        public TrainFileXML() 
        {
            Wagons = new List<Wagon>();
        }

        public void Save(string filename) 
        {
            using (TextWriter textWriter = new StreamWriter(filename))
            {
                var xmlSerializer = new XmlSerializer(typeof(TrainFileXML));
                xmlSerializer.Serialize(textWriter, this);
            }
        }

        public static TrainFileXML FromFile(string filename) 
        {
            TrainFileXML tfXml;
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                var xmlSerializer = new XmlSerializer(typeof(TrainFileXML));
                tfXml = (TrainFileXML)xmlSerializer.Deserialize(fs);
            }
            return tfXml;
        }

        public static void Converting(string filename) 
        {
            var tfXml = new TrainFileXML();
            XDocument docTrains = XDocument.Load(filename);
            tfXml.Name = docTrains.Element("Состав").FirstAttribute.Value;
            var query = docTrains.Element("Состав").Elements();
            foreach (var item in query)
            {
                var wag = new Wagon();
                wag.Item = Convert.ToInt32(item.FirstAttribute.Value);
                wag.Number = item.FirstAttribute.NextAttribute.Value;
                wag.Veracity = Convert.ToInt32(item.FirstAttribute.NextAttribute.NextAttribute.Value);
                wag.StartIndx = Convert.ToInt32(item.FirstAttribute.NextAttribute.NextAttribute.NextAttribute.Value);
                wag.EndIndx = Convert.ToInt32(item.LastAttribute.PreviousAttribute.Value);
                wag.RoiIndx = Convert.ToInt32(item.LastAttribute.Value);
                wag.WheelCount = 0;
                wag.Speed = 0;
                tfXml.Wagons.Add(wag);
            }
            System.IO.File.Move(filename, filename + "_old.xml");
            var xmlSerializer = new XmlSerializer(typeof(TrainFileXML));
            TextWriter textWriter = new StreamWriter(filename);
            xmlSerializer.Serialize(textWriter, tfXml);
            textWriter.Close();
        }

        public class Wagon
        {
            [XmlAttribute]
            public int Item { get; set; }
            [XmlAttribute]
            public string Number { get; set; }
            [XmlAttribute]
            public int Veracity { get; set; }
            [XmlAttribute]
            public int StartIndx { get; set; }
            [XmlAttribute]
            public int EndIndx { get; set; }
            [XmlAttribute]
            public int RoiIndx { get; set; }
            [XmlAttribute]
            public int WheelCount { get; set; }
            [XmlAttribute]
            public double Speed { get; set; }

            [XmlIgnore]
            public bool Flag
            {
                get
                {
                    _flag = Veracity < 2;
                    return _flag;
                }
                set { _flag = value; }
            }

            [XmlIgnore]
            private bool _flag;
        }
    }
}
