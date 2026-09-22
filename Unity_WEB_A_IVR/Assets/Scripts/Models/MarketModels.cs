using System;
using System.Collections.Generic;

namespace IVR.Models
{
    [Serializable]
    public class StockDto
    {
        public string id;
        public string ticker;
        public string name;
        public string sector;
        public string sectorName;
        public double price;
        public double changePercent;
    }

    [Serializable]
    public class StockListWrapper
    {
        public List<StockDto> items;
    }

    [Serializable]
    public class PricePointDto
    {
        public int day;
        public double price;
    }

    [Serializable]
    public class PricePointListWrapper
    {
        public List<PricePointDto> items;
    }

    [Serializable]
    public class NewsDto
    {
        public string id;
        public int day;
        public string sector;
        public string sectorName;
        public string headline;
        public int sentiment;
        public string createdAt;
    }

    [Serializable]
    public class NewsListWrapper
    {
        public List<NewsDto> items;
    }

    [Serializable]
    public class HoldingDto
    {
        public string stockId;
        public string ticker;
        public string name;
        public double quantity;
        public double avgPrice;
        public double currentPrice;
        public double unrealizedPnl;
    }

    [Serializable]
    public class PortfolioDto
    {
        public double cash;
        public double holdingsValue;
        public double totalValue;
        public List<HoldingDto> holdings;
    }

    // type: "BUY" | "SELL" | "SHORT" | "COVER"
    [Serializable]
    public class OrderRequest
    {
        public string stockId;
        public string type;
        public double quantity;
    }

    [Serializable]
    public class MarketStateDto
    {
        public int currentDay;
        public int tickIntervalMinutes;
        public string lastTickAt;
    }

    [Serializable]
    public class LeaderboardEntryDto
    {
        public int rank;
        public string playerId;
        public string nickname;
        public double cash;
        public double holdingsValue;
        public double totalValue;
    }

    [Serializable]
    public class LeaderboardListWrapper
    {
        public List<LeaderboardEntryDto> items;
    }
}
