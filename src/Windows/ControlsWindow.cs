using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace RomExchangeScanner.src.Windows
{
    public class ControlsWindow : Window
    {

        RadioGroup statusGroup;
        Button btnSkipItem;
        Button btnRestartClient;

        public ControlsWindow() : base("Controls")
        {
            X = Pos.Right(Program.log);
            Y = Pos.Bottom(Program.status);
            Width = Dim.Fill();
            Height = Dim.Fill();


            Add(
                btnRestartClient = new Button(1, 1, "Restart client"),
                btnSkipItem = new Button(1, 2, "Skip item"),
                statusGroup = new RadioGroup(1, 3, new[] { "_Idle", "_Rare Item", "_Equip" })
                );;


            btnSkipItem.Clicked = () => { Program.CancelScan = true; };
            btnRestartClient.Clicked = async () =>
            {
                Program.status.SetStatus("Initializing restart", "");
                Program.CancelScan = true;
                Program.Restart = true;
                await Task.Delay(5000);
            };

            statusGroup.SelectionChanged += OnStatusChange;
        }


        public void SetStatus(Program.Status status)
        {
            Program.Invoke(() =>
            {
                if (status == Program.Status.Idle)
                    statusGroup.Selected = 0;
                else if (status == Program.Status.Rare)
                    statusGroup.Selected = 1;
                else if (status == Program.Status.Equip)
                    statusGroup.Selected = 2;
            });
        }

        private void OnStatusChange(int obj)
        {
            if (Program.CancelScan)
                return;
            if (statusGroup.Selected == 0)
                Program._CurrentStatus = Program.Status.Idle;
            else if (statusGroup.Selected == 1)
                Program._CurrentStatus = Program.Status.Rare;
            else if (statusGroup.Selected == 2)
                Program._CurrentStatus = Program.Status.Equip;
        }
    }
}
