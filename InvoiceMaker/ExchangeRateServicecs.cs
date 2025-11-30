using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace InvoiceMaker.Services
{
    public class ExchangeRateService
    {
        private const string NaverFxUrl =
            "https://finance.naver.com/marketindex/exchangeList.naver";

        // KRW 기준 환율 가져오기 (1 통화 = ? KRW)
        private async Task<decimal?> GetKrwPerAsync(string currencyCode)
        {
            using (var client = new HttpClient())
            {
                var bytes = await client.GetByteArrayAsync(NaverFxUrl);
                var html = Encoding.UTF8.GetString(bytes);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var rows = doc.DocumentNode.SelectNodes("//table//tr");
                if (rows == null) return null;

                foreach (var row in rows)
                {
                    var tds = row.SelectNodes("td");
                    if (tds == null || tds.Count < 2) continue;

                    var currencyText = tds[0].InnerText.Trim(); // "미국 USD", "멕시코 페소 MXN" 등

                    if (!currencyText.ToUpper().Contains(currencyCode.ToUpper()))
                        continue;

                    var rateText = tds[1].InnerText.Trim().Replace(",", "");

                    if (decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    {
                        return rate;   // 1 통화 = rate KRW
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// 1 USD = ? MXN (USD -> MXN 환율)
        /// </summary>
        public async Task<decimal?> GetUsdToMxnAsync()
        {
            var usdKrw = await GetKrwPerAsync("USD"); // 1 USD  = ? KRW
            var mxnKrw = await GetKrwPerAsync("MXN"); // 1 MXN  = ? KRW

            if (!usdKrw.HasValue || !mxnKrw.HasValue || mxnKrw.Value == 0)
                return null;

            // 1 USD = (KRW/USD) / (KRW/MXN) = ? MXN
            var usdToMxn = usdKrw.Value / mxnKrw.Value;
            return decimal.Round(usdToMxn, 4); // 소수 4자리 정도로 반올림
        }
    }
}
