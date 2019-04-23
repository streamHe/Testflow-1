﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using Testflow.Common;
using Testflow.CoreCommon.Common;
using Testflow.CoreCommon.Messages;
using Testflow.Utility.MessageUtil;

namespace Testflow.SlaveCore.Common
{
    internal class MessageTransceiver : IDisposable
    {
        private readonly Messenger _uplinkMessenger;
        private readonly Messenger _downLinkMessenger;
        private readonly SlaveContext _slaveContext;
        private readonly LocalMessageQueue<MessageBase> _messageQueue;
        private Thread _peakThread;
        private CancellationTokenSource _cancellation;

        public MessageTransceiver(SlaveContext contextManager, int session)
        {
            this._slaveContext = contextManager;

            // 创建上行队列
            FormatterType formatterType = contextManager.GetProperty<FormatterType>("EngineQueueFormat");
            MessengerOption receiveOption = new MessengerOption(CoreConstants.UpLinkMQName, typeof(ControlMessage),
                typeof(DebugMessage), typeof(RmtGenMessage), typeof(StatusMessage), typeof(TestGenMessage))
            {
                Type = contextManager.GetProperty<MessengerType>("MessengerType"),
                Formatter = formatterType
            };
            _uplinkMessenger = Messenger.GetMessenger(receiveOption);
            // 创建下行队列
            MessengerOption sendOption = new MessengerOption(CoreConstants.DownLinkMQName, typeof(ControlMessage),
                typeof(DebugMessage), typeof(RmtGenMessage), typeof(StatusMessage), typeof(TestGenMessage))
            {
                Type = contextManager.GetProperty<MessengerType>("MessengerType"),
                Formatter = formatterType
            };
            _downLinkMessenger = Messenger.GetMessenger(sendOption);

            _messageQueue = new LocalMessageQueue<MessageBase>(CoreConstants.DefaultEventsQueueSize);
            this.SessionId = session;
        }

        public LocalMessageQueue<MessageBase> MessageQueue => _messageQueue;

        public void SendMessage(MessageBase message)
        {
            _uplinkMessenger.Send(message, _slaveContext.GetProperty<FormatterType>("EngineQueueFormat"),
                message.GetType());
        }

        public void StartReceive()
        {
            _messageQueue.Clear();
            _cancellation = new CancellationTokenSource();
            this._peakThread = new Thread(PeakMessage)
            {
                Name = "PeakThread",
                IsBackground = true
            };
            _peakThread.Start();
        }

        public int SessionId { get; }

        private void PeakMessage()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    IMessage message = _downLinkMessenger.Peak();
                    if (message.Id == SessionId)
                    {
                        IMessage receive = _downLinkMessenger.Receive();
                        _messageQueue.Enqueue((MessageBase)message);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                _slaveContext.LogSession.Print(LogLevel.Warn, CommonConst.PlatformLogSession, "Transceiver peak thread aborted");
            }
        }

        public void StopReceive()
        {
            _cancellation.Cancel();
            Thread.Sleep(100);
            if (_peakThread.IsAlive)
            {
                _peakThread.Abort();
            }
            _messageQueue.Clear();
        }

        public MessageBase Receive()
        {
            return _messageQueue.WaitUntilMessageCome();
        }

        public void Dispose()
        {
            StopReceive();
        }

        #region 调用WinApi的跨进程同步函数

        [DllImport("kernel32", EntryPoint = "CreateSemaphore", SetLastError = true, CharSet = CharSet.Unicode)]
         private static extern uint CreateSemaphore(SecurityAttributes auth, int initialCount, int maximumCount,
            string name);

        [DllImport("kernel32", EntryPoint = "WaitForSingleObject",
            SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WaitForSingleObject(uint hHandle, uint dwMilliseconds);

        [DllImport("kernel32", EntryPoint = "ReleaseSemaphore", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        private static extern bool ReleaseSemaphore(uint hHandle, int lReleaseCount, out int lpPreviousCount);

        [DllImport("kernel32", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        private static extern bool CloseHandle(uint hHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct SecurityAttributes
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        #endregion

    }
}