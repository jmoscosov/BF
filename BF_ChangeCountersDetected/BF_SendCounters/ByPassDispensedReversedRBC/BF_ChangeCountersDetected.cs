using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Threading;
using Microsoft.Win32;
using BF_ChangeCountersDetected;

namespace BF_ChangeCountersDetected
    {
    public class SendCountersBF
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
                LoggerClass.Log($"Incoming function entry message : {NDCMessageIn.FromMessagePointer(message)} ");
                var msg = NDCMessageIn.FromMessagePointer(message);
                
                if (msg != null)
                {
                    LoggerClass.Log($"msg <> null");
                    var msgStr = msg.ToString();
                    LoggerClass.Log($"Calling ProcessManager Function");
                    if (ProcessMessage(ref msgStr))
                    {
                        //LoggerClass.Log("Updating message pointer");

                        msg.Data = Encoding.ASCII.GetBytes(msgStr);
                        msg.ToMessagePointer(message);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerClass.Log($"EXCEPTION: {ex.Message}");
            }
            LoggerClass.Log("Incoming function exit");
            return CONTINUE;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe byte Outgoing(byte** message)
        {
            try
            {
               LoggerClass.Log($"Outgoing function entry message : {NDCMessageOut.FromMessagePointer(message)} ");
                var msg = NDCMessageOut.FromMessagePointer(message);
                
                if (msg != null)
                {
                    var msgStr = msg.ToString();
                   // LoggerClass.Log($"Outgoing function msgStr value : {msgStr}");
                    if (ProcessMessage(ref msgStr))
                    {
                       // LoggerClass.Log("Updating message pointer");

                        msg.Data = Encoding.ASCII.GetBytes(msgStr);
                        msg.ToMessagePointer(message);
                        //int intTimer03 = NDCMessage.GetIntVal(3129);
                        //int intIdHostTimeOut = NDCMessage.GetIntVal(1375);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerClass.Log($"EXCEPTION: {ex.Message}");
            }
            LoggerClass.Log("Outgoing function exit");
            return CONTINUE;
        }

        public static bool ProcessMessage(ref string message)
        {
            // SendStatusNotSolicited statusMessage = new SendStatusNotSolicited();
            //SendStatusNotSolicited statusMessage2 = new SendStatusNotSolicited();
            SendStatusNotSolicited statusMessage = new SendStatusNotSolicited();
            string sCounters = string.Empty;

            LoggerClass.Log($"ProcessMessage function entry : {message} ");
            if (string.IsNullOrEmpty(message))
                return false;

            var arr = message.Split((char)NDCMessage.FS);
            LoggerClass.Log($"ARR Value: {arr[0]}");

            if (arr != null)
            {
                LoggerClass.Log($"ARR <> NULL");


                if (arr.Length > 0 && arr[0].Equals("12") && arr[3].Equals("P21"))
                {
                    LoggerClass.Log("JM185384  - Supervisor Entry Detected");
                    // LoggerClass.Log("Message is Send Configuration Information - Send hardware configuration data only");
                    NDCMessage.GetIntValUCDI("sendCountersUCDI");
                    if (WriteCountersFile (ref arr))
                    {
                        message = ReassembleMessage(arr);
                        return true;
                    }

                }
                if (arr.Length > 0 && arr[0].Equals("12") && arr[3].Equals("P20"))
                {
                    LoggerClass.Log("JM185384  - Supervisor Exit Detected");
                    // LoggerClass.Log("Message is Send Configuration Information - Send hardware configuration data only");

                    if (!ReadCountersFile(ref arr))
                    {
                        statusMessage.send();
                        return true;
                    }

                }
                if (arr.Length > 4 && arr[0].Equals("22") && arr[3].Equals("F") && arr[4].StartsWith("IA"))
                {
                    /* LoggerClass.Log("Send Configuration Information - Send supplies data only");

                     if (ChangeSuppliesData(ref arr))
                     {
                         message = ReassembleMessage(arr);

                         return true;
                     }
                    */
                }

                if (arr.Length > 4 && arr[0].Equals("22") && arr[3].Equals("F") && arr[4].StartsWith("JA"))
                {
                    /* LoggerClass.Log("Message is Send Configuration Information - Send fitness data only");

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
                    /* LoggerClass.Log("Unsolicited Message with fitness data - send forced device status to OK");

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
                LoggerClass.Log($"EXCEPTION: ARR == NULL");
            }
            LoggerClass.Log("ProcessMessage function exit");
            return false;
        }

        private static string ReassembleMessage(string[] arr)
        {
            LoggerClass.Log("Re-assembling message for AANDC");

            var sb = new StringBuilder();

            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0)
                    sb.Append((char)NDCMessage.FS);

                sb.Append(arr[i]);
            }

            var message = sb.ToString();

            LoggerClass.Log($"Message was modified: {message}");

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
                LoggerClass.Log($"WriteCountersFile Data index : {arr[i]}");
                //LoggerClass.Log($"AddCountersToReady sendCountersUCDI : {NDCMessage.GetIntValUCDI("sendCountersUCDI")}");

                if (arr[i] == "P21")
                {
                    LoggerClass.Log($"185384 - > WriteCountersFile -> P21");

                    try
                    {
                        /*Capture Data from Buffers*/
                         idNotesInType1 = NDCMessage.GetIntVal(3095);
                         idNotesInType2 = NDCMessage.GetIntVal(3096);
                         idNotesInType3 = NDCMessage.GetIntVal(3097);
                         idNotesInType4 = NDCMessage.GetIntVal(3098);
                      
                       /* idNotesInType1 = 10;
                        idNotesInType2 = 20;
                        idNotesInType3 = 30;
                        idNotesInType4 = 40;*/
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

                        ConfigClass.WriteTextFile(Lista);

                    }
                    catch (Exception exception)
                    {

                        LoggerClass.Log($"Exception : {exception.ToString()}");
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
                LoggerClass.Log($"ReadCountersFile Data index : {arr[i]}");
                //LoggerClass.Log($"AddCountersToReady sendCountersUCDI : {NDCMessage.GetIntValUCDI("sendCountersUCDI")}");

                if (arr[i] == "P20")
                {
                    LoggerClass.Log($"185384 - > ReadCountersFile");

                    try
                    {
                        /*Capture Data from Buffers*/
                        idNotesInType1 = NDCMessage.GetIntVal(3095);
                        idNotesInType2 = NDCMessage.GetIntVal(3096);
                        idNotesInType3 = NDCMessage.GetIntVal(3097);
                        idNotesInType4 = NDCMessage.GetIntVal(3098);

                     /*     idNotesInType1 = 10;
                          idNotesInType2 = 20;
                          idNotesInType3 = 30;
                          idNotesInType4 = 40;
                        */

                        Lista = ConfigClass.ReadTextFile();
                        foreach (var item in Lista)
                        {
                            switch (item.Type)
                            {
                                case "1":
                                    if (item.NotesIn.Trim() != idNotesInType1.ToString().Trim())
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type1 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type1 OK");
                                    }
                                    break;
                                case "2":
                                    if (item.NotesIn.Trim() != idNotesInType2.ToString().Trim())
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type2 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type2 OK");
                                    }
                                    break;
                                case "3":
                                    if (item.NotesIn.Trim() != idNotesInType3.ToString().Trim())
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type3 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type3 OK");
                                    }
                                    break;
                                case "4":
                                    if (item.NotesIn.Trim() != idNotesInType4.ToString().Trim())
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type4 NOK");
                                        resp = false;
                                    }
                                    else
                                    {
                                        LoggerClass.Log($"185384 - > ReadCountersFile -> Type4 OK");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }


                    }
                    catch (Exception exception)
                    {

                        LoggerClass.Log($"Exception : {exception.ToString()}");
                    }

                }
            }
            int retorno = ConfigClass.DeleteFile();
            if (resp == true)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private static bool AddCountersToReady(ref string[] arr)
        {
            if (arr == null)
                return false;

            var index = 0;
            
            for (int i = 0; i < arr.Length; i++)
            {
                /*if (!string.IsNullOrEmpty(arr[i]) && arr[i].StartsWith("w"))
                {
                    index = i;

                    break;
                }*/
                LoggerClass.Log($"AddCountersToReady Data index : {arr[i]}");
                LoggerClass.Log($"AddCountersToReady sendCountersUCDI : {NDCMessage.GetIntValUCDI("sendCountersUCDI")}");

                if (arr[i] == "B" && NDCMessage.GetIntValUCDI("sendCountersUCDI") == 1)
                {
                    LoggerClass.Log($"185384 - > Ready B - TXReply and Function ID 2 detected");
                    int idNotesDispType1 = NDCMessage.GetIntVal(3091);
                    int idNotesDispType2 = NDCMessage.GetIntVal(3092);
                    int idNotesDispType3 = NDCMessage.GetIntVal(3093);
                    int idNotesDispType4 = NDCMessage.GetIntVal(3094);

                    int idNotesInType1 = NDCMessage.GetIntVal(3095);
                    int idNotesInType2 = NDCMessage.GetIntVal(3096);
                    int idNotesInType3 = NDCMessage.GetIntVal(3097);
                    int idNotesInType4 = NDCMessage.GetIntVal(3098);

                    int idNotesPurgedType1 = NDCMessage.GetIntVal(3099);
                    int idNotesPurgedType2 = NDCMessage.GetIntVal(3100);
                    int idNotesPurgedType3 = NDCMessage.GetIntVal(3101);
                    int idNotesPurgedType4 = NDCMessage.GetIntVal(3102);

                    // BNA Counters

                    int idCass1CountDenom1 = NDCMessage.GetIntVal(3501);
                    int idCass1CountDenom2 = NDCMessage.GetIntVal(3502);
                    int idCass1CountDenom3 = NDCMessage.GetIntVal(3503);
                    int idCass1CountDenom4 = NDCMessage.GetIntVal(3504);
                    int idCass1CountDenom5 = NDCMessage.GetIntVal(3505);
                    int idCass1CountDenom6 = NDCMessage.GetIntVal(3506);
                    int idCass1CountDenom7 = NDCMessage.GetIntVal(3507);
                    int idCass1CountDenom8 = NDCMessage.GetIntVal(3508);
                    int idCass1CountDenom9 = NDCMessage.GetIntVal(3509);
                    int idCass1CountDenom10 = NDCMessage.GetIntVal(3510);
                    int idCass1CountDenom11 = NDCMessage.GetIntVal(3511);


                    LoggerClass.Log($"JM185384 Notes Dispensed Type 1 :  {idNotesDispType1.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Dispensed Type 2 :  {idNotesDispType2.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Dispensed Type 3 :  {idNotesDispType3.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Dispensed Type 4 :  {idNotesDispType4.ToString("D5")}");

                    LoggerClass.Log($"JM185384 Notes Cassette Type 1 :  {idNotesInType1.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Cassette Type 2 :  {idNotesInType2.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Cassette Type 3 :  {idNotesInType3.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Cassette Type 4 :  {idNotesInType4.ToString("D5")}");

                    LoggerClass.Log($"JM185384 Notes Rejected Type 1 :  {idNotesPurgedType1.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Rejected Type 2 :  {idNotesPurgedType2.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Rejected Type 3 :  {idNotesPurgedType3.ToString("D5")}");
                    LoggerClass.Log($"JM185384 Notes Rejected Type 4 :  {idNotesPurgedType4.ToString("D5")}");

                   /* LoggerClass.Log($"JM185384 BNA Cassette 1 Den  1   :  {idCass1CountDenom1.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  2   :  {idCass1CountDenom2.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  3   :  {idCass1CountDenom3.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  4   :  {idCass1CountDenom4.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  5   :  {idCass1CountDenom5.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  6   :  {idCass1CountDenom6.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  7   :  {idCass1CountDenom7.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  8   :  {idCass1CountDenom8.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  9   :  {idCass1CountDenom9.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  10  :  {idCass1CountDenom10.ToString("D4")}");
                    LoggerClass.Log($"JM185384 BNA Cassette 1 Den  11  :  {idCass1CountDenom11.ToString("D4")}");
                   */

                    arr[i] += (char)NDCMessage.FS + idNotesDispType1.ToString("D5") + idNotesDispType2.ToString("D5") + idNotesDispType3.ToString("D5") + idNotesDispType4.ToString("D5") + idNotesInType1.ToString("D5") + idNotesInType2.ToString("D5") + idNotesInType3.ToString("D5") + idNotesInType4.ToString("D5") + idNotesPurgedType1.ToString("D5") + idNotesPurgedType2.ToString("D5") + idNotesPurgedType3.ToString("D5") + idNotesPurgedType4.ToString("D5");


                    //arr[i] += (char)NDCMessage.FS + "0000000100020003";
                    NDCMessage.PutIntValUCDI("sendCountersUCDI", 0);
                }
            }

            return true;
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

                if (ConfigClass.StateDefinitions.ContainsKey(number))
                {
                    LoggerClass.Log($"Changing state data for state {number}");

                    var data = ConfigClass.StateDefinitions[number].InnerText;

                    if (!string.IsNullOrEmpty(data))
                    {
                        states[i] = $"{number}{data}";
                        changed |= true;
                    }
                }
                else
                {
                    LoggerClass.Log($"No state definition for state {number}");
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
                    if (ConfigClass.ConfigurationParameters.ContainsKey(option))
                    {
                        if (int.TryParse(ConfigClass.ConfigurationParameters[option].InnerText, out int optionValue))
                        {
                            var maskAttr = ConfigClass.ConfigurationParameters[option].Attributes["Mask"];

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

            foreach (var param in ConfigClass.ConfigurationParameters)
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
                        if (ConfigClass.NoteMappings.ContainsKey(current))
                        {
                            sb.Append(ConfigClass.NoteMappings[current].InnerText);

                            modified |= true;
                        }
                        else
                        {
                            sb.Append(current);
                        }

                        current = string.Empty;
                        noteTypeId = false;
                    }
                    else if (current.Length == ConfigClass.NoteCountLength && !noteTypeId)
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
        //        LoggerClass.Log($"Last transaction status information: {arr[13]}");

        //        if (arr[13].Length > ConfigClass.LastTxStatusMaxLength)
        //        {
        //            LoggerClass.Log("Last transaction status is greater than max length");

        //            arr[13] = arr[13].Substring(0, ConfigClass.LastTxStatusMaxLength);

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

            LoggerClass.Log($"Adding/removing devices for {messageId}: {devicesData}");

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
                        LoggerClass.Log($"Not enought information to obtain DIG and data: {s}");
                    }
                }

                // Add or remove devices according to configuration
                foreach (var f in GetDevices(messageId))
                {
                    if (string.IsNullOrEmpty(f.Value.InnerText))
                    {
                        LoggerClass.Log($"Removing device {f.Key}");

                        if (dict.ContainsKey(f.Key))
                            dict.Remove(f.Key);
                    }
                    else
                    {
                        LoggerClass.Log($"Adding device {f.Key}");

                        if (dict.ContainsKey(f.Key))
                            dict[f.Key] = GetDataValue(f.Value.InnerText, dict[f.Key]);
                        else
                            dict.Add(f.Key, GetDataValue(f.Value.InnerText));
                    }
                }

                // Re-assemble devices data
                if (dict.Count > 0)
                {
                    LoggerClass.Log("Re-assembling devices data");

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

            foreach (var node in ConfigClass.Devices)
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
    }
}
