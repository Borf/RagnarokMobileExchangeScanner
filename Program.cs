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
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using Tesseract;

namespace RomExchangeScanner
{
    class Program
    {
        static string ApiEndPoint = "http://localhost";

        static void Main(string[] args)
        {
            string hostname = "10.10.0.26";
            if (args.Length > 0)
                hostname = args[0];

            AndroidConnector androidConnection = new AndroidConnector(hostname);

            using (Scanner scanner = new Scanner())
            {
                if (!scanner.IsExchangeOpen(androidConnection).Result)
                {
                    scanner.RestartRo(androidConnection).Wait();
                    scanner.OpenExchange(androidConnection, 0).Wait();
                }

                /*  Console.WriteLine(scanner.ScanRareItem(androidConnection, new ScanInfo()
                  {
                      RealName = "Archer Skeleton ★Card",
                      SearchName = "Archer Skeleton ★Card",
                      SearchIndex = -1,
                      Override = false
                  }).Result);*/


                int errorCount = 0;
                int majorErrorcount = 0;

                using (HttpClient client = new HttpClient())
                {
                    while (true)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiEndPoint}/api/scanner/nextscanitem");
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("User-Agent", "BorfRoScanner");
                        var response = client.SendAsync(request).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            using var responseStream = response.Content.ReadAsStreamAsync().Result;
                            var options = new JsonSerializerOptions
                            {
                                IgnoreNullValues = true
                            };
                            var data = JsonSerializer.DeserializeAsync<NextScanItemResponse>(responseStream, options).Result;

                            Console.WriteLine("\nScanning for " + data.name);

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


                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            ExchangeInfo exchangeInfo = scanner.ScanRareItem(androidConnection, scanInfo).Result;
                            sw.Stop();
                            
                            Console.WriteLine($"Found Item in {sw.Elapsed.TotalSeconds} seconds\n{exchangeInfo}\n");
                            if (!exchangeInfo.Error)
                            {
                                request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndPoint}/api/scanner/result");
                                request.Headers.Add("Accept", "application/json");
                                request.Headers.Add("User-Agent", "BorfRoScanner");
                                request.Content = new StringContent(JsonSerializer.Serialize(new
                                {
                                    data.id,
                                    price = exchangeInfo.Price,
                                    amount = exchangeInfo.Amount,
                                    snapping = exchangeInfo.Snapping,
                                    snapTime = exchangeInfo.SnapTime,
                                    ScanIndex = exchangeInfo.ScanInfo.SearchIndex
                                }), Encoding.UTF8, "application/json");

                                response = client.SendAsync(request).Result;
                                errorCount = 0;
                                majorErrorcount = 0;
                            } else
                            {
                                errorCount++;
                                Console.WriteLine("Error scanning card!");
                                Console.WriteLine(exchangeInfo.ScanInfo.Message);
                            }


                        }


                        if(errorCount > 10)
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
        }

        class NextScanItemResponse
        {
            public string name { get; set; }
            public int id { get; set; }
            public string scanname { get; set; }
            public int scanindex { get; set; }
            public bool @override {get; set;}
        }

    }
}
