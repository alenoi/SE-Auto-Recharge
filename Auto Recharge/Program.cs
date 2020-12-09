using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {



        double serverLimit;
        double avg = 0;
        int cdCount = 0;
        double avgcd = 0;
        int autobattery = 0;
        string error = "";
        bool init = true;
        bool dampenerManagement;
        bool batteryManagement;
        bool batteryBackupManagement;
        bool scriptSpeedManagement;
        int heatLimit;
        bool connectorState = false;
        int rounds = 0;

        List<IMyTerminalBlock> connector;
        List<IMyShipController> cockpit;
        List<IMyBatteryBlock> battery;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }



        public void Main(string argument, UpdateType updateSource)
        {
            error = "";
            Initialization(ref error);
            PBDisplay();
            if (error == "")
            {
                if ((cdCount <= 0 && avg < serverLimit & scriptSpeedManagement) || !scriptSpeedManagement)
                {
                    if (rounds >= 10)
                    {
                        BatteryBackupManagement(ref batteryBackupManagement, connectorState);
                        ShowBatteryLevel();
                        rounds = 0;
                    }
                    else
                    {
                        rounds++;
                        ShowBatteryLevel();
                    }
                    if (ConnectorCheck() != connectorState)
                    {
                        connectorState = ConnectorCheck();
                        if (connectorState)
                        {
                            SetDampeners(false, ref dampenerManagement);
                            BatteryManagement(true, ref batteryManagement, ref batteryBackupManagement);
                        }
                        else
                        {
                            SetDampeners(true, ref dampenerManagement);
                            BatteryManagement(false, ref batteryManagement, ref batteryBackupManagement);
                        } 
                    }
                }
                else
                {
                    cdCount--;
                }
            }
        }

        private void BatteryManagement(bool connected, ref bool batteryManagement, ref bool batteryBackupManagement)
        {
            if (batteryManagement)
            {
                if (connected)
                {
                    autobattery = 0;
                    foreach (IMyBatteryBlock batt in battery)
                    {

                        if (batt.ChargeMode == ChargeMode.Auto)
                        {
                            autobattery++;
                        }
                    }
                    foreach (IMyBatteryBlock batt in battery)
                    {
                        if (autobattery > 1 && batt.ChargeMode == ChargeMode.Auto)
                        {
                            batt.ChargeMode = ChargeMode.Recharge;
                        }

                    }
                    BatteryBackupManagement(ref batteryBackupManagement, connected);
                }
                else
                {
                    for (int i = 0; i < battery.Count; i++)
                    {
                        battery[i].ChargeMode = ChargeMode.Auto;
                    }
                }
            }
        }

        private void BatteryBackupManagement(ref bool batteryBackupManagement, bool connectorState)
        {
            if (batteryBackupManagement)
            {
                IMyBatteryBlock maxbattery = battery[0];
                if (maxbattery.CurrentStoredPower / maxbattery.MaxStoredPower < 0.9)
                {
                    foreach (IMyBatteryBlock batt in battery)
                    {
                        if (batt.CurrentStoredPower > maxbattery.CurrentStoredPower)
                        {
                            maxbattery = batt;
                        }
                    }
                }
                if (connectorState)
                {
                    foreach (IMyBatteryBlock batt in battery)
                    {
                        if (autobattery > 1 && batt.ChargeMode == ChargeMode.Auto)
                        {
                            batt.ChargeMode = ChargeMode.Recharge;
                        }
                    } 
                }
                maxbattery.ChargeMode = ChargeMode.Auto;
            }
        }

        private void PBDisplay()
        {
            Echo(" -- Auto Recharge Manager -- \n");
            if (error != "")
            {
                Echo(" -- ERROR -- \n\n" + error);
            }
            else
            {
                Echo("Script speed management: " + scriptSpeedManagement.ToString() + "\n"
                    + "Battery management: " + batteryManagement.ToString() + "\n"
                    + "Battery backup management: " + batteryBackupManagement.ToString() + "\n"
                    + "Dampener management: " + dampenerManagement.ToString() + "\n");
                Echo(ScriptSpeedManagement());
            }
        }

        private void ShowBatteryLevel()
        {
            if (batteryManagement)
            {
                if (rounds >=10)
                {
                    autobattery = 0;
                    foreach (IMyBatteryBlock batt in battery)
                    {
                        if (batt.ChargeMode == ChargeMode.Recharge)
                        {
                            batt.CustomName = "[+ " + (Math.Round((batt.CurrentStoredPower / batt.MaxStoredPower) * 100)).ToString() + "%]" + batt.CustomName.Split(']')[batt.CustomName.Split(']').Length - 1];
                        }
                        else if (batt.ChargeMode == ChargeMode.Auto)
                        {
                            batt.CustomName = "[- " + (Math.Round((batt.CurrentStoredPower / batt.MaxStoredPower) * 100)).ToString() + "%]" + batt.CustomName.Split(']')[batt.CustomName.Split(']').Length - 1];
                            autobattery++;
                        }
                    } 
                }
                Echo("Auto: " + autobattery + " || Recharge: " + (battery.Count - autobattery).ToString() + "\n");
            }
        }

        private void SetDampeners(bool dir, ref bool dampenerManagement)
        {
            if (dampenerManagement)
            {
                for (int i = 0; i < cockpit.Count; i++)
                {
                    cockpit[i].DampenersOverride = dir;
                }
            }
        }

        private void CustomData()
        {
            if (Me.CustomData.Length > 0)
            {
                CustomDataProcess(Me.CustomData);
            }
            else
            {
                Me.CustomData =
                    "With setting the next variables you can turn ON and OFF the features \nof the script \n\n"
                    + "dampenerManagement=true\n"
                    + "batteryManagement=true\n"
                    + "batteryBackupManagement=true\n\n"
                    + "If you play on a multiplayer server and the runtime of the PB is limited, \nmodify the next variables accordingly \n\n"
                    + "scriptSpeedManagement=true\n"
                    + "serverLimit=0.3 ms\n"
                    + "heatLimit=30 %\n";
                CustomData();
            }
        }

        private void CustomDataProcess(string customData)
        {
            List<string[]> customInput = new List<string[]>();
            string[] customDataA = customData.Split('\n');
            for (int i = 0; i < customDataA.Length; i++)
            {
                if (customDataA[i].Contains("="))
                {
                    customInput.Add(customDataA[i].Split('='));
                }
            }
            foreach (var item in customInput)
            {
                item[0] = item[0].Substring(0, item[0].Length);
                item[1] = item[1].Split(' ')[0];
            }
            foreach (var item in customInput)
            {
                switch (item[0])
                {
                    default:
                        break;
                    case "dampenerManagement":
                        dampenerManagement = bool.Parse(item[1]);
                        break;
                    case "batteryManagement":
                        batteryManagement = bool.Parse(item[1]);
                        break;
                    case "batteryBackupManagement":
                        batteryBackupManagement = bool.Parse(item[1]);
                        break;
                    case "scriptSpeedManagement":
                        scriptSpeedManagement = bool.Parse(item[1]);
                        break;
                    case "serverLimit":
                        serverLimit = double.Parse(item[1]);
                        break;
                    case "heatLimit":
                        heatLimit = int.Parse(item[1]);
                        break;
                }
            }
        }


        private bool ConnectorCheck()
        {
            bool result0connector = false;
            for (int i = 0; i < connector.Count; i++)
            {
                if (((IMyShipConnector)connector[i]).Status == MyShipConnectorStatus.Connected)
                {
                    result0connector = true;
                    break;
                }
            }
            return result0connector;
        }

        private string ScriptSpeedManagement()
        {

            string scriptspeed = "";
            if (scriptSpeedManagement)
            {
                if (Math.Round(((avg / serverLimit) * 100), 1) > heatLimit)
                {
                    cdCount = (int)Math.Round(Math.Pow(2, ((avg / serverLimit) * 10)));
                }
                avg = avg * 0.995 + Runtime.LastRunTimeMs * 0.005;
                avgcd = avgcd * 0.995 + cdCount * 0.005;
                scriptspeed += "Server limit: " + serverLimit + " ms\n";
                scriptspeed += "Avg runtime: " + Math.Round(avg, 3).ToString() + " ms\n";
                scriptspeed += "PB heat: " + Math.Round(((avg / serverLimit) * 100), 1) + "%\n";
                scriptspeed += "Script performance: " + Math.Round((1 / (avgcd + 1)) * 100).ToString() + "%\n";
            }
            return scriptspeed;
        }
        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }
        private void Initialization(ref string error)
        {
            CustomData();

            if (init)
            {
                connector = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connector, filterThis);
                if (connector.Count == 0)
                {
                    error += "No connector\n";
                }

                cockpit = new List<IMyShipController>();
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(cockpit, filterThis);
                if (cockpit.Count == 0)
                {
                    error += "No cockpit\n";
                }

                battery = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(battery, filterThis);
                if (battery.Count == 0)
                {
                    error += "No battery\n";
                }
                init = false;
                connectorState = ConnectorCheck();
            }

        }
    }
}

