using System;
using System.Collections.Generic;

namespace RomExchangeScanner
{
    public class ScanResult
    {
        /// <summary>
        /// If error is set, check ScanInfo for error information
        /// </summary>
        public bool Error { get; set; } = false;
        public bool Found { get; set; }

        public long Price { get; set; }
        public DateTime? SnapTime { get; set; }
        public bool Snapping { get { return SnapTime.HasValue; } }


        public ScanInfo ScanInfo { get; set; }

        public static T BuildError<T>(ScanInfo info) where T : ScanResult, new()
        {
            return new T() { Error = true, ScanInfo = info };
        }

        public override string ToString()
        {
            return $"Exchange info: \n" +
                $"Error: {Error}\n" +
                $"Found: {Found}\n" +

                ScanInfo;
        }


    }
}