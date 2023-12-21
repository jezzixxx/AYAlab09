using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Text;

internal class Task1
{
    public static async Task Main()
    {
        string ticker = "C:\\Users\\pkapa\\source\\repos\\lab09\\ticker.txt";
        string[] codes = await File.ReadAllLinesAsync(ticker);
        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        await LoadData(codes, semaphore);
    }

    public static async Task MiddleCounter(string code, SemaphoreSlim semaphore)
    {
        UnicodeEncoding uniencoding = new UnicodeEncoding();
        double middles = 0;
        int days = 0;
        using (TextFieldParser parser = new TextFieldParser($"{code}.csv"))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                if (fields[1] == "null") break;
                if (fields[0] != "Date")
                {
                    middles += (Convert.ToDouble(fields[1].Replace('.', ',')) + Convert.ToDouble(fields[2].Replace('.', ','))) / 2;
                    ++days;
                }
            }
            await semaphore.WaitAsync();
            try
            {
                using (FileStream fs = File.Open("result.txt", FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs, uniencoding))
                    {
                        await writer.WriteAsync(code + ": " + (middles / days) + "\n");
                    }
                    Console.WriteLine("count");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    public static async Task LoadData(string[] codes, SemaphoreSlim semaphore)
    {
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        long year_ago = now - 31556926;
        string url;
        WebClient client = new WebClient();
        foreach (string code in codes)
        {
            url = $"https://query1.finance.yahoo.com/v7/finance/download/{code}?period1={year_ago}&period2={now}&interval=1d&events=history&includeAdjustedClose=true";
            Console.WriteLine(code);
            await semaphore.WaitAsync();
            try
            {
                await client.DownloadFileTaskAsync(new Uri(url), $"{code}.csv");
            }
            finally
            {
                semaphore.Release();
                await MiddleCounter(code, semaphore);
            }
        }
    }
}