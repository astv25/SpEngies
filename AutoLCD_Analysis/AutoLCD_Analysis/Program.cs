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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /* v:2.0131 [Commands Docked, ChargeTime, TanksV & several bar only variants]
* In-game script by MMaster
*
* Last Update: Docked command to show name(s) of docked ship(s) on specified connectors with variants showing connector name and/or showing/hiding empty connectors
*  (make sure to use same grid filtering if you use name filter so you don't get to show connectors of the docked ships)
*  ChargeTime command to display remaining time to charge jump drive
*  Tanks command variants TanksV (exact tank liters) and TanksP (percentage with bar)
*  Tanks command default now shows both exact values and percentage with bar
*  DetailsNoN variant to hide names of blocks in details output
*  AmountBar, CargoBar, CargoAllBar, ChargeBar, PowerTimeBar & TanksBar variants to display only progress bars with no text
*
* Previous updates: Look at Change notes tab on Steam workshop page.
*
* Customize these: */

        // Use this tag to identify LCDs managed by this script
        // Name filtering rules can be used here so you can use even G:Group or T:[My LCD]
        public string LCD_TAG = "T:[GCLCD]";

        // How many lines to scroll per step
        public const int SCROLL_LINES_PER_STEP = 1;

        // Script automatically figures if LCD is using monospace font
        // if you use custom font scroll down to the bottom, then scroll a bit up until you find AddCharsSize lines
        // monospace font name and size definition is above those

        // Enable initial boot sequence (after compile / world load)
        public const bool ENABLE_BOOT = true;

        /* READ THIS FULL GUIDE
        http://steamcommunity.com/sharedfiles/filedetails/?id=407158161

        Basic video guide
        Please watch the video guide even if you don't understand my English. You can see how things are done there.

        https://youtu.be/vqpPQ_20Xso


        Read Change Notes (above screenshots) for latest updates and new features.
        I notify about updates on twitter so follow if interested.

        Please carefully read the FULL GUIDE before asking questions I had to remove guide from here to add more features :(
        Please DO NOT publish this script or its derivations without my permission! Feel free to use it in blueprints!

        Special Thanks
        Keen Software House for awesome Space Engineers game
        Malware for contributing to programmable blocks game code
        Textor and CyberVic for their great script related contributions on Keen forums.

        Watch Twitter: https://twitter.com/MattsPlayCorner
        and Facebook: https://www.facebook.com/MattsPlayCorner1080p
        for more crazy stuff from me in the future :)
        */


        /* Customize characters used by script */
        class MMStyle
        {
            // Monospace font characters (\uXXXX is special character code)
            public const char BAR_MONO_START = '[';
            public const char BAR_MONO_END = ']';
            public const char BAR_MONO_EMPTY = '\u2591'; // 25% rect
            public const char BAR_MONO_FILL = '\u2588'; // full rect

            // Classic (Debug) font characters
            // Start and end characters of progress bar need to be the same width!
            public const char BAR_START = '[';
            public const char BAR_END = ']';
            // Empty and fill characters of progress bar need to be the same width!
            public const char BAR_EMPTY = '\'';
            public const char BAR_FILL = '|';

        }


        // (for developer) Debug level to show
        public const int DebugLevel = 0;

        // (for modded lcds) Affects all LCDs managed by this programmable block
        /* LCD height modifier
        0.5f makes the LCD have only 1/2 the lines of normal LCD
        2.0f makes it fit 2x more lines on LCD */
        public const float HEIGHT_MOD = 1.0f;

        /* line width modifier
        0.5f moves the right edge to 50% of normal LCD width
        2.0f makes it fit 200% more text on line */
        public const float WIDTH_MOD = 1.0f;

        List<string> BOOT_FRAMES = new List<string>() {
/* BOOT FRAMES
* Each @"<text>" marks single frame, add as many as you want each will be displayed for one second
* @"" is multiline string so you can write multiple lines */
@"
Initializing systems"
,
@"
Verifying connections"
,
@"
Loading commands"
};

        void ItemsConf()
        {
            // ITEMS AND QUOTAS LIST
            // (subType, mainType, quota, display name, short name)
            // VANILLA ITEMS
            Add("Stone", "Ore");
            Add("Iron", "Ore");
            Add("Nickel", "Ore");
            Add("Cobalt", "Ore");
            Add("Magnesium", "Ore");
            Add("Silicon", "Ore");
            Add("Silver", "Ore");
            Add("Gold", "Ore");
            Add("Platinum", "Ore");
            Add("Uranium", "Ore");
            Add("Ice", "Ore");
            Add("Scrap", "Ore");
            Add("Stone", "Ingot", 40000, "Gravel", "gravel");
            Add("Iron", "Ingot", 300000);
            Add("Nickel", "Ingot", 900000);
            Add("Cobalt", "Ingot", 120000);
            Add("Magnesium", "Ingot", 80000);
            Add("Silicon", "Ingot", 80000);
            Add("Silver", "Ingot", 800000);
            Add("Gold", "Ingot", 80000);
            Add("Platinum", "Ingot", 45000);
            Add("Uranium", "Ingot", 12000);
            Add("AutomaticRifleItem", "Tool", 0, "Automatic Rifle");
            Add("PreciseAutomaticRifleItem", "Tool", 0, "* Precise Rifle");
            Add("RapidFireAutomaticRifleItem", "Tool", 0, "** Rapid-Fire Rifle");
            Add("UltimateAutomaticRifleItem", "Tool", 0, "*** Elite Rifle");
            Add("WelderItem", "Tool", 0, "Welder");
            Add("Welder2Item", "Tool", 0, "* Enh. Welder");
            Add("Welder3Item", "Tool", 0, "** Prof. Welder");
            Add("Welder4Item", "Tool", 0, "*** Elite Welder");
            Add("AngleGrinderItem", "Tool", 0, "Angle Grinder");
            Add("AngleGrinder2Item", "Tool", 0, "* Enh. Grinder");
            Add("AngleGrinder3Item", "Tool", 0, "** Prof. Grinder");
            Add("AngleGrinder4Item", "Tool", 0, "*** Elite Grinder");
            Add("HandDrillItem", "Tool", 0, "Hand Drill");
            Add("HandDrill2Item", "Tool", 0, "* Enh. Drill");
            Add("HandDrill3Item", "Tool", 0, "** Prof. Drill");
            Add("HandDrill4Item", "Tool", 0, "*** Elite Drill");
            Add("Construction", "Component", 50000);
            Add("MetalGrid", "Component", 15500, "Metal Grid");
            Add("InteriorPlate", "Component", 55000, "Interior Plate");
            Add("SteelPlate", "Component", 300000, "Steel Plate");
            Add("Girder", "Component", 3500);
            Add("SmallTube", "Component", 26000, "Small Tube");
            Add("LargeTube", "Component", 6000, "Large Tube");
            Add("Motor", "Component", 16000);
            Add("Display", "Component", 500);
            Add("BulletproofGlass", "Component", 12000, "Bulletp. Glass", "bpglass");
            Add("Computer", "Component", 6500);
            Add("Reactor", "Component", 10000);
            Add("Thrust", "Component", 16000, "Thruster", "thruster");
            Add("GravityGenerator", "Component", 250, "GravGen", "gravgen");
            Add("Medical", "Component", 120);
            Add("RadioCommunication", "Component", 250, "Radio-comm", "radio");
            Add("Detector", "Component", 400);
            Add("Explosives", "Component", 500);
            Add("SolarCell", "Component", 2800, "Solar Cell");
            Add("PowerCell", "Component", 2800, "Power Cell");
            Add("Superconductor", "Component", 3000);
            Add("Canvas", "Component", 300);
            Add("NATO_5p56x45mm", "Ammo", 8000, "5.56x45mm", "5.56x45mm");
            Add("NATO_25x184mm", "Ammo", 2500, "25x184mm", "25x184mm");
            Add("Missile200mm", "Ammo", 1600, "200mm Missile", "200mmmissile");
            Add("OxygenBottle", "OxygenContainerObject", 5, "Oxygen Bottle");
            Add("HydrogenBottle", "GasContainerObject", 5, "Hydrogen Bottle");

            // MODDED ITEMS
            // (subType, mainType, quota, display name, short name, used)
            // * if used is true, item will be shown in inventory even for 0 items
            // * if used is false, item will be used only for display name and short name
            // AzimuthSupercharger
            Add("AzimuthSupercharger", "Component", 1600, "Supercharger", "supercharger", false);
            // OKI Ammo
            Add("OKI23mmAmmo", "Ammo", 500, "23x180mm", "23x180mm", false);
            Add("OKI50mmAmmo", "Ammo", 500, "50x450mm", "50x450mm", false);
            Add("OKI122mmAmmo", "Ammo", 200, "122x640mm", "122x640mm", false);
            Add("OKI230mmAmmo", "Ammo", 100, "230x920mm", "230x920mm", false);

            // REALLY REALLY REALLY
            // DO NOT MODIFY ANYTHING BELOW THIS (TRANSLATION STRINGS ARE AT THE BOTTOM)
        }
        void Add(string sT, string mT, int q = 0, string dN = "", string sN = "", bool u = true)
        {
            MMItems.Add(sT, mT, q, dN, sN, u);
        }
        vl MMItems;
        vu ki;
        vd kj;
        vw kk = null;
        void kl(string a) { }
        void km(string b, string d)
        {
            var e = b.ToLower();
            switch (e)
            {
                case "lcd_tag": LCD_TAG = d;
                break;
            }
        }
        void kn()
        {
            string[] f = Me.CustomData.Split('\n'); //Appears to be parsing its own code???, splits by line break into array
            for (int g = 0; g < f.Length; g++) //runs through each array entry
            {
                var i = f[g];
                int j = i.IndexOf('=');
                if (j < 0)
                {
                    kl(i); //but kl() doesn't do anything?
                    continue;
                }
                var k = i.Substring(0, j).Trim();
                var l = i.Substring(j + 1).Trim();
                km(k, l);
            }
        }
        void ko(vu n) //looks like initialization
        {
            MMItems = new vl();
            ItemsConf();
            kn();
            kk = new vw(this, DebugLevel, n);
            kk.MMItems = MMItems;
            kk.tA = LCD_TAG;
            kk.tB = SCROLL_LINES_PER_STEP;
            kk.tC = ENABLE_BOOT;
            kk.tD = BOOT_FRAMES;
            kk.tE = false;
            kk.tH = HEIGHT_MOD;
            kk.tG = WIDTH_MOD;
            kk.uA();
        }
        void kp() //what
        {
            ki.sR = this;
            kk.u0 = this;
        }
        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        void Main(string o, UpdateType q)
        {
            try
            {
                if (ki == null) //init catch?
                {
                    ki = new vu(this, DebugLevel);
                    ko(ki);
                    kj = new vd(kk);
                    ki.sT(kj, 0);
                }
                else
                {
                    kp();
                    kk.u2.r0();
                }
                if (o.Length == 0 && (q & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) == 0) //if no argument and we're invoked by auto-update
                {
                    ki.sY();//?
                    return;
                }
                if (o != "")
                {
                    if (kj.p9(o))
                    {
                        ki.sY();//??
                        return;
                    }
                }
                kj.oV = 0; ki.sZ();
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());//stop and disable self if error
                Me.Enabled = false;
            }
        }
        class uC : v8
        {
            public uC()
            {
                sB = 7;
                sx = "CmdInvList";
            }
            float kq = -1;
            float kr = -1;
            public override void Init()
            {
                kF = new vj(sF, o5.u2);
                kG = new vn(sF, o5);
            }
            Dictionary<string, string> ks = new Dictionary<string, string>();
            void kt(string r, double u, int v)
            {
                if (v > 0)
                {
                    o5.ul(Math.Min(100, 100 * u / v), 0.3f);
                    string w;
                    if (ks.ContainsKey(r))
                    {
                        w = ks[r];
                    }
                    else
                    {
                        w = o5.uz(r, o5.tJ * 0.5f - kv - kr);
                        ks[r] = w;
                    }
                    o5.Add(' ' + w + ' ');
                    o5.uh(o5.ut(u), 1.0f, kv + kr);
                    o5.ud(" / " + o5.ut(v));
                }
                else
                {
                    o5.Add(r + ':');
                    o5.ug(o5.ut(u), 1.0f, kq);
                }
            }
            void ku(string x, double y, double z, int A)
            {
                if (A > 0)
                {
                    o5.Add(x + ' ');
                    o5.uh(o5.ut(y), 0.51f);
                    o5.Add(" / " + o5.ut(A));
                    o5.ug(" +" + o5.ut(z) + " " + o7.T("I1"), 1.0f);
                    o5.uk(Math.Min(100, 100 * y / A));
                }
                else
                {
                    o5.Add(x + ':');
                    o5.uh(o5.ut(y), 0.51f);
                    o5.ug(" +" + o5.ut(z) + " " + o7.T("I1"), 1.0f);
                }
            }
            float kv = 0; bool kw(vo B)
            {
                int C= (kK ? B.rE : B.rF);
                if (C < 0) return true;
                float D = o5.uy(o5.ut(C), o5.u9);
                if (D > kv) kv = D;
                return true;
            }
            List<vo> kx;
            int ky = 0;
            int kz = 0;
            bool kA(bool E, bool F, string G, string H)
            {
                if (!E)
                {
                    kz = 0;
                    ky = 0;
                }
                if (kz == 0)
                {
                    if (kQ)
                    {
                        if ((kx = kG.rB(G, E, kw)) == null) return false;
                    }
                    else
                    {
                        if ((kx = kG.rB(G,E)) == null) return false;
                    }
                    kz++; E = false;
                }
                if (kx.Count > 0)
                {
                    if (!F && !E)
                    {
                        if (!o5.ua) o5.ud(""); o5.ui("<< " + H + " " + o7.T("I2") + " >>");
                    }
                    for (; ky < kx.Count; ky++)
                    {
                        if (!sF.t3(30)) return false;
                        double I = kx[ky].rJ;
                        if (kK && I >= kx[ky].rE) continue;
                        int J = kx[ky].rF;
                        if (kK) J = kx[ky].rE;
                        var K = o5.ur(kx[ky].rG, kx[ky].rH);
                        kt(K, I, J);
                    }
                }
                return true;
            }
            List<vo> kB; int kC = 0; int kD = 0; bool kE(bool L)
            {
                if (!L) { kC = 0; kD = 0; }
                if (kD == 0)
                {
                    if ((
kB = kG.rB("Ingot", L)) == null) return false; kD++; L = false;
                }
                if (kB.Count > 0)
                {
                    if (!kL && !L)
                    {
                        if (!o5.ua) o5.ud(""); o5.ui("<< " + o7.T("I4") + " " + o7
.T("I2") + " >>");
                    }
                    for (; kC < kB.Count; kC++)
                    {
                        if (!sF.t3(40)) return false; double N = kB[kC].rJ; if (kK && N >= kB[kC].rE) continue; int O = kB[kC].rF
; if (kK) O = kB[kC].rE; var P = o5.ur(kB[kC].rG, kB[kC].rH); if (kB[kC].rG != "Scrap")
                        {
                            double Q = kG.ry(kB[kC].rG + " Ore", kB[kC].rG, "Ore").rJ; ku(
P, N, Q, O);
                        }
                        else kt(P, N, O);
                    }
                }
                return true;
            }
            vj kF = null; vn kG; List<vh> kH; bool kI, kJ, kK, kL; int kM, kN; string kO = ""; float kP = 0; bool kQ =
true; void kR()
            {
                if (o5.u9 != kO || kP != o5.tJ) { ks.Clear(); kP = o5.tJ; }
                if (o5.u9 != kO) { kr = o5.uy(" / ", o5.u9); kq = o5.ux(' ', o5.u9); kO = o5.u9; }
                kF.
qU(); kI = o4.pQ.EndsWith("x") || o4.pQ.EndsWith("xs"); kJ = o4.pQ.EndsWith("s") || o4.pQ.EndsWith("sx"); kK = o4.pQ.StartsWith("missing"); kL =
o4.pQ.StartsWith("invlist"); kG.rt(); kH = o4.pX; if (kH.Count == 0) kH.Add(new vh("all"));
            }
            bool kS(bool R)
            {
                if (!R) kM = 0; for (; kM < kH.Count; kM
++)
                {
                    vh S = kH[kM]; S.q6(); var U = S.q3.ToLower(); if (!R) kN = 0; else R = false; for (; kN < S.q5.Count; kN++)
                    {
                        if (!sF.t3(30)) return false; string[] V =
S.q5[kN].ToLower().Split(':'); double W; if (V[0] == "all") V[0] = ""; int X = 1; int Y = -1; if (V.Length > 1)
                        {
                            if (Double.TryParse(V[1], out W))
                            {
                                if (
kK) X = (int)Math.Ceiling(W);
                                else Y = (int)Math.Ceiling(W);
                            }
                        }
                        var Z = V[0]; if (U != "") Z += ' ' + U; kG.ru(Z, (S.q2 == "-"), X, Y);
                    }
                }
                return true;
            }
            int
kT = 0; int kU = 0; int kV = 0; bool kW(bool _)
            {
                vj a0 = kF; if (!_) kT = 0; for (; kT < a0.qV(); kT++)
                {
                    if (!_) kU = 0; for (; kU < a0.qA[kT].InventoryCount; kU++)
                    {
                        if (!_) kV = 0; else _ = false; IMyInventory a1 = a0.qA[kT].GetInventory(kU); List<IMyInventoryItem> a2 = a1.GetItems(); for (; kV < a2.Count; kV++)
                        {
                            if (!sF.t3(40)) return false; IMyInventoryItem a3 = a2[kV]; var a4 = o5.up(a3); var a5 = a4.ToLower(); string a6, a7; o5.uq(a5, out a6, out a7); if
                                             (a7 == "ore") { if (kG.rw(a6.ToLower() + " ingot", a6, "Ingot") && kG.rw(a4, a6, a7)) continue; }
                            else { if (kG.rw(a4, a6, a7)) continue; }
                            o5.uq(a4, out
a6, out a7); vo a8 = kG.ry(a5, a6, a7); a8.rJ += (double)a3.Amount;
                        }
                    }
                }
                return true;
            }
            int kX = 0; public override bool RunCmd(bool a9)
            {
                if (!a9)
                {
                    kR
(); kX = 0;
                }
                for (; kX <= 9; kX++)
                {
                    switch (kX)
                    {
                        case 0: if (!kF.qK(o4.pR, a9)) return false; break;
                        case 1:
                            if (!kS(a9)) return false; if (!kI)
                            {
                                if (!kG.
rD(a9)) return false;
                            }
                            break;
                        case 2: if (!kW(a9)) return false; break;
                        case 3: if (!kA(a9, kL, "Ore", o7.T("I3"))) return false; break;
                        case 4:
                            if
(kJ) { if (!kA(a9, kL, "Ingot", o7.T("I4"))) return false; }
                            else { if (!kE(a9)) return false; }
                            break;
                        case 5:
                            if (!kA(a9, kL, "Component", o7.T("I5")
)) return false; break;
                        case 6: if (!kA(a9, kL, "OxygenContainerObject", o7.T("I6"))) return false; break;
                        case 7:
                            if (!kA(a9, true,
"GasContainerObject", "")) return false; break;
                        case 8: if (!kA(a9, kL, "AmmoMagazine", o7.T("I7"))) return false; break;
                        case 9:
                            if (!kA(a9, kL,
"PhysicalGunObject", o7.T("I8"))) return false; break;
                    }
                    a9 = false;
                }
                kQ = false; return true;
            }
        }
        class uD : v8
        {
            vj kY; public uD()
            {
                sB = 2; sx =
"CmdCargo";
            }
            public override void Init() { kY = new vj(sF, o5.u2); }
            bool kZ = true; bool k_ = false; bool l0 = false; bool l1 = false; double l2 = 0;
            double l3 = 0; int l4 = 0; public override bool RunCmd(bool aa)
            {
                if (!aa)
                {
                    kY.qU(); kZ = o4.pQ.Contains("all"); l1 = o4.pQ.EndsWith("bar"); k_ = (o4
.pQ[o4.pQ.Length - 1] == 'x'); l0 = (o4.pQ[o4.pQ.Length - 1] == 'p'); l2 = 0; l3 = 0; l4 = 0;
                }
                if (l4 == 0)
                {
                    if (kZ) { if (!kY.qK(o4.pR, aa)) return false; }
                    else
                    {
                        if (!kY.qS("cargocontainer", o4.pR, aa)) return false;
                    }
                    l4++; aa = false;
                }
                double ab = kY.qC(ref l2, ref l3, aa); if (Double.IsNaN(ab)) return
false; if (l1) { o5.uk(ab); return true; }
                o5.Add(o7.T("C2") + " "); if (!k_ && !l0)
                {
                    o5.ug(o5.ut(l2) + "L / " + o5.ut(l3) + "L"); o5.ul(ab, 1.0f, o5.tQ)
; o5.ud(' ' + o5.uv(ab) + "%");
                }
                else if (l0) { o5.ug(o5.uv(ab) + "%"); o5.uk(ab); } else o5.ug(o5.uv(ab) + "%"); return true;
            }
        }
        class uE : v8
        {
            vj l5;
            public uE() { sB = 2; sx = "CmdMass"; }
            public override void Init() { l5 = new vj(sF, o5.u2); }
            bool l6 = false; bool l7 = false; int l8 = 0; public
override bool RunCmd(bool ac)
            {
                if (!ac) { l5.qU(); l6 = (o4.pQ[o4.pQ.Length - 1] == 'x'); l7 = (o4.pQ[o4.pQ.Length - 1] == 'p'); l8 = 0; }
                if (l8 == 0)
                {
                    if (!
l5.qK(o4.pR, ac)) return false; l8++; ac = false;
                }
                double ad = l5.qF(ac); if (Double.IsNaN(ad)) return false; double ae = 0; int af = o4.pX.Count; if
(af > 0)
                {
                    double.TryParse(o4.pX[0].q4.Trim(), out ae); if (af > 1)
                    {
                        var ag = o4.pX[1].q4.Trim().ToLower(); if (ag != "") ae *= Math.Pow(1000.0,
"kmgtpezy".IndexOf(ag[0]));
                    }
                    ae *= 1000.0;
                }
                o5.Add(o7.T("M1") + " "); if (ae <= 0) { o5.ug(o5.uu(ad, false)); return true; }
                double ah = ad / ae * 100;
                if (!l6 && !l7) { o5.ug(o5.uu(ad) + " / " + o5.uu(ae)); o5.ul(ah, 1.0f, o5.tQ); o5.ud(' ' + o5.uv(ah) + "%"); }
                else if (l7)
                {
                    o5.ug(o5.uv(ah) + "%"); o5.
uk(ah);
                }
                else o5.ug(o5.uv(ah) + "%"); return true;
            }
        }
        class uF : v8
        {
            vq l9; vj la; public uF() { sB = 3; sx = "CmdOxygen"; }
            public override void
Init()
            { l9 = o5.u1; la = new vj(sF, o5.u2); }
            int lb = 0; int lc = 0; bool ld = false; int le = 0; double lg = 0; double lh = 0; double li; public override
bool RunCmd(bool aj)
            {
                if (!aj) { la.qU(); lb = 0; lc = 0; li = 0; }
                if (lb == 0)
                {
                    if (!la.qS("airvent", o4.pR, aj)) return false; ld = (la.qV() > 0); lb++; aj =
false;
                }
                if (lb == 1)
                {
                    for (; lc < la.qV(); lc++)
                    {
                        if (!sF.t3(8)) return false; IMyAirVent ak = la.qA[lc] as IMyAirVent; li = Math.Max(ak.
GetOxygenLevel() * 100, 0f); o5.Add(ak.CustomName); if (ak.CanPressurize) o5.ug(o5.uv(li) + "%"); else o5.ug(o7.T("O1")); o5.uk(li);
                    }
                    lb++; aj =
false;
                }
                if (lb == 2) { if (!aj) la.qU(); if (!la.qS("oxyfarm", o4.pR, aj)) return false; le = la.qV(); lb++; aj = false; }
                if (lb == 3)
                {
                    if (le > 0)
                    {
                        if (!aj) lc =
0; double al = 0; for (; lc < le; lc++) { if (!sF.t3(4)) return false; IMyOxygenFarm am = la.qA[lc] as IMyOxygenFarm; al += am.GetOutput() * 100; }
                        li = al /
le; if (ld) o5.ud(""); ld |= (le > 0); o5.Add(o7.T("O2")); o5.ug(o5.uv(li) + "%"); o5.uk(li);
                    }
                    lb++; aj = false;
                }
                if (lb == 4)
                {
                    if (!aj) la.qU(); if (!la.qS
("oxytank", o4.pR, aj)) return false; le = la.qV(); if (le == 0) { if (!ld) o5.ud(o7.T("O3")); return true; }
                    lb++; aj = false;
                }
                if (lb == 5)
                {
                    if (!aj)
                    {
                        lg = 0
; lh = 0; lc = 0;
                    }
                    if (!l9.s1(la.qA, "oxygen", ref lh, ref lg, aj)) return false; if (lg == 0) { if (!ld) o5.ud(o7.T("O3")); return true; }
                    li = lh / lg * 100;
                    if (ld) o5.ud(""); o5.Add(o7.T("O4")); o5.ug(o5.uv(li) + "%"); o5.uk(li); lb++;
                }
                return true;
            }
        }
        class uG : v8
        {
            vq lj; vj lk; public uG()
            {
                sB = 2; sx
= "CmdTanks";
            }
            public override void Init() { lj = o5.u1; lk = new vj(sF, o5.u2); }
            int ll = 0; string lm; string ln; double lo = 0; double lp = 0; double
lq; bool lr = false; public override bool RunCmd(bool an)
            {
                List<vh> ao = o4.pX; if (ao.Count == 0) { o5.ud(o7.T("T4")); return true; }
                if (!an)
                {
                    lm = (
o4.pQ.EndsWith("x") ? "s" : (o4.pQ.EndsWith("p") ? "p" : (o4.pQ.EndsWith("v") ? "v" : "n"))); lr = o4.pQ.EndsWith("bar"); ll = 0; ln = ao[0].q4.Trim().
ToLower(); lk.qU(); lo = 0; lp = 0;
                }
                if (ll == 0) { if (!lk.qS("oxytank", o4.pR, an)) return false; an = false; ll++; }
                if (ll == 1)
                {
                    if (!lj.s1(lk.qA, ln, ref
lo, ref lp, an)) return false; an = false; ll++;
                }
                if (lp == 0) { o5.ud(String.Format(o7.T("T5"), ln)); return true; }
                lq = lo / lp * 100; if (lr)
                {
                    o5.uk(lq)
; return true;
                }
                ln = char.ToUpper(ln[0]) + ln.Substring(1); o5.Add(ln + " " + o7.T("T6")); switch (lm)
                {
                    case "s": o5.ug(' ' + o5.uv(lq) + "%"); break;
                    case "v": o5.ug(o5.ut(lo) + "L / " + o5.ut(lp) + "L"); break;
                    case "p": o5.ug(' ' + o5.uv(lq) + "%"); o5.uk(lq); break;
                    default:
                        o5.ug(o5.ut(lo) +
"L / " + o5.ut(lp) + "L"); o5.ul(lq, 1.0f, o5.tQ); o5.ug(' ' + lq.ToString("0.0") + "%"); break;
                }
                return true;
            }
        }
        class uH : v8
        {
            public uH()
            {
                sB = 7; sx
= "CmdPowerTime";
            }
            class uI { public TimeSpan lR = new TimeSpan(-1); public double lS = -1; public double lT = 0; }
            uI ls = new uI(); vj lt; vj lu;
            public override void Init() { lt = new vj(sF, o5.u2); lu = new vj(sF, o5.u2); }
            int lv = 0; double lw = 0; double lx = 0, ly = 0; double lz = 0, lA = 0, lB = 0;
            double lC = 0, lD = 0; int lE = 0; bool lF(string ap, out TimeSpan aq, out double ar, bool at)
            {
                MyResourceSourceComponent au;
                MyResourceSinkComponent av; double aw = sA; uI ax = ls; aq = ax.lR; ar = ax.lS; if (!at)
                {
                    lt.qU(); lu.qU(); ax.lS = 0; lv = 0; lw = 0; lx = ly = 0; lz = 0; lA = lB = 0;
                    lC = lD = 0; lE = 0;
                }
                if (lv == 0) { if (!lt.qS("reactor", ap, at)) return false; at = false; lv++; }
                if (lv == 1)
                {
                    for (; lE < lt.qA.Count; lE++)
                    {
                        if (!sF.t3(6))
                            return false; IMyReactor ay = lt.qA[lE] as IMyReactor; if (ay == null || !ay.IsWorking) continue; if (ay.Components.TryGet<
                                        MyResourceSourceComponent>(out au)) { lx += au.CurrentOutputByType(o5.u1.rU); ly += au.MaxOutputByType(o5.u1.rU); }
                        lw += (double)ay.
GetInventory(0).CurrentMass;
                    }
                    at = false; lv++;
                }
                if (lv == 2) { if (!lu.qS("battery", ap, at)) return false; at = false; lv++; }
                if (lv == 3)
                {
                    if (!at) lE = 0
; for (; lE < lu.qA.Count; lE++)
                    {
                        if (!sF.t3(15)) return false; IMyBatteryBlock ay = lu.qA[lE] as IMyBatteryBlock; if (ay == null || !ay.IsWorking)
                            continue; if (ay.Components.TryGet<MyResourceSourceComponent>(out au))
                        {
                            lA = au.CurrentOutputByType(o5.u1.rU); lB = au.MaxOutputByType(o5.
u1.rU);
                        }
                        if (ay.Components.TryGet<MyResourceSinkComponent>(out av)) { lA -= av.CurrentInputByType(o5.u1.rU); }
                        double az = (lA < 0 ? (ay.
MaxStoredPower - ay.CurrentStoredPower) / (-lA / 3600) : 0); if (az > ax.lS) ax.lS = az; if (ay.OnlyRecharge) continue; lC += lA; lD += lB; lz += ay.
CurrentStoredPower;
                    }
                    at = false; lv++;
                }
                double aA = lx + lC; if (aA <= 0) ax.lR = TimeSpan.FromSeconds(-1);
                else
                {
                    double aB = ax.lR.TotalSeconds;
                    double aC; double aD = (ax.lT - lw) / aw; if (lx <= 0) aD = Math.Min(aA, ly) / 3600000; double aE = 0; if (lD > 0) aE = Math.Min(aA, lD) / 3600; if (aD <= 0 && aE <= 0)
                        aC = -1;
                    else if (aD <= 0) aC = lz / aE; else if (aE <= 0) aC = lw / aD; else { double aF = aE; double aG = (lx <= 0 ? aA / 3600 : aD * aA / lx); aC = lz / aF + lw / aG; }
                    if (aB <= 0
|| aC < 0) aB = aC;
                    else aB = (aB + aC) / 2; try { ax.lR = TimeSpan.FromSeconds(aB); } catch { ax.lR = TimeSpan.FromSeconds(-1); }
                }
                ax.lT = lw; ar = ax.lS; aq = ax.
lR; return true;
            }
            int lG = 0; bool lH = false; bool lI = false; bool lJ = false; double lK = 0; TimeSpan lL; int lM = 0, lN = 0, lO = 0; int lP = 0; int lQ = 0;
            public override bool RunCmd(bool aI)
            {
                if (!aI)
                {
                    lH = o4.pQ.EndsWith("bar"); lI = (o4.pQ[o4.pQ.Length - 1] == 'x'); lJ = (o4.pQ[o4.pQ.Length - 1] ==
'p'); lG = 0; lM = lN = lO = lP = 0; lQ = 0; lK = 0;
                }
                if (lG == 0)
                {
                    if (o4.pX.Count > 0)
                    {
                        for (; lQ < o4.pX.Count; lQ++)
                        {
                            if (!sF.t3(100)) return false; o4.pX[lQ].q6(
); if (o4.pX[lQ].q5.Count <= 0) continue; var aJ = o4.pX[lQ].q5[0]; int.TryParse(aJ, out lP); if (lQ == 0) lM = lP;
                            else if (lQ == 1) lN = lP;
                            else if (lQ ==
2) lO = lP;
                        }
                    }
                    lG++; aI = false;
                }
                if (lG == 1) { if (!lF(o4.pR, out lL, out lK, aI)) return false; lG++; aI = false; }
                if (!sF.t3(30)) return false; double aK
= 0; TimeSpan aL; try { aL = new TimeSpan(lM, lN, lO); } catch { aL = TimeSpan.FromSeconds(-1); }
                string aM; if (lL.TotalSeconds > 0 || lK <= 0)
                {
                    if (!lH) o5.
Add(o7.T("PT1") + " "); aM = o5.u1.s2(lL); aK = lL.TotalSeconds;
                }
                else
                {
                    if (!lH) o5.Add(o7.T("PT2") + " "); aM = o5.u1.s2(TimeSpan.FromSeconds(lK))
; if (aL.TotalSeconds >= lK) aK = aL.TotalSeconds - lK; else aK = 0;
                }
                if (aL.Ticks <= 0) { o5.ug(aM); return true; }
                double aN = aK / aL.TotalSeconds * 100;
                if (aN > 100) aN = 100; if (lH) { o5.uk(aN); return true; }
                if (!lI && !lJ) { o5.ug(aM); o5.ul(aN, 1.0f, o5.tQ); o5.ud(' ' + aN.ToString("0.0") + "%"); }
                else
if (lJ) { o5.ug(aN.ToString("0.0") + "%"); o5.uk(aN); } else o5.ug(aN.ToString("0.0") + "%"); return true;
            }
        }
        class uJ : v8
        {
            public uJ()
            {
                sB = 7; sx =
"CmdPowerUsed";
            }
            vq lU; vj lV; public override void Init() { lV = new vj(sF, o5.u2); lU = o5.u1; }
            string lW; string lX; string lY; void lZ(double
aO, double aP)
            {
                double aQ = (aP > 0 ? aO / aP * 100 : 0); switch (lW)
                {
                    case "s": o5.ug(aQ.ToString("0.0") + "%", 1.0f); break;
                    case "v":
                        o5.ug(o5.ut(aO) +
"W / " + o5.ut(aP) + "W", 1.0f); break;
                    case "c": o5.ug(o5.ut(aO) + "W", 1.0f); break;
                    case "p":
                        o5.ug(aQ.ToString("0.0") + "%", 1.0f); o5.uk(aQ);
                        break;
                    default: o5.ug(o5.ut(aO) + "W / " + o5.ut(aP) + "W"); o5.ul(aQ, 1.0f, o5.tQ); o5.ug(' ' + aQ.ToString("0.0") + "%"); break;
                }
            }
            double l_ = 0, m0 =
0; int m1 = 0; int m2 = 0; uK m3 = new uK(); public override bool RunCmd(bool aR)
            {
                if (!aR)
                {
                    lW = (o4.pQ.EndsWith("x") ? "s" : (o4.pQ.EndsWith(
"usedp") || o4.pQ.EndsWith("topp") ? "p" : (o4.pQ.EndsWith("v") ? "v" : (o4.pQ.EndsWith("c") ? "c" : "n")))); lX = (o4.pQ.Contains("top") ? "top" : "")
; lY = (o4.pX.Count > 0 ? o4.pX[0].q4 : o7.T("PU1")); l_ = m0 = 0; m2 = 0; m1 = 0; lV.qU(); m3.m8();
                }
                if (m2 == 0)
                {
                    if (!lV.qK(o4.pR, aR)) return false; aR = false
; m2++;
                }
                MyResourceSinkComponent aS; MyResourceSourceComponent aT; switch (lX)
                {
                    case "top":
                        if (m2 == 1)
                        {
                            for (; m1 < lV.qA.Count; m1++)
                            {
                                if (!sF.t3
(20)) return false; IMyTerminalBlock aU = lV.qA[m1]; if (aU.Components.TryGet<MyResourceSinkComponent>(out aS))
                                {
                                    ListReader<
MyDefinitionId> aV = aS.AcceptedResources; if (aV.IndexOf(lU.rU) < 0) continue; l_ = aS.CurrentInputByType(lU.rU) * 0xF4240;
                                }
                                else continue; m3.
m5(l_, aU);
                            }
                            aR = false; m2++;
                        }
                        if (m3.m6() <= 0) { o5.ud("PowerUsedTop: " + o7.T("D2")); return true; }
                        int aW = 10; if (o4.pX.Count > 0) if (!int.
TryParse(lY, out aW)) { aW = 10; }
                        if (aW > m3.m6()) aW = m3.m6(); if (m2 == 2)
                        {
                            if (!aR) { m1 = m3.m6() - 1; m3.m9(); }
                            for (; m1 >= m3.m6() - aW; m1--)
                            {
                                if (!sF.t3(
30)) return false; IMyTerminalBlock aU = m3.m7(m1); var aX = o5.uz(aU.CustomName, o5.tJ * 0.4f); if (aU.Components.TryGet<
MyResourceSinkComponent>(out aS)) { l_ = aS.CurrentInputByType(lU.rU) * 0xF4240; m0 = aS.MaxRequiredInputByType(lU.rU) * 0xF4240; }
                                o5.Add(aX +
" "); lZ(l_, m0);
                            }
                        }
                        break;
                    default:
                        for (; m1 < lV.qA.Count; m1++)
                        {
                            if (!sF.t3(10)) return false; double aY; IMyTerminalBlock aU = lV.qA[m1]; if (aU.
Components.TryGet<MyResourceSinkComponent>(out aS))
                            {
                                ListReader<MyDefinitionId> aV = aS.AcceptedResources; if (aV.IndexOf(lU.rU) < 0)
                                    continue; aY = aS.CurrentInputByType(lU.rU); m0 += aS.MaxRequiredInputByType(lU.rU);
                            }
                            else continue; if (aU.Components.TryGet<
MyResourceSourceComponent>(out aT) && (aU as IMyBatteryBlock != null)) { aY -= aT.CurrentOutputByType(lU.rU); if (aY <= 0) continue; }
                            l_ += aY;
                        }
                        o5
.Add(lY); lZ(l_ * 0xF4240, m0 * 0xF4240); break;
                }
                return true;
            }
            public class uK
            {
                List<KeyValuePair<double, IMyTerminalBlock>> m4 = new List<
KeyValuePair<double, IMyTerminalBlock>>(); public void m5(double b1, IMyTerminalBlock b2)
                {
                    m4.Add(new KeyValuePair<double,
IMyTerminalBlock>(b1, b2));
                }
                public int m6() { return m4.Count; }
                public IMyTerminalBlock m7(int b3) { return m4[b3].Value; }
                public void m8
()
                { m4.Clear(); }
                public void m9() { m4.Sort((b4, b5) => (b4.Key.CompareTo(b5.Key))); }
            }
        }
        class uL : v8
        {
            public uL() { sB = 3; sx = "CmdPower"; }
            vq ma
; vj mb; vj mc; vj md; vj me; public override void Init()
            {
                mb = new vj(sF, o5.u2); mc = new vj(sF, o5.u2); md = new vj(sF, o5.u2); me = new vj(sF, o5.
u2); ma = o5.u1;
            }
            string mf; bool mg; string mh; string mi; int mj; int mk = 0; public override bool RunCmd(bool b6)
            {
                if (!b6)
                {
                    mf = (o4.pQ.
EndsWith("x") ? "s" : (o4.pQ.EndsWith("p") ? "p" : (o4.pQ.EndsWith("v") ? "v" : "n"))); mg = (o4.pQ.StartsWith("powersummary")); mh = "a"; mi = ""; if (
o4.pQ.Contains("stored")) mh = "s";
                    else if (o4.pQ.Contains("in")) mh = "i"; else if (o4.pQ.Contains("out")) mh = "o"; mk = 0; mb.qU(); mc.qU(); md.
   qU();
                }
                if (mh == "a")
                {
                    if (mk == 0) { if (!mb.qS("reactor", o4.pR, b6)) return false; b6 = false; mk++; }
                    if (mk == 1)
                    {
                        if (!mc.qS("solarpanel", o4.pR, b6))
                            return false; b6 = false; mk++;
                    }
                }
                else if (mk == 0) mk = 2; if (mk == 2) { if (!md.qS("battery", o4.pR, b6)) return false; b6 = false; mk++; }
                int b7 = mb.qV()
; int b8 = mc.qV(); int b9 = md.qV(); if (mk == 3)
                {
                    mj = 0; if (b7 > 0) mj++; if (b8 > 0) mj++; if (b9 > 0) mj++; if (mj < 1) { o5.ud(o7.T("P6")); return true; }
                    if (o4
.pX.Count > 0) { if (o4.pX[0].q4.Length > 0) mi = o4.pX[0].q4; }
                    mk++; b6 = false;
                }
                if (mh != "a")
                {
                    if (!my(md, (mi == "" ? o7.T("P7") : mi), mh, mf, b6)) return
false; return true;
                }
                var ba = o7.T("P8"); if (!mg)
                {
                    if (mk == 4)
                    {
                        if (b7 > 0) if (!mq(mb, (mi == "" ? o7.T("P9") : mi), mf, b6)) return false; mk++; b6 = false;
                    }
                    if (mk == 5) { if (b8 > 0) if (!mq(mc, (mi == "" ? o7.T("P10") : mi), mf, b6)) return false; mk++; b6 = false; }
                    if (mk == 6)
                    {
                        if (b9 > 0) if (!my(md, (mi == "" ? o7.T(
"P7") : mi), mh, mf, b6)) return false; mk++; b6 = false;
                    }
                }
                else { ba = o7.T("P11"); mj = 10; if (mk == 4) mk = 7; }
                if (mj == 1) return true; if (!b6)
                {
                    me.qU(); me.
qT(mb); me.qT(mc); me.qT(md);
                }
                if (!mq(me, ba, mf, b6)) return false; return true;
            }
            void ml(double bb, double bc)
            {
                double bd = (bc > 0 ? bb / bc * 100 : 0
); switch (mf)
                {
                    case "s": o5.ug(' ' + bd.ToString("0.0") + "%"); break;
                    case "v": o5.ug(o5.ut(bb) + "W / " + o5.ut(bc) + "W"); break;
                    case "c":
                        o5.ug(
o5.ut(bb) + "W"); break;
                    case "p": o5.ug(' ' + bd.ToString("0.0") + "%"); o5.uk(bd); break;
                    default:
                        o5.ug(o5.ut(bb) + "W / " + o5.ut(bc) + "W"); o5.
ul(bd, 1.0f, o5.tQ); o5.ug(' ' + bd.ToString("0.0") + "%"); break;
                }
            }
            double mm = 0; double mn = 0, mo = 0; int mp = 0; bool mq(vj be, string bf, string
bg, bool bh)
            {
                if (!bh) { mn = 0; mo = 0; mp = 0; }
                if (mp == 0) { if (!ma.r_(be.qA, ma.rU, ref mm, ref mm, ref mn, ref mo, bh)) return false; mp++; bh = false; }
                if
(!sF.t3(50)) return false; double bi = (mo > 0 ? mn / mo * 100 : 0); o5.Add(bf + ": "); ml(mn * 0xF4240, mo * 0xF4240); return true;
            }
            double mr = 0, ms = 0, mt = 0
, mu = 0; double mv = 0, mw = 0; int mx = 0; bool my(vj bj, string bk, string bl, string bm, bool bn)
            {
                if (!bn) { mr = ms = 0; mt = mu = 0; mv = mw = 0; mx = 0; }
                if (mx ==
0)
                {
                    if (!ma.rY(bj.qA, ref mt, ref mu, ref mr, ref ms, ref mv, ref mw, bn)) return false; mt *= 0xF4240; mu *= 0xF4240; mr *= 0xF4240; ms *= 0xF4240; mv *=
                                       0xF4240; mw *= 0xF4240; mx++; bn = false;
                }
                double bo = (mw > 0 ? mv / mw * 100 : 0); double bp = (ms > 0 ? mr / ms * 100 : 0); double bq = (mu > 0 ? mt / mu * 100 : 0); var br =
                    bl == "a"; if (mx == 1)
                {
                    if (!sF.t3(50)) return false; if (br)
                    {
                        if (bm != "p")
                        {
                            o5.Add(bk + ": "); o5.ug("(IN " + o5.ut(mt) + "W / OUT " + o5.ut(mr) + "W)");
                        }
                        else o5.ud(bk + ": "); o5.Add("  " + o7.T("P3") + ": ");
                    }
                    else o5.Add(bk + ": "); if (br || bl == "s") switch (bm)
                        {
                            case "s":
                                o5.ug(' ' + bo.ToString(
"0.0") + "%"); break;
                            case "v": o5.ug(o5.ut(mv) + "Wh / " + o5.ut(mw) + "Wh"); break;
                            case "p":
                                o5.ug(' ' + bo.ToString("0.0") + "%"); o5.uk(bo);
                                break;
                            default: o5.ug(o5.ut(mv) + "Wh / " + o5.ut(mw) + "Wh"); o5.ul(bo, 1.0f, o5.tQ); o5.ug(' ' + bo.ToString("0.0") + "%"); break;
                        }
                    if (bl == "s")
                        return true; mx++; bn = false;
                }
                if (mx == 2)
                {
                    if (!sF.t3(50)) return false; if (br) o5.Add("  " + o7.T("P4") + ": "); if (br || bl == "o") switch (bm)
                        {
                            case
"s":
                                o5.ug(' ' + bp.ToString("0.0") + "%"); break;
                            case "v": o5.ug(o5.ut(mr) + "W / " + o5.ut(ms) + "W"); break;
                            case "p":
                                o5.ug(' ' + bp.ToString(
"0.0") + "%"); o5.uk(bp); break;
                            default:
                                o5.ug(o5.ut(mr) + "W / " + o5.ut(ms) + "W"); o5.ul(bp, 1.0f, o5.tQ); o5.ug(' ' + bp.ToString("0.0") + "%");
                                break;
                        }
                    if (bl == "o") return true; mx++; bn = false;
                }
                if (!sF.t3(50)) return false; if (br) o5.Add("  " + o7.T("P5") + ": "); if (br || bl == "i") switch (
bm)
                    {
                        case "s": o5.ug(' ' + bq.ToString("0.0") + "%"); break;
                        case "v": o5.ug(o5.ut(mt) + "W / " + o5.ut(mu) + "W"); break;
                        case "p":
                            o5.ug(' ' + bq.
ToString("0.0") + "%"); o5.uk(bq); break;
                        default:
                            o5.ug(o5.ut(mt) + "W / " + o5.ut(mu) + "W"); o5.ul(bq, 1.0f, o5.tQ); o5.ug(' ' + bq.ToString(
"0.0") + "%"); break;
                    }
                return true;
            }
        }
        class uM : v8
        {
            public uM() { sB = 0.5; sx = "CmdSpeed"; }
            public override bool RunCmd(bool bs)
            {
                double bt = 0;
                double bu = 1; var bv = "m/s"; if (o4.pQ.Contains("kmh")) { bu = 3.6; bv = "km/h"; } else if (o4.pQ.Contains("mph")) { bu = 2.23694; bv = "mph"; }
                if (o4.pR
!= "") double.TryParse(o4.pR.Trim(), out bt); o5.Add(o7.T("S1") + " "); o5.ug((o5.u3.qb * bu).ToString("F1") + " " + bv + " "); if (bt > 0) o5.uk(o5.
u3.qb / bt * 100); return true;
            }
        }
        class uN : v8
        {
            public uN() { sB = 0.5; sx = "CmdAccel"; }
            public override bool RunCmd(bool bw)
            {
                double bx = 0; if (o4.
pR != "") double.TryParse(o4.pR.Trim(), out bx); o5.Add(o7.T("AC1") + " "); o5.ug(o5.u3.qd.ToString("F1") + " m/s²"); if (bx > 0)
                {
                    double by = o5.
u3.qd / bx * 100; o5.uk(by);
                }
                return true;
            }
        }
        class uO : v8
        {
            public uO() { sB = 30; sx = "CmdEcho"; }
            public override bool RunCmd(bool bz)
            {
                var bA = (o4
.pQ == "center" ? "c" : (o4.pQ == "right" ? "r" : "n")); switch (bA)
                {
                    case "c": o5.ui(o4.pT); break;
                    case "r": o5.ug(o4.pT); break;
                    default:
                        o5.ud(o4.pT
); break;
                }
                return true;
            }
        }
        class uP : v8
        {
            public uP() { sB = 3; sx = "CmdCharge"; }
            vj mz; public override void Init() { mz = new vj(sF, o5.u2); }
            int mA
= 0; int mB = 0; bool mC = false; bool mD = false; bool mE = false; Dictionary<long, double> mF = new Dictionary<long, double>(); Dictionary<long,
double> mG = new Dictionary<long, double>(); Dictionary<long, double> mH = new Dictionary<long, double>(); Dictionary<long, double> mI = new
Dictionary<long, double>(); Dictionary<long, double> mJ = new Dictionary<long, double>(); double mK(long bB, double bC, double bD)
            {
                double bE
= 0; double bF = 0; double bG = 0; double bH = 0; if (mG.TryGetValue(bB, out bG)) { bH = mI[bB]; }
                if (mF.TryGetValue(bB, out bE)) { bF = mH[bB]; }
                double bI
= (sF.sN - bG); double bJ = 0; if (bI > 0) bJ = (bC - bH) / bI; if (bJ < 0) { if (!mJ.TryGetValue(bB, out bJ)) bJ = 0; } else mJ[bB] = bJ; if (bE > 0)
                {
                    mG[bB] = mF[bB];
                    mI[bB] = mH[bB];
                }
                mF[bB] = sF.sN; mH[bB] = bC; return (bJ > 0 ? (bD - bC) / bJ : 0);
            }
            public override bool RunCmd(bool bK)
            {
                if (!bK)
                {
                    mz.qU(); mE = o4.pQ.
EndsWith("bar"); mC = o4.pQ.Contains("x"); mD = o4.pQ.Contains("time"); mB = 0; mA = 0;
                }
                if (mA == 0)
                {
                    if (!mz.qS("jumpdrive", o4.pR, bK)) return false
; if (mz.qV() <= 0) { o5.ud("Charge: " + o7.T("D2")); return true; }
                    mA++; bK = false;
                }
                for (; mB < mz.qV(); mB++)
                {
                    if (!sF.t3(25)) return false;
                    IMyJumpDrive bL = mz.qA[mB] as IMyJumpDrive; double bM, bN, bO; bO = o5.u2.r8(bL, out bM, out bN); if (mE) { o5.uk(bO); }
                    else
                    {
                        o5.Add(bL.CustomName
+ " "); if (mD)
                        {
                            TimeSpan bP = TimeSpan.FromSeconds(mK(bL.EntityId, bM, bN)); o5.ug(o5.u1.s2(bP)); if (!mC)
                            {
                                o5.ul(bO, 1.0f, o5.tQ); o5.ug(' ' + bO
.ToString("0.0") + "%");
                            }
                        }
                        else
                        {
                            if (!mC) { o5.ug(o5.ut(bM) + "Wh / " + o5.ut(bN) + "Wh"); o5.ul(bO, 1.0f, o5.tQ); }
                            o5.ug(' ' + bO.ToString("0.0") +
"%");
                        }
                    }
                }
                return true;
            }
        }
        class uQ : v8
        {
            public uQ() { sB = 1; sx = "CmdDateTime"; }
            public override bool RunCmd(bool bQ)
            {
                var bR = (o4.pQ.
StartsWith("datetime")); var bS = (o4.pQ.StartsWith("date")); var bT = o4.pQ.Contains("c"); int bU = o4.pQ.IndexOf('+'); if (bU < 0) bU = o4.pQ.
IndexOf('-'); float bV = 0; if (bU >= 0) float.TryParse(o4.pQ.Substring(bU), out bV); DateTime bW = DateTime.Now.AddHours(bV); var bX = ""; int bY
= o4.pS.IndexOf(' '); if (bY >= 0) bX = o4.pS.Substring(bY + 1); if (!bR) { if (!bS) bX += bW.ToShortTimeString(); else bX += bW.ToShortDateString(); }
                else
                {
                    if (bX == "") bX = String.Format("{0:d} {0:t}", bW);
                    else
                    {
                        bX = bX.Replace("/", "\\/"); bX = bX.Replace(":", "\\:"); bX = bX.Replace("\"", "\\\""
); bX = bX.Replace("'", "\\'"); bX = bW.ToString(bX + ' '); bX = bX.Substring(0, bX.Length - 1);
                    }
                }
                if (bT) o5.ui(bX); else o5.ud(bX); return true;
            }
        }
        class uR : v8
        {
            public uR() { sB = 1; sx = "CmdCountdown"; }
            public override bool RunCmd(bool bZ)
            {
                var b_ = o4.pQ.EndsWith("c"); var c0 = o4.pQ.
EndsWith("r"); var c1 = ""; int c2 = o4.pS.IndexOf(' '); if (c2 >= 0) c1 = o4.pS.Substring(c2 + 1).Trim(); DateTime c3 = DateTime.Now; DateTime c4; if
(!DateTime.TryParseExact(c1, "H:mm d.M.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.
None, out c4)) { o5.ud(o7.T("C3")); o5.ud("  Countdown 19:02 28.2.2015"); return true; }
                TimeSpan c5 = c4 - c3; var c6 = ""; if (c5.Ticks <= 0) c6 = o7
.T("C4");
                else
                {
                    if ((int)c5.TotalDays > 0) c6 += (int)c5.TotalDays + " " + o7.T("C5") + " "; if (c5.Hours > 0 || c6 != "") c6 += c5.Hours + "h "; if (c5.
                                Minutes > 0 || c6 != "") c6 += c5.Minutes + "m "; c6 += c5.Seconds + "s";
                }
                if (b_) o5.ui(c6); else if (c0) o5.ug(c6); else o5.ud(c6); return true;
            }
        }
        class
