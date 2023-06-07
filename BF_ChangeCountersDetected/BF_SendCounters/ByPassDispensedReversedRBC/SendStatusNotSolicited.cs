﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;

namespace BF_ChangeCountersDetected
{
    public class SendStatusNotSolicited
    {
        RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NCR\Advance NDC\NotSolicitedMessageLetter");
        string BFMessageLetter;
        public void send()
        {

            if (key != null)
            {
                BFMessageLetter = (string)key.GetValue("LetterMessageID");
            }
            else
            {
                BFMessageLetter = "S";
            }

            LoggerClass.Log($"JM185384 Sending Message not solicited with ID : {BFMessageLetter}");
            NDCMessage.SendStatus(BFMessageLetter, false, false);
        }
    }
}
