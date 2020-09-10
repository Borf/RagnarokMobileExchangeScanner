using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace RomExchangeScanner.src.Windows
{
    public class StatusWindow : Window
    {
        public Label Status { get; set; }
        public Label SubStatus { get; set; }
        public Label Item { get; set; }

        public StatusWindow() : base("Status")
        {
            X = Pos.Right(Program.log);
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Percent(50);


            Add(
                new Label(1, 1, "Status: "),
                Status = new Label(20, 1, "-"),
                new Label(1, 2, "Substatus: "),
                SubStatus = new Label(20, 2, "-"),
                new Label(1, 3, "Item Scanning"),
                Item = new Label(20, 3, "-"),
                new Label(1, 4, "Last Error"),
                new Label(20, 4, "-"),
                new Label(1, 5, "Progress"),
                new Label(20, 5, "10%"),
                new Label(1, 6, "Error count"),
                new Label(20, 6, "0")
            );
        }

        internal void SetStatus(string status, string subStatus)
        {
            Program.Invoke(() =>
            {
                Status.Text = status;
                SubStatus.Text = subStatus;
            });
        }
        internal void SetSubStatus(string subStatus)
        {
            Program.Invoke(() =>
            {
                SubStatus.Text = subStatus;
            });
        }
        internal void SetItem(string item)
        {
            Program.Invoke(() =>
            {
                Item.Text = item;
            });
        }
    }
}
