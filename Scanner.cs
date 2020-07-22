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
            Console.WriteLine("Stopping RO");
            await android.StopRo();
            await Task.Delay(1000);
            Console.WriteLine("Starting RO");
            await android.StartRo();
            await Task.Delay(30000);

            while (true)
            {
                await android.Screenshot("login.png");
                using (var image = Image.Load<Rgba32>("login.png"))
                    image.Clone(ctx => ctx.Crop(new Rectangle(855, 585, 215, 55))).Save($"servername.png");
                string servername = GetTextFromImage("servername.png");
                Console.WriteLine(servername);
                if (servername.ToLower() == "eternal love")
                    break;
                await Task.Delay(5000);
            }
            Console.WriteLine("Tapping Login");
            await android.Tap(500, 500);        //login tap
            await Task.Delay(10000);

            while (true)
            {
                await android.Screenshot("charselect.png");
                using (var image = Image.Load<Rgba32>("charselect.png"))
                using (var cmp = Image.Load<Rgba32>("data/startbutton.png"))
                    if (IsSame(image.Clone(ctx => ctx.Crop(new Rectangle(1518, 943, 243, 67))), cmp))
                        break;
                await Task.Delay(2000);
            }
            Console.WriteLine("Selecting char");
            await android.Tap(1624, 975);       //select char button
            await Task.Delay(30000);
            Console.WriteLine("Closing event popup");
            await android.Tap(1400, 133);       //close event popup
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
                await android.Tap(700, 495);       //click big cat man
            }
            else
                Console.WriteLine("Player is at unknown map: " + mapname);


                Console.WriteLine("Waiting for exchange popup to open");
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
            Console.WriteLine("Exchange opened, done");
            await android.Tap(1660, 705);
        }

        public async Task<bool> IsExchangeOpen(AndroidConnector android)
        {
            await android.Screenshot("isExchangeOpen.png");
            using (var image = Image.Load<Rgba32>("isExchangeOpen.png"))
            {
                using (var cmp = Image.Load<Rgba32>("data/search.png"))
                    if (IsSame(image.Clone(ctx => ctx.Crop(new Rectangle(223, 200, 268, 63))), cmp))
                        return true;
                using (var cmp = Image.Load<Rgba32>("data/search2.png"))
                    if (IsSame(image.Clone(ctx => ctx.Crop(new Rectangle(1356, 235, 117, 55))), cmp))
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


        public async Task<ExchangeInfo> ParseResultWindow(string fileName, ScanInfo scanInfo, AndroidConnector android)
        {
            ExchangeInfo exchangeInfo = new ExchangeInfo()
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
                    ( scanInfo.RealName.Contains("★") && !itemName.Contains("*"))
                    )
                {
                    scanInfo.Message = "Something is wrong, names do NOT match";
                    return ExchangeInfo.BuildError(scanInfo);
                }

                //scan price
                string price = "";
                if (scanInfo.Equip)
                    price = GetPriceEquip(image);
                else
                    price = GetPrice(image);
                exchangeInfo.Price = int.Parse(price, CultureInfo.InvariantCulture);

//scan amount, can only do this if it is not equipment
                if (!scanInfo.Equip)
                {
                    string amount = GetAmount(image);
                    if (amount == "")
                    {
                        scanInfo.Message = "Could not find the right amount";
                        return ExchangeInfo.BuildError(scanInfo);
                    }
                    exchangeInfo.Amount = int.Parse(amount, CultureInfo.InvariantCulture);
                }


//scan if the item is snapping
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
                        exchangeInfo.SnapTime = DateTime.Now.AddMinutes(minutes).AddSeconds(seconds);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine("Unable to parse snapping time!");
                    }

                }
//equip scanning
                if(scanInfo.Equip)
                {
                    while (true)
                    {
                        using (var image2 = Image.Load<Rgba32>(fileName))
                            image2.Clone(ctx => ctx.Crop(new Rectangle(383, 261, 553, 453))).Save($"enchant.png");
                        string hasEnchant = GetTextFromImage("enchant.png");
                        if (hasEnchant.ToLower().Contains("enchanted"))
                        {
                            Console.WriteLine(hasEnchant);
                        }
                        if (hasEnchant.ToLower().Contains("equipment upgrade"))
                        {
                            break;
                        }
                        await android.Swipe(555, 500, 555, 300, 500);
                        await android.Tap(555, 500);
                        await android.Screenshot(fileName);
                    }





                }

                exchangeInfo.Found = true;
            }
            return exchangeInfo;
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
