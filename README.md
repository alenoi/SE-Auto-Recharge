
Small script that checks whether a Connector in the grid is connected. If it is then turns off the Inertia Dampeners to save energy and sets the Batteries to Recharge mode, except one. During the recharge session the script checks and changes the backup battery if has discharged too much so the ship will be charged as much to the max as possible.

Features:
- Setting batteries to Recharge and Dampeners off if the ship is docked
- Showing the Current Power level of the batteries in their names
- Showing the Current Charging state of the batteries in their names ( +(recharge) or -(auto))
- Ability to turn on and off feauter
- Ability to customise the dynamic script speed in CustomData

Version: 2020-12-06


--------------------------------------------------------------------
You can turn on and off features and configurate the heat resistance of the PB in Custom data.

Please make sure during the configuration to optimize the script performance with the heatLimit 
parameter as high as possible. If the performance is low the ship might be unable to detect 
that it is not docked any more. This can cause the fall and crash of the ship.
