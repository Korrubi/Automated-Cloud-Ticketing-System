using System;
using System.Xml;

namespace XMLParse
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            parseDMV();
        }

        public static void parseDMV()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("F:\\DMVDatabase.xml");

            XmlElement root = xmlDoc.DocumentElement;

            XmlNode plate = root.SelectSingleNode("vehicle[@plate=\"6TRJ244\"]");

            if (plate != null)
            {
                XmlNode make = plate.SelectSingleNode("make");

                if (make != null)
                {
                    Console.WriteLine("{0}", make.InnerText);
                    //WriteToLog("make: " + make.InnerText);
                }

                XmlNode model = plate.SelectSingleNode("model");

                if (model != null)
                {
                    //Console.WriteLine(model.InnerText);
                    Console.WriteLine("{0}", model.InnerText);
                }

                XmlNode color = plate.SelectSingleNode("color");

                if (color != null)
                {
                    //storeColor = color.InnerText;
                    Console.WriteLine("{0}", color.InnerText);
                }

                XmlNode language = plate.SelectSingleNode("owner/@preferredLanguage");
                if (language != null)
                {
                    //storePreferredLanguage = language.InnerText;
                    Console.WriteLine("{0}", language.InnerText);
                }
            }
        }
    }
}
