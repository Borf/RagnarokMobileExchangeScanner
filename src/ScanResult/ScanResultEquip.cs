using System;
using System.Collections.Generic;
using System.Text;

namespace RomExchangeScanner
{
    public class ScanResultEquip : ScanResult
    {
        public bool Broken { get; set; }
        public int RefinementLevel { get; set; }
        public List<string> Enchantments { get; set; } = new List<string>();
        public string EnchantmentImage { get; set; }
    }
}
