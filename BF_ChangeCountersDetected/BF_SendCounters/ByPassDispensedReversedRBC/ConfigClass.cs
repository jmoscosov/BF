using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using BF_ChangeCountersDetected;

namespace BF_ChangeCountersDetected
{
    public static class ConfigClass
    {
        private const string CONFIG_FILE_NAME = @"C:\Recycler.xml";
        public static string TEXT_FILE_NAME = @"C:\NCRLogs\TempCaptureChangeCounters.txt";

        public static int NoteCountLength { set; get; } = 2;
        //public static int LastTxStatusMaxLength { set; get; } = 26;

        public static IDictionary<string, XmlNode> ConfigurationParameters { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> StateDefinitions { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> NoteMappings { set; get; } = new Dictionary<string, XmlNode>();
        public static IDictionary<string, XmlNode> Devices { set; get; } = new Dictionary<string, XmlNode>();

        static ConfigClass()
        {
            try
            {
                var doc = new XmlDocument();

                doc.Load(CONFIG_FILE_NAME);

                NoteCountLength = ReadIntegerValue(doc, "/Config/NoteCountLength", NoteCountLength);
                //LastTxStatusMaxLength = ReadIntegerValue(doc, "/Config/LastTxStatusMaxLength", LastTxStatusMaxLength);

                ConfigurationParameters = ReadDictionaryValue(doc, "/Config/Parameters/Option", "Number");
                StateDefinitions = ReadDictionaryValue(doc, "/Config/StateDefinitions/State", "Number");
                NoteMappings = ReadDictionaryValue(doc, "/Config/NoteMappings/Note", "ID");
                Devices = ReadDictionaryValue(doc, "/Config/Devices/Message", "ID");
            }
            catch (Exception ex)
            {
                LoggerClass.Log($"Error reading configuration: {ex.Message}");
            }
        }

        public static List<CassetteType> ReadTextFile()
        {
            String line = string.Empty;
            StreamReader sr = new StreamReader(TEXT_FILE_NAME);
            List<CassetteType> lista = new List<CassetteType>();
            line = sr.ReadLine();
            int count = 0;
            while (line != null)
            {
                count += 1;
                CassetteType cassette = new CassetteType();
                cassette.Type = count.ToString();
                cassette.NotesIn = line;
                lista.Add(cassette);
                line = sr.ReadLine();

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
                try
                {
                    File.Delete(TEXT_FILE_NAME);
                    LoggerClass.Log($"File Deleted!!");
                    return 1;
                }
                catch (Exception ex)
                {
                    LoggerClass.Log($"File Delete Error : {ex.ToString()}");
                }

            }
            return 0;
        }

        private static int ReadIntegerValue(XmlDocument doc, string xpath, int defaultValue)
        {
            LoggerClass.Log("Start reading integer value");

            var node = doc.SelectSingleNode(xpath);

            if (node != null)
            {
                LoggerClass.Log($"Found '{xpath}' node");

                if (int.TryParse(node.InnerText, out int val))
                {
                    LoggerClass.Log($"Reading integer value: {val}");

                    return val;
                }
                else
                {
                    LoggerClass.Log("Unable to parse integer value");
                }
            }
            else
            {
                LoggerClass.Log($"Specified node '{xpath}' not found");
            }

            return defaultValue;
        }

        private static IDictionary<string, XmlNode> ReadDictionaryValue(XmlDocument doc, string xpath, string key)
        {
            LoggerClass.Log("Start reading dictionary");

            var dict = new Dictionary<string, XmlNode>();
            var nodes = doc.SelectNodes(xpath);

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var keyAttr = node.Attributes[key];
                    
                    if (keyAttr != null)
                    {
                        LoggerClass.Log($"Added new node for key '{keyAttr.Value}'");

                        dict.Add(keyAttr.Value, node);
                    }
                    else
                    {
                        LoggerClass.Log("Key attribute not found");
                    }
                }
            }
            else
            {
                LoggerClass.Log("Nodes for dictionary not found");
            }

            return dict;
        }
    }
}
