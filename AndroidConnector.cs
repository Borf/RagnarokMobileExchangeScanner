using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RomExchangeScanner
{
    class AndroidConnector
    {
        private string Host;
        public AndroidConnector(string host)
        {
            Process.Start("adb.exe", $"connect {host}").WaitForExit();
            Host = "-s " + host;
        }


        public async Task Tap(int x, int y)
        {
            await Process.Start("adb.exe", $"{Host} shell input tap {x} {y}").WaitForExitAsync();
        }

        public async Task Text(string text)
        {
            text = text
                .Replace(" ", "%s")
                .Replace("\'", "\\\'"); ;
            await Process.Start("adb.exe", $"{Host} shell input text {text}").WaitForExitAsync();
        }

        public async Task Screenshot(string fileName = "screencap.png")
        {
            await Process.Start("adb.exe", $"{Host} shell screencap -p /sdcard/{fileName}").WaitForExitAsync();
            var pull = new Process();
            pull.StartInfo.RedirectStandardOutput = true;
            pull.StartInfo.FileName = "adb.exe";
            pull.StartInfo.Arguments = $"{Host} pull /sdcard/{fileName}";
            pull.Start();
            await pull.WaitForExitAsync();
            await Process.Start("adb.exe", $"{Host} shell rm /sdcard/{fileName}").WaitForExitAsync();
        }

    }
}
