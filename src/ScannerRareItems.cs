using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace RomExchangeScanner
{
    partial class Scanner
    {
        public async Task<ScanResultItem> ScanRareItem(AndroidConnector android, ScanInfo scanInfo)
        {
            Console.WriteLine("- Opening search window");
            await CloseSearch(android);
            await ClickSearchButton(android);
            await ClickSearchBox(android);

            string itemName = scanInfo.SearchName;
            if (itemName.Contains("★"))
                itemName = itemName.Substring(0, itemName.IndexOf("★")).Trim();

            await android.Text(itemName);
            await ClickSearchWindowSearchButton(android); //to close text input
            await ClickSearchWindowSearchButton(android);

            Console.WriteLine("- Scanning search result");
            await android.Screenshot("searchresult.png");
            List<int> indices = FindSearchResult("searchresult.png", scanInfo);
            if(indices.Count == 0)
            {
                //TODO: do something with item0.png - item9.png
                Console.WriteLine("- Error, could not find item");
                await CloseSearch(android);
                return ScanResult.BuildError<ScanResultItem>(scanInfo);
            }
            if (indices[0] != scanInfo.SearchIndex)
            {
                if(scanInfo.SearchIndex != -1)
                    Console.WriteLine("- Warning, search index not correct");
                scanInfo.SearchIndex = indices[0];
            }


            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] < 0 || indices[i] >= SearchResultPositions.Length)
                {
                    if(indices[i] == -1)
                    {
                        return new ScanResultItem()
                        {
                            Found = false,
                            ScanInfo = scanInfo
                        };
                    }
                    scanInfo.Message = "Warning, search index out of bounds";
                    return ScanResult.BuildError<ScanResultItem>(scanInfo);
                }
                await ClickSearchWindowIndex(android, indices[i]);
                await Task.Delay(1500); // the UI needs some time to load the card

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
                        return new ScanResultItem()
                        {
                            Found = false,
                            ScanInfo = scanInfo
                        };
                }

                if (!nosale)
                {
                    await ClickShopItem(android, 0); // these items should only give 1 item result
                    await ClickShopBuyButton(android);
                    await Task.Delay(1500); // the UI needs some time to load the card
                    Console.WriteLine("- Scanning item");
                    await android.Screenshot("shopresult.png");
                    await ClickShopCloseItem(android);

                    ScanResultItem priceInfo = ParseResultWindowRareItem("shopresult.png", scanInfo, android);
                    if (!priceInfo.Error || i + 1 >= indices.Count)
                    {
                        priceInfo.Error = false;
                        return priceInfo;
                    }
                }
                scanInfo.Message = "";
                Console.WriteLine("Item does not match, or not found on exchange with multiple items, trying to rescan it");
                await CloseSearch(android);
                await ClickSearchButton(android);
                await ClickSearchBox(android);

                await android.Text(itemName);
                await ClickSearchWindowSearchButton(android); //to close text input
                await ClickSearchWindowSearchButton(android);


            }

            return null;
        }


    }
}
