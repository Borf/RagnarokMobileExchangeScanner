using System;

namespace RomExchangeScanner
{
    public class ExchangeInfo
    {
        /// <summary>
        /// If error is set, check ScanInfo for error information
        /// </summary>
        public bool Error { get; set; } = false;
        public bool Found { get; set; }
        public int Amount { get; set; }
        public long Price { get; set; }
        public DateTime? SnapTime { get; set; }
        public bool Snapping { get { return SnapTime.HasValue; } }
        public ScanInfo ScanInfo { get; set; }

        public static ExchangeInfo BuildError(ScanInfo info)
        {
            return new ExchangeInfo() { Error = true, ScanInfo = info };
        }


        public override string ToString()
        {
            return $"Exchange info: \n" +
                $"Error: {Error}\n" +
                $"Found: {Found}\n" +
                $"Amount: {Amount}\n" +
                $"Snapping: {Snapping}\n" +
                (Snapping ? $"Snaptime: {SnapTime}\n" : "") +
                $"Price: {Price}\n" +
                ScanInfo;
        }


    }
}