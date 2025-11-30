using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace InvoiceMaker.Services
{
    /// <summary>
    /// 환율 정보를 가져오는 서비스.
    /// 지금 버전은 "컴파일 + 실행"이 목적이라
    /// 실제 HTTP 호출 대신, 기본값(null)을 돌려주고
    /// 화면에서 수동 입력하는 구조로 되어 있음.
    /// </summary>
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
        /// 1 USD = ? targetCurrency
        /// targetCurrency: "KRW" or "MXN"
        /// </summary>
        public async Task<decimal?> GetRateAsync(string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
                return null;

            targetCurrency = targetCurrency.ToUpperInvariant();

            // 1 USD = ? KRW
            var usdKrw = await GetKrwPerAsync("USD");
            if (!usdKrw.HasValue)
                return null;

            if (targetCurrency == "KRW")
            {
                // 그대로 KRW 값 리턴
                return decimal.Round(usdKrw.Value, 4);
            }

            if (targetCurrency == "MXN")
            {
                // 1 MXN = ? KRW
                var mxnKrw = await GetKrwPerAsync("MXN");
                if (!mxnKrw.HasValue || mxnKrw.Value == 0)
                    return null;

                // 1 USD = (KRW/USD) / (KRW/MXN) = ? MXN
                var usdToMxn = usdKrw.Value / mxnKrw.Value;
                return decimal.Round(usdToMxn, 4);
            }

            // 그 외 통화는 아직 지원 안 함
            return null;
        }
    }
}
