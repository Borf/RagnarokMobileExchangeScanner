using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace RomExchangeScanner
{
    partial class Scanner : IDisposable
    {
        private TesseractEngine engine;
        private Dictionary<int, Image<Rgba32>> singleDigits = new Dictionary<int, Image<Rgba32>>();

        public Scanner()
        {
            engine = new TesseractEngine(@"./data/tessdata", "eng", EngineMode.Default);

            foreach (var file in Directory.GetFiles("data/amount"))
                singleDigits[int.Parse(Path.GetFileNameWithoutExtension(file))] = Image.Load<Rgba32>(file);
        }

        public void Dispose()
        {
            engine.Dispose();
            foreach (var img in singleDigits)
                img.Value.Dispose();
        }


        private readonly Point[] SearchResultPositions = { 
            new Point(772, 370), new Point(1132, 370),
            new Point(772, 475), new Point(1132, 475),
            new Point(772, 580), new Point(1132, 580),
            new Point(772, 685), new Point(1132, 685),
            new Point(772, 790), new Point(1132, 790),
        };



        public async Task RestartRo(AndroidConnector android)
        {
            Program.status.SetSubStatus("Stopping RO");
            await android.StopRo();
            await Task.Delay(1000);
            Program.status.SetSubStatus("Starting RO");
            await android.StartRo();
            await Task.Delay(30000);

            Program.status.SetSubStatus("Scanning if logged in");
            //TODO: check for google/facebook popup
            for(int i = 0; i < 25; i++)
            {
                await android.Screenshot("login.png");
                using (var image = Image.Load<Rgba32>("login.png"))
                    image.Clone(ctx => ctx.Crop(new Rectangle(855, 585, 215, 55))).Save($"servername.png");
                string servername = GetTextFromImage("servername.png");
                Console.WriteLine(servername);
                if (servername.ToLower() == "eternal love")
                    break;
                await Task.Delay(5000);
                if (i == 24)
                {
                    Program.status.SetSubStatus("Unable to login");
                    return;
                }
            }
            Program.status.SetSubStatus("Tapping Login");
            await android.Tap(500, 500);        //login tap
            await Task.Delay(10000);

            for (int i = 0; i < 25; i++)
            {
                await android.Screenshot("charselect.png");
                using (var image = Image.Load<Rgba32>("charselect.png"))
                using (var cmp = Image.Load<Rgba32>("data/startbutton.png"))
                    if (IsSame(image.Clone(ctx => ctx.Crop(new Rectangle(1518, 943, 243, 67))), cmp))
                        break;
                await Task.Delay(2000);
                if (i == 24)
                {
                    Program.status.SetSubStatus("Unable to select character");
                    return;
                }
            }
            Program.status.SetSubStatus("Selecting char");
            await android.Tap(1624, 975);       //select char button
            await Task.Delay(30000);
            Program.status.SetSubStatus("Closing stupid ninja popup");
            await android.Tap(1893, 50);       //
            await Task.Delay(1000);
            Program.status.SetSubStatus("Closing event popup");
            await android.Tap(1400, 133);       //close event popup
            await android.Tap(1495, 111);       //close anotherpopup
        }

        public async Task OpenExchange(AndroidConnector android, int map)
        {
            await android.Tap(1800, 160);       //open minimap
            await Task.Delay(500);
            await android.Screenshot("minimap.png");
            using (var image = Image.Load<Rgba32>("minimap.png"))
                image.Clone(ctx => ctx.Crop(new Rectangle(1309, 175, 523, 60))).Save($"mapname.png");
            string mapname = GetTextFromImage("mapname.png").ToLower();
            await android.Tap(1325, 861);       //click world button
            await Task.Delay(1500);

            if (mapname == "prontera south gate")
            {
                await android.Tap(1100, 700);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 665);       //click big cat man
            }
            else if (mapname == "prontera")
            {
                await android.Tap(1100, 600);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 831);       //click big cat man
            }
            else if (mapname == "morroc")
            {
                await android.Tap(1000, 800);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 782);       //click big cat man
            }
            else if (mapname == "geffen")
            {
                await android.Tap(900, 600);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 718);       //click big cat man            }
            }
            else if (mapname == "payon")
            {
                await android.Tap(1300, 800);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 716);       //click big cat man
            }
            else if (mapname == "izlude island")
            {
                await android.Tap(1200, 700);       //click map
                await Task.Delay(1500);
                await android.Tap(700, 432);        //click big cat man
            }
            else
                Program.status.SetSubStatus("Player is at unknown map: " + mapname);


            Program.status.SetSubStatus("Waiting for exchange popup");
            await Task.Delay(1000);
            //wait for buy button to appear
            while (true)
            {
                await android.Screenshot("exchange.png");
                using (var image = Image.Load<Rgba32>("exchange.png"))
                using (var cmp = Image.Load<Rgba32>("data/exchange.png"))
                    if (IsSame(image.Clone(ctx => ctx.Crop(new Rectangle(1525, 683, 329, 54))), cmp))
                        break;
                await Task.Delay(2000);
            }
            Program.status.SetSubStatus("Exchange opened, done");
            await android.Tap(1660, 705);
        }

        public async Task<bool> IsExchangeOpen(AndroidConnector android)
        {
            await android.Screenshot("isExchangeOpen.png");
            using (var image = Image.Load<Rgba32>("isExchangeOpen.png"))
            {
                using (var cmp = Image.Load<Rgba32>("data/search.png"))
                    if (ImageDistance(image.Clone(ctx => ctx.Crop(new Rectangle(223, 200, 268, 63))), cmp) < 10)
                        return true;
                using (var cmp = Image.Load<Rgba32>("data/search2.png"))
                    if (ImageDistance(image.Clone(ctx => ctx.Crop(new Rectangle(1356, 235, 117, 55))), cmp) < 10)
                        return true;
                return false;
            }
        }


        private async Task ClickSearchButton(AndroidConnector android)
        {
            await android.Tap(300, 200);
        }

        private async Task CloseSearch(AndroidConnector android)
        {
            await android.Tap(1467, 187);
        }

        private async Task ClickSearchBox(AndroidConnector android)
        {
            await android.Tap(800, 270);
        }
        private async Task ClickSearchWindowSearchButton(AndroidConnector android)
        {
            await android.Tap(1400, 260);
        }


        private async Task ClickSearchWindowIndex(AndroidConnector android, int index)
        {
            await android.Tap(SearchResultPositions[index].X + 155, SearchResultPositions[index].Y + 44);
        }

        private async Task ClickShopItem(AndroidConnector android, int index, int offsety = 0)
        {
            await android.Tap(800 + (index % 2) * 600, offsety + 302 + 180 * (index/2));
        }
        private async Task ClickShopBuyButton(AndroidConnector android)
        {
            await android.Tap(1600, 984);
        }
        private async Task ClickShopCloseItem(AndroidConnector android)
        {
            await android.Tap(525, 900);
        }


        public ScanResultItem ParseResultWindowRareItem(string fileName, ScanInfo scanInfo, AndroidConnector android)
        {
            ScanResultItem exchangeInfo = new ScanResultItem()
            {
                Found = true,
                ScanInfo = scanInfo
            };

            using (var image = Image.Load<Rgba32>(fileName))
            {
                image.Clone(ctx => ctx.Crop(new Rectangle(996, 159, 549, 66))).Save($"itemname.png");
                string itemName = GetTextFromImage("itemname.png");
                if(
                    (!scanInfo.RealName.Contains("★") && itemName.ToLower() != scanInfo.RealName.ToLower()) ||
                    ( scanInfo.RealName.Contains("★") && !itemName.Contains("*") &&
                    itemName.ToLower() != "andrei card" &&
                    itemName.ToLower() != "zipper beartcard" &&
                    itemName.ToLower() != "archer skeletontcard"
)
                    )
                {
                    if(scanInfo.RealName.Contains("★"))
                        Console.WriteLine("Star card not found");
                    scanInfo.Message = "Something is wrong, names do NOT match";
                    return ScanResult.BuildError<ScanResultItem>(scanInfo);
                }


                exchangeInfo.Price = int.Parse(GetPrice(image), CultureInfo.InvariantCulture);

                string amount = GetAmount(image);
                if (amount == "")
                {
                    scanInfo.Message = "Could not find the right amount";
                    return ScanResult.BuildError<ScanResultItem>(scanInfo);
                }
                exchangeInfo.Amount = int.Parse(amount, CultureInfo.InvariantCulture);
                exchangeInfo.SnapTime = GetSnapTime(image);


                exchangeInfo.Found = true;
            }
            return exchangeInfo;
        }


        public async Task<ScanResultEquip> ParseResultWindowEquip(string fileName, ScanInfo scanInfo, AndroidConnector android)
        {
            ScanResultEquip exchangeInfo = new ScanResultEquip()
            {
                Found = true,
                ScanInfo = scanInfo
            };

            using (var image = Image.Load<Rgba32>(fileName))
            {
                image.Clone(ctx => ctx.Crop(new Rectangle(996, 159, 549, 66))).Save($"itemname.png");
                string itemName = GetTextFromImage("itemname.png");
                if (itemName.ToLower() != scanInfo.RealName.ToLower())
                {
                    scanInfo.Message = $"Something is wrong, names do NOT match. Expected {scanInfo.RealName.ToLower()} but got {itemName.ToLower()}";
                    return ScanResult.BuildError<ScanResultEquip>(scanInfo);
                }

                exchangeInfo.Price = int.Parse(GetPriceEquip(image), CultureInfo.InvariantCulture);
                exchangeInfo.SnapTime = GetSnapTime(image);

                int i = 0;
                //scan for multiple items
                bool enchanted = false;
                bool foundRefine = false;
                while (!Program.CancelScan)
                {
                    using (var image2 = Image.Load<Rgba32>(fileName))
                        image2.Clone(ctx => ctx.Crop(new Rectangle(383, 261, 553, 453))).Save($"enchant{i}.png");
                    string hasEnchant = GetTextFromImage($"enchant{i}.png");

                    Console.WriteLine($"- Text Read: \n\n{hasEnchant}\n\n");
                    if (hasEnchant.ToLower().Contains("refine ") && !foundRefine && !enchanted && hasEnchant.ToLower().IndexOf("refine ") != hasEnchant.ToLower().IndexOf("refine +6 effective"))
                    {
                        string refine = hasEnchant.ToLower();
                        refine = refine.Replace("\r\n", "\n");
                        while (refine.Contains("\n\n"))
                            refine = refine.Replace("\n\n", "\n");
                        refine = refine.Substring(refine.IndexOf("\nrefine ") + 8).Trim();
                        if(refine.IndexOf("\n") > 0)
                            refine = refine.Substring(0, refine.IndexOf("\n")).Trim();
                        Console.WriteLine(refine);
                        int refineLevel = 0;
                        if (refine.Contains("/"))
                        {
                            try
                            {
                                refineLevel = int.Parse(refine.Substring(0, refine.IndexOf("/")));
                                foundRefine = true;
                            }
                            catch(FormatException e)
                            {
                                scanInfo.Message = "Something is wrong, error parsing refine level: " + refine;
                                Console.WriteLine(e);
                                return ScanResult.BuildError<ScanResultEquip>(scanInfo);
                            }
                        }
                        if(refine.Contains("atk+"))
                        {
                            int atk = int.Parse(refine.Substring(refine.IndexOf("atk+") + 4));
                            foundRefine = true;
                            //TODO: check if atk matches refineLevel
                        }
                        exchangeInfo.RefinementLevel = refineLevel;
                    }


                    if (hasEnchant.ToLower().Contains("enchanted"))
                    {
                        enchanted = true;
                    }
                    if (hasEnchant.ToLower().Contains("equipment upgrade"))
                    {
                        if(enchanted)
                        {
                            MemoryStream ms = new MemoryStream();
                            var jpegEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 10 };
                            using (var image2 = Image.Load<Rgba32>($"enchant{i}.png"))
                                image2.Clone(ctx => ctx.Resize(new ResizeOptions { Size = image2.Size() / 2 })).SaveAsJpeg(ms, jpegEncoder);

                            exchangeInfo.EnchantmentImage = System.Convert.ToBase64String(ms.GetBuffer());
                            if (!hasEnchant.ToLower().Contains("enchanted"))
                            {
                                Console.WriteLine("Scrolled too far");
                                exchangeInfo.Enchantments = new List<string>() { "scrolled too far" };
                                break;
                            }
                            hasEnchant = hasEnchant.ToLower();
                            hasEnchant = hasEnchant.Replace("\r\n", "\n");
                            while(hasEnchant.Contains("\n\n"))
                                hasEnchant = hasEnchant.Replace("\n\n", "\n");
                            hasEnchant = hasEnchant.Substring(hasEnchant.IndexOf("enchanted attribute:") + 20).Trim();
                            hasEnchant = hasEnchant.Substring(0, hasEnchant.IndexOf("equipment upgrade")).Trim();
                            
                            hasEnchant = hasEnchant.Replace("mapr ", "maxhp ");
                            exchangeInfo.Enchantments = hasEnchant.Split("\n", 4).ToList();
                        }
                        break;
                    }
                    if (hasEnchant.ToLower().Contains("exchange price"))
                    {
                        Console.WriteLine("Scrolled wayyyyyy too far");
                        break;
                    }
                    try
                    {
                        File.Delete($"enchant{i}.png");
                    }catch(UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Could not delete enchant{i}.png, {e}");
                    }


                    await android.Swipe(555, 500, 555, 300, 500);
                    await Task.Delay(100);
                    await android.Tap(1500, 960);
                    await android.Screenshot(fileName);
                    i++;
                }
                exchangeInfo.Found = true;
            }
            return exchangeInfo;
        }

        private DateTime? GetSnapTime(Image<Rgba32> image)
        {
            image.Clone(ctx => ctx.Crop(new Rectangle(989, 218, 557, 312))).Save($"snapping.png");
            string snappingText = GetTextFromImage("snapping.png");
            if (snappingText.ToLower().Contains("snapping"))
            {
                snappingText = snappingText.Substring(24);
                snappingText = snappingText.Substring(0, snappingText.IndexOf("\n"));
                if (snappingText.Contains(" sec"))
                    snappingText = snappingText.Substring(0, snappingText.IndexOf(" sec"));

                try
                {
                    int minutes = int.Parse(snappingText.Substring(0, snappingText.IndexOf(" ")), CultureInfo.InvariantCulture);
                    int seconds = int.Parse(snappingText.Substring(snappingText.LastIndexOf(" ")), CultureInfo.InvariantCulture);
                    return DateTime.Now.AddMinutes(minutes).AddSeconds(seconds);
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Unable to parse snapping time!");
                }
            }
            return null;
        }


        private string GetAmount(Image<Rgba32> image)
        {
            var amountImage = image.Clone(ctx => ctx.Crop(new Rectangle(725, 154, 203, 41)));
            amountImage.Save($"amount.png");
            string amount = GetTextFromImage($"amount.png");
            //remove all junk that's not numbers
            amount = amount
                .Replace(",", "")
                .Replace("'", "")
                .Replace(".", "")
                .Replace("\"", "")
                .Replace(" ", "");
            for (char c = 'a'; c <= 'z'; c++)
                amount = amount.Replace(c + "", "");
            for (char c = 'A'; c <= 'Z'; c++)
                amount = amount.Replace(c + "", "");

            if (amount == "")
            {
                foreach(var kv in singleDigits)
                {
                    if (IsSame(kv.Value, amountImage))
                        amount = kv.Key + "";
                }
            }
            if(amount == "")
            {
                try
                {
                    File.Copy("amount.png", $"unknown/unknown{Directory.GetFiles("unknown").Length}.png");
                    File.Copy("shopresult.png", $"unknown/unknown{Directory.GetFiles("unknown").Length}.png");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return amount;
        }

        private string GetPrice(Image<Rgba32> image)
        {
            image.Clone(ctx => ctx.Crop(new Rectangle(633, 671, 283, 67))).Save($"price.png");
            string price = GetTextFromImage($"price.png");
            price = price.Replace(",", "");
            price = price.Replace(" ", "");
            return price;
        }
        private string GetPriceEquip(Image<Rgba32> image)
        {
            image.Clone(ctx => ctx.Crop(new Rectangle(632, 754, 261, 72))).Save($"price.png");
            string price = GetTextFromImage($"price.png");
            price = price.Replace(",", "");
            price = price.Replace(".", "");
            price = price.Replace(" ", "");
            return price;
        }

        private bool IsSame(Image<Rgba32> image, Image<Rgba32> amountImage)
        {
            if (image.Width != amountImage.Width || image.Height != amountImage.Height)
                return false;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image[x, y] != amountImage[x, y])
                        return false;
                }
            }
            return true;
        }
        private int ImageDistance(Image<Rgba32> image, Image<Rgba32> amountImage)
        {
            int dist = 0;
            for (int x = 1; x < image.Width-1; x++)
            {
                for (int y = 1; y < image.Height-1; y++)
                {
                    int localDist = 255;
                    for (int xx = x-1; xx <= x+1; xx++)
                    {
                        for(int yy = y - 1; yy <= y+1; yy++)
                        {
                            localDist = Math.Min(localDist, Math.Abs(image[x, y].R - amountImage[xx, yy].R));
                            localDist = Math.Min(localDist, Math.Abs(image[x, y].G - amountImage[xx, yy].G));
                            localDist = Math.Min(localDist, Math.Abs(image[x, y].B - amountImage[xx, yy].B));
                        }
                    }
                    dist = Math.Max(dist, localDist);
                }
            }
            return dist;
        }

        private List<int> FindSearchResult(string imageName, ScanInfo info)
        {
            if (info.Override)
                return new List<int>() { info.SearchIndex };

            List<int> resultIds = new List<int>();

            string itemName = info.RealName;
            bool star = itemName.Contains("★");
            if (star)
                itemName = itemName.Substring(0, itemName.IndexOf("★"));

            string matchItemName = itemName
                .Replace(".", "")
                .ToLower()
                .Trim(new char[] { '\r', '\n', '\t', ' ', '.' });


            List<string> results = new List<string>();
            List<int> searchOrder = Enumerable.Range(0, SearchResultPositions.Length).ToList();
            if (info != null && info.SearchIndex != -1)
                searchOrder.Swap(0, info.SearchIndex);

            using (var image = Image.Load(imageName))
            {
                foreach (int i in searchOrder)
                {
                    image.Clone(ctx => ctx.Crop(new Rectangle(SearchResultPositions[i].X, SearchResultPositions[i].Y, 313, 83))).Save($"item{i}.png");
                    string result = GetTextFromImage($"item{i}.png");
                    if (result == "")
                        continue;
                    results.Add(result);
                    result = result
                        .Replace(".", "")
                        .ToLower()
                        .Trim(new char[] { '\r', '\n', '\t', ' ', '.' });

                    if (result.EndsWith(" bi") && info.RealName.ToLower().Contains("blueprint"))
                        result = result.Substring(0, result.Length - 1) + "l";


                    if (!star)
                    {
                        if (matchItemName.IndexOf(result) == 0)
                            resultIds.Add(i);
                    }
                    else
                    {
                        if (matchItemName.IndexOf(result) == 0 && result != matchItemName + " card")
                            resultIds.Add(i);
                        else if ((matchItemName + " *card").IndexOf(result) == 0)
                            resultIds.Add(i);
                        else if ((matchItemName + "*card").IndexOf(result) == 0)
                            resultIds.Add(i);
                        else if ((matchItemName + " * card").IndexOf(result) == 0)
                            resultIds.Add(i);
                        else if ((matchItemName + "izcard").IndexOf(result) == 0)
                            resultIds.Add(i);
                        else if ((matchItemName + " iv").IndexOf(result) == 0)
                            resultIds.Add(i);
                    }

                }
            }

        
            if(resultIds.Count == 0 && results.Count == 10)
            {
                //could be on the next page
                return new List<int>() { -1 };
            }

            if(resultIds.Count == 0)
                info.Message = $"Could not find item {itemName}, results were ({string.Join(", ", results)})";
            return resultIds;
        }

        private string GetTextFromImage(string fileName)
        {
            try
            {
                using (var img = Pix.LoadFromFile(fileName))
                {
                    using (var page = engine.Process(img))
                    {
                         return page.GetText().Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }
            return "";
        }

    }
}
