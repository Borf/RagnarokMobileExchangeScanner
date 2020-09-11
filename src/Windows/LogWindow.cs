using NStack;
using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace RomExchangeScanner.src.Windows
{
    public class LogWindow : Window
    {
        ListView items;
        TextView logView;

        Dictionary<string, string> logMessages = new Dictionary<string, string>();
        List<string> keys = new List<string>();

      
        public LogWindow() : base("Log")
        {
            X = 0;
            Y = 1;
            Width = Dim.Percent(60);
            Height = Dim.Fill();

            Add(items = new ListView(keys)
            {
                X = 0,
                Y = 0,
                Width = Dim.Sized(20),
                Height = Dim.Fill(),
                CanFocus = true,
            });
            items.SelectedChanged += () => {
                Console.WriteLine("CHANGE!");
            };

            logView = new TextView()
            {
                X = 20,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = false
            };
            Add(logView);


            keys.Add("Test");
        }


        public void Log(string item, string text)
        {
            Program.Invoke(() =>
            {

                if (logMessages.ContainsKey(item))
                    logMessages[item] += text + "\n";
                else
                    logMessages[item] = text + "\n";

                int index = keys.IndexOf(item);
                if (index == -1)
                    keys.Insert(0, item);
                else if (index > 0)
                {
                    keys.RemoveAt(index);
                    keys.Insert(0, item);
                }

                items.SelectedItem = 0;
                items.Redraw(items.Frame);
                logView.Text = logMessages[item];
            });
            
        }


    }
}
