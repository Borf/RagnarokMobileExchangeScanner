﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RomExchangeScanner
{
    public partial class Scanner
    {
        public async Task<List<ScanResultEquip>> ScanEquip(AndroidConnector android, ScanInfo scanInfo)
        {
            List<ScanResultEquip> result = new List<ScanResultEquip>();


            if (scanInfo.RealName.Contains("[1]"))
                scanInfo.Slots = 1;
            if (scanInfo.RealName.Contains("[2]"))
                scanInfo.Slots = 2;

            if (scanInfo.SearchName.Contains("["))
                scanInfo.SearchName = scanInfo.SearchName.Substring(0, scanInfo.SearchName.IndexOf("[")).Trim();
            scanInfo.Equip = true;

            Program.status.SetSubStatus("Opening search window");
            await CloseSearch(android);
            await ClickSearchButton(android);
            await ClickSearchBox(android);

            await android.Text(scanInfo.SearchName);
            await ClickSearchWindowSearchButton(android); //to close text input
            await ClickSearchWindowSearchButton(android);

            Program.status.SetSubStatus("Scanning search result");
            await android.Screenshot("searchresult.png");

            using (var image = Image.Load<Rgba32>("searchresult.png"))
            using (var cmp = Image.Load<Rgba32>("data/bigerror.png"))
                if(ImageDistance(image.Clone(c => c.Crop(new Rectangle(650, 150, 700, 200))), cmp) < 100)
                {
                    Program.Restart = true;
                    return null;
                }


            List<int> indices = FindSearchResult("searchresult.png", scanInfo);
            if(indices.Count == 1 && indices[0] == -1)
            {
                await android.Swipe(1100, 863, 1100, 355, 200);
                await Task.Delay(500);
                await android.Screenshot("searchresult.png");
                indices = FindSearchResult("searchresult.png", scanInfo);
            }

            await Task.Delay(500);
            if (indices.Count == 0)
            {
                //TODO: do something with item0.png - item9.png
                Program.status.SetSubStatus("- Error, could not find item");
                await CloseSearch(android);
                return new List<ScanResultEquip>() { ScanResult.BuildError<ScanResultEquip>(scanInfo) };
            }
            if (indices[0] != scanInfo.SearchIndex)
            {
                if (scanInfo.SearchIndex != -1)
                    Program.log.Log(scanInfo.RealName, "Warning, search index not correct");
                scanInfo.SearchIndex = indices[0];
            }


            for (int i = 0; i < indices.Count; i++)
            {
                await ClickSearchWindowIndex(android, indices[i]);
                await Task.Delay(2500);

                Program.status.SetSubStatus("Checking for sales");
                await android.Screenshot("shopitems.png");
                using (var image = Image.Load<Rgba32>("shopitems.png"))
                    image.Clone(ctx => ctx.Crop(new Rectangle(975, 505, 368, 64))).Save($"nosale.png");

                bool onSale = true;
                if (GetTextFromImage("nosale.png").ToLower().Contains("currently"))
                {
                    onSale = false;
                    Program.log.Log(scanInfo.RealName, "Currently not for sale");
                    if (i + 1 >= indices.Count)
                        return new List<ScanResultEquip>() { new ScanResultEquip()
                        {
                            Found = false,
                            ScanInfo = scanInfo
                        } };
                }

                if (onSale)
                {
                    var images = new List<Image<Rgba32>>();
                    int subPage = 0;
                    while(!Program.CancelScan)
                    {
                        Program.log.Log(scanInfo.RealName, "Scanning page " + subPage);
                        int foundResults = images.Count;
                        bool done = await ScanPage(images, android, scanInfo, result);
                        if(foundResults == images.Count)
                        {
                            Program.log.Log(scanInfo.RealName, "No new items found in last sweep, done");
                            break;
                        }
                        if (subPage % 2 == 0)
                            await SwipeDown(android);
                        else
                            if (await NextPage(android))
                                break;

                        await android.Screenshot("shopitems.png");
                        subPage++;
                    }
                    Program.status.SetSubStatus("done scanning");

                    foreach (var img in images)
                        img.Dispose();


                    return result;

                }
                //this should not happen
                scanInfo.Message = "";
                Console.WriteLine("Item does not match, or not found on exchange with multiple items, trying to rescan it");
                await CloseSearch(android);
                await ClickSearchButton(android);
                await ClickSearchBox(android);

                await android.Text(scanInfo.SearchName);
                await ClickSearchWindowSearchButton(android); //to close text input
                await ClickSearchWindowSearchButton(android);
            }

            return null;
        }

        private async Task<bool> NextPage(AndroidConnector android)
        {
            using (var image = Image.Load<Rgba32>("shopitems.png"))
            {
                var maxPageImage = image.Clone(ctx => ctx.Crop(new Rectangle(763, 956, 39, 52)));
                int maxPage = -1;
                //TODO: cache these
                foreach (var file in Directory.GetFiles("data/pagenumbers/total"))
                    using (var pageImage = Image.Load<Rgba32>(file))
                        if (IsSame(pageImage, maxPageImage))
                            maxPage = int.Parse(Path.GetFileNameWithoutExtension(file), CultureInfo.InvariantCulture);

                if(maxPage == -1)
                    maxPageImage.Save($"unknown/maxpage{Directory.GetFiles("unknown").Length}.png");

                var curPageImage = image.Clone(ctx => ctx.Crop(new Rectangle(700, 962, 63, 44)));
                int curPage = -1;
                //TODO: cache these
                foreach (var file in Directory.GetFiles("data/pagenumbers/current"))
                    using (var pageImage = Image.Load<Rgba32>(file))
                        if (IsSame(pageImage, curPageImage))
                            curPage = int.Parse(Path.GetFileNameWithoutExtension(file), CultureInfo.InvariantCulture);
                if (curPage == -1)
                    curPageImage.Save($"unknown/curpage{Directory.GetFiles("unknown").Length}.png");


                if(curPage < maxPage)
                {
                    await android.Tap(944, 982);
                    await Task.Delay(500); // wait for next page to load
                    return false;
                }
            }


            return true;
        }

        private async Task SwipeDown(AndroidConnector android)
        {
            await Task.Delay(250);
            await android.Swipe(1160, 860, 1160, -350, 1000);
            await Task.Delay(250);
            await android.Tap(530, 845); // against overlay
            await Task.Delay(500);
        }


        private async Task<bool> ScanPage(List<Image<Rgba32>> images, AndroidConnector android, ScanInfo scanInfo, List<ScanResultEquip> result)
        {
            string codename = scanInfo.RealName.ToLower();
            codename = codename.Replace(" ", "_");
            codename = codename.Replace("[1]", "1s");
            codename = codename.Replace("[2]", "2s");


            using (var image = Image.Load<Rgba32>("shopitems.png"))
            {
                //first, look for the top blue line of the item box (RGB(171, 210, 243) if not scrolled half a pixel)
                int starty = FindFirstResult(image);
                Program.log.Log(scanInfo.RealName, $"Item list starting at {starty}");
                await ClickShopItem(android, 0, starty - 230);

                //go through all 8 pictures
                for (int ii = 0; ii < 8; ii++)
                {
                    int newstarty = FindFirstResult(image);
                    if(newstarty != starty)
                    {
                        Program.log.Log(scanInfo.RealName, $"Scrolled a bit, adjusting");
                        starty = newstarty;
                    }
                    if (starty + 180 * (ii / 2) + 132 > 902)
                        continue;
                    
                    //make a capture of the item, so we don't scan items twice
                    var subImage = image.Clone(ctx => ctx.Crop(new Rectangle(595 + 600 * (ii % 2), starty + 180 * (ii / 2), 400, 132)));

                    if (TestIfScanned(subImage, images))
                    {
                        Program.log.Log(scanInfo.RealName, $"Item {ii} already scanned, skipping");
                        continue;
                    }
                    images.Add(subImage);
                    subImage.Save($"unknown/unknown{Directory.GetFiles("unknown").Length}.png");

                    //click it as visualisation
                    await ClickShopItem(android, ii, starty - 230);

                    //check if item has multiple on sale. If it does, we don't have to scan it because it won't have an enchantment
                    var rect = new Rectangle(694 + 600 * (ii % 2), starty + 103 + 180 * (ii / 2), 33, 28);
                    using (var amountImage = image.Clone(ctx => ctx.Crop(rect)))
                    {

                        //image.Clone(ctx => ctx.Crop(rect)).Save($"unknown/unknown{Directory.GetFiles("unknown").Length}.png");
                        if (!File.Exists($"data/equip_br/{codename}.png"))
                        {
                            if (!Directory.Exists($"data/equip_br/{codename}"))
                                Directory.CreateDirectory($"data/equip_br/{codename}");
                            amountImage.Save($"data/equip_br/{codename}/{Directory.GetFiles($"data/equip_br/{codename}/").Length}.png");
                        }
                        else using (var noamount = Image.Load<Rgba32>($"data/equip_br/{codename}.png"))
                        {
                            int dist = ImageDistance(amountImage, noamount);
                            if (dist > 100)
                            {
                                Program.log.Log(scanInfo.RealName, $"Item {ii} has an amount, not scanning. Distance {dist}");
                                    Console.WriteLine($"- Item {ii} has an amount, not scanning! Distance {dist}");
                                continue;
                            }
                        }
                    }

                    //scan item for price and enchantments
                    await Task.Delay(100);
                    Console.WriteLine($"- Scanning item {ii}");
                    await ClickShopBuyButton(android);
                    await Task.Delay(1000);
                    Console.WriteLine("- Scanning item");
                    await android.Screenshot("shopresult.png");
                    ScanResultEquip priceInfo = await ParseResultWindowEquip("shopresult.png", scanInfo, android);
                    result.Add(priceInfo);
                    await Task.Delay(100);
                    await ClickShopCloseItem(android);
                    if(priceInfo.Error) //if error, last one is found
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TestIfScanned(Image<Rgba32> subImage, List<Image<Rgba32>> images)
        {
            return images.Any(img => ImageDistance(img, subImage) < 100);
        }

        private int FindFirstResult(Image<Rgba32> image)
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
                return 0;
            }

            //1st time, starty == 230
            //2nd time, starty == 389, could be swiped just not far enough, so fix that
            if (starty > 387 && starty < 391)
                starty -= 180;
            return starty;
        }
    }
}
