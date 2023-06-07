using BF_CaptureChangeCounters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BF_CaptureChangeCounters
{
    public static class Config
    {
        public static string CONFIG_FILE_NAME = @"C:\Counters.xml";
        public static string TEXT_FILE_NAME = @"C:\NCRLogs\TempCaptureChangeCounters.txt";

        public static int NoteCountLength { set; get; } = 2;
        //public static int LastTxStatusMaxLength { set; get; } = 26;

        public static IDictionary<string, XmlNode> ConfigurationParameters { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> StateDefinitions { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> NoteMappings { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> Devices { set; get; } = new Dictionary<string, XmlNode>();

        static Config()
        {
            try
            {
                var doc = new XmlDocument();

                doc.Load(CONFIG_FILE_NAME);

               // NoteCountLength = ReadIntegerValue(doc, "/Config/NoteCountLength", NoteCountLength);
                //LastTxStatusMaxLength = ReadIntegerValue(doc, "/Config/LastTxStatusMaxLength", LastTxStatusMaxLength);

               // ConfigurationParameters = ReadDictionaryValue(doc, "/Config/Parameters/Option", "Number");
               // StateDefinitions = ReadDictionaryValue(doc, "/Config/StateDefinitions/State", "Number");
                NoteMappings = ReadDictionaryValue(doc, "/Config/NoteTypes/Type", "ID");
               // Devices = ReadDictionaryValue(doc, "/Config/Devices/Message", "ID");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading configuration: {ex.Message}");
            }
        }

        private static int ReadIntegerValue(XmlDocument doc, string xpath, int defaultValue)
        {
            Logger.Log("Start reading integer value");

            var node = doc.SelectSingleNode(xpath);

            if (node != null)
            {
                Logger.Log($"Found '{xpath}' node");

                if (int.TryParse(node.InnerText, out int val))
                {
                    Logger.Log($"Reading integer value: {val}");

                    return val;
                }
                else
                {
                    Logger.Log("Unable to parse integer value");
                }
            }
            else
            {
                Logger.Log($"Specified node '{xpath}' not found");
            }

            return defaultValue;
        }

        private static IDictionary<string, XmlNode> ReadDictionaryValue(XmlDocument doc, string xpath, string key)
        {
            Logger.Log("Start reading dictionary");

            var dict = new Dictionary<string, XmlNode>();
            var nodes = doc.SelectNodes(xpath);

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var keyAttr = node.Attributes[key];
                    
                    if (keyAttr != null)
                    {
                        Logger.Log($"Added new node for key '{keyAttr.Value}'");

                        dict.Add(keyAttr.Value, node);
                    }
                    else
                    {
                        Logger.Log("Key attribute not found");
                    }
                }
            }
            else
            {
                Logger.Log("Nodes for dictionary not found");
            }

            return dict;
        }

        public static XmlDocument ReadXMLDocument()
        {
            XmlDocument xmlCountryDocument = new XmlDocument();
            xmlCountryDocument.Load(CONFIG_FILE_NAME);
            if (!(xmlCountryDocument == null))
                return xmlCountryDocument;
            else
                return null;
        }

        public static List<CassetteType> ReadTextFile()
        {
            String line = string.Empty;
            StreamReader sr = new StreamReader(TEXT_FILE_NAME);
            List<CassetteType> lista = new List<CassetteType>();
            line = sr.ReadLine();
            int count = 0;
            while (line !=null)
            {
                count += 1;
                CassetteType cassette = new CassetteType();
                cassette.Type = count.ToString();
                line = sr.ReadLine();
                cassette.NotesIn = line;
               
            }
            sr.Close();
            return lista;
        }
        public static void WriteTextFile(List<CassetteType> lista)
        {
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter(TEXT_FILE_NAME);

                foreach (var item in lista)
                {
                    //Write a line of text
                    sw.WriteLine(item.NotesIn);
                }
                //Close the file
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }
        }

        public static int DeleteFile()
        {
            if (File.Exists(TEXT_FILE_NAME))
            {
                File.Delete(TEXT_FILE_NAME);
                return 1;
            }
            return 0;
        }

    }
}