uS : v8
        {
            public uS() { sB = 1; sx = "CmdTextLCD"; }
            public override bool RunCmd(bool c7)
            {
                IMyTextPanel c8 = o6.oq.s6; if (c8 == null) return true; var
c9 = ""; if (o4.pR != "" && o4.pR != "*")
                {
                    IMyTextPanel ca = o5.u5.GetBlockWithName(o4.pR) as IMyTextPanel; if (ca == null)
                    {
                        o5.ud("TextLCD: " + o7.T(
"T1") + o4.pR); return true;
                    }
                    c9 = ca.GetPublicText();
                }
                else { o5.ud("TextLCD:" + o7.T("T2")); return true; }
                if (c9.Length == 0) return true; o5.ue(
c9); return true;
            }
        }
        class uT : v8
        {
            public uT() { sB = 15; sx = "CmdBlockCount"; }
            vj mL; public override void Init() { mL = new vj(sF, o5.u2); }
            bool
mM; bool mN; int mO = 0; int mP = 0; public override bool RunCmd(bool cb)
            {
                if (!cb)
                {
                    mM = (o4.pQ == "enabledcount"); mN = (o4.pQ == "prodcount"); mO = 0;
                    mP = 0;
                }
                if (o4.pX.Count == 0)
                {
                    if (mP == 0) { if (!cb) mL.qU(); if (!mL.qK(o4.pR, cb)) return false; mP++; cb = false; }
                    if (!mZ(mL, "blocks", mM, mN, cb))
                        return false; return true;
                }
                for (; mO < o4.pX.Count; mO++) { vh cc = o4.pX[mO]; if (!cb) cc.q6(); if (!mS(cc, cb)) return false; cb = false; }
                return
true;
            }
            int mQ = 0; int mR = 0; bool mS(vh cd, bool ce)
            {
                if (!ce) { mQ = 0; mR = 0; }
                for (; mQ < cd.q5.Count; mQ++)
                {
                    if (mR == 0)
                    {
                        if (!ce) mL.qU(); if (!mL.qS(cd.
q5[mQ], o4.pR, ce)) return false; mR++; ce = false;
                    }
                    if (!mZ(mL, cd.q5[mQ], mM, mN, ce)) return false; mR = 0; ce = false;
                }
                return true;
            }
            Dictionary<
string, int> mT = new Dictionary<string, int>(); Dictionary<string, int> mU = new Dictionary<string, int>(); List<string> mV = new List<string>()
; int mW = 0; int mX = 0; int mY = 0; bool mZ(vj cf, string cg, bool ch, bool ci, bool cj)
            {
                string ck; if (cf.qV() == 0)
                {
                    ck = cg.ToLower(); ck = char.
ToUpper(ck[0]) + ck.Substring(1).ToLower(); o5.Add(ck + " " + o7.T("C1") + " "); var cl = (ch || ci ? "0 / 0" : "0"); o5.ug(cl); return true;
                }
                if (!cj)
                {
                    mT.Clear(); mU.Clear(); mV.Clear(); mW = 0; mX = 0; mY = 0;
                }
                if (mY == 0)
                {
                    for (; mW < cf.qV(); mW++)
                    {
                        if (!sF.t3(15)) return false; IMyProductionBlock cm =
cf.qA[mW] as IMyProductionBlock; ck = cf.qA[mW].DefinitionDisplayNameText; if (mV.Contains(ck))
                        {
                            mT[ck]++; if ((ch && cf.qA[mW].IsWorking) || (
ci && cm != null && cm.IsProducing)) mU[ck]++;
                        }
                        else
                        {
                            mT.Add(ck, 1); mV.Add(ck); if (ch || ci) if ((ch && cf.qA[mW].IsWorking) || (ci && cm != null && cm.
IsProducing)) mU.Add(ck, 1);
                                else mU.Add(ck, 0);
                        }
                    }
                    mY++; cj = false;
                }
                for (; mX < mT.Count; mX++)
                {
                    if (!sF.t3(8)) return false; o5.Add(mV[mX] + " " + o7
.T("C1") + " "); var cl = (ch || ci ? mU[mV[mX]] + " / " : "") + mT[mV[mX]]; o5.ug(cl);
                }
                return true;
            }
        }
        class uU : v8
        {
            public uU()
            {
                sB = 5; sx =
"CmdShipCtrl";
            }
            vj m_; public override void Init() { m_ = new vj(sF, o5.u2); }
            public override bool RunCmd(bool co)
            {
                if (!co) m_.qU(); if (!m_.
qS("shipctrl", o4.pR, co)) return false; if (m_.qV() <= 0)
                {
                    if (o4.pR != "" && o4.pR != "*") o5.ud(o4.pQ + ": " + o7.T("SC1") + " (" + o4.pR + ")");
                    else o5.
ud(o4.pQ + ": " + o7.T("SC1")); return true;
                }
                if (o4.pQ.StartsWith("damp"))
                {
                    var cp = (m_.qA[0] as IMyShipController).DampenersOverride; o5.
Add(o7.T("SCD")); o5.ug(cp ? "ON" : "OFF");
                }
                else
                {
                    var cp = (m_.qA[0] as IMyShipController).IsUnderControl; o5.Add(o7.T("SCO")); o5.ug(cp ?
"YES" : "NO");
                }
                return true;
            }
        }
        class uV : v8
        {
            public uV() { sB = 5; sx = "CmdWorking"; }
            vj n0; public override void Init() { n0 = new vj(sF, o5.u2); }
            int n1 = 0; int n2 = 0; bool n3; public override bool RunCmd(bool cr)
            {
                if (!cr) { n1 = 0; n3 = (o4.pQ == "workingx"); n2 = 0; }
                if (o4.pX.Count == 0)
                {
                    if (n1
== 0) { if (!cr) n0.qU(); if (!n0.qK(o4.pR, cr)) return false; n1++; cr = false; }
                    if (!nc(n0, n3, "", cr)) return false; return true;
                }
                for (; n2 < o4.pX.
Count; n2++) { vh cs = o4.pX[n2]; if (!cr) cs.q6(); if (!n9(cs, cr)) return false; cr = false; }
                return true;
            }
            int n4 = 0; int n5 = 0; string[] n6; string
n7; string n8; bool n9(vh cu, bool cv)
            {
                if (!cv) { n4 = 0; n5 = 0; }
                for (; n5 < cu.q5.Count; n5++)
                {
                    if (n4 == 0)
                    {
                        if (!cv)
                        {
                            if (cu.q5[n5] == "") continue; n0.qU
(); n6 = cu.q5[n5].ToLower().Split(':'); n7 = n6[0]; n8 = (n6.Length > 1 ? n6[1] : "");
                        }
                        if (n7 != "") { if (!n0.qS(n7, o4.pR, cv)) return false; }
                        else
                        {
                            if (!
n0.qK(o4.pR, cv)) return false;
                        }
                        n4++; cv = false;
                    }
                    if (!nc(n0, n3, n8, cv)) return false; n4 = 0; cv = false;
                }
                return true;
            }
            string na(
IMyTerminalBlock cw)
            {
                vk cx = o5.u2; if (!cw.IsWorking) return o7.T("W1"); IMyProductionBlock cy = cw as IMyProductionBlock; if (cy != null) if (
        cy.IsProducing) return o7.T("W2");
                    else return o7.T("W3"); IMyAirVent cz = cw as IMyAirVent; if (cz != null)
                {
                    if (cz.CanPressurize) return (cz.
GetOxygenLevel() * 100).ToString("F1") + "%";
                    else return o7.T("W4");
                }
                IMyGasTank cA = cw as IMyGasTank; if (cA != null) return (cA.FilledRatio *
100).ToString("F1") + "%"; IMyBatteryBlock cB = cw as IMyBatteryBlock; if (cB != null) return cx.r6(cB); IMyJumpDrive cC = cw as IMyJumpDrive;
                if (cC != null) return cx.r9(cC).ToString("0.0") + "%"; IMyLandingGear cD = cw as IMyLandingGear; if (cD != null)
                {
                    switch ((int)cD.LockMode)
                    {
                        case
0:
                            return o7.T("W8");
                        case 1: return o7.T("W10");
                        case 2: return o7.T("W7");
                    }
                }
                IMyDoor cE = cw as IMyDoor; if (cE != null)
                {
                    if (cE.Status ==
DoorStatus.Open) return o7.T("W5"); return o7.T("W6");
                }
                IMyShipConnector cF = cw as IMyShipConnector; if (cF != null)
                {
                    if (cF.Status ==
MyShipConnectorStatus.Unconnected) return o7.T("W8"); if (cF.Status == MyShipConnectorStatus.Connected) return o7.T("W7");
                    else return o7
.T("W10");
                }
                IMyLaserAntenna cG = cw as IMyLaserAntenna; if (cG != null) return cx.r7(cG); IMyRadioAntenna cH = cw as IMyRadioAntenna; if (cH !=
                null) return o5.ut(cH.Radius) + "m"; IMyBeacon cI = cw as IMyBeacon; if (cI != null) return o5.ut(cI.Radius) + "m"; IMyThrust cJ = cw as IMyThrust
                              ; if (cJ != null && cJ.ThrustOverride > 0) return o5.ut(cJ.ThrustOverride) + "N"; return o7.T("W9");
            }
            int nb = 0; bool nc(vj cK, bool cL, string cM,
bool cN)
            {
                if (!cN) nb = 0; for (; nb < cK.qV(); nb++)
                {
                    if (!sF.t3(20)) return false; IMyTerminalBlock cO = cK.qA[nb]; var cP = (cL ? (cO.IsWorking ? o7.T(
"W9") : o7.T("W1")) : na(cO)); if (cM != "" && cP.ToLower() != cM) continue; if (cL) cP = na(cO); var cQ = cO.CustomName; cQ = o5.uz(cQ, o5.tJ * 0.7f); o5.Add
           (cQ); o5.ug(cP);
                }
                return true;
            }
        }
        class uW : v8
        {
            public uW() { sB = 5; sx = "CmdDamage"; }
            vj nd; public override void Init()
            {
                nd = new vj(sF, o5.u2);
            }
            bool ne = false; int nf = 0; public override bool RunCmd(bool cR)
            {
                var cS = o4.pQ.StartsWith("damagex"); var cT = o4.pQ.EndsWith("noc"); var
cU = (!cT && o4.pQ.EndsWith("c")); float cV = 100; if (!cR) { nd.qU(); ne = false; nf = 0; }
                if (!nd.qK(o4.pR, cR)) return false; if (o4.pX.Count > 0)
                {
                    if (!
float.TryParse(o4.pX[0].q4, out cV)) cV = 100;
                }
                cV -= 0.00001f; for (; nf < nd.qV(); nf++)
                {
                    if (!sF.t3(30)) return false; IMyTerminalBlock cW = nd.qA
[nf]; IMySlimBlock cX = cW.CubeGrid.GetCubeBlock(cW.Position); if (cX == null) continue; float cY = (cT ? cX.MaxIntegrity : cX.BuildIntegrity); if
(!cU) cY -= cX.CurrentDamage; float cZ = 100 * (cY / cX.MaxIntegrity); if (cZ >= cV) continue; ne = true; var c_ = o5.uz(cX.FatBlock.DisplayNameText, o5
.tJ * 0.69f - o5.tQ); o5.Add(c_ + ' '); if (!cS) { o5.uh(o5.ut(cY) + " / ", 0.69f); o5.Add(o5.ut(cX.MaxIntegrity)); }
                    o5.ug(' ' + cZ.ToString("0.0") +
'%'); o5.uk(cZ);
                }
                if (!ne) o5.ud(o7.T("D3")); return true;
            }
        }
        class uX : v8
        {
            public uX() { sB = 2; sx = "CmdAmount"; }
            vj ng; public override void
Init()
            { ng = new vj(sF, o5.u2); }
            bool nh; bool ni = false; int nj = 0; int nk = 0; int nl = 0; public override bool RunCmd(bool d0)
            {
                if (!d0)
                {
                    nh = !o4.
pQ.EndsWith("x"); ni = o4.pQ.EndsWith("bar"); if (ni) nh = true; if (o4.pX.Count == 0) o4.pX.Add(new vh(
"reactor,gatlingturret,missileturret,interiorturret,gatlinggun,launcherreload,launcher,oxygenerator")); nk = 0;
                }
                for (; nk < o4.pX.Count;
nk++)
                {
                    vh d1 = o4.pX[nk]; if (!d0) { d1.q6(); nj = 0; nl = 0; }
                    for (; nl < d1.q5.Count; nl++)
                    {
                        if (nj == 0)
                        {
                            if (!d0) { if (d1.q5[nl] == "") continue; ng.qU(); }
                            var d2 = d1.q5[nl]; if (!ng.qS(d2, o4.pR, d0)) return false; nj++; d0 = false;
                        }
                        if (!nw(d0)) return false; d0 = false; nj = 0;
                    }
                }
                return true;
            }
            int nm = 0;
            int nn = 0; double no = 0; double np = 0; double nq = 0; int nr = 0; IMyTerminalBlock ns; IMyInventory nt; List<IMyInventoryItem> nu; string nv = "";
            bool nw(bool d3)
            {
                if (!d3) { nm = 0; nn = 0; }
                for (; nm < ng.qV(); nm++)
                {
                    if (nn == 0)
                    {
                        if (!sF.t3(50)) return false; ns = ng.qA[nm]; nt = ns.GetInventory(0);
                        if (nt == null) continue; nn++; d3 = false;
                    }
                    if (!d3) { nu = nt.GetItems(); nv = (nu.Count > 0 ? nu[0].Content.ToString() : ""); nr = 0; no = 0; np = 0; nq = 0; }
                    for (
; nr < nu.Count; nr++)
                    {
                        if (!sF.t3(30)) return false; IMyInventoryItem d4 = nu[nr]; if (d4.Content.ToString() != nv) nq += (double)d4.Amount;
                        else
                            no += (double)d4.Amount;
                    }
                    var d5 = o7.T("A1"); var d6 = ns.CustomName; if (no > 0 && (double)nt.CurrentVolume > 0)
                    {
                        double d7 = nq * (double)nt.
CurrentVolume / (no + nq); np = Math.Floor(no * ((double)nt.MaxVolume - d7) / (double)nt.CurrentVolume - d7); d5 = o5.ut(no) + " / " + (nq > 0 ? "~" : "") + o5.
ut(np);
                    }
                    if (!ni || np <= 0) { d6 = o5.uz(d6, o5.tJ * 0.8f); o5.Add(d6); o5.ug(d5); }
                    if (nh && np > 0) { double d8 = 100 * no / np; o5.uk(d8); }
                    nn = 0; d3 = false;
                }
                return true;
            }
        }
        class uY : v8
        {
            public uY() { sB = 1; sx = "CmdPosition"; }
            public override bool RunCmd(bool d9)
            {
                var da = (o4.pQ == "posxyz"); var db
= (o4.pQ == "posgps"); IMyTerminalBlock dc = o6.oq.s6; if (o4.pR != "" && o4.pR != "*")
                {
                    dc = o5.u5.GetBlockWithName(o4.pR); if (dc == null)
                    {
                        o5.ud(
"Pos: " + o7.T("P1") + ": " + o4.pR); return true;
                    }
                }
                if (db)
                {
                    VRageMath.Vector3D dd = dc.GetPosition(); o5.ud("GPS:" + o7.T("P2") + ":" + dd.GetDim(0
).ToString("F2") + ":" + dd.GetDim(1).ToString("F2") + ":" + dd.GetDim(2).ToString("F2") + ":"); return true;
                }
                o5.Add(o7.T("P2") + ": "); if (!da)
                { o5.ug(dc.GetPosition().ToString("F0")); return true; }
                o5.ud(""); o5.Add(" X: "); o5.ug(dc.GetPosition().GetDim(0).ToString("F0")); o5.
Add(" Y: "); o5.ug(dc.GetPosition().GetDim(1).ToString("F0")); o5.Add(" Z: "); o5.ug(dc.GetPosition().GetDim(2).ToString("F0"));
                return true;
            }
        }
        class uZ : v8
        {
            public uZ() { sB = 5; sx = "CmdDetails"; }
            string nx = ""; vj ny; public override void Init()
            {
                ny = new vj(sF, o5.u2); if
(o4.pX.Count > 0) nx = o4.pX[0].q4.Trim();
            }
            int nz = 0; int nA = 1; bool nB = false; IMyTerminalBlock nC; public override bool RunCmd(bool de)
            {
                if (
o4.pR == "" || o4.pR == "*") { o5.ud("Details: " + o7.T("D1")); return true; }
                if (!de) { ny.qU(); nB = o4.pQ.Contains("non"); nz = 0; nA = 1; }
                if (nz == 0)
                {
                    if
(!ny.qK(o4.pR, de)) return true; if (ny.qV() <= 0) { o5.ud("Details: " + o7.T("D2")); return true; }
                    nz++; de = false;
                }
                int df = (o4.pQ.EndsWith("x")
? 1 : 0); if (nz == 1) { if (!de) { nC = ny.qA[0]; if (!nB) o5.ud(nC.CustomName); } if (!nG(nC, df, de)) return false; nz++; de = false; }
                for (; nA < ny.qV(); nA++
) { if (!de) { nC = ny.qA[nA]; if (!nB) { o5.ud(""); o5.ud(nC.CustomName); } } if (!nG(nC, df, de)) return false; de = false; }
                return true;
            }
            string[] nD;
            int nE = 0; bool nF = false; bool nG(IMyTerminalBlock dg, int dh, bool di)
            {
                if (!di)
                {
                    nD = (dg.DetailedInfo + "\n" + dg.CustomInfo).Split('\n'); nE =
dh; nF = (nx == "");
                }
                for (; nE < nD.Length; nE++)
                {
                    if (!sF.t3(5)) return false; if (nD[nE] == "") continue; if (!nF)
                    {
                        if (!nD[nE].Contains(nx)) continue;
                        nF = true;
                    }
                    o5.ud("  " + nD[nE]);
                }
                return true;
            }
        }
        class u_ : v8
        {
            public u_() { sB = 1; sx = "CmdShipMass"; }
            public override bool RunCmd(bool dj)
            {
                var dk = o4.pQ.EndsWith("base"); double dl = 0; if (o4.pR != "") double.TryParse(o4.pR.Trim(), out dl); int dm = o4.pX.Count; if (dm > 0)
                {
                    var dn = o4.
pX[0].q4.Trim().ToLower(); if (dn != "") dl *= Math.Pow(1000.0, "kmgtpezy".IndexOf(dn[0]));
                }
                double dp = (dk ? o5.u3.ql : o5.u3.qk); if (!dk) o5.Add
(o7.T("SM1") + " ");
                else o5.Add(o7.T("SM2") + " "); o5.ug(o5.uu(dp, true, 'k') + " "); if (dl > 0) o5.uk(dp / dl * 100); return true;
            }
        }
        class v0 : v8
        {
            public v0() { sB = 1; sx = "CmdDistance"; }
            string nH = ""; string[] nI; Vector3D nJ; string nK = ""; bool nL = false; public override void Init()
            {
                nL =
false; if (o4.pX.Count <= 0) return; nH = o4.pX[0].q4.Trim(); nI = nH.Split(':'); if (nI.Length < 5 || nI[0] != "GPS") return; double dq, dr, ds; if (!
double.TryParse(nI[2], out dq)) return; if (!double.TryParse(nI[3], out dr)) return; if (!double.TryParse(nI[4], out ds)) return; nJ = new
Vector3D(dq, dr, ds); nK = nI[1]; nL = true;
            }
            public override bool RunCmd(bool dt)
            {
                if (!nL)
                {
                    o5.ud("Distance: " + o7.T("DTU") + " '" + nH + "'.");
                    return true;
                }
                IMyTerminalBlock du = o6.oq.s6; if (o4.pR != "" && o4.pR != "*")
                {
                    du = o5.u5.GetBlockWithName(o4.pR); if (du == null)
                    {
                        o5.ud(
"Distance: " + o7.T("P1") + ": " + o4.pR); return true;
                    }
                }
                double dv = Vector3D.Distance(du.GetPosition(), nJ); o5.Add(nK + ": "); o5.ug(o5.ut(dv)
+ "m "); return true;
            }
        }
        class v1 : v8
        {
            public v1() { sB = 1; sx = "CmdAltitude"; }
            public override bool RunCmd(bool dw)
            {
                var dx = (o4.pQ.EndsWith(
"sea") ? "sea" : "ground"); switch (dx)
                {
                    case "sea": o5.Add(o7.T("ALT1")); o5.ug(o5.u3.qn.ToString("F0") + " m"); break;
                    default:
                        o5.Add(o7.T(
"ALT2")); o5.ug(o5.u3.qp.ToString("F0") + " m"); break;
                }
                return true;
            }
        }
        class v2 : v8
        {
            public v2() { sB = 1; sx = "CmdStopTask"; }
            public override
bool RunCmd(bool dy)
            {
                double dz = 0; if (o4.pQ.Contains("best")) dz = o5.u3.qb / o5.u3.qf; else dz = o5.u3.qb / o5.u3.qi; double dA = o5.u3.qb / 2 * dz;
                if (o4.pQ.Contains("time"))
                {
                    o5.Add(o7.T("ST")); if (double.IsNaN(dz)) { o5.ug("N/A"); return true; }
                    var dB = ""; try
                    {
                        TimeSpan dC = TimeSpan.
FromSeconds(dz); if ((int)dC.TotalDays > 0) dB = " > 24h";
                        else
                        {
                            if (dC.Hours > 0) dB = dC.Hours + "h "; if (dC.Minutes > 0 || dB != "") dB += dC.Minutes + "m "
; dB += dC.Seconds + "s";
                        }
                    }
                    catch { dB = "N/A"; }
                    o5.ug(dB); return true;
                }
                o5.Add(o7.T("SD")); if (!double.IsNaN(dA) && !double.IsInfinity(dA)) o5.ug
(o5.ut(dA) + "m ");
                else o5.ug("N/A"); return true;
            }
        }
        class v3 : v8
        {
            public v3() { sB = 1; sx = "CmdGravity"; }
            public override bool RunCmd(bool
dD)
            {
                var dE = (o4.pQ.Contains("nat") ? "n" : (o4.pQ.Contains("art") ? "a" : (o4.pQ.Contains("tot") ? "t" : "s"))); Vector3D dF; if (o5.u3.qs == null)
                {
                    o5.ud("Gravity: " + o7.T("GNC")); return true;
                }
                switch (dE)
                {
                    case "n":
                        o5.Add(o7.T("G2") + " "); dF = o5.u3.qs.GetNaturalGravity(); o5.ug(dF.
Length().ToString("F1") + " m/s²"); break;
                    case "a":
                        o5.Add(o7.T("G3") + " "); dF = o5.u3.qs.GetArtificialGravity(); o5.ug(dF.Length().
ToString("F1") + " m/s²"); break;
                    case "t":
                        o5.Add(o7.T("G1") + " "); dF = o5.u3.qs.GetTotalGravity(); o5.ug(dF.Length().ToString("F1") +
" m/s²"); break;
                    default:
                        o5.Add(o7.T("GN")); o5.uh(" | ", 0.33f); o5.uh(o7.T("GA") + " | ", 0.66f); o5.ug(o7.T("GT"), 1.0f); o5.Add(""); dF = o5
           .u3.qs.GetNaturalGravity(); o5.uh(dF.Length().ToString("F1") + " | ", 0.33f); dF = o5.u3.qs.GetArtificialGravity(); o5.uh(dF.Length().
                   ToString("F1") + " | ", 0.66f); dF = o5.u3.qs.GetTotalGravity(); o5.ug(dF.Length().ToString("F1") + " "); break;
                }
                return true;
            }
        }
        class v4 : v8
        {
            public v4() { sB = 1; sx = "CmdCustomData"; }
            public override bool RunCmd(bool dG)
            {
                IMyTextPanel dH = o6.oq.s6; if (dH == null) return true; var dI =
""; if (o4.pR != "" && o4.pR != "*")
                {
                    IMyTerminalBlock dJ = o5.u5.GetBlockWithName(o4.pR) as IMyTerminalBlock; if (dJ == null)
                    {
                        o5.ud(
"CustomData: " + o7.T("CD1") + o4.pR); return true;
                    }
                    dI = dJ.CustomData;
                }
                else { o5.ud("CustomData:" + o7.T("CD2")); return true; }
                if (dI.Length ==
0) return true; o5.ue(dI); return true;
            }
        }
        class v5 : v8
        {
            vj nM; public v5() { sB = 1; sx = "CmdProp"; }
            public override void Init()
            {
                nM = new vj(sF,
o5.u2);
            }
            int nN = 0; int nO = 0; bool nP = false; string nQ = null; string nR = null; string nS = null; string nT = null; public override bool RunCmd(
                         bool dK)
            {
                if (!dK) { nP = o4.pQ.StartsWith("props"); nQ = nR = nS = nT = null; nO = 0; nN = 0; }
                if (o4.pX.Count < 1)
                {
                    o5.ud(o4.pQ + ": " +
"Missing property name."); return true;
                }
                if (nN == 0) { if (!dK) nM.qU(); if (!nM.qK(o4.pR, dK)) return false; nU(); nN++; dK = false; }
                if (nN == 1)
                {
                    int
dL = nM.qV(); if (dL == 0) { o5.ud(o4.pQ + ": " + "No blocks found."); return true; }
                    for (; nO < dL; nO++)
                    {
                        if (!sF.t3(50)) return false;
                        IMyTerminalBlock dM = nM.qA[nO]; if (dM.GetProperty(nQ) != null)
                        {
                            if (nR == null) { var dN = o5.uz(dM.CustomName, o5.tJ * 0.7f); o5.Add(dN); }
                            else o5
.Add(nR); o5.ug(nV(dM, nQ, nS, nT)); if (!nP) return true;
                        }
                    }
                }
                return true;
            }
            void nU()
            {
                nQ = o4.pX[0].q4; if (o4.pX.Count > 1)
                {
                    if (!nP) nR = o4.pX[1].
q4;
                    else nS = o4.pX[1].q4; if (o4.pX.Count > 2) { if (!nP) nS = o4.pX[2].q4; else nT = o4.pX[2].q4; if (o4.pX.Count > 3 && !nP) nT = o4.pX[3].q4; }
                }
            }
            string
nV(IMyTerminalBlock dO, string dP, string dQ = null, string dR = null)
            {
                return (dO.GetValue<bool>(dP) ? (dQ != null ? dQ : o7.T("W9")) : (dR != null ? dR
: o7.T("W1")));
            }
        }
        class v6 : v8
        {
            public v6() { sB = 0.5; sx = "CmdHScroll"; }
            StringBuilder nW = new StringBuilder(); int nX = 1; public override
bool RunCmd(bool dS)
            {
                if (nW.Length == 0)
                {
                    var dT = o4.pT + "  "; if (dT.Length == 0) return true; float dU = o5.tJ; float dV = o5.uy(dT, o5.u9); float
dW = dU / dV; if (dW > 1) nW.Append(string.Join("", Enumerable.Repeat(dT, (int)Math.Ceiling(dW)))); else nW.Append(dT); if (dT.Length > 40) nX = 3;
                    else if (dT.Length > 5) nX = 2; else nX = 1; o5.ud(nW.ToString()); return true;
                }
                var dX = o4.pQ.EndsWith("r"); if (dX)
                {
                    nW.Insert(0, nW.ToString(nW.
Length - nX, nX)); nW.Remove(nW.Length - nX, nX);
                }
                else { nW.Append(nW.ToString(0, nX)); nW.Remove(0, nX); }
                o5.ud(nW.ToString()); return true;
            }
        }
        class v7 : v8
        {
            vj nY; public v7() { sB = 2; sx = "CmdDocked"; }
            public override void Init() { nY = new vj(sF, o5.u2); }
            int nZ = 0; int n_ = 0; bool o0 =
false; bool o1 = false; IMyShipConnector o2; public override bool RunCmd(bool dY)
            {
                if (!dY)
                {
                    if (o4.pQ.EndsWith("e")) o0 = true; if (o4.pQ.
Contains("cn")) o1 = true; nY.qU(); nZ = 0;
                }
                if (nZ == 0) { if (!nY.qS("connector", o4.pR, dY)) return false; nZ++; n_ = 0; dY = false; }
                if (nY.qV() <= 0)
                {
                    o5.
ud("Docked: " + o7.T("DO1")); return true;
                }
                for (; n_ < nY.qV(); n_++)
                {
                    o2 = nY.qA[n_] as IMyShipConnector; if (o2.Status == MyShipConnectorStatus.
Connected)
                    {
                        if (o1) { o5.Add(o2.CustomName + ":"); o5.ug(o2.OtherConnector.CubeGrid.CustomName); }
                        else
                        {
                            o5.ud(o2.OtherConnector.CubeGrid.
CustomName);
                        }
                    }
                    else { if (o0) { if (o1) { o5.Add(o2.CustomName + ":"); o5.ug("-"); } else o5.ud("-"); } }
                }
                return true;
            }
        }
        class v8 : vt
        {
            public vv o3 =
null; protected vg o4; protected vw o5; protected vb o6; protected TranslationTable o7; public v8() { sB = 3600; sx = "CommandTask"; }
            public
void o8(vb dZ, vg d_)
            { o6 = dZ; o5 = o6.oo; o4 = d_; o7 = o5.u4; }
            public virtual bool RunCmd(bool e0)
            {
                o5.ud(o7.T("UC") + ": '" + o4.pS + "'"); return
true;
            }
            public override bool Run(bool e1) { o3 = o5.ub(o3, o6.oq); if (!e1) o5.um(); return RunCmd(e1); }
        }
        class v9 : vt
        {
            vd o9; vw oa; string ob =
""; public v9(vw e2, vd e3, string e4) { sB = -1; sx = "ArgScroll"; ob = e4; o9 = e3; oa = e2; }
            int oc; vj od; public override void Init()
            {
                od = new vj(sF,
oa.u2);
            }
            int oe = 0; int of = 0; vg og; public override bool Run(bool e5)
            {
                if (!e5) { of = 0; od.qU(); og = new vg(sF); oe = 0; }
                if (of == 0)
                {
                    if (!og.q1(ob,
e5)) return false; if (og.pX.Count > 0) { if (!int.TryParse(og.pX[0].q4, out oc)) oc = 1; else if (oc < 1) oc = 1; }
                    if (og.pQ.EndsWith("up")) oc = -oc;
                    else if (!og.pQ.EndsWith("down")) oc = 0; of++; e5 = false;
                }
                if (of == 1) { if (!od.qS("textpanel", og.pR, e5)) return false; of++; e5 = false; }
                vr e6;
                for (; oe < od.qV(); oe++)
                {
                    if (!sF.t3(20)) return false; IMyTextPanel e7 = od.qA[oe] as IMyTextPanel; if (!o9.p0.TryGetValue(e7, out e6))
                        continue; if (e6 == null || e6.s6 != e7) continue; if (e6.sa) e6.s5.tu = 10; if (oc > 0) e6.s5.tt(oc); else if (oc < 0) e6.s5.ts(-oc); else e6.s5.tv(); e6.
                                                  sl();
                }
                return true;
            }
        }
        class va : vt
        {
            vw oh; vd oi; public int oj = 0; public va(vw e8, vd e9)
            {
                sx = "BootPanelsTask"; sB = 1; oh = e8; oi = e9; if (!oh.tC
) { oj = int.MaxValue; oi.p1 = true; }
            }
            TranslationTable ok; public override void Init() { ok = oh.u4; }
            public override bool Run(bool ea)
            {
                if (oj >
oh.tD.Count) { sJ(); return true; }
                if (oj == 0) { oi.p1 = false; }
                om(); oj++; return true;
            }
            public override void End() { oi.p1 = true; }
            public void ol
()
            { ve eb = oi.oX; for (int ec = 0; ec < eb.pd(); ec++) { vr ed = eb.pf(ec); oh.ub(ed.s5, ed); oh.um(); oh.un(ed); } oj = (oh.tC ? 0 : int.MaxValue); }
            public
void om()
            {
                ve ee = oi.oX; for (int ef = 0; ef < ee.pd(); ef++)
                {
                    vr eg = ee.pf(ef); oh.ub(eg.s5, eg); oh.um(); if (eg.s6.FontSize > 3f) continue; oh.ui(ok
.T("B1")); double eh = (double)oj / oh.tD.Count * 100; oh.uk(eh); if (oj == oh.tD.Count)
                    {
                        oh.ud(""); oh.ui("Automatic LCDs 2"); oh.ui(
"by MMaster");
                    }
                    else oh.ue(oh.tD[oj]); var ei = eg.sa; eg.sa = false; oh.un(eg); eg.sa = ei;
                }
            }
            public bool on() { return oj <= oh.tD.Count; }
        }
        class
