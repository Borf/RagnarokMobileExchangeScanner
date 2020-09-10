﻿using RomExchangeScanner.src.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Tesseract;

namespace RomExchangeScanner
{
    class Program
    {
        static string ApiEndPoint = "http://zenybus.borf.nl";

        public static AndroidConnector androidConnection;
        public static Scanner scanner;

        public static LogWindow log { get; set; }
        public static StatusWindow status { get; set; }
        public static ControlsWindow controls { get; set; }

        static void Main(string[] args)
        {
            string hostname = "10.10.0.32:1234";
            if (args.Length > 0)
                hostname = args[0];


            Application.Init();
            var top = Application.Top;
            top.Add(log = new LogWindow());
            top.Add(status = new StatusWindow());
            top.Add(controls = new ControlsWindow());



            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_New", "", null),
                    new MenuItem ("_Close", "", null),
                    new MenuItem ("_Quit", "", null)
                }),
                new MenuBarItem ("_Edit", new MenuItem [] {
                    new MenuItem ("_Copy", "", null),
                    new MenuItem ("C_ut", "", null),
                    new MenuItem ("_Paste", "", null)
                })
            });
            top.Add(menu);

            androidConnection = new AndroidConnector(hostname);
            using (scanner = new Scanner())
            {
                Task.Run(RunScanner);
                Application.Run();
            }

            return;

          /*  using (Scanner scanner = new Scanner())
            {
                ScanInfo scanInfo = new ScanInfo()
                {
                    RealName = "Gakkung Bow [1]",
                    SearchName = "Gakkung Bow",
                    SearchIndex = 0,
                    Override = true
                };

                Stopwatch sw = new Stopwatch();
                sw.Start();
                List<ScanResultEquip> exchangeInfo = scanner.ScanEquip(androidConnection, scanInfo).Result;
                sw.Stop();
                Console.WriteLine($"Found Item in {sw.Elapsed.TotalSeconds} seconds\n{exchangeInfo}\n");

                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    id = 1,
                    items = exchangeInfo
                }));

            }

            return;*/

            /*using (Scanner scanner = new Scanner())
            {
                ScanInfo scanInfo = new ScanInfo()
                {
                    RealName = "Archer Skeleton ★Card",
                    SearchName = "Archer Skeleton",
                    SearchIndex = 0,
                    Override = false
                };

                Stopwatch sw = new Stopwatch();
                sw.Start();
                ExchangeInfo exchangeInfo = scanner.ScanRareItem(androidConnection, scanInfo).Result;
                sw.Stop();
                Console.WriteLine($"Found Item in {sw.Elapsed.TotalSeconds} seconds\n{exchangeInfo}\n");
                Console.WriteLine(exchangeInfo);

            }
            return;*/
        }

        public static void Invoke(Action action)
        {
            Application.MainLoop.Invoke(action);
        }

        private async static void RunScanner()
        {
            status.SetStatus("Starting up", "Checking if exchange is open");
            if (!await scanner.IsExchangeOpen(androidConnection))
            {
                status.SetStatus("Restarting RO", "");
                await scanner.RestartRo(androidConnection);
                status.SetStatus("Opening Exchange", "");
                await scanner.OpenExchange(androidConnection, 0);
            }


            int errorCount = 0;
            int majorErrorcount = 0;

            using (HttpClient client = new HttpClient())
            {
                while (true)
                {
                    Program.status.SetStatus("Finding new item to scan","");
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiEndPoint}/api/scanner/nextscanequip");
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("User-Agent", "BorfRoScanner");
                    var response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using var responseStream = await response.Content.ReadAsStreamAsync();
                        var options = new JsonSerializerOptions
                        {
                            IgnoreNullValues = true
                        };
                        var data = await JsonSerializer.DeserializeAsync<NextScanItemResponse>(responseStream, options);

                        Program.status.SetStatus("Scanning item", "");
                        Program.status.SetItem(data.name);

                        ScanInfo scanInfo = new ScanInfo()
                        {
                            RealName = data.name,
                            SearchName = data.scanname,
                            SearchIndex = data.scanindex,
                            Override = data.@override
                        };
                        if (data.scanname == "")
                        {
                            scanInfo.SearchName = data.name;
                            if (scanInfo.SearchName.Contains("["))
                                scanInfo.SearchName = scanInfo.SearchName.Substring(0, scanInfo.SearchName.IndexOf("["));
                        }


                        if (data.type.ToLower().StartsWith("equipment"))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            List<ScanResultEquip> exchangeInfo = await scanner.ScanEquip(androidConnection, scanInfo);
                            sw.Stop();
                            Console.WriteLine($"Found Item in {sw.Elapsed.TotalSeconds} seconds\n{exchangeInfo}\n");
                            bool error = exchangeInfo.Any(e => e.Error);

                            request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndPoint}/api/scanner/result");
                            request.Headers.Add("Accept", "application/json");
                            request.Headers.Add("User-Agent", "BorfRoScanner");
                            request.Content = new StringContent(JsonSerializer.Serialize(new
                            {
                                data.id,
                                error = error,
                                results = exchangeInfo
                            }), Encoding.UTF8, "application/json");
                            try
                            {
                                response = await client.SendAsync(request);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        else
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            ScanResultItem exchangeInfo = await scanner.ScanRareItem(androidConnection, scanInfo);
                            sw.Stop();
                            Console.WriteLine($"Found Item in {sw.Elapsed.TotalSeconds} seconds\n{exchangeInfo}\n");
                            request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndPoint}/api/scanner/result");
                            request.Headers.Add("Accept", "application/json");
                            request.Headers.Add("User-Agent", "BorfRoScanner");
                            request.Content = new StringContent(JsonSerializer.Serialize(new
                            {
                                data.id,
                                price = exchangeInfo.Price,
                                amount = exchangeInfo.Amount,
                                error = exchangeInfo.Error,
                                errormsg = exchangeInfo.ScanInfo.Message,
                                snapping = exchangeInfo.Snapping,
                                snapTime = exchangeInfo.SnapTime,
                                ScanIndex = exchangeInfo.ScanInfo.SearchIndex
                            }), Encoding.UTF8, "application/json");

                            response = await client.SendAsync(request);
                            errorCount = 0;
                            majorErrorcount = 0;
                            if (exchangeInfo.Error)
                            {
                                errorCount++;
                                Console.WriteLine("Error scanning card!");
                                Console.WriteLine(exchangeInfo.ScanInfo.Message);
                            }
                        }


                    }


                    if (errorCount > 10)
                    {
                        if (majorErrorcount > 2)
                        {
                            Console.WriteLine("Too many errors, stopping app");
                            break;
                        }
                        Console.WriteLine("Too many errors, restarting");
                        scanner.RestartRo(androidConnection).Wait();
                        scanner.OpenExchange(androidConnection, 0).Wait();
                        majorErrorcount++;
                        errorCount = 0;
                    }


                }
            }
        }

        class NextScanItemResponse
        {
            public string name { get; set; }
            public int id { get; set; }
            public string type { get; set; }
            public string scanname { get; set; }
            public int scanindex { get; set; }
            public bool @override {get; set;}
        }

    }
}