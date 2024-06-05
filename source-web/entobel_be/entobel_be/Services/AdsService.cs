using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;
using TcEventLoggerAdsProxyLib;
using MongoDB.Bson;

using entobel_be.Models;


namespace entobel_be.Services
{
    public class AdsService
    {
        public AdsClient tcAdsClient = new AdsClient();
        //private AdsState tcAdsState = new AdsState();
        private TcEventLogger tcEventLogger = new TcEventLogger();
        //private string AmsNetId = "127.0.0.1.1.1";
        //private int port = 851;

        // Init connection to ADS
        public AdsService()
        {
            // check state
            //try
            //{
            //    // open ADS route connection to client
            //    tcAdsClient.Connect(AmsNetId, 851);
            //    // open connection to logger
            //    tcEventLogger.Connect(AmsNetId);
            //    //tcEventLogger.MessageSent += OnMessageSent;
            //    //tcEventLogger.AlarmRaised += OnAlarmRaised;
            //    //tcAdsState = tcAdsClient.ReadState().AdsState;
            //    System.Diagnostics.Debug.WriteLine("ADS connected");
            //}
            //catch (Exception err)
            //{
            //    System.Diagnostics.Debug.WriteLine(err.Message);
            //    System.Diagnostics.Debug.WriteLine("ADS not connected");
            //}
        }

        public bool AdsConnect(string AmsNetId, int port)
        {
            if (!AdsCheckConnection())
            {
                try
                {
                    // open ADS route connection to client
                    tcAdsClient.Connect(AmsNetId, port);
                    // open connection to logger
                    tcEventLogger.Connect(AmsNetId);
                    System.Diagnostics.Debug.WriteLine("ADS connected");
                    return true;
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                    System.Diagnostics.Debug.WriteLine("ADS not connected");
                    return false;
                }
            }
            else return true;
        }

        public void AdsDisconnect()
        {
            //tcAdsClient.Connect(AmsNetId, 851);
            //tcEventLogger.Connect(AmsNetId);
            if (AdsCheckConnection())
            {
                try
                {
                    tcAdsClient.Disconnect();
                    System.Diagnostics.Debug.WriteLine("ADS disconnected");
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                    System.Diagnostics.Debug.WriteLine("ADS disconnection failed");
                }
            }
        }

        public bool AdsCheckConnection()
        {
            try
            {
                if (tcAdsClient.ReadState().AdsState == AdsState.Run && tcEventLogger.IsConnected) { return true; }
                else return false;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine(err.Message);
                return false;
            }
        }

        public object AdsRead(AdsClient TcAdsClient, string Symbol, Type SymbolType)
        {
            object result;
            uint hValue;
            if (AdsCheckConnection())
            {
                try
                {
                    hValue = TcAdsClient.CreateVariableHandle(Symbol);
                    // check type Datetime and parse
                    if (SymbolType == typeof(DateTime))
                    {
                        var date = (string)TcAdsClient.ReadValue(Symbol, typeof(string)); // DATE
                        result = DateTime.Parse(date.Remove(10, 1).Insert(10, "T")); ;
                    }
                    // other symbol types
                    else result = TcAdsClient.ReadValue(Symbol, SymbolType);
                    TcAdsClient.DeleteVariableHandle(hValue);
                    return result;
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                }
            }
            //else return "0"; // var in PLC always return numbers, therefore must return 0 if no data read
            // if exception or not connected
            if (SymbolType == typeof(DateTime))
            {
                return new DateTime(1980, 01, 01, 0, 0, 0); ;
            }
            else if (SymbolType == typeof(bool))
            {
                bool val = false;
                result = val;
                return result;
            }
            else
            {
                double val = 0;
                result = val;
                return result;
            }
        }

        public void AdsWrite(AdsClient TcAdsClient, string Symbol, object value)
        {
            uint hValue;
            if (AdsCheckConnection())
            {
                try
                {
                    hValue = TcAdsClient.CreateVariableHandle(Symbol);
                    // other symbol types
                    TcAdsClient.WriteAny(hValue, value);
                    TcAdsClient.DeleteVariableHandle(hValue);
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                }
            }
        }

        // read events
        public List<AdsEvent> AdsReadEvents(uint EventsCount)
        {
            // check ADS connection
            if (AdsCheckConnection())
            {
                List<AdsEvent> Events = new List<AdsEvent>();
                try
                {
                    // get the number of events from TC 
                    TcLoggedEventCollection table = tcEventLogger.GetLoggedEvents(EventsCount);
                    foreach (ITcLoggedEvent4 HappenedEvent in table)
                    {
                        AdsEvent Event = new AdsEvent();
                        Event.EventId = HappenedEvent.EventId;
                        Event.EventClass = HappenedEvent.GetEventClassName(1033); // 1033 for English
                        Event.Severity = HappenedEvent.SeverityLevel.ToString();
                        Event.Text = HappenedEvent.GetText(1033); // 1033 for English
                        Event.Source = HappenedEvent.SourceName;
                        Event.TimeRaised = HappenedEvent.TimeRaised.ToString();
                        //Event.TimeConfirmed = HappenedEvent.TimeConfirmed.ToString();
                        //Event.TimeClear = HappenedEvent.TimeCleared.ToString();
                        // add Event to Events list
                        Events.Add(Event);
                    }
                    return Events;
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                    return null;
                }
            }
            else return null;
        }

        private void OnMessageSent(TcMessage message)
        {
            String Text = message.GetText(1033); // 1033 for English
            String Severity = message.SeverityLevel.ToString();
            DateTime T_Raised = message.TimeRaised;
            //Chương trình xử lý các giá trị đọc về

            //
        }

        private void OnAlarmRaised(TcAlarm alarm)
        {
            String Text = alarm.GetText(1033); // 1033 for English
            String Severity = alarm.SeverityLevel.ToString();
            DateTime T_Raised = alarm.TimeRaised;
            DateTime T_Confirmed = alarm.TimeConfirmed;
            DateTime T_Clear = alarm.TimeCleared;
            //Chương trình xử lý các giá trị đọc về

            //
        }

    }
}
