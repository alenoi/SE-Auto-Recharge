using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {

        }

        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }

        double serverLimit = 0.3;
        double avg = 0.1;
        int cdCount = 0;
        double avgcd = 10;
        int autobattery = 0;
        string error = "";
        bool init = true;
        bool dampenerManagement = true;
        bool batteryRechargeManagement = true;
        bool batteryBackupManagement = true;
        bool scriptSpeedManagement = true;

        List<IMyTerminalBlock> connector;
        List<IMyShipController> cockpit;
        List<IMyBatteryBlock> battery;



        public void Main(string argument, UpdateType updateSource)
        {
            error = "";

            Initialization(ref error);

            if (error == "")
            {
                if ((cdCount <= 0 && avg < serverLimit) || !scriptSpeedManagement)
                {

                    ShowBatteryLevel();
                    if (ConnectorCheck())
                    {
                        SetDampeners(false);
                        BatteryManagement(true);
                    }
                    else
                    {
                        BatteryManagement(false);
                        SetDampeners(true);
                    }
                }
                else
                {
                    cdCount--;
                }
            }

            PBDisplay();
        }

        private void BatteryManagement(bool connected)
        {
            if (batteryRechargeManagement)
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
                    BatteryBackupManagement();
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

        private void BatteryBackupManagement()
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
                Echo("Auto: " + autobattery + " || Recharge: " + (battery.Count - autobattery).ToString() + "\n");
                Echo(ScriptSpeedManagement());
            }



        }

        private void ShowBatteryLevel()
        {
            if (batteryRechargeManagement)
            {
                foreach (IMyBatteryBlock batt in battery)
                {
                    if (batt.ChargeMode == ChargeMode.Recharge)
                    {
                        batt.CustomName = "[+ " + (Math.Round((batt.CurrentStoredPower / batt.MaxStoredPower) * 100)).ToString() + "%]" + batt.CustomName.Split(']')[batt.CustomName.Split(']').Length - 1];
                    }
                    else if (batt.ChargeMode == ChargeMode.Auto)
                    {
                        batt.CustomName = "[- " + (Math.Round((batt.CurrentStoredPower / batt.MaxStoredPower) * 100)).ToString() + "%]" + batt.CustomName.Split(']')[batt.CustomName.Split(']').Length - 1];
                    }
                }
            }
        }

        private void SetDampeners(bool dir)
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
                    + "bool dampenerManagement = true \n"
                    + "bool batteryRechargeManagement = true \n"
                    + "bool batteryBackupManagement = true \n\n"
                    + "If you play on a multiplayer server and the runtime of the PB is limited, \nmodify the next variables accordingly \n\n"
                    + "bool scriptSpeedManagement = true \n"
                    + "double serverLimit = 0.3 \n";
            }
        }

        private void CustomDataProcess(string customData)
        {
            ////////////////////////////////////////////////////TODO
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
                cdCount = (int)Math.Round(Math.Pow(2, ((avg / serverLimit) * 10)));
                avg = avg * 0.995 + Runtime.LastRunTimeMs * 0.005;
                avgcd = avgcd * 0.995 + cdCount * 0.005;
                scriptspeed += "Avg runtime: " + Math.Round(avg, 3).ToString() + " ms\n";
                scriptspeed += "PB heat: " + Math.Round((avg / serverLimit), 1) + "%\n";
                scriptspeed += "Script performance: " + Math.Round((1 / avgcd) * 100).ToString() + "%\n";
            }
            return scriptspeed;
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
            }

        }
    }
}

