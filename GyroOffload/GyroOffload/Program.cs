using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public IMyRemoteControl getRC()
        {
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0];
            return RC;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "INIT")
            {
                try
                {
                    getRC();
                }
                catch (Exception ex)
                {
                    Echo("getRC() exception: " + ex.Message);
                    Me.CustomData = "FAULT";
                }
                try
                {
                    var tmGy = new List<IMyGyro>();
                    GridTerminalSystem.GetBlocksOfType<IMyGyro>(tmGy);
                }
                catch (Exception ex)
                {
                    Echo("Exception when detecting gyros: " + ex.Message);
                    Me.CustomData = "FAULT";
                }
                try
                {
                    IMyRemoteControl RC = getRC();
                    Matrix orient;
                    RC.Orientation.GetMatrix(out orient);
                    Vector3D down = orient.Down;
                    Vector3D grav = RC.GetNaturalGravity();
                    grav.Normalize();
                }
                catch (Exception ex)
                {
                    Echo("Unable to detect gravity! ( " + ex.Message + " )");
                    Me.CustomData = "FAULT";
                }
                Me.CustomData = "READY";
            }
            if(Convert.ToDouble(argument) <= 1.00)
            {
                correctOrient(Convert.ToDouble(argument));
            }
        }
        public void correctOrient(double CTRL_COEFF)
        {
            Me.CustomData = "PROCESSING";
            IMyRemoteControl RC = getRC();
            //Get gyros
            var tmGy = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(tmGy);
            //Check pitch/roll/yaw compared to gravity
            Matrix orient;
            RC.Orientation.GetMatrix(out orient);
            Vector3D down = orient.Down;
            Vector3D grav = RC.GetNaturalGravity();
            grav.Normalize();
            foreach (IMyGyro gyro in tmGy)
            {
                gyro.Orientation.GetMatrix(out orient);
                var lDown = Vector3D.Transform(down, MatrixD.Transpose(orient));
                var lGrav = Vector3D.Transform(grav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));
                var rot = Vector3D.Cross(lDown, lGrav);
                double ang = rot.Length();
                ang = Math.Atan2(ang, Math.Sqrt(Math.Max(00, 1.0 - ang * ang)));
                if (ang > 0.01)
                {
                    double ctrl_vel = gyro.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                    ctrl_vel = Math.Min(gyro.GetMaximum<float>("Yaw"), ctrl_vel);
                    ctrl_vel = Math.Max(0.01, ctrl_vel);
                    rot.Normalize();
                    rot *= ctrl_vel;
                    gyro.SetValueFloat("Pitch", (float)rot.GetDim(0));
                    gyro.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                    gyro.SetValueFloat("Roll", -(float)rot.GetDim(2));
                    gyro.SetValueFloat("Power", 1.0f);
                    gyro.GyroOverride = true;
                }
                if (ang < 0.01) { gyro.GyroOverride = false; gyro.SetValueFloat("Pitch", 0); gyro.SetValueFloat("Yaw", 0); gyro.SetValueFloat("Roll", 0); }
            }
            Me.CustomData = "READY";
        }
    }
}
