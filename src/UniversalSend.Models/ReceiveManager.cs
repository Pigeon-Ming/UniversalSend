﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalSend.Models.HttpData;
using UniversalSend.Models.Tasks;

namespace UniversalSend.Models
{
    public class ReceiveManager
    {
        public static event EventHandler SendRequestReceived;//接收到Send-Request事件

        public static event EventHandler SendDataReceived;//接收到Send事件

        public static event EventHandler CancelReceived;//接收到Cancel事件

        public enum QuickSaveMode
        {
            Off,
            Favorites,
            On,
        }

        public static QuickSaveMode QuickSave { get; set; } = QuickSaveMode.Off;

        public static bool? ChosenOption { get; set; }

        public static async Task<bool> GetChosenOption()
        {
            await Task.Run(async () =>
            {
                int waitTime = 0;
                while (ChosenOption == null)
                {
                    await Task.Delay(500);
                    if (waitTime++ > 100)
                    {
                        ChosenOption = false;
                        return;
                    }

                }
            });
            bool option = (bool)ChosenOption;
            ChosenOption = null;
            return option;
        }

        public static void SendRequestEvent(SendRequestData sendRequestData)
        {
            SendRequestReceived?.Invoke(sendRequestData,EventArgs.Empty);
        }


        public static void SendDataReceivedEvent(ReceiveTask receiveTask)
        {
            SendDataReceived?.Invoke(receiveTask,EventArgs.Empty);
        }

        public static void CancelReceivedEvent()
        {
            ReceiveTaskManager.ReceivingTasks.Clear();
            CancelReceived?.Invoke(null,EventArgs.Empty);
        }
    }
}