vb : vt
        {
            public vw oo; public vr oq; public vc or = null; string os = "N/A"; public Dictionary<string, v8> ot = new Dictionary<string, v8>();
            public List<string> ou = null; public vd ov; public bool ow { get { return ov.p1; } }
            public vb(vd ej, vr ek)
            {
                sB = 5; oq = ek; ov = ej; oo = ej.oW; sx =
"PanelProcess";
            }
            TranslationTable ox; public override void Init() { ox = oo.u4; }
            vg oy = null; v8 oz(string el, bool em)
            {
                if (!em) oy = new vg(sF)
; if (!oy.q1(el, em)) return null; v8 en = oy.pW(); en.o8(this, oy); sF.sT(en, 0); return en;
            }
            string oA = ""; void oB()
            {
                try { oA = oq.s6.CustomData; }
                catch { oA = ""; oq.s6.CustomData = ""; }
                oA = oA.Replace("\\\n", "");
            }
            int oC = 0; int oD = 0; List<string> oE = null; HashSet<string> oF = new HashSet<
string>(); int oG = 0; bool oH(bool eo)
            {
                if (!eo)
                {
                    char[] ep = { ';', '\n' }; var eq = oA.Replace("\\;", "\f"); oE = new List<string>(eq.Split(ep,
StringSplitOptions.RemoveEmptyEntries)); oF.Clear(); oC = 0; oD = 0; oG = 0;
                } while (oC < oE.Count)
                {
                    if (!sF.t3(500)) return false; if (oE[oC].
StartsWith("//")) { oE.RemoveAt(oC); continue; }
                    oE[oC] = oE[oC].Replace('\f', ';'); if (!ot.ContainsKey(oE[oC]))
                    {
                        if (oG != 1) eo = false; oG = 1; v8
er = oz(oE[oC], eo); if (er == null) return false; eo = false; ot.Add(oE[oC], er); oG = 0;
                    }
                    if (!oF.Contains(oE[oC])) oF.Add(oE[oC]); oC++;
                }
                if (ou !=
null)
                {
                    v8 es; while (oD < ou.Count)
                    {
                        if (!sF.t3(7)) return false; if (!oF.Contains(ou[oD])) if (ot.TryGetValue(ou[oD], out es))
                            {
                                es.sJ(); ot.
Remove(ou[oD]);
                            }
                        oD++;
                    }
                }
                ou = oE; return true;
            }
            public override void End()
            {
                if (ou != null)
                {
                    v8 et; for (int eu = 0; eu < ou.Count; eu++)
                    {
                        if (ot.
TryGetValue(ou[eu], out et)) et.sJ();
                    }
                    ou = null;
                }
                if (or != null) { or.sJ(); or = null; }
            }
            string oI = ""; bool oJ = false; public override bool Run(
bool ev)
            {
                if (oq.s4.ss() <= 0) { sJ(); return true; }
                if (!ev)
                {
                    oq.s5 = oo.ub(oq.s5, oq); oB(); if (oq.s6.CustomName != oI) { oJ = true; } else { oJ = false; }
                    oI = oq.s6.CustomName;
                }
                if (oA != os)
                {
                    if (!oH(ev)) return false; if (oA == "")
                    {
                        if (ov.p1) { oo.um(); oo.ud(ox.T("H1")); oo.un(oq); return true; }
                        return this.sH(2);
                    }
                    oJ = true;
                }
                os = oA; if (or != null && oJ) { sF.sU(or); or.oO(); sF.sT(or, 0); } else if (or == null) { or = new vc(this); sF.sT(or, 0); }
                return true;
            }
        }
        class vc : vt
        {
            public vw oK; public vr oL; vb oM; public vc(vb ew) { oM = ew; oK = oM.oo; oL = oM.oq; sB = 0.5; sx = "PanelDisplay"; }
            double oN = 0; public void oO() { oN = 0; }
            int oP = 0; int oQ = 0; bool oR = true; double oS = double.MaxValue; int oT = 0; public override bool Run(bool
ey)
            {
                v8 ez; if (!ey && (!oM.ow || oM.ou == null || oM.ou.Count <= 0)) return true; if (oM.ov.oV > 5) return sH(0); if (!ey)
                {
                    oQ = 0; oR = false; oS = double.
MaxValue; oT = 0;
                }
                if (oT == 0)
                {
                    while (oQ < oM.ou.Count)
                    {
                        if (!sF.t3(5)) return false; if (oM.ot.TryGetValue(oM.ou[oQ], out ez))
                        {
                            if (!ez.sD) return
sH(ez.sy - sF.sN + 0.001); if (ez.sz > oN) oR = true; if (ez.sy < oS) oS = ez.sy;
                        }
                        oQ++;
                    }
                    oT++; ey = false;
                }
                double eA = oS - sF.sN + 0.001; if (!oR && !oL.sb())
                    return sH(eA); oK.uc(oL); if (oR)
                {
                    if (!ey)
                    {
                        oN = sF.sN; oK.um(); var eB = oL.s6.CustomName; eB = (eB.Contains("#") ? eB.Substring(eB.LastIndexOf(
'#') + 1) : ""); if (eB != "") oK.ud(eB); oP = 0;
                    } while (oP < oM.ou.Count)
                    {
                        if (!sF.t3(7)) return false; if (!oM.ot.TryGetValue(oM.ou[oP], out ez))
                        {
                            oK.
ud("ERR: No cmd task (" + oM.ou[oP] + ")"); oP++; continue;
                        }
                        oK.uf(ez.o3.tm()); oP++;
                    }
                }
                oK.un(oL); oM.ov.oV++; if (sB < eA && !oL.sb()) return sH(
eA); return true;
            }
        }
        class vd : vt
        {
            public int oV = 0; public vw oW; public ve oX = new ve(); vj oY; vj oZ; Dictionary<vr, vb> o_ = new Dictionary<
vr, vb>(); public Dictionary<IMyTextPanel, vr> p0 = new Dictionary<IMyTextPanel, vr>(); public bool p1 = false; va p2 = null; public vd(vw eC)
            {
                sB = 5; oW = eC; sx = "ProcessPanels";
            }
            public override void Init() { oY = new vj(sF, oW.u2); oZ = new vj(sF, oW.u2); p2 = new va(oW, this); }
            int p3 = 0;
            bool p4(bool eD)
            {
                if (!eD) p3 = 0; if (p3 == 0) { if (!oY.qS("textpanel", oW.tA, eD)) return false; p3++; eD = false; }
                if (p3 == 1)
                {
                    if (oW.tA == "T:[LCD]" &&
"T:!LCD!" != "") if (!oY.qS("textpanel", "T:!LCD!", eD)) return false; p3++; eD = false;
                }
                return true;
            }
            string p5(IMyTextPanel eE)
            {
                return eE.
CustomName + " " + eE.NumberInGrid + " " + eE.Position.ToString();
            }
            void p6(IMyTextPanel eF)
            {
                vr eG = null; if (!p0.TryGetValue(eF, out eG))
                {
                    return;
                }
                eG.s4.sr(eF); p0.Remove(eF); if (eG.s4.ss() <= 0) { vb eH; if (o_.TryGetValue(eG, out eH)) { oX.pg(eG.s9); o_.Remove(eG); eH.sJ(); } }
            }
            int
p7 = 0; int p8 = 0; public override bool Run(bool eI)
            {
                if (!eI) { oY.qU(); p7 = 0; p8 = 0; }
                if (!p4(eI)) return false; while (p7 < oY.qV())
                {
                    if (!sF.t3(20)
) return false; IMyTextPanel eJ = (oY.qA[p7] as IMyTextPanel); if (eJ == null || !eJ.IsWorking) { oY.qA.RemoveAt(p7); continue; }
                    vr eK = null; var
eL = false; var eM = p5(eJ); int eN = eM.IndexOf("!LINK:"); if (eN >= 0 && eM.Length > eN + 6) { eM = eM.Substring(eN + 6); eL = true; }
                    string[] eO = eM.Split(
' '); var eP = eO[0]; if (p0.ContainsKey(eJ)) { eK = p0[eJ]; if (eK.s9 == eM || (eL && eK.s9 == eP)) { p7++; continue; } this.p6(eJ); }
                    if (!eL)
                    {
                        eK = new vr(oW
, eM); eK.s4.sp(eM, eJ); vb eQ = new vb(this, eK); sF.sT(eQ, 0); o_.Add(eK, eQ); oX.pc(eM, eK); p0.Add(eJ, eK); p7++; continue;
                    }
                    eK = oX.pe(eP); if (eK
== null) { eK = new vr(oW, eP); oX.pc(eP, eK); vb eQ = new vb(this, eK); sF.sT(eQ, 0); o_.Add(eK, eQ); }
                    eK.s4.sp(eM, eJ); p0.Add(eJ, eK); p7++;
                } while (
p8 < oZ.qV())
                {
                    if (!sF.t3(300)) return false; IMyTextPanel eJ = oZ.qA[p8] as IMyTextPanel; if (eJ == null) continue; if (!oY.qA.Contains(eJ))
                    {
                        this
.p6(eJ);
                    }
                    p8++;
                }
                oZ.qU(); oZ.qT(oY); if (!p2.sC && p2.on()) sF.sT(p2, 0); return true;
            }
            public bool p9(string eT)
            {
                var eU = eT.ToLower(); if (eU ==
"clear") { p2.ol(); if (!p2.sC) sF.sT(p2, 0); return true; }
                if (eU == "boot") { p2.oj = 0; if (!p2.sC) sF.sT(p2, 0); return true; }
                if (eU.StartsWith(
"scroll")) { v9 eV = new v9(oW, this, eT); sF.sT(eV, 0); return true; }
                if (eU == "props")
                {
                    vk eW = oW.u2; var eX = new List<IMyTerminalBlock>(); var
eY = new List<ITerminalAction>(); var eZ = new List<ITerminalProperty>(); IMyTextPanel e_ = sF.sR.GridTerminalSystem.GetBlockWithName(
"DEBUG") as IMyTextPanel; if (e_ == null) { return true; }
                    e_.WritePublicText("Properties: "); foreach (var item in eW.q_)
                    {
                        e_.WritePublicText
(item.Key + " ==============" + "\n", true); item.Value(eX, null); if (eX.Count <= 0) { e_.WritePublicText("No blocks\n", true); continue; }
                        eX[0].
GetProperties(eZ, f0 => { return f0.Id != "Name" && f0.Id != "OnOff" && !f0.Id.StartsWith("Show"); }); foreach (var prop in eZ)
                        {
                            e_.
WritePublicText("P " + prop.Id + " " + prop.TypeName + "\n", true);
                        }
                        eZ.Clear(); eX.Clear();
                    }
                }
                return false;
            }
        }
        public class ve
        {
            Dictionary<
string, vr> pa = new Dictionary<string, vr>(); List<string> pb = new List<string>(); public void pc(string f1, vr f2)
            {
                if (!pa.ContainsKey(f1))
                { pb.Add(f1); pa.Add(f1, f2); }
            }
            public int pd() { return pa.Count; }
            public vr pe(string f3)
            {
                if (pa.ContainsKey(f3)) return pa[f3]; return
null;
            }
            public vr pf(int f4) { return pa[pb[f4]]; }
            public void pg(string f5) { pa.Remove(f5); pb.Remove(f5); }
            public void ph()
            {
                pb.Clear();
                pa.Clear();
            }
            public void pi() { pb.Sort(); }
        }
        public enum vf
        {
            pj = 0, pk = 1, pl = 2, pm = 3, pn = 4, po = 5, pp = 6, pq = 7, pr = 8, ps = 9, pt = 10, pu = 11, pv = 12, pw = 13
, px = 14, py = 15, pz = 16, pA = 17, pB = 18, pC = 19, pD = 20, pE = 21, pF = 22, pG = 23, pH = 24, pI = 25, pJ = 26, pK = 27, pL = 28, pM = 29, pN = 30, pO = 31,
        }
        class vg
        {
            vu pP;
            public string pQ = ""; public string pR = ""; public string pS = ""; public string pT = ""; public vf pU = vf.pj; public vg(vu f6) { pP = f6; }
            vf pV()
            {
                if (pQ == "echo" || pQ == "center" || pQ == "right") return vf.pk; if (pQ.StartsWith("hscroll")) return vf.pN; if (pQ.StartsWith("inventory") || pQ
                               == "missing" || pQ.StartsWith("invlist")) return vf.pl; if (pQ.StartsWith("working")) return vf.pB; if (pQ.StartsWith("cargo")) return vf.pm
                                         ; if (pQ.StartsWith("mass")) return vf.pn; if (pQ.StartsWith("shipmass")) return vf.pG; if (pQ == "oxygen") return vf.po; if (pQ.StartsWith(
                                                     "tanks")) return vf.pp; if (pQ.StartsWith("powertime")) return vf.pq; if (pQ.StartsWith("powerused")) return vf.pr; if (pQ.StartsWith(
                                                             "power")) return vf.ps; if (pQ.StartsWith("speed")) return vf.pt; if (pQ.StartsWith("accel")) return vf.pu; if (pQ.StartsWith("alti"))
                    return vf.pI; if (pQ.StartsWith("charge")) return vf.pv; if (pQ.StartsWith("docked")) return vf.pO; if (pQ.StartsWith("time") || pQ.
                           StartsWith("date")) return vf.pw; if (pQ.StartsWith("countdown")) return vf.px; if (pQ.StartsWith("textlcd")) return vf.py; if (pQ.EndsWith
                                   ("count")) return vf.pz; if (pQ.StartsWith("dampeners") || pQ.StartsWith("occupied")) return vf.pA; if (pQ.StartsWith("damage")) return vf.
                                            pC; if (pQ.StartsWith("amount")) return vf.pD; if (pQ.StartsWith("pos")) return vf.pE; if (pQ.StartsWith("distance")) return vf.pH; if (pQ.
                                                      StartsWith("details")) return vf.pF; if (pQ.StartsWith("stop")) return vf.pJ; if (pQ.StartsWith("gravity")) return vf.pK; if (pQ.StartsWith
                                                              ("customdata")) return vf.pL; if (pQ.StartsWith("prop")) return vf.pM; return vf.pj;
            }
            public v8 pW()
            {
                switch (pU)
                {
                    case vf.pk:
                        return new uO
();
                    case vf.pl: return new uC();
                    case vf.pm: return new uD();
                    case vf.pn: return new uE();
                    case vf.po: return new uF();
                    case vf.pp:
                        return
new uG();
                    case vf.pq: return new uH();
                    case vf.pr: return new uJ();
                    case vf.ps: return new uL();
                    case vf.pt: return new uM();
                    case vf.pu:
                        return new uN();
                    case vf.pv: return new uP();
                    case vf.pw: return new uQ();
                    case vf.px: return new uR();
                    case vf.py: return new uS();
                    case
vf.pz:
                        return new uT();
                    case vf.pA: return new uU();
                    case vf.pB: return new uV();
                    case vf.pC: return new uW();
                    case vf.pD: return new uX();
                    case vf.pE: return new uY();
                    case vf.pF: return new uZ();
                    case vf.pG: return new u_();
                    case vf.pH: return new v0();
                    case vf.pI:
                        return new
v1();
                    case vf.pJ: return new v2();
                    case vf.pK: return new v3();
                    case vf.pL: return new v4();
                    case vf.pM: return new v5();
                    case vf.pN:
                        return
new v6();
                    case vf.pO: return new v7();
                    default: return new v8();
                }
            }
            public List<vh> pX = new List<vh>(); string[] pY = null; string pZ = ""; bool
p_ = false; int q0 = 1; public bool q1(string f7, bool f8)
            {
                if (!f8)
                {
                    pU = vf.pj; pR = ""; pQ = ""; pS = f7.TrimStart(' '); pX.Clear(); if (pS == "") return
true; int f9 = pS.IndexOf(' '); if (f9 < 0 || f9 >= pS.Length - 1) pT = ""; else pT = pS.Substring(f9 + 1); pY = pS.Split(' '); pZ = ""; p_ = false; pQ = pY[0].
        ToLower(); q0 = 1;
                }
                for (; q0 < pY.Length; q0++)
                {
                    if (!pP.t3(40)) return false; var fa = pY[q0]; if (fa == "") continue; if (fa[0] == '{' && fa[fa.Length - 1]
== '}') { fa = fa.Substring(1, fa.Length - 2); if (fa == "") continue; if (pR == "") pR = fa; else pX.Add(new vh(fa)); continue; }
                    if (fa[0] == '{')
                    {
                        p_ = true;
                        pZ = fa.Substring(1); continue;
                    }
                    if (fa[fa.Length - 1] == '}')
                    {
                        p_ = false; pZ += ' ' + fa.Substring(0, fa.Length - 1); if (pR == "") pR = pZ;
                        else pX.Add(new
vh(pZ)); continue;
                    }
                    if (p_) { if (pZ.Length != 0) pZ += ' '; pZ += fa; continue; }
                    if (pR == "") pR = fa; else pX.Add(new vh(fa));
                }
                pU = pV(); return true;
            }
        }
        public class vh
        {
            public string q2 = ""; public string q3 = ""; public string q4 = ""; public List<string> q5 = new List<string>(); public vh(
        string fb)
            { q4 = fb; }
            public void q6()
            {
                if (q4 == "" || q2 != "" || q3 != "" || q5.Count > 0) return; var fc = q4.Trim(); if (fc[0] == '+' || fc[0] == '-')
                {
                    q2 += fc
[0]; fc = q4.Substring(1);
                }
                string[] fd = fc.Split('/'); var fe = fd[0]; if (fd.Length > 1) { q3 = fd[0]; fe = fd[1]; } else q3 = ""; if (fe.Length > 0)
                {
                    string
[] ff = fe.Split(','); for (int fg = 0; fg < ff.Length; fg++) if (ff[fg] != "") q5.Add(ff[fg]);
                }
            }
        }
        public class vi : vt
        {
            MyShipVelocities q7; public
VRageMath.Vector3D q8
            { get { return q7.LinearVelocity; } }
            public VRageMath.Vector3D q9 { get { return q7.AngularVelocity; } }
            double qa = 0;
            public double qb { get { if (qq != null) return qq.GetShipSpeed(); else return qa; } }
            double qc = 0; public double qd { get { return qc; } }
            double qe =
0; public double qf { get { return qe; } }
            double qg = 0; double qh = 0; public double qi { get { return qg; } }
            MyShipMass qj; public double qk
            {
                get
                {
                    return qj.TotalMass;
                }
            }
            public double ql { get { return qj.BaseMass; } }
            double qm = double.NaN; public double qn { get { return qm; } }
            double qo =
double.NaN; public double qp { get { return qo; } }
            IMyShipController qq = null; IMySlimBlock qr = null; public IMyShipController qs
            {
                get
                {
                    return
qq;
                }
            }
            VRageMath.Vector3D qt; public vi(vu fh) { sx = "ShipMgr"; sF = fh; qt = sF.sR.Me.GetPosition(); sB = 0.5; }
            List<IMyTerminalBlock> qu = new List
<IMyTerminalBlock>(); int qv = 0; public override bool Run(bool fi)
            {
                if (!fi)
                {
                    qu.Clear(); sF.sR.GridTerminalSystem.GetBlocksOfType<
IMyShipController>(qu); qv = 0; if (qq != null && qq.CubeGrid.GetCubeBlock(qq.Position) != qr) qq = null;
                }
                if (qu.Count > 0)
                {
                    for (; qv < qu.Count; qv++)
                    {
                        if (!sF.t3(20)) return false; IMyShipController fj = qu[qv] as IMyShipController; if (fj.IsMainCockpit || fj.IsUnderControl)
                        {
                            qq = fj; qr = fj.
CubeGrid.GetCubeBlock(fj.Position); if (fj.IsMainCockpit) { qv = qu.Count; break; }
                        }
                    }
                    if (qq == null)
                    {
                        qq = qu[0] as IMyShipController; qr = qq.
CubeGrid.GetCubeBlock(qq.Position);
                    }
                    qj = qq.CalculateShipMass(); if (!qq.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out qm)) qm =
double.NaN; if (!qq.TryGetPlanetElevation(MyPlanetElevation.Surface, out qo)) qo = double.NaN; q7 = qq.GetShipVelocities();
                }
                double fk = qa; qa
= q8.Length(); qc = (qa - fk) / sA; if (-qc > qe) qe = -qc; if (-qc > qg) { qg = -qc; qh = sF.sN; }
                if (sF.sN - qh > 5 && -qc > 0.1) qg -= (qg + qc) * 0.3f; return true;
            }
        }
        public class vj
        {
            vu qw = null; vk qx; IMyCubeGrid qy { get { return qw.sR.Me.CubeGrid; } }
            IMyGridTerminalSystem qz
            {
                get
                {
                    return qw.sR.
GridTerminalSystem;
                }
            }
            public List<IMyTerminalBlock> qA = new List<IMyTerminalBlock>(); public vj(vu fl, vk fm) { qw = fl; qx = fm; }
            int qB = 0;
            public double qC(ref double fn, ref double fo, bool fp)
            {
                if (!fp) qB = 0; for (; qB < qA.Count; qB++)
                {
                    if (!qw.t3(4)) return Double.NaN;
                    IMyInventory fq = qA[qB].GetInventory(0); if (fq == null) continue; fn += (double)fq.CurrentVolume; fo += (double)fq.MaxVolume;
                }
                fn *= 1000; fo *=
1000; return (fo > 0 ? fn / fo * 100 : 100);
            }
            int qD = 0; double qE = 0; public double qF(bool fr)
            {
                if (!fr) { qD = 0; qE = 0; }
                for (; qD < qA.Count; qD++)
                {
                    if (!qw.
t3(6)) return Double.NaN; for (int fs = 0; fs < 2; fs++)
                    {
                        IMyInventory ft = qA[qD].GetInventory(fs); if (ft == null) continue; qE += (double)ft.
CurrentMass;
                    }
                }
                return qE * 1000;
            }
            int qG = 0; bool qH(bool fu = false)
            {
                if (!fu) qG = 0; while (qG < qA.Count)
                {
                    if (!qw.t3(4)) return false; if (qA[qG].
CubeGrid != qy) { qA.RemoveAt(qG); continue; }
                    qG++;
                }
                return true;
            }
            List<IMyBlockGroup> qI = new List<IMyBlockGroup>(); int qJ = 0; public bool qK
(string fv, bool fw)
            {
                int fx = fv.IndexOf(':'); var fy = (fx >= 1 && fx <= 2 ? fv.Substring(0, fx) : ""); var fz = fy.Contains("T"); if (fy != "") fv = fv.
                    Substring(fx + 1); if (fv == "" || fv == "*")
                {
                    if (!fw) { var fA = new List<IMyTerminalBlock>(); qz.GetBlocks(fA); qA.AddList(fA); }
                    if (fz) if (!qH(fw))
                            return false; return true;
                }
                var fB = (fy.Contains("G") ? fv.Trim().ToLower() : ""); if (fB != "")
                {
                    if (!fw)
                    {
                        qI.Clear(); qz.GetBlockGroups(qI); qJ =
0;
                    }
                    for (; qJ < qI.Count; qJ++)
                    {
                        IMyBlockGroup fC = qI[qJ]; if (fC.Name.ToLower() == fB)
                        {
                            if (!fw) fC.GetBlocks(qA); if (fz) if (!qH(fw)) return false;
                            return true;
                        }
                    }
                    return true;
                }
                if (!fw) qz.SearchBlocksOfName(fv, qA); if (fz) if (!qH(fw)) return false; return true;
            }
            List<IMyBlockGroup> qL =
new List<IMyBlockGroup>(); List<IMyTerminalBlock> qM = new List<IMyTerminalBlock>(); int qN = 0; int qO = 0; public bool qP(string fD, string
fE, bool fF, bool fG)
            {
                if (!fG) { qL.Clear(); qz.GetBlockGroups(qL); qN = 0; }
                for (; qN < qL.Count; qN++)
                {
                    IMyBlockGroup fH = qL[qN]; if (fH.Name.
ToLower() == fE)
                    {
                        if (!fG) { qO = 0; qM.Clear(); fH.GetBlocks(qM); } else fG = false; for (; qO < qM.Count; qO++)
                        {
                            if (!qw.t3(6)) return false; if (fF && qM[
qO].CubeGrid != qy) continue; if (qx.r3(qM[qO], fD)) qA.Add(qM[qO]);
                        }
                        return true;
                    }
                }
                return true;
            }
            List<IMyTerminalBlock> qQ = new List<
IMyTerminalBlock>(); int qR = 0; public bool qS(string fI, string fJ, bool fK)
            {
                int fL = fJ.IndexOf(':'); var fM = (fL >= 1 && fL <= 2 ? fJ.Substring(
0, fL) : ""); var fN = fM.Contains("T"); if (fM != "") fJ = fJ.Substring(fL + 1); if (!fK) { qQ.Clear(); qR = 0; }
                var fO = (fM.Contains("G") ? fJ.Trim().
ToLower() : ""); if (fO != "") { if (!qP(fI, fO, fN, fK)) return false; return true; }
                if (!fK) qx.r2(ref qQ, fI); if (fJ == "" || fJ == "*")
                {
                    if (!fK) qA.
AddList(qQ); if (fN) if (!qH(fK)) return false; return true;
                }
                for (; qR < qQ.Count; qR++)
                {
                    if (!qw.t3(4)) return false; if (fN && qQ[qR].CubeGrid != qy
) continue; if (qQ[qR].CustomName.Contains(fJ)) qA.Add(qQ[qR]);
                }
                return true;
            }
            public void qT(vj fP) { qA.AddList(fP.qA); }
            public void qU()
            { qA.Clear(); }
            public int qV() { return qA.Count; }
        }
        public class vk
        {
            vu qW; vw qX; public MyGridProgram qY { get { return qW.sR; } }
            public
IMyGridTerminalSystem qZ
            { get { return qW.sR.GridTerminalSystem; } }
            public Dictionary<string, Action<List<IMyTerminalBlock>, Func<
IMyTerminalBlock, bool>>> q_ = null; public vk(vu fQ, vw fR) { qW = fQ; qX = fR; }
            public void r0()
            {
                if (q_ != null && qZ.GetBlocksOfType<
IMyCargoContainer> == q_["CargoContainer"]) return; q_ = new Dictionary<string, Action<List<IMyTerminalBlock>, Func<IMyTerminalBlock, bool>
>>(){{"CargoContainer",qZ.GetBlocksOfType<IMyCargoContainer>},{"TextPanel",qZ.GetBlocksOfType<IMyTextPanel>},{"Assembler",qZ.
GetBlocksOfType<IMyAssembler>},{"Refinery",qZ.GetBlocksOfType<IMyRefinery>},{"Reactor",qZ.GetBlocksOfType<IMyReactor>},{
"SolarPanel",qZ.GetBlocksOfType<IMySolarPanel>},{"BatteryBlock",qZ.GetBlocksOfType<IMyBatteryBlock>},{"Beacon",qZ.GetBlocksOfType<
IMyBeacon>},{"RadioAntenna",qZ.GetBlocksOfType<IMyRadioAntenna>},{"AirVent",qZ.GetBlocksOfType<IMyAirVent>},{"ConveyorSorter",qZ.
GetBlocksOfType<IMyConveyorSorter>},{"OxygenTank",qZ.GetBlocksOfType<IMyGasTank>},{"OxygenGenerator",qZ.GetBlocksOfType<
IMyGasGenerator>},{"OxygenFarm",qZ.GetBlocksOfType<IMyOxygenFarm>},{"LaserAntenna",qZ.GetBlocksOfType<IMyLaserAntenna>},{"Thrust",
qZ.GetBlocksOfType<IMyThrust>},{"Gyro",qZ.GetBlocksOfType<IMyGyro>},{"SensorBlock",qZ.GetBlocksOfType<IMySensorBlock>},{
"ShipConnector",qZ.GetBlocksOfType<IMyShipConnector>},{"ReflectorLight",qZ.GetBlocksOfType<IMyReflectorLight>},{"InteriorLight",qZ
.GetBlocksOfType<IMyInteriorLight>},{"LandingGear",qZ.GetBlocksOfType<IMyLandingGear>},{"ProgrammableBlock",qZ.GetBlocksOfType<
IMyProgrammableBlock>},{"TimerBlock",qZ.GetBlocksOfType<IMyTimerBlock>},{"MotorStator",qZ.GetBlocksOfType<IMyMotorStator>},{
"PistonBase",qZ.GetBlocksOfType<IMyPistonBase>},{"Projector",qZ.GetBlocksOfType<IMyProjector>},{"ShipMergeBlock",qZ.
GetBlocksOfType<IMyShipMergeBlock>},{"SoundBlock",qZ.GetBlocksOfType<IMySoundBlock>},{"Collector",qZ.GetBlocksOfType<IMyCollector>
},{"JumpDrive",qZ.GetBlocksOfType<IMyJumpDrive>},{"Door",qZ.GetBlocksOfType<IMyDoor>},{"GravityGeneratorSphere",qZ.GetBlocksOfType
<IMyGravityGeneratorSphere>},{"GravityGenerator",qZ.GetBlocksOfType<IMyGravityGenerator>},{"ShipDrill",qZ.GetBlocksOfType<
IMyShipDrill>},{"ShipGrinder",qZ.GetBlocksOfType<IMyShipGrinder>},{"ShipWelder",qZ.GetBlocksOfType<IMyShipWelder>},{"Parachute",qZ
.GetBlocksOfType<IMyParachute>},{"LargeGatlingTurret",qZ.GetBlocksOfType<IMyLargeGatlingTurret>},{"LargeInteriorTurret",qZ.
GetBlocksOfType<IMyLargeInteriorTurret>},{"LargeMissileTurret",qZ.GetBlocksOfType<IMyLargeMissileTurret>},{"SmallGatlingGun",qZ.
GetBlocksOfType<IMySmallGatlingGun>},{"SmallMissileLauncherReload",qZ.GetBlocksOfType<IMySmallMissileLauncherReload>},{
"SmallMissileLauncher",qZ.GetBlocksOfType<IMySmallMissileLauncher>},{"VirtualMass",qZ.GetBlocksOfType<IMyVirtualMass>},{"Warhead",
qZ.GetBlocksOfType<IMyWarhead>},{"FunctionalBlock",qZ.GetBlocksOfType<IMyFunctionalBlock>},{"LightingBlock",qZ.GetBlocksOfType<
IMyLightingBlock>},{"ControlPanel",qZ.GetBlocksOfType<IMyControlPanel>},{"Cockpit",qZ.GetBlocksOfType<IMyCockpit>},{"MedicalRoom",
qZ.GetBlocksOfType<IMyMedicalRoom>},{"RemoteControl",qZ.GetBlocksOfType<IMyRemoteControl>},{"ButtonPanel",qZ.GetBlocksOfType<
IMyButtonPanel>},{"CameraBlock",qZ.GetBlocksOfType<IMyCameraBlock>},{"OreDetector",qZ.GetBlocksOfType<IMyOreDetector>},{
"ShipController",qZ.GetBlocksOfType<IMyShipController>}};
            }
            public void r1(ref List<IMyTerminalBlock> fS, string fT)
            {
                Action<List<
IMyTerminalBlock>, Func<IMyTerminalBlock, bool>> fU = null; if (q_.TryGetValue(fT, out fU)) fU(fS, null);
                else
                {
                    if (fT == "CryoChamber")
                    {
                        qZ.
GetBlocksOfType<IMyCockpit>(fS, fV => fV.BlockDefinition.ToString().Contains("Cryo")); return;
                    }
                }
            }
            public void r2(ref List<
IMyTerminalBlock> fW, string fX)
            { r1(ref fW, r4(fX.Trim())); }
            public bool r3(IMyTerminalBlock fY, string fZ)
            {
                var f_ = r4(fZ); switch (f_)
                {
                    case "FunctionalBlock": return true;
                    case "ShipController": return (fY as IMyShipController != null);
                    default:
                        return fY.BlockDefinition.
ToString().Contains(r4(fZ));
                }
            }
            public string r4(string g0)
            {
                g0 = g0.ToLower(); if (g0.StartsWith("carg") || g0.StartsWith("conta")) return
"CargoContainer"; if (g0.StartsWith("text") || g0.StartsWith("lcd")) return "TextPanel"; if (g0.StartsWith("ass")) return "Assembler"; if (
g0.StartsWith("refi")) return "Refinery"; if (g0.StartsWith("reac")) return "Reactor"; if (g0.StartsWith("solar")) return "SolarPanel"; if
(g0.StartsWith("bat")) return "BatteryBlock"; if (g0.StartsWith("bea")) return "Beacon"; if (g0.Contains("vent")) return "AirVent"; if (g0.
Contains("sorter")) return "ConveyorSorter"; if (g0.Contains("tank")) return "OxygenTank"; if (g0.Contains("farm") && g0.Contains("oxy"))
                    return "OxygenFarm"; if (g0.Contains("gene") && g0.Contains("oxy")) return "OxygenGenerator"; if (g0.Contains("cryo")) return
                            "CryoChamber"; if (g0 == "laserantenna") return "LaserAntenna"; if (g0.Contains("antenna")) return "RadioAntenna"; if (g0.StartsWith(
                                     "thrust")) return "Thrust"; if (g0.StartsWith("gyro")) return "Gyro"; if (g0.StartsWith("sensor")) return "SensorBlock"; if (g0.Contains(
                                             "connector")) return "ShipConnector"; if (g0.StartsWith("reflector")) return "ReflectorLight"; if ((g0.StartsWith("inter") && g0.EndsWith(
                                                  "light"))) return "InteriorLight"; if (g0.StartsWith("land")) return "LandingGear"; if (g0.StartsWith("program")) return
                                                         "ProgrammableBlock"; if (g0.StartsWith("timer")) return "TimerBlock"; if (g0.StartsWith("motor")) return "MotorStator"; if (g0.StartsWith(
                                                                "piston")) return "PistonBase"; if (g0.StartsWith("proj")) return "Projector"; if (g0.Contains("merge")) return "ShipMergeBlock"; if (g0.
                                                                        StartsWith("sound")) return "SoundBlock"; if (g0.StartsWith("col")) return "Collector"; if (g0.Contains("jump")) return "JumpDrive"; if (g0
                                                                                == "door") return "Door"; if ((g0.Contains("grav") && g0.Contains("sphe"))) return "GravityGeneratorSphere"; if (g0.Contains("grav")) return
                                                                                          "GravityGenerator"; if (g0.EndsWith("drill")) return "ShipDrill"; if (g0.Contains("grind")) return "ShipGrinder"; if (g0.EndsWith("welder"
                                                                                                 )) return "ShipWelder"; if (g0.StartsWith("parach")) return "Parachute"; if ((g0.Contains("turret") && g0.Contains("gatl"))) return
                                                                                                          "LargeGatlingTurret"; if ((g0.Contains("turret") && g0.Contains("inter"))) return "LargeInteriorTurret"; if ((g0.Contains("turret") && g0.
                                                                                                                Contains("miss"))) return "LargeMissileTurret"; if (g0.Contains("gatl")) return "SmallGatlingGun"; if ((g0.Contains("launcher") && g0.
                                                                                                                     Contains("reload"))) return "SmallMissileLauncherReload"; if ((g0.Contains("launcher"))) return "SmallMissileLauncher"; if (g0.Contains(
                                                                                                                          "mass")) return "VirtualMass"; if (g0 == "warhead") return "Warhead"; if (g0.StartsWith("func")) return "FunctionalBlock"; if (g0 == "shipctrl"
                                                                                                                                    ) return "ShipController"; if (g0.StartsWith("light")) return "LightingBlock"; if (g0.StartsWith("contr")) return "ControlPanel"; if (g0.
                                                                                                                                            StartsWith("coc")) return "Cockpit"; if (g0.StartsWith("medi")) return "MedicalRoom"; if (g0.StartsWith("remote")) return "RemoteControl"
                                                                                                                                                   ; if (g0.StartsWith("but")) return "ButtonPanel"; if (g0.StartsWith("cam")) return "CameraBlock"; if (g0.Contains("detect")) return
                                                                                                                                                            "OreDetector"; return "Unknown";
            }
            public List<double> r5(IMyTerminalBlock g1, int g2 = -1)
            {
                var g3 = new List<double>(); string[] g4 = g1.
DetailedInfo.Split('\n'); int g5 = Math.Min(g4.Length, (g2 > 0 ? g2 : g4.Length)); for (int g6 = 0; g6 < g5; g6++)
                {
                    string[] g7 = g4[g6].Split(':'); if (
g7.Length < 2) { g7 = g4[g6].Split('r'); if (g7.Length < 2) g7 = g4[g6].Split('x'); }
                    var g8 = (g7.Length < 2 ? g7[0] : g7[1]); string[] g9 = g8.Trim().Split
(' '); var ga = g9[0].Trim(); var gb = (g9.Length > 1 && g9[1].Length > 1 ? g9[1][0] : '.'); double gc; if (Double.TryParse(ga, out gc))
                    {
                        double gd = gc *
Math.Pow(1000.0, ".kMGTPEZY".IndexOf(gb)); g3.Add(gd);
                    }
                }
                return g3;
            }
            public string r6(IMyBatteryBlock ge)
            {
                var gf = ""; if (ge.OnlyRecharge
) gf = "(+) ";
                else if (ge.OnlyDischarge) gf = "(-) "; else gf = "(±) "; return gf + qX.uv((ge.CurrentStoredPower / ge.MaxStoredPower) * 100.0f) + "%"
             ;
            }
            public string r7(IMyLaserAntenna gg) { string[] gh = gg.DetailedInfo.Split('\n'); return gh[gh.Length - 1].Split(' ')[0].ToUpper(); }
            public double r8(IMyJumpDrive gi, out double gj, out double gk)
            {
                gj = gi.CurrentStoredPower; gk = gi.MaxStoredPower; return (gk > 0 ? gj / gk * 100 :
0);
            }
            public double r9(IMyJumpDrive gl)
            {
                List<double> gm = r5(gl, 5); double gn = 0, go = 0; if (gm.Count < 4) return 0; gn = gm[1]; go = gm[3]; return (gn >
0 ? go / gn * 100 : 0);
            }
        }
        public class vl
        {
            public Dictionary<string, vm> ra = new Dictionary<string, vm>(); Dictionary<string, vm> rb = new
Dictionary<string, vm>(); public List<string> rc = new List<string>(); public Dictionary<string, vm> rd = new Dictionary<string, vm>(); public
void Add(string gp, string gq, int gr, string gs, string gt, bool gu)
            {
                if (gq == "Ammo") gq = "AmmoMagazine";
                else if (gq == "Tool") gq =
"PhysicalGunObject"; var gv = gp + ' ' + gq; vm gw = new vm(gp, gq, gr, gs, gt, gu); ra.Add(gv, gw); if (!rb.ContainsKey(gp)) rb.Add(gp, gw); if (gt != "")
                    rd.Add(gt.ToLower(), gw); rc.Add(gv);
            }
            public vm re(string gx = "", string gy = "")
            {
                if (ra.ContainsKey(gx + " " + gy)) return ra[gx + " " + gy]; if (
gy == "") { vm gz = null; rb.TryGetValue(gx, out gz); return gz; }
                if (gx == "") for (int gA = 0; gA < ra.Count; gA++)
                    {
                        vm gz = ra[rc[gA]]; if (gy == gz.rg)
                            return gz;
                    }
                return null;
            }
        }
        public class vm
        {
            public string rf; public string rg; public int rh; public string ri; public string rj; public
bool rk; public vm(string gC, string gD, int gE = 0, string gF = "", string gG = "", bool gH = true) { rf = gC; rg = gD; rh = gE; ri = gF; rj = gG; rk = gH; }
        }
        public class vn
        {
            readonly Dictionary<string, string> rl = new Dictionary<string, string>(){{"ingot","ingot" },{"ore","ore" },{
"component","component" },{"tool","physicalgunobject" },{"ammo","ammomagazine" },{"oxygen","oxygencontainerobject" },{"gas",
"gascontainerobject" }}; vu rm; vw rn; vp ro; vp rp; vp rq; vl MMItems; bool rr; public vp rs; public vn(vu gI, vw gJ, int gK = 20)
            {
                ro = new vp()
; rp = new vp(); rq = new vp(); rr = false; rs = new vp(); rm = gI; rn = gJ; MMItems = rn.MMItems;
            }
            public void rt()
            {
                rq.rQ(); rp.rQ(); ro.rQ(); rr = false; rs
.rQ();
            }
            public void ru(string gL, bool gM = false, int gN = 1, int gO = -1)
            {
                if (gL == "") { rr = true; return; }
                string[] gP = gL.Split(' '); var gQ = ""; vo
gR = new vo(gM, gN, gO); if (gP.Length == 2) { if (!rl.TryGetValue(gP[1], out gQ)) gQ = gP[1]; }
                var gS = gP[0]; if (rl.TryGetValue(gS, out gR.rH))
                {
                    rp.
rM(gR.rH, gR); return;
                }
                rn.us(ref gS, ref gQ); if (gQ == "") { gR.rG = gS.ToLower(); ro.rM(gR.rG, gR); return; }
                gR.rG = gS; gR.rH = gQ; rq.rM(gS.ToLower
() + ' ' + gQ.ToLower(), gR);
            }
            public vo rv(string gT, string gU, string gV)
            {
                vo gW; gT = gT.ToLower(); gW = rq.rO(gT); if (gW != null) return gW; gU =
gU.ToLower(); gW = ro.rO(gU); if (gW != null) return gW; gV = gV.ToLower(); gW = rp.rO(gV); if (gW != null) return gW; return null;
            }
            public bool rw(
string gX, string gY, string gZ)
            {
                vo g_; var h0 = false; g_ = rp.rO(gZ.ToLower()); if (g_ != null) { if (g_.rI) return true; h0 = true; }
                g_ = ro.rO(gY.
ToLower()); if (g_ != null) { if (g_.rI) return true; h0 = true; }
                g_ = rq.rO(gX.ToLower()); if (g_ != null) { if (g_.rI) return true; h0 = true; }
                return !(rr
|| h0);
            }
            public vo rx(string h1, string h2, string h3)
            {
                vo h4 = new vo(); h1 = h1.ToLower(); vo h5 = rv(h1, h2.ToLower(), h3.ToLower()); if (h5 !=
null) { h4.rE = h5.rE; h4.rF = h5.rF; }
                h4.rG = h2; h4.rH = h3; rs.rM(h1, h4); return h4;
            }
            public vo ry(string h6, string h7, string h8)
            {
                vo h9 = rs.rO(
h6.ToLower()); if (h9 == null) h9 = rx(h6, h7, h8); return h9;
            }
            int rz = 0; List<vo> rA; public List<vo> rB(string ha, bool hb, Func<vo, bool> hc = null)
            {
                if (!hb) { rA = new List<vo>(); rz = 0; }
                for (; rz < rs.rN(); rz++)
                {
                    if (!rm.t3(5)) return null; vo hd = rs.rP(rz); if (rw((hd.rG + ' ' + hd.rH).ToLower(),
hd.rG, hd.rH)) continue; if (hd.rH == ha && (hc == null || hc(hd))) rA.Add(hd);
                }
                return rA;
            }
            int rC = 0; public bool rD(bool he)
            {
                if (!he) { rC = 0; }
                for (;
rC < MMItems.rc.Count; rC++)
                {
                    if (!rm.t3(10)) return false; vm hf = MMItems.ra[MMItems.rc[rC]]; if (!hf.rk) continue; var hg = hf.rf + ' ' + hf.rg; if
          (rw(hg, hf.rf, hf.rg)) continue; vo hh = ry(hg, hf.rf, hf.rg); if (hh.rF == -1) hh.rF = hf.rh;
                }
                return true;
            }
        }
        public class vo
        {
            public int rE;
            public int rF; public string rG = ""; public string rH = ""; public bool rI; public double rJ; public vo(bool hi = false, int hj = 1, int hk = -1)
            {
                rE = hj; rI = hi; rF = hk;
            }
        }
        public class vp
        {
            Dictionary<string, vo> rK = new Dictionary<string, vo>(); List<string> rL = new List<string>(); public
void rM(string hl, vo hm)
            { if (!rK.ContainsKey(hl)) { rL.Add(hl); rK.Add(hl, hm); } }
            public int rN() { return rK.Count; }
            public vo rO(string
hn)
            { if (rK.ContainsKey(hn)) return rK[hn]; return null; }
            public vo rP(int ho) { return rK[rL[ho]]; }
            public void rQ()
            {
                rL.Clear(); rK.Clear(
);
            }
            public void rR() { rL.Sort(); }
        }
        public class vq
        {
            vu rS; vw rT; public MyDefinitionId rU = new MyDefinitionId(typeof(VRage.Game.
ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity"); public MyDefinitionId rV = new MyDefinitionId(typeof(VRage.
Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen"); public MyDefinitionId rW = new MyDefinitionId(typeof(VRage.
Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen"); public vq(vu hp, vw hq) { rS = hp; rT = hq; }
            int rX = 0; public
bool rY(List<IMyTerminalBlock> hr, ref double hs, ref double ht, ref double hu, ref double hv, ref double hw, ref double hx, bool hy)
            {
                if (!
hy) rX = 0; MyResourceSinkComponent hz; MyResourceSourceComponent hA; for (; rX < hr.Count; rX++)
                {
                    if (!rS.t3(8)) return false; if (hr[rX].
Components.TryGet<MyResourceSinkComponent>(out hz)) { hs += hz.CurrentInputByType(rU); ht += hz.MaxRequiredInputByType(rU); }
                    if (hr[rX].
Components.TryGet<MyResourceSourceComponent>(out hA)) { hu += hA.CurrentOutputByType(rU); hv += hA.MaxOutputByType(rU); }
                    hw += (hr[rX] as
IMyBatteryBlock).CurrentStoredPower; hx += (hr[rX] as IMyBatteryBlock).MaxStoredPower;
                }
                return true;
            }
            int rZ = 0; public bool r_(List<
IMyTerminalBlock> hB, MyDefinitionId hC, ref double hD, ref double hE, ref double hF, ref double hG, bool hH)
            {
                if (!hH) rZ = 0;
                MyResourceSinkComponent hI; MyResourceSourceComponent hJ; for (; rZ < hB.Count; rZ++)
                {
                    if (!rS.t3(6)) return false; if (hB[rZ].Components.
TryGet<MyResourceSinkComponent>(out hI)) { hD += hI.CurrentInputByType(hC); hE += hI.MaxRequiredInputByType(hC); }
                    if (hB[rZ].Components.
TryGet<MyResourceSourceComponent>(out hJ)) { hF += hJ.CurrentOutputByType(hC); hG += hJ.MaxOutputByType(hC); }
                }
                return true;
            }
            int s0 = 0;
            public bool s1(List<IMyTerminalBlock> hK, string hL, ref double hM, ref double hN, bool hO)
            {
                hL = hL.ToLower(); if (!hO) { s0 = 0; hN = 0; hM = 0; }
                MyResourceSinkComponent hP; for (; s0 < hK.Count; s0++)
                {
                    if (!rS.t3(30)) return false; IMyGasTank hQ = hK[s0] as IMyGasTank; if (hQ == null)
                        continue; double hR = 0; if (hQ.Components.TryGet<MyResourceSinkComponent>(out hP))
                    {
                        ListReader<MyDefinitionId> hS = hP.AcceptedResources;
                        int hT = 0; for (; hT < hS.Count; hT++) { if (hS[hT].SubtypeId.ToString().ToLower() == hL) { hR = hQ.Capacity; hN += hR; hM += hR * hQ.FilledRatio; break; } }
                    }
                }
                return true;
            }
            public string s2(TimeSpan hU)
            {
                var hV = ""; if (hU.Ticks <= 0) return "-"; if ((int)hU.TotalDays > 0) hV += (long)hU.TotalDays + " "
+ rT.u4.T("C5") + " "; if (hU.Hours > 0 || hV != "") hV += hU.Hours + "h "; if (hU.Minutes > 0 || hV != "") hV += hU.Minutes + "m "; return hV + hU.Seconds + "s";
            }
        }
        public class vr
        {
            vw s3 = null; public vs s4 = new vs(); public vv s5 = null; public IMyTextPanel s6 = null; public int s7 = 0; public string s8 =
          ""; public string s9 = ""; public bool sa = true; public vr(vw hW, string hX) { s3 = hW; s9 = hX; }
            public bool sb()
            {
                return s5.te.Count > s5.ta || s5.
tb != 0;
            }
            public void sc(float hY) { for (int hZ = 0; hZ < s4.ss(); hZ++) s4.su(hZ).SetValueFloat("FontSize", hY); }
            public void sd()
            {
                s4.sw(); s6 =
s4.su(0); int h_ = s6.CustomName.IndexOf("!MARGIN:"); if (h_ < 0 || h_ + 8 >= s6.CustomName.Length) { s7 = 1; s8 = " "; }
                else
                {
                    var i0 = s6.CustomName.
Substring(h_ + 8); int i1 = i0.IndexOf(" "); if (i1 >= 0) i0 = i0.Substring(0, i1); if (!int.TryParse(i0, out s7)) s7 = 1; s8 = new String(' ', s7);
                }
                if (
s6.CustomName.Contains("!NOSCROLL")) sa = false;
                else sa = true;
            }
            public bool se()
            {
                return (s6.BlockDefinition.SubtypeId.Contains("Wide") ||
s6.DefinitionDisplayNameText == "Computer Monitor");
            }
            float sf = 1.0f; bool sg = false; public float sh()
            {
                if (sg) return sf; sg = true; sf = (se() ?
2.0f : 1.0f); return sf;
            }
            float si = 1.0f; bool sj = false; public float sk()
            {
                if (sj) return si; sj = true; if (s6.BlockDefinition.SubtypeId.
Contains("Corner_LCD"))
                {
                    si = 0.15f; if (s6.BlockDefinition.SubtypeId.Contains("Flat")) si = 0.1765f; if (s6.BlockDefinition.SubtypeId.
    Contains("Small")) si *= 1.8f;
                }
                return si;
            }
            public void sl()
            {
                if (s5 == null || s6 == null) return; float i2 = s6.FontSize; var i3 = s6.Font; for (int
i4 = 0; i4 < s4.ss(); i4++)
                {
                    IMyTextPanel i5 = s4.su(i4);
#if !VERSION_185
                    i5.SetValue<Int64>("alignment", 0);

#endif
                    if (i4 > 0) { i5.FontSize = i2; i5.Font = i3; }
                    i5.WritePublicText(s5.tr(i4)); if (s3.tE) i5.ShowTextureOnScreen();
                    i5.ShowPublicTextOnScreen();
                }
            }
        }
        public class vs
        {
            Dictionary<string, IMyTextPanel> sm = new Dictionary<string, IMyTextPanel>(); Dictionary
<IMyTextPanel, string> sn = new Dictionary<IMyTextPanel, string>(); List<string> so = new List<string>(); public void sp(string i6,
IMyTextPanel i7)
            { if (!so.Contains(i6)) { so.Add(i6); sm.Add(i6, i7); sn.Add(i7, i6); } }
            public void sq(string i8)
            {
                if (so.Contains(i8))
                {
                    so.
Remove(i8); sn.Remove(sm[i8]); sm.Remove(i8);
                }
            }
            public void sr(IMyTextPanel i9)
            {
                if (sn.ContainsKey(i9))
                {
                    so.Remove(sn[i9]); sm.Remove(sn
[i9]); sn.Remove(i9);
                }
            }
            public int ss() { return sm.Count; }
            public IMyTextPanel st(string ia)
            {
                if (so.Contains(ia)) return sm[ia]; return
null;
            }
            public IMyTextPanel su(int ib) { return sm[so[ib]]; }
            public void sv() { so.Clear(); sm.Clear(); sn.Clear(); }
            public void sw()
            {
                so.
Sort();
            }
        }
        public class vt
        {
            public string sx = "MMTask"; public double sy = 0; public double sz = 0; public double sA = 0; public double sB = -1;
            public bool sC = false; public bool sD = false; double sE = 0; protected vu sF; public void sG(vu ic) { sF = ic; }
            protected bool sH(double id)
            {
                sE
= Math.Max(id, 0.0001); return true;
            }
            public bool sI()
            {
                if (sz > 0)
                {
                    sA = sF.sN - sz; sF.sV((sD ? "Running" : "Resuming") + " task: " + sx); sD = Run(!sD);
                }
                else { sA = 0; sF.sV("Init task: " + sx); Init(); sF.sV("Running.."); sD = Run(false); if (!sD) sz = 0.001; }
                if (sD)
                {
                    sz = sF.sN; if ((sB >= 0 || sE > 0) && sC)
                        sF.sT(this, (sE > 0 ? sE : sB));
                    else { sC = false; sz = 0; }
                }
                else { if (sC) sF.sT(this, 0, true); }
                sF.sV("Task " + (sD ? "" : "NOT ") + "finished. " + (sC ? (sE > 0 ?
"Postponed by " + sE.ToString("F1") + "s" : "Scheduled after " + sB.ToString("F1") + "s") : "Stopped.")); sE = 0; return sD;
            }
            public void sJ()
            {
                sF.
sU(this); End(); sC = false; sD = false; sz = 0;
            }
            public virtual void Init() { }
            public virtual bool Run(bool ie) { return true; }
            public virtual
void End()
            { }
        }
        public class vu
        {
            public double sN { get { return sP; } }
            int sO = 1000; double sP = 0; List<vt> sQ = new List<vt>(100); public
MyGridProgram sR; int sS = 0; public vu(MyGridProgram ig, int ih = 1) { sR = ig; sS = ih; }
            public void sT(vt ii, double ij, bool ik = false)
            {
                sV(
"Scheduling task: " + ii.sx + " after " + ij.ToString("F2")); ii.sC = true; ii.sG(this); if (ik) { ii.sy = sN; sQ.Insert(0, ii); return; }
                if (ij <= 0) ij =
0.001; ii.sy = sN + ij; for (int il = 0; il < sQ.Count; il++)
                {
                    if (sQ[il].sy > ii.sy) { sQ.Insert(il, ii); return; }
                    if (ii.sy - sQ[il].sy < 0.05) ii.sy = sQ[il]
.sy + 0.05;
                }
                sQ.Add(ii);
            }
            public void sU(vt im) { if (sQ.Contains(im)) { sQ.Remove(im); im.sC = false; } }
            public void sV(string io, int ip = 1)
            {
                if (
sS == ip) sR.Echo(io);
            }
            double sX = 0; public void sY() { sX += sR.Runtime.TimeSinceLastRun.TotalSeconds * (16.66666666 / 16); }
            public void sZ()
            {
                double iq = sR.Runtime.TimeSinceLastRun.TotalSeconds * (16.66666666 / 16) + sX; sX = 0; sP += iq; sV("Total time: " + sP.ToString("F1") +
                               " Time Step: " + iq.ToString("F2")); sO = (int)Math.Min((iq * 60) * 1000, 20000 - 1000); sV("Total tasks: " + sQ.Count + " InstrPerRun: " + sO); while
                                                   (sQ.Count >= 1)
                {
                    vt ir = sQ[0]; if (sO - sR.Runtime.CurrentInstructionCount <= 0) break; if (ir.sy > sP)
                    {
                        int it = (int)(60 * (ir.sy - sP)); if (it >= 100)
                        {
                            sR.Runtime.UpdateFrequency = UpdateFrequency.Update100;
                        }
                        else
                        {
                            if (it >= 10) sR.Runtime.UpdateFrequency = UpdateFrequency.Update10;
                            else sR.
Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        }
                        break;
                    }
                    sQ.Remove(ir); if (!ir.sI()) break; sV("Done. NextExecTime: " + ir.sy.ToString(
"F1")); sV("Remaining Instr: " + t2().ToString());
                }
            }
            int s_ = 0; StringBuilder t0 = new StringBuilder(); public void t1()
            {
                double iu = sR.
Runtime.LastRunTimeMs * 1000; if (s_ == 5000)
                {
                    IMyTextPanel iv = sR.GridTerminalSystem.GetBlockWithName("AUTOLCD Profiler") as IMyTextPanel;
                    if (iv == null) return; iv.WritePublicText(t0.ToString()); s_++; return;
                }
                t0.Append(s_).Append(";").AppendLine(iu.ToString("F2")); s_++;
            }
            public int t2() { return (20000 - sR.Runtime.CurrentInstructionCount); }
            public bool t3(int iw)
            {
                return ((20000 - sR.Runtime.
CurrentInstructionCount) >= iw);
            }
            public void t4() { sV("Remaining Instr: " + t2().ToString()); }
        }
        public class vv
        {
            vw t5 = null; public float
t6 = 1.0f; public string t7 = "Debug"; public float t8 = 1.0f; public float t9 = 1.0f; public int ta = 17; public int tb = 0; int tc = 1; int td = 1;
            public List<string> te = new List<string>(); public int tf = 0; public float tg = 0; public vv(vw ix, float iy = 1.0f)
            {
                t5 = ix; th(iy); te.Add("");
            }
            public void th(float iz) { t6 = iz; }
            public void ti(int iA) { td = iA; }
            public void tj() { ta = (int)Math.Floor(vw.tx * t9 * td / vw.tz); }
            public void
tk(string iB)
            { te[tf] += iB; }
            public void tl(List<string> iC)
            {
                if (te[tf] == "") te.RemoveAt(tf); else tf++; te.AddList(iC); tf += iC.Count; te.
Add(""); tg = 0;
            }
            public List<string> tm() { if (te[tf] == "") return te.GetRange(0, tf); else return te; }
            public void tn(string iD, string iE = ""
)
            { string[] iF = iD.Split('\n'); for (int iG = 0; iG < iF.Length; iG++) to(iE + iF[iG]); }
            public void to(string iH)
            {
                te[tf] += iH; te.Add(""); tf++; tg =
0;
            }
            public void tp() { te.Clear(); te.Add(""); tg = 0; tf = 0; }
            public string tq() { return String.Join("\n", te); }
            public string tr(int iI = 0)
            {
                if
(te.Count <= iI * ta / td) return ""; if (te.Count <= ta / td) { tb = 0; tc = 1; return tq(); }
                int iJ = tb + iI * (ta / td); if (iJ > te.Count) iJ = te.Count; List<
string> iK = te.GetRange(iJ, Math.Min(te.Count - iJ, ta / td)); return String.Join("\n", iK);
            }
            public bool ts(int iL = -1)
            {
                if (iL <= 0) iL = t5.tB; if (
tb - iL <= 0) { tb = 0; return true; }
                tb -= iL; return false;
            }
            public bool tt(int iM = -1)
            {
                if (iM <= 0) iM = t5.tB; int iN = te.Count - 1; if (tb + iM + ta >= iN)
                {
                    tb
= Math.Max(iN - ta, 0); return true;
                }
                tb += iM; return false;
            }
            public int tu = 0; public void tv()
            {
                if (tu > 0) { tu--; return; }
                if (te.Count - 1 <= ta)
                {
                    tb =
0; tc = 1; return;
                }
                if (tc > 0) { if (tt()) { tc = -1; tu = 2; } } else { if (ts()) { tc = 1; tu = 2; } }
            }
        }
        public class vw
        {
            public const float tx = 512 / 0.7783784f;
            public const float ty = 512 / 0.7783784f; public const float tz = 37; public string tA = "T:[LCD]"; public int tB = 1; public bool tC = true;
            public List<string> tD = null; public bool tE = true; public int tF = 0; public float tG = 1.0f; public float tH = 1.0f; public float tI
            {
                get
                {
                    return ty * u8.t8;
                }
            }
            public float tJ { get { return (float)tI - 2 * tR[u9] * tU; } }
            string tK; string tL; float tM = -1; Dictionary<string, float> tN = new
Dictionary<string, float>(2); Dictionary<string, float> tO = new Dictionary<string, float>(2); Dictionary<string, float> tP = new Dictionary<
string, float>(2); public float tQ { get { return tP[u9]; } }
            Dictionary<string, float> tR = new Dictionary<string, float>(2); Dictionary<string,
float> tS = new Dictionary<string, float>(2); Dictionary<string, float> tT = new Dictionary<string, float>(2); int tU = 0; string tV = "";
            Dictionary<string, char> tW = new Dictionary<string, char>(2); Dictionary<string, char> tX = new Dictionary<string, char>(2); Dictionary<
                        string, char> tY = new Dictionary<string, char>(2); Dictionary<string, char> tZ = new Dictionary<string, char>(2); vu t_; public MyGridProgram
                                     u0; public vq u1; public vk u2; public vi u3; public vl MMItems; public TranslationTable u4; public IMyGridTerminalSystem u5
            {
                get
                {
                    return
u0.GridTerminalSystem;
                }
            }
            public IMyProgrammableBlock u6 { get { return u0.Me; } }
            public Action<string> u7 { get { return u0.Echo; } }
            public vw(
MyGridProgram iO, int iP, vu iQ)
            {
                t_ = iQ; tF = iP; u0 = iO; u4 = new TranslationTable(); u1 = new vq(iQ, this); u2 = new vk(iQ, this); u2.r0(); u3 = new vi
      (t_); t_.sT(u3, 0);
            }
            vv u8 = null; public string u9 { get { return u8.t7; } }
            public bool ua { get { return !(u8.tf > 0 && u8.te[0] != ""); } }
            public vv ub(
vv iR, vr iS)
            {
                iS.sd(); IMyTextPanel iT = iS.s6; if (iR == null) iR = new vv(this, iT.FontSize); else iR.th(iT.FontSize); iR.t7 = iS.s6.Font; if (!tR
                   .ContainsKey(iR.t7)) iR.t7 = tK; iR.ti(iS.s4.ss()); iR.t8 = iS.sh() * tG / iR.t6; iR.t9 = iS.sk() * tH / iR.t6; iR.tj(); tV = iS.s8; tU = iS.s7; u8 = iR;
                return iR;
            }
            public void uc(vr iU) { u8 = iU.s5; }
            public void ud(string iV) { if (u8.tg <= 0) iV = tV + iV; u8.to(iV); }
            public void ue(string iW)
            {
                u8.
tn(iW, tV);
            }
            public void uf(List<string> iX) { u8.tl(iX); }
            public void Add(string iY)
            {
                if (u8.tg <= 0) iY = tV + iY; u8.tk(iY); u8.tg += uy(iY, u8.t7)
;
            }
            public void ug(string iZ, float i_ = 1.0f, float j0 = 0f) { uh(iZ, i_, j0); ud(""); }
            public void uh(string j1, float j2 = 1.0f, float j3 = 0f)
            {
                float j4 = uy(j1, u8.t7); float j5 = j2 * ty * u8.t8 - u8.tg - j3; if (tU > 0) j5 -= tR[u8.t7] * tU; if (j5 < j4) { u8.tk(j1); u8.tg += j4; return; }
                j5 -= j4; int j6 = (
int)Math.Floor(j5 / tR[u8.t7]); float j7 = j6 * tR[u8.t7]; u8.tk(new String(' ', j6) + j1); u8.tg += j7 + j4;
            }
            public void ui(string j8)
            {
                uj(j8); ud(
"");
            }
            public void uj(string j9)
            {
                float ja = uy(j9, u8.t7); float jb = ty / 2 * u8.t8 - u8.tg; if (jb < ja / 2) { u8.tk(j9); u8.tg += ja; return; }
                jb -= ja / 2;
                int jc = (int)Math.Round(jb / tR[u8.t7], MidpointRounding.AwayFromZero); float jd = jc * tR[u8.t7]; u8.tk(new String(' ', jc) + j9); u8.tg += jd + ja
                               ;
            }
            public void uk(double je, float jf = 1.0f, float jg = 0f)
            {
                if (tU > 0) jg += tU * tR[u8.t7] * ((u8.tg <= 0) ? 2 : 1); float jh = ty * jf * u8.t8 - u8.tg - jg; if (
Double.IsNaN(je)) je = 0; int ji = (int)(jh / tS[u8.t7]) - 2; if (ji <= 0) ji = 2; int jj = Math.Min((int)(je * ji) / 100, ji); if (jj < 0) jj = 0; u8.to((u8.tg <= 0
                       ? tV : "") + tW[u8.t7] + new String(tZ[u8.t7], jj) + new String(tY[u8.t7], ji - jj) + tX[u8.t7]);
            }
            public void ul(double jk, float jl = 1.0f, float jm
= 0f)
            {
                if (tU > 0) jm += tU * tR[u8.t7] * ((u8.tg <= 0) ? 2 : 1); float jn = ty * jl * u8.t8 - u8.tg - jm; if (Double.IsNaN(jk)) jk = 0; int jo = (int)(jn / tS[u8.t7]) - 2
                                            ; if (jo <= 0) jo = 2; int jp = Math.Min((int)(jk * jo) / 100, jo); if (jp < 0) jp = 0; u8.tk((u8.tg <= 0 ? tV : "") + tW[u8.t7] + new String(tZ[u8.t7], jp) + new
                                                                   String(tY[u8.t7], jo - jp) + tX[u8.t7]); u8.tg += (u8.tg <= 0 ? tU * tR[u8.t7] : 0) + tS[u8.t7] * jo + 2 * tT[u8.t7];
            }
            public void um() { u8.tp(); }
            public
void un(vr jq)
            { jq.sl(); if (jq.sa) u8.tv(); }
            public void uo(string jr, string js)
            {
                IMyTextPanel jt = u0.GridTerminalSystem.
GetBlockWithName(jr) as IMyTextPanel; if (jt == null) return; jt.WritePublicText(js + "\n", true);
            }
            public string up(IMyInventoryItem ju)
            {
                var
jv = ju.Content.TypeId.ToString(); jv = jv.Substring(jv.LastIndexOf('_') + 1); return ju.Content.SubtypeId + " " + jv;
            }
            public void uq(string
jw, out string jx, out string jy)
            {
                int jz = jw.LastIndexOf(' '); if (jz >= 0) { jx = jw.Substring(0, jz); jy = jw.Substring(jz + 1); return; }
                jx = jw; jy =
"";
            }
            public string ur(string jA) { string jB, jC; uq(jA, out jB, out jC); return ur(jB, jC); }
            public string ur(string jD, string jE)
            {
                vm jF =
MMItems.re(jD, jE); if (jF != null) { if (jF.ri != "") return jF.ri; return jF.rf; }
                return System.Text.RegularExpressions.Regex.Replace(jD,
"([a-z])([A-Z])", "$1 $2");
            }
            public void us(ref string jG, ref string jH)
            {
                var jI = jG.ToLower(); vm jJ; if (MMItems.rd.TryGetValue(jI, out
jJ)) { jG = jJ.rf; jH = jJ.rg; return; }
                jJ = MMItems.re(jG, jH); if (jJ != null) { jG = jJ.rf; if (jH == "Ore" || jH == "Ingot") return; jH = jJ.rg; }
            }
            public
string ut(double jK, bool jL = true, char jM = ' ')
            {
                if (!jL) return jK.ToString("#,###,###,###,###,###,###,###,###,###"); var jN =
" kMGTPEZY"; double jO = jK; int jP = jN.IndexOf(jM); int jQ = (jP < 0 ? 0 : jP); while (jO >= 1000 && jQ + 1 < jN.Length) { jO /= 1000; jQ++; }
                var jR = Math.Round
(jO, 1, MidpointRounding.AwayFromZero).ToString(); if (jQ > 0) jR += " " + jN[jQ]; return jR;
            }
            public string uu(double jS, bool jT = true, char jU =
' ')
            {
                if (!jT) return jS.ToString("#,###,###,###,###,###,###,###,###,###"); var jV = " ktkMGTPEZY"; double jW = jS; int jX = jV.IndexOf(jU);
                int jY = (jX < 0 ? 0 : jX); while (jW >= 1000 && jY + 1 < jV.Length) { jW /= 1000; jY++; }
                var jZ = Math.Round(jW, 1, MidpointRounding.AwayFromZero).ToString()
; if (jY == 1) jZ += " kg"; else if (jY == 2) jZ += " t"; else if (jY > 2) jZ += " " + jV[jY] + "t"; return jZ;
            }
            public string uv(double j_)
            {
                return (Math.
Floor(j_ * 10) / 10).ToString("F1");
            }
            Dictionary<char, float> uw = new Dictionary<char, float>(); void AddCharsSize(string k0, float k1)
            {
                k1 += 1
; for (int k2 = 0; k2 < k0.Length; k2++) { if (k1 > tN[tK]) tN[tK] = k1; uw.Add(k0[k2], k1); }
            }
            public float ux(char k3, string k4)
            {
                float k5; if (k4 == tL
|| !uw.TryGetValue(k3, out k5)) return tN[k4]; return k5;
            }
            public float uy(string k6, string k7)
            {
                if (k7 == tL) return k6.Length * tN[k7]; float
k8 = 0; for (int k9 = 0; k9 < k6.Length; k9++) k8 += ux(k6[k9], k7); return k8;
            }
            public string uz(string ka, float kb)
            {
                if (kb / tN[u8.t7] >= ka.Length)
                    return ka; float kc = uy(ka, u8.t7); if (kc <= kb) return ka; float kd = kc / ka.Length; kb -= tO[u8.t7]; int ke = (int)Math.Max(kb / kd, 1); if (ke < ka.
                                            Length / 2) { ka = ka.Remove(ke); kc = uy(ka, u8.t7); }
                else { ke = ka.Length; } while (kc > kb && ke > 1) { ke--; kc -= ux(ka[ke], u8.t7); }
                if (ka.Length > ke) ka = ka
.Remove(ke); return ka + "..";
            }
            void SetupClassicFont(string kf)
            {
                tK = kf; tW[tK] = MMStyle.BAR_START; tX[tK] = MMStyle.BAR_END; tY[tK] = MMStyle.
BAR_EMPTY; tZ[tK] = MMStyle.BAR_FILL; tN[tK] = 0f;
            }
            void SetupMonospaceFont(string kg, float kh)
            {
                tL = kg; tM = kh; tN[tL] = tM + 1; tO[tL] = 2 * (tM + 1);
                tW[tL] = MMStyle.BAR_MONO_START; tX[tL] = MMStyle.BAR_MONO_END; tY[tL] = MMStyle.BAR_MONO_EMPTY; tZ[tL] = MMStyle.BAR_MONO_FILL; tR[tL] = ux(' '
                            , tL); tS[tL] = ux(tY[tL], tL); tT[tL] = ux(tW[tL], tL); tP[tL] = uy(" 100.0%", tL);
            }
            public void uA()
            {
                if (uw.Count > 0) return;


                // Monospace font name, width of single character
                // Change this if you want to use different (modded) monospace font
                SetupMonospaceFont("Monospace", 24f);

                // Classic/Debug font name (uses widths of characters below)
                // Change this if you want to use different font name (non-monospace)
                SetupClassicFont("Debug");
                // Font characters width (font "aw" values here)
                AddCharsSize("3FKTabdeghknopqsuy£µÝàáâãäåèéêëðñòóôõöøùúûüýþÿāăąďđēĕėęěĝğġģĥħĶķńņňŉōŏőśŝşšŢŤŦũūŭůűųŶŷŸșȚЎЗКЛбдекруцяёђћўџ", 17f);
                AddCharsSize("ABDNOQRSÀÁÂÃÄÅÐÑÒÓÔÕÖØĂĄĎĐŃŅŇŌŎŐŔŖŘŚŜŞŠȘЅЊЖф□", 21f);
                AddCharsSize("#0245689CXZ¤¥ÇßĆĈĊČŹŻŽƒЁЌАБВДИЙПРСТУХЬ€", 19f);
                AddCharsSize("￥$&GHPUVY§ÙÚÛÜÞĀĜĞĠĢĤĦŨŪŬŮŰŲОФЦЪЯжы†‡", 20f);
                AddCharsSize("！ !I`ijl ¡¨¯´¸ÌÍÎÏìíîïĨĩĪīĮįİıĵĺļľłˆˇ˘˙˚˛˜˝ІЇії‹›∙", 8f);
                AddCharsSize("？7?Jcz¢¿çćĉċčĴźżžЃЈЧавийнопсъьѓѕќ", 16f);
                AddCharsSize("（）：《》，。、；【】(),.1:;[]ft{}·ţťŧț", 9f);
                AddCharsSize("+<=>E^~¬±¶ÈÉÊË×÷ĒĔĖĘĚЄЏЕНЭ−", 18f);
                AddCharsSize("L_vx«»ĹĻĽĿŁГгзлхчҐ–•", 15f);
                AddCharsSize("\"-rª­ºŀŕŗř", 10f);
                AddCharsSize("WÆŒŴ—…‰", 31f);
                AddCharsSize("'|¦ˉ‘’‚", 6f);
                AddCharsSize("@©®мшњ", 25f);
                AddCharsSize("mw¼ŵЮщ", 27f);
                AddCharsSize("/ĳтэє", 14f);
                AddCharsSize("\\°“”„", 12f);
                AddCharsSize("*²³¹", 11f);
                AddCharsSize("¾æœЉ", 28f);
                AddCharsSize("%ĲЫ", 24f);
                AddCharsSize("MМШ", 26f);
                AddCharsSize("½Щ", 29f);
                AddCharsSize("ю", 23f);
                AddCharsSize("ј", 7f);
                AddCharsSize("љ", 22f);
                AddCharsSize("ґ", 13f);
                AddCharsSize("™", 30f);
                // End of font characters width
                tR[tK] = ux(' ', tK); tS[tK] = ux(tY[tK], tK); tT[tK] = ux(tW[tK], tK); tP[tK] = uy(" 100.0%", tK); tO[tK] = ux('.', tK
                               ) * 2;
            }
        }

        public class TranslationTable
        {
            public string T(string msgid) { return TT[msgid]; }

            readonly Dictionary<string, string> TT = new Dictionary<string, string>
{
// TRANSLATION STRINGS
// msg id, text
{ "AC1", "Acceleration:" },
// amount
{ "A1", "EMPTY" },
{ "ALT1", "Altitude:"},
{ "ALT2", "Ground:"},
{ "B1", "Booting up..." },
{ "C1", "count:" },
{ "C2", "Cargo Used:" },
{ "C3", "Invalid countdown format, use:" },
{ "C4", "EXPIRED" },
{ "C5", "days" },
// customdata
{ "CD1", "Block not found: " },
{ "CD2", "Missing block name" },
{ "D1", "You need to enter name." },
{ "D2", "No blocks found." },
{ "D3", "No damaged blocks found." },
{ "DO1", "No connectors found." }, // NEW
{ "DTU", "Invalid GPS format" },
{ "GA", "Artif."}, // (not more than 5 characters)
{ "GN", "Natur."}, // (not more than 5 characters)
{ "GT", "Total"}, // (not more than 5 characters)
{ "G1", "Total Gravity:"},
{ "G2", "Natur. Gravity:"},
{ "G3", "Artif. Gravity:"},
{ "GNC", "No cockpit!"},
{ "H1", "Write commands to Custom Data of this panel." },
// inventory
{ "I1", "ore" },
{ "I2", "summary" },
{ "I3", "Ores" },
{ "I4", "Ingots" },
{ "I5", "Components" },
{ "I6", "Gas" },
{ "I7", "Ammo" },
{ "I8", "Tools" },
{ "M1", "Cargo Mass:" },
// oxygen
{ "O1", "Leaking" },
{ "O2", "Oxygen Farms" },
{ "O3", "No oxygen blocks found." },
{ "O4", "Oxygen Tanks" },
// position
{ "P1", "Block not found" },
{ "P2", "Location" },
// power
{ "P3", "Stored" },
{ "P4", "Output" },
{ "P5", "Input" },
{ "P6", "No power source found!" },
{ "P7", "Batteries" },
{ "P8", "Total Output" },
{ "P9", "Reactors" },
{ "P10", "Solars" },
{ "P11", "Power" },
{ "PT1", "Power Time:" },
{ "PT2", "Charge Time:" },
{ "PU1", "Power Used:" },
{ "S1", "Speed:" },
{ "SM1", "Ship Mass:" },
{ "SM2", "Ship Base Mass:" },
{ "SD", "Stop Distance:" },
{ "ST", "Stop Time:" },
// text
{ "T1", "Source LCD not found: " },
{ "T2", "Missing source LCD name" },
{ "T3", "LCD Private Text is empty" },
// tanks
{ "T4", "Missing tank type. eg: 'Tanks * Hydrogen'" },
{ "T5", "No {0} tanks found." }, // {0} is tank type
{ "T6", "Tanks" },
{ "UC", "Unknown command" },
// occupied & dampeners
{ "SC1", "Cannot find control block." },
{ "SCD", "Dampeners: " },
{ "SCO", "Occupied: " },
// working
{ "W1", "OFF" },
{ "W2", "WORK" },
{ "W3", "IDLE" },
{ "W4", "LEAK" },
{ "W5", "OPEN" },
{ "W6", "CLOSED" },
{ "W7", "LOCK" },
{ "W8", "UNLOCK" },
{ "W9", "ON" },
{ "W10", "READY" }
};
        }
    }
}