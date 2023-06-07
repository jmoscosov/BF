using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Threading;
using Microsoft.Win32;
using AANDC_CL;
using BF_CaptureChangeCounters;
using System.Linq;

namespace BF_CaptureChangeCounters
    {
    public class BF_CaptureChangeCounters
    {
        string sCounters = string.Empty;
        private int istxReply;

        public int MyProperty
        {
            get { return istxReply; }
            set { istxReply = value; }
        }

        private const byte CONTINUE = 0;
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe byte Incoming(byte** message)
        {
            try
            {
                Logger.Log($"Incoming function entry message : {NDCMessageIn.FromMessagePointer(message)} ");
                var msg = NDCMessageIn.FromMessagePointer(message);
                
                if (msg != null)
                {
                    Logger.Log($"msg <> null");
                    var msgStr = msg.ToString();
                    Logger.Log($"Calling ProcessManager Function");
                    if (ProcessMessage(ref msgStr))
                    {
                        //Logger.Log("Updating message pointer");

                        msg.Data = Encoding.ASCII.GetBytes(msgStr);
                        msg.ToMessagePointer(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EXCEPTION: {ex.Message}");
            }
            Logger.Log("Incoming function exit");
            return CONTINUE;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe byte Outgoing(byte** message)
        {
            try
            {
               Logger.Log($"Outgoing function entry message : {NDCMessageOut.FromMessagePointer(message)} ");
                var msg = NDCMessageOut.FromMessagePointer(message);
                
                if (msg != null)
                {
                    var msgStr = msg.ToString();
                   // Logger.Log($"Outgoing function msgStr value : {msgStr}");
                    if (ProcessMessage(ref msgStr))
                    {
                       // Logger.Log("Updating message pointer");

                        msg.Data = Encoding.ASCII.GetBytes(msgStr);
                        msg.ToMessagePointer(message);
                        //int intTimer03 = NDCMessage.GetIntVal(3129);
                        //int intIdHostTimeOut = NDCMessage.GetIntVal(1375);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EXCEPTION: {ex.Message}");
            }
            Logger.Log("Outgoing function exit");
            return CONTINUE;
        }

        public static bool ProcessMessage(ref string message)
        {
            // SendStatusNotSolicited statusMessage = new SendStatusNotSolicited();
            //SendStatusNotSolicited statusMessage2 = new SendStatusNotSolicited();
            SendStatusNotSolicited statusMessage = new SendStatusNotSolicited();
            string sCounters = string.Empty;
            
            Logger.Log($"ProcessMessage function entry : {message} ");
            if (string.IsNullOrEmpty(message))
                return false;

            var arr = message.Split((char)NDCMessage.FS);
            Logger.Log($"ARR Value: {arr[0]}");

            if (arr != null)
            {
                Logger.Log($"ARR <> NULL");


               if (arr.Length > 0 && arr[0].Equals("12") && arr[3].Equals("P21")) 
                {
                    Logger.Log("JM185384  - Supervisor Entry Detected");
                    // Logger.Log("Message is Send Configuration Information - Send hardware configuration data only");

                     if (ReadCountersFile(ref arr))
                     {
                         message = ReassembleMessage(arr); 

                         return true;
                     }
                    
                }
                if (arr.Length > 0 && arr[0].Equals("12") && arr[3].Equals("P20"))
                {
                    Logger.Log("JM185384  - Supervisor Exit Detected");
                    // Logger.Log("Message is Send Configuration Information - Send hardware configuration data only");

                    if (WriteCountersFile(ref arr))
                    {
                        statusMessage.send();

                        return true;
                    }

                }
                if (arr.Length > 4 && arr[0].Equals("22") && arr[3].Equals("F") && arr[4].StartsWith("IA"))
                {
                   /* Logger.Log("Send Configuration Information - Send supplies data only");

                    if (ChangeSuppliesData(ref arr))
                    {
                        message = ReassembleMessage(arr);

                        return true;
                    }
                   */
                }

                if (arr.Length > 4 && arr[0].Equals("22") && arr[3].Equals("F") && arr[4].StartsWith("JA"))
                {
                   /* Logger.Log("Message is Send Configuration Information - Send fitness data only");

                    if (ChangeFitnessData(ref arr))
                    {
                        message = ReassembleMessage(arr);

                        return true;
                    }
                   */
                }

                // ELIMINA LOS STATUS DEL MENSAJE NO SOLICITADO
                // Y CAMBIA LA POSICION DE LAS DENOMINACIONES DEL TEMPLATE
                if (arr.Length > 4 && arr[0].Equals("12") && arr[3].StartsWith("w"))
                {
                   /* Logger.Log("Unsolicited Message with fitness data - send forced device status to OK");

                    if (ChangeUnsolicitedFitnessData(ref arr))
                    {
                        message = ReassembleMessage(arr);

                        return true;
                    }
                   */
                }
            }
            else
            {
                Logger.Log($"EXCEPTION: ARR == NULL");
            }
            Logger.Log("ProcessMessage function exit");
            return false;
        }

        private static string ReassembleMessage(string[] arr)
        {
            Logger.Log("Re-assembling message for AANDC");

            var sb = new StringBuilder();

            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0)
                    sb.Append((char)NDCMessage.FS);

                sb.Append(arr[i]);
            }

            var message = sb.ToString();

            Logger.Log($"Message was modified: {message}");

            return message;
        }
        private static bool WriteCountersFile(ref string[] arr)
        {
            int idNotesInType1 = 0;
            int idNotesInType2 = 0;
            int idNotesInType3 = 0;
            int idNotesInType4 = 0;
            
            List<CassetteType> Lista = new List<CassetteType>();
            List<CassetteType> Lista2 = new List<CassetteType>();
            CassetteType type1, type2, type3, type4;

            
            if (arr == null)
                return false;
            for (int i = 0; i < arr.Length; i++)
            {
                Logger.Log($"WriteCountersFile Data index : {arr[i]}");
                //Logger.Log($"AddCountersToReady sendCountersUCDI : {NDCMessage.GetIntValUCDI("sendCountersUCDI")}");

                if (arr[i] == "P21")
                {
                    Logger.Log($"185384 - > WriteCountersFile -> P21");

                    try
                    {
                        /*Capture Data from Buffers*/
                        idNotesInType1 = NDCMessage.GetIntVal(3095);
                        idNotesInType2 = NDCMessage.GetIntVal(3096);
                        idNotesInType3 = NDCMessage.GetIntVal(3097);
                        idNotesInType4 = NDCMessage.GetIntVal(3098);
                       
                        type1 = new CassetteType();
                        type1.Type = "1";
                        type1.NotesIn = idNotesInType1.ToString();

                        type2 = new CassetteType();
                        type2.Type = "2";
                        type2.NotesIn = idNotesInType2.ToString();

                        type3 = new CassetteType();
                        type3.Type = "3";
                        type3.NotesIn = idNotesInType3.ToString();

                        type4 = new CassetteType();
                        type4.Type = "4";
                        type4.NotesIn = idNotesInType4.ToString();

                        Lista.Add(type1);
                        Lista.Add(type2);
                        Lista.Add(type3);
                        Lista.Add(type4);

                        Config.WriteTextFile(Lista);

                    }
                    catch (Exception exception)
                    {

                        Logger.Log($"Exception : {exception.ToString()}");
                    }

                    //NDCMessage.PutIntValUCDI("sendCountersUCDI", 0);
                }
            }

            return true;
        }

        private static bool ReadCountersFile(ref string[] arr)
        {
            int idNotesInType1 = 0;
            int idNotesInType2 = 0;
            int idNotesInType3 = 0;
            int idNotesInType4 = 0;
            bool resp = true;
            List<CassetteType> Lista = new List<CassetteType>();


            if (arr == null)
                return false;
            for (int i = 0; i < arr.Length; i++)
            {
                Logger.Log($"ReadCountersFile Data index : {arr[i]}");
                //Logger.Log($"AddCountersToReady sendCountersUCDI : {NDCMessage.GetIntValUCDI("sendCountersUCDI")}");

                if (arr[i] == "P20")
                {
                    Logger.Log($"185384 - > ReadCountersFile");

                    try
                    {
                        /*Capture Data from Buffers*/
                         idNotesInType1 = NDCMessage.GetIntVal(3095);
                         idNotesInType2 = NDCMessage.GetIntVal(3096);
                         idNotesInType3 = NDCMessage.GetIntVal(3097);
                         idNotesInType4 = NDCMessage.GetIntVal(3098);

                        /*  idNotesInType1 = 10;
                          idNotesInType2 = 20;
                          idNotesInType3 = 30;
                          idNotesInType4 = 40;
                        */

                        Lista = Config.ReadTextFile();
                        foreach (var item in Lista)
                        {
                            switch (item.Type)
                            {
                                case "1":
                                    if (item.NotesIn.Trim() != idNotesInType1.ToString().Trim())
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type1 NOK");
                                        resp= false;
                                    }
                                    else
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type1 OK");
                                    }
                                    break;
                                case "2":
                                    if (item.NotesIn.Trim() != idNotesInType2.ToString().Trim())
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type2 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type2 OK");
                                    }
                                    break;
                                case "3":
                                    if (item.NotesIn.Trim() != idNotesInType3.ToString().Trim())
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type3 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type3 OK");
                                    }
                                    break;
                                case "4":
                                    if (item.NotesIn.Trim() != idNotesInType4.ToString().Trim())
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type4 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        Logger.Log($"185384 - > ReadCountersFile -> Type4 OK");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        
                        
                    }
                    catch (Exception exception)
                    {

                        Logger.Log($"Exception : {exception.ToString()}");
                    }

                }
            }
            if (resp == true)
            {
                int retorno = Config.DeleteFile();
                if (retorno == 1)
                {
                    Logger.Log($"185384 - > ReadCountersFile -> Delete Temp File");
                    return true;
                }
                else
                {
                    Logger.Log($"185384 - > ReadCountersFile -> Delete Temp File NOK");
                    return false;
                }
            }
            else
            {
                    return false;
            }
            
        }
        private static bool ChangeStateData(ref string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            var states = s.Split((char)NDCMessage.GS);
            var changed = false;

            for (int i = 0; i < states.Length; i++)
            {
                var number = states[i].Substring(0, 3);

                if (Config.StateDefinitions.ContainsKey(number))
                {
                    Logger.Log($"Changing state data for state {number}");

                    var data = Config.StateDefinitions[number].InnerText;

                    if (!string.IsNullOrEmpty(data))
                    {
                        states[i] = $"{number}{data}";
                        changed |= true;
                    }
                }
                else
                {
                    Logger.Log($"No state definition for state {number}");
                }
            }

            if (changed)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < states.Length; i++)
                {
                    if (i > 0)
                        sb.Append((char)NDCMessage.GS);

                    sb.Append(states[i]);
                }

                s = sb.ToString();

                return true;
            }

            return false;
        }

        private static bool ChangeConfigurationParameters(ref string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            var sb = new StringBuilder();
            var current = string.Empty;
            var option = string.Empty;
            var modified = false;
            var optionsFound = new List<string>();

            for (int i = 0; i < s.Length; i++)
            {
                current += s[i];

                if (current.Length == 2 && string.IsNullOrEmpty(option))
                {
                    sb.Append(current);
                    optionsFound.Add(current);

                    option = current;
                    current = string.Empty;
                }
                else if (current.Length == 3 && !string.IsNullOrEmpty(option))
                {
                    if (Config.ConfigurationParameters.ContainsKey(option))
                    {
                        if (int.TryParse(Config.ConfigurationParameters[option].InnerText, out int optionValue))
                        {
                            var maskAttr = Config.ConfigurationParameters[option].Attributes["Mask"];

                            if (maskAttr != null)
                            {
                                if (bool.TryParse(maskAttr.Value, out bool isMask) && isMask)
                                {
                                    if (int.TryParse(current, out int value))
                                    {
                                        value |= optionValue;

                                        sb.Append(value.ToString("D3"));
                                    }
                                }
                                else
                                {
                                    sb.Append(optionValue.ToString("D3"));
                                }
                            }
                            else
                            {
                                sb.Append(optionValue.ToString("D3"));
                            }

                            modified |= true;
                        }
                        else
                        {
                            sb.Append(current);
                        }
                    }
                    else
                    {
                        sb.Append(current);
                    }

                    option = string.Empty;
                    current = string.Empty;
                }
            }

            foreach (var param in Config.ConfigurationParameters)
            {
              /*  if (!optionsFound.Contains(param.Key))
                {
                    sb.Append(param.Key);

                    if (int.TryParse(param.Value.InnerText, out int value))
                        sb.Append(value.ToString("D3"));
                    else
                        sb.Append(param.Value.InnerText);

                    modified |= true;
                }*/
            }

            if (modified)
            {
                s = sb.ToString();

                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ChangeNoteTypes(ref string[] arr)
        {
            if (arr == null)
                return false;

            var index = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (!string.IsNullOrEmpty(arr[i]) && arr[i].StartsWith("w"))
                {
                    index = i;

                    break;
                }
            }

            var modified = false;

            if (index > 0)
            {
                var sb = new StringBuilder();
                var current = string.Empty;
                var noteTypeId = true;

                sb.Append("w");

                for (int i = 1; i < arr[index].Length; i++)
                {
                    current += arr[index][i];

                    if (current.Length == 2 && noteTypeId)
                    {
                        if (Config.NoteMappings.ContainsKey(current))
                        {
                            sb.Append(Config.NoteMappings[current].InnerText);

                            modified |= true;
                        }
                        else
                        {
                            sb.Append(current);
                        }

                        current = string.Empty;
                        noteTypeId = false;
                    }
                    else if (current.Length == Config.NoteCountLength && !noteTypeId)
                    {
                        sb.Append(current);

                        current = string.Empty;
                        noteTypeId = true;
                    }
                }

                if (modified)
                    arr[index] = sb.ToString();
            }

            return modified;
        }

        //private static bool ChangeLastTxStatusMaxLength(ref string[] arr)
        //{
        //    if (arr?.Length > 13 && !string.IsNullOrEmpty(arr[13]) && arr[13].StartsWith("2"))
        //    {
        //        Logger.Log($"Last transaction status information: {arr[13]}");

        //        if (arr[13].Length > Config.LastTxStatusMaxLength)
        //        {
        //            Logger.Log("Last transaction status is greater than max length");

        //            arr[13] = arr[13].Substring(0, Config.LastTxStatusMaxLength);

        //            return true;
        //        }
        //    }

        //    return false;
        //}

        private static bool ChangeFitnessData(ref string[] arr)
        {
            if (string.IsNullOrEmpty(arr?[4]))
                return false;

            return AddOrRemoveDevices(ref arr[4], "JA", "w");
        }

        private static bool ChangeHardwareConfiguration(ref string[] arr)
        {
            if (string.IsNullOrEmpty(arr?[6]))
                return false;

            return AddOrRemoveDevices(ref arr[6], "HA", "w");
        }

        private static bool ChangeSuppliesData(ref string[] arr)
        {
            if (string.IsNullOrEmpty(arr?[4]))
                return false;

            return AddOrRemoveDevices(ref arr[4], "IA", "w");
        }

        private static bool ChangeUnsolicitedFitnessData(ref string[] arr)
        {
            if (string.IsNullOrEmpty(arr?[4]))
                return false;

            // CHANGE TEMPLATE DENOMINATION ORDER
            string digStatus = arr[3].Substring(0, 2);
            string cassetteData = arr[3].Substring(2, 150);
            string cassetteDetail1 = ChangeTemplateDenominationOrder(cassetteData.Substring(0, 50));
            string cassetteDetail2 = ChangeTemplateDenominationOrder(cassetteData.Substring(50, 50));
            string cassetteDetail3 = ChangeTemplateDenominationOrder(cassetteData.Substring(100, 50));
            string totalNotesCount = arr[3].Substring(152);

            arr[3] = digStatus + cassetteDetail1 + cassetteDetail2 + cassetteDetail3 + totalNotesCount;

            // CHANGE FITNESS
            if (arr[4].Length > 1)
                arr[4] = "0";

            if (arr[5].Length > 2)
                arr[5] = "00";

            if (arr[6].Length > 1)
                arr[6] = "0";

            return true;
        }

        private static string ChangeTemplateDenominationOrder(string denominations)
        {
            StringBuilder newDetail = new StringBuilder();
            newDetail.Append(' ', 9);

            char[] denomDetail = denominations.Substring(0, 5).ToCharArray();

            newDetail.Append(denomDetail[1]);
            newDetail.Append(denomDetail[2]);
            newDetail.Append(denomDetail[3]);
            newDetail.Append(denomDetail[0]);
            newDetail.Append(denomDetail[4]);

            newDetail.Append('~', 36);
            return newDetail.ToString();
        }

        private static bool AddOrRemoveDevices(ref string devices, string messageId, string prefix = "")
        {
            if (string.IsNullOrEmpty(devices))
                return false;

            //var devicesData = devices.Substring(prefix.Length);
            var devicesData = devices;

            Logger.Log($"Adding/removing devices for {messageId}: {devicesData}");

            var devsArr = devicesData.Split((char)NDCMessage.GS);

            if (devsArr.Length > 0)
            {
                var dict = new Dictionary<string, string>();

                // Parse device data
                foreach (var s in devsArr)
                {
                    if (s.Length >= 2)
                    {
                        var dig = s.Substring(0, 1);
                        var data = s.Substring(1);

                        dict.Add(dig, data);
                    }
                    else
                    {
                        Logger.Log($"Not enought information to obtain DIG and data: {s}");
                    }
                }

                // Add or remove devices according to configuration
                foreach (var f in GetDevices(messageId))
                {
                    if (string.IsNullOrEmpty(f.Value.InnerText))
                    {
                        Logger.Log($"Removing device {f.Key}");

                        if (dict.ContainsKey(f.Key))
                            dict.Remove(f.Key);
                    }
                    else
                    {
                        Logger.Log($"Adding device {f.Key}");

                        if (dict.ContainsKey(f.Key))
                            dict[f.Key] = GetDataValue(f.Value.InnerText, dict[f.Key]);
                        else
                            dict.Add(f.Key, GetDataValue(f.Value.InnerText));
                    }
                }

                // Re-assemble devices data
                if (dict.Count > 0)
                {
                    Logger.Log("Re-assembling devices data");

                    var sb = new StringBuilder();
                    int i = 0;

                    //sb.Append(prefix);

                    foreach (var dev in dict)
                    {
                        if (i > 0)
                            sb.Append((char)NDCMessage.GS);

                        sb.Append(dev.Key);
                        sb.Append(dev.Value);

                        i++;
                    }

                    devices = sb.ToString();

                    return true;
                }
            }

            return false;
        }

        private static IDictionary<string, XmlNode> GetDevices(string messageId)
        {
            var dict = new Dictionary<string, XmlNode>();

            foreach (var node in Config.Devices)
            {
                if (node.Key == messageId)
                {
                    foreach (XmlNode subNode in node.Value?.ChildNodes)
                    {
                        var dig = subNode.Attributes?["DIG"]?.Value;

                        if (!string.IsNullOrEmpty(dig))
                            dict.Add(dig, subNode);
                    }
                }
            }

            return dict;
        }

        private static string GetDataValue(string newValue, string currentValue = "")
        {
            if (string.IsNullOrEmpty(newValue))
                return string.Empty;

            if (string.IsNullOrEmpty(currentValue))
                return newValue.Replace("?", "");

            string data = null;
            string mask = null;

            if (newValue.Length < currentValue.Length)
            {
                data = currentValue;
                mask = newValue + new string('\0', currentValue.Length - newValue.Length);
            }
            else if (currentValue.Length < newValue.Length)
            {
                data = currentValue + new string('\0', newValue.Length - currentValue.Length);
                mask = newValue;
            }
            else
            {
                data = currentValue;
                mask = newValue;
            }

            if (!string.IsNullOrEmpty(newValue) && !string.IsNullOrEmpty(currentValue))
            {
                var sb = new StringBuilder();

                for (int i = 0; i < mask.Length; i++)
                {
                    if (mask[i] == '?')
                    {
                        if (data[i] != '\0')
                            sb.Append(data[i]);
                    }
                    else
                    {
                        if (mask[i] != '\0')
                            sb.Append(mask[i]);
                    }
                }

                return sb.ToString();
            }

            return newValue.Replace("?", "");
        }
        private static List<CassetteType> readAndWriteXML(List<CassetteType> ListaType)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc = Config.ReadXMLDocument();

            List<CassetteType> CassetteTypeLista = new List<CassetteType>();

            XmlNodeList CassetteTypeList = xmldoc.SelectNodes("root/row");
            foreach (XmlNode TypeNode in CassetteTypeList)
            {
                CassetteType _CassetteType = new CassetteType();
                foreach (XmlNode varElement in TypeNode.ChildNodes)
                {
                    if (ListaType.Count == 0)
                    {

                       switch (varElement.Attributes["name"].Value)
                        {
                            case "Cassette":
                                _CassetteType.Type = varElement.Attributes["value"].Value;
                                break;
                            case "NotesIn":
                                _CassetteType.NotesIn = varElement.Attributes["value"].Value;
                                break;
                        }
                    }
                    else
                    {
                        //var resultado = ListaType.Where(x=>x.Type == )
                        switch (varElement.Attributes["name"].Value)
                        {
                            case "Cassette":
                                var resultado = ListaType.Where(x => x.Type == varElement.Attributes["value"].Value);
                                foreach(var item in resultado)
                                {
                                    varElement.Attributes["value"].Value = item.Type;
                                }
                                break;
                            case "NotesIn":
                                var resultado2 = ListaType.Where(x => x.Type == varElement.Attributes["value"].Value);
                                foreach (var item in resultado2)
                                {
                                    varElement.Attributes["value"].Value = item.NotesIn;
                                }
                               
                                break;
                        }
                    }
                }
                xmldoc.Save(Config.CONFIG_FILE_NAME);
                CassetteTypeLista.Add(_CassetteType);

            }

            return CassetteTypeLista;
        }
    }
}
