namespace RomExchangeScanner
{
    public class ScanInfo
    {
        public string RealName { get; set; }
        public string SearchName { get; set; }
        public int SearchIndex { get; set; } = -1;
        public bool Override { get; set; } = false;
        public string Message { get; set; }
        public int Slots { get; internal set; }
        public bool Equip { get; internal set; }

        public override string ToString()
        {
            return $"Scan info: \n" +
                $"Search Name: {SearchName}\n" +
                $"Search Index: {SearchIndex}\n" +
                $"Message: {Message}";
        }
    }
}