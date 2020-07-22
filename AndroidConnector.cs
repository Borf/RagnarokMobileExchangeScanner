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

        public async Task Swipe(int x1, int y1, int x2, int y2, int duration = 500)
        {
            await Process.Start("adb.exe", $"{Host} shell input swipe {x1} {y1} {x2} {y2} {duration}").WaitForExitAsync();
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

        public async Task StopRo()
        {
            Process.Start("adb.exe", $"connect {Host.Substring(Host.IndexOf(" ")+1)}").WaitForExit();

            await Process.Start("adb.exe", $"{Host} shell am force-stop com.gravity.romEUg").WaitForExitAsync();
        }

        public async Task StartRo()
        {
            await Process.Start("adb.exe", $"{Host} shell monkey -p com.gravity.romEUg -c android.intent.category.LAUNCHER 1").WaitForExitAsync();
        }


    }
}
