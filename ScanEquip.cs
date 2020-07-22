﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RomExchangeScanner
{
    partial class Scanner
    {
        public async Task<ExchangeInfo> ScanEquip(AndroidConnector android, ScanInfo scanInfo)
        {

            if (scanInfo.RealName.Contains("[1]"))
                scanInfo.Slots = 1;
            if (scanInfo.RealName.Contains("[2]"))
                scanInfo.Slots = 2;

            if (scanInfo.SearchName.Contains("["))
                scanInfo.SearchName = scanInfo.SearchName.Substring(0, scanInfo.SearchName.IndexOf("["));
            scanInfo.Equip = true;

            Console.WriteLine("- Opening search window");
            await CloseSearch(android);
            await ClickSearchButton(android);
            await ClickSearchBox(android);

            await android.Text(scanInfo.SearchName);
            await ClickSearchWindowSearchButton(android); //to close text input
            await ClickSearchWindowSearchButton(android);

            Console.WriteLine("- Scanning search result");
            await android.Screenshot("searchresult.png");
            List<int> indices = FindSearchResult("searchresult.png", scanInfo);
            if (indices.Count == 0)
            {
                //TODO: do something with item0.png - item9.png
                Console.WriteLine("- Error, could not find item");
                await CloseSearch(android);
                return ExchangeInfo.BuildError(scanInfo);
            }
            if (indices[0] != scanInfo.SearchIndex)
            {
                if (scanInfo.SearchIndex != -1)
                    Console.WriteLine("- Warning, search index not correct");
                scanInfo.SearchIndex = indices[0];
            }


            for (int i = 0; i < indices.Count; i++)
            {
                await ClickSearchWindowIndex(android, indices[i]);
                await Task.Delay(1500); 

                Console.WriteLine("- Checking if any items are on sale");
                await android.Screenshot("shopitems.png");
                using (var image = Image.Load<Rgba32>("shopitems.png"))
                    image.Clone(ctx => ctx.Crop(new Rectangle(975, 505, 368, 64))).Save($"nosale.png");

                bool nosale = false;
                if (GetTextFromImage("nosale.png").ToLower().Contains("currently"))
                {
                    nosale = true;
                    Console.WriteLine("- No items currently on sale");
                    if (i + 1 >= indices.Count)
                        return new ExchangeInfo()
                        {
                            Found = false,
                            ScanInfo = scanInfo
                        };
                }

                if (!nosale)
                {
                    var images = new List<Image<Rgba32>>();

                    for (int runs = 0; runs < 10; runs++)
                    {
                        int foundResults = images.Count;

                        using (var image = Image.Load<Rgba32>("shopitems.png"))
                        {
                            int starty = 0;
                            for (int y = 200; y < 600; y++)
                            {
                                if ((image[650, y] == new Rgba32(171, 210, 243, 255) ||
                                    image[650, y] == new Rgba32(185, 218, 245, 255)) &&
                                    image[650, y - 1] == new Rgba32(255, 255, 255, 255))
                                {
                                    starty = y;
                                    break;
                                }
                            }
                            if (starty == 0)
                            {
                                Console.WriteLine("Could not find starting line!");
                                break;
                            }
                            Console.WriteLine($"Item list starting at {starty}");
                            for (int ii = 0; ii < 8; ii++)
                            {
                                if (starty + 180 * (ii / 2) + 132 > 902)
                                    continue;
                                var subImage = image.Clone(ctx => ctx.Crop(new Rectangle(595 + 600 * (ii % 2), starty + 180 * (ii / 2), 400, 132)));

                                bool alreadyScanned = false;
                                foreach (var img in images)
                                {
                                    int dist = ImageDistance(img, subImage);
                                    if (dist < 100)
                                    {
                                        alreadyScanned = true;
                                        break;
                                    }
                                }

                                if (alreadyScanned)
                                    continue;
                                images.Add(subImage);

                                await ClickShopItem(android, ii, starty-230);

                                var rect = new Rectangle(694 + 600 * (ii % 2), starty + 103 + 180 * (ii / 2), 33, 28);
                                //image.Clone(ctx => ctx.Crop(rect)).Save($"unknown/unknown{Directory.GetFiles("unknown").Length}.png");
                                using (var noamount = Image.Load<Rgba32>("data/equip_br/glove_1s.png"))
                                {
                                    int dist = ImageDistance(image.Clone(ctx => ctx.Crop(rect)), noamount);
                                    if (dist > 100)
                                    {
                                        Console.WriteLine($"- Item {ii} has an amount, not scanning!");
                                        continue;
                                    }
                                }


                                Console.WriteLine($"- Scanning item {ii}");
                                await ClickShopBuyButton(android);
                                await Task.Delay(500); // the UI needs some time to load the card
                                Console.WriteLine("- Scanning item");
                                await android.Screenshot("shopresult.png");

                                ExchangeInfo priceInfo = await ParseResultWindow("shopresult.png", scanInfo, android);
                                await ClickShopCloseItem(android);
                            }

                            /*                        if (!priceInfo.Error || i + 1 >= indices.Count)
                                                    {
                                                        priceInfo.Error = false;
                                                        return priceInfo;
                                                    }*/
                        }
                        if(foundResults == images.Count)
                        {
                            Console.WriteLine("- No more results, stopping");
                            runs = 10;
                        }


                        await android.Swipe(1160, 860, 1160, 120, 1500);
                        await android.Tap(530, 845); // against overlay
                        await android.Swipe(1160, 860, 1160, 650, 500);
                        await android.Tap(530, 845); // against overlay
                        await Task.Delay(500);
                        await android.Screenshot("shopitems.png");

                    }


                    foreach (var img in images)
                        img.Dispose();


                }
                scanInfo.Message = "";
                Console.WriteLine("Item does not match, or not found on exchange with multiple items, trying to rescan it");
                break;
                await CloseSearch(android);
                await ClickSearchButton(android);
                await ClickSearchBox(android);

                await android.Text(scanInfo.SearchName);
                await ClickSearchWindowSearchButton(android); //to close text input
                await ClickSearchWindowSearchButton(android);


            }

            return null;
        }
    }
}