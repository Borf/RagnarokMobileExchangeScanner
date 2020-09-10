using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace RomExchangeScanner.src.Windows
{
    public class ControlsWindow : Window
    {
        public ControlsWindow() : base("Controls")
        {
            X = Pos.Right(Program.log);
            Y = Pos.Bottom(Program.status);
            Width = Dim.Fill();
            Height = Dim.Fill();


            Add(
                new Button(1, 1, "Restart client"),
                new Button(1, 2, "Skip item"),
                new RadioGroup(1, 3, new[] { "_Rare Item", "_Equip" })
                );

        }
    }
}
