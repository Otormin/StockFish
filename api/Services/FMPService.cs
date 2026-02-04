using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Newtonsoft.Json;

namespace api.Services
{
    public class FMPService : IFMPService
    {
        private readonly HttpClient _httpClient;
        private readonly string FMPKey;
        private readonly string FMPUrl;
        private readonly IConfiguration _config;
        public FMPService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            FMPKey = _config["FMPKey"];
            FMPUrl = _config["FMPUrl"];
        }
        public async Task<Stock> FindStockBySymbolAsync(string symbol)
        {
            try
            {
                var result = await _httpClient.GetAsync($"{FMPUrl}?symbol={symbol}&apikey={FMPKey}");
                                
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var tasks = JsonConvert.DeserializeObject<FMPStock[]>(content);
                    var stock = tasks[0];

                    if(stock != null)
                    {
                        return stock.ToStockFromFMP();
                    }
                    return null;
                }
                return null;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}