using System.Collections.Generic;
using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class MarketService
    {
        private readonly ApiClient _client;

        public MarketService(ApiClient client)
        {
            _client = client;
        }

        public async Task<ApiResult<MarketStateDto>> GetState()
        {
            var result = await _client.Get("/market/state");
            if (!result.Success) return ApiResult<MarketStateDto>.Fail(result.Error);
            return ApiResult<MarketStateDto>.Ok(JsonUtility.FromJson<MarketStateDto>(result.Data));
        }

        public async Task<ApiResult<List<StockDto>>> GetStocks()
        {
            var result = await _client.Get("/market/stocks");
            if (!result.Success) return ApiResult<List<StockDto>>.Fail(result.Error);

            var wrapper = JsonUtility.FromJson<StockListWrapper>($"{{\"items\":{result.Data}}}");
            return ApiResult<List<StockDto>>.Ok(wrapper.items);
        }

        public async Task<ApiResult<List<PricePointDto>>> GetHistory(string stockId)
        {
            var result = await _client.Get($"/market/stocks/{stockId}/history");
            if (!result.Success) return ApiResult<List<PricePointDto>>.Fail(result.Error);

            var wrapper = JsonUtility.FromJson<PricePointListWrapper>($"{{\"items\":{result.Data}}}");
            return ApiResult<List<PricePointDto>>.Ok(wrapper.items);
        }

        public async Task<ApiResult<List<NewsDto>>> GetNews()
        {
            var result = await _client.Get("/market/news");
            if (!result.Success) return ApiResult<List<NewsDto>>.Fail(result.Error);

            var wrapper = JsonUtility.FromJson<NewsListWrapper>($"{{\"items\":{result.Data}}}");
            return ApiResult<List<NewsDto>>.Ok(wrapper.items);
        }

        public async Task<ApiResult<PortfolioDto>> GetPortfolio()
        {
            var result = await _client.Get("/market/portfolio");
            if (!result.Success) return ApiResult<PortfolioDto>.Fail(result.Error);
            return ApiResult<PortfolioDto>.Ok(JsonUtility.FromJson<PortfolioDto>(result.Data));
        }

        public async Task<ApiResult<List<LeaderboardEntryDto>>> GetLeaderboard()
        {
            var result = await _client.Get("/market/leaderboard");
            if (!result.Success) return ApiResult<List<LeaderboardEntryDto>>.Fail(result.Error);
            var wrapper = JsonUtility.FromJson<LeaderboardListWrapper>($"{{\"items\":{result.Data}}}");
            return ApiResult<List<LeaderboardEntryDto>>.Ok(wrapper.items);
        }

        public async Task<ApiResult<PortfolioDto>> PlaceOrder(string stockId, string type, double quantity)
        {
            var req = new OrderRequest { stockId = stockId, type = type, quantity = quantity };
            var result = await _client.Post("/market/order", req);
            if (!result.Success) return ApiResult<PortfolioDto>.Fail(result.Error);
            return ApiResult<PortfolioDto>.Ok(JsonUtility.FromJson<PortfolioDto>(result.Data));
        }
    }
}
