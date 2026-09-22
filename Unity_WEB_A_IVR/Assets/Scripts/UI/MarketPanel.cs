using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IVR.Models;
using UnityEngine.UIElements;

namespace IVR.UI
{
    // Внутренний экран биржи. Отдельный класс оставляет навигацию HubScreen простой.
    public sealed class MarketPanel
    {
        private readonly VisualElement _root;
        private readonly VisualElement _stockList;
        private readonly VisualElement _newsList;
        private readonly VisualElement _detail;
        private readonly Label _status;
        private readonly IntegerField _quantity;
        private readonly Button[] _tradeButtons;

        private PortfolioDto _portfolio;
        private StockDto _selected;

        public MarketPanel(VisualElement root, Action back)
        {
            _root = root;
            _stockList = root.Q<VisualElement>("market-stock-list");
            _newsList = root.Q<VisualElement>("market-news-list");
            _detail = root.Q<VisualElement>("market-detail");
            _status = root.Q<Label>("market-status");
            _quantity = root.Q<IntegerField>("market-quantity");
            _tradeButtons = new[]
            {
                root.Q<Button>("market-buy"), root.Q<Button>("market-sell"),
                root.Q<Button>("market-short"), root.Q<Button>("market-cover")
            };

            root.Q<Button>("market-back").clicked += back;
            _tradeButtons[0].clicked += () => _ = Trade("BUY");
            _tradeButtons[1].clicked += () => _ = Trade("SELL");
            _tradeButtons[2].clicked += () => _ = Trade("SHORT");
            _tradeButtons[3].clicked += () => _ = Trade("COVER");
        }

        public async Task Refresh()
        {
            _status.text = "Загружаем рынок...";
            _stockList.Clear();
            _newsList.Clear();
            _detail.style.display = DisplayStyle.None;
            _selected = null;
            var stateTask = IvrApi.Market.GetState();
            var stocksTask = IvrApi.Market.GetStocks();
            var portfolioTask = IvrApi.Market.GetPortfolio();
            var newsTask = IvrApi.Market.GetNews();
            await Task.WhenAll(stateTask, stocksTask, portfolioTask, newsTask);

            var state = stateTask.Result;
            var stocks = stocksTask.Result;
            var portfolio = portfolioTask.Result;
            var news = newsTask.Result;
            if (!state.Success || !stocks.Success || !portfolio.Success || !news.Success)
            {
                _status.text = "Не удалось загрузить рынок. Вернись и открой экран снова.";
                return;
            }

            _portfolio = portfolio.Data;
            _root.Q<Label>("market-day").text = "Игровой день " + state.Data.currentDay;
            RenderBalance();
            RenderStocks(stocks.Data);
            RenderNews(news.Data);
            _status.text = "";
        }

        private void RenderBalance()
        {
            if (_portfolio == null) return;
            _root.Q<Label>("market-cash").text = "Деньги: " + _portfolio.cash.ToString("F0") + " ₸";
            _root.Q<Label>("market-total").text = "Капитал: " + _portfolio.totalValue.ToString("F0") + " ₸";
        }

        private void RenderStocks(List<StockDto> stocks)
        {
            _stockList.Clear();
            foreach (var stock in stocks)
            {
                var captured = stock;
                var sign = stock.changePercent >= 0 ? "+" : "";
                var button = new Button(() => SelectStock(captured))
                {
                    text = stock.ticker + " · " + stock.name + "     " + stock.price.ToString("F2") + " ₸  (" + sign + stock.changePercent.ToString("F1") + "%)"
                };
                button.AddToClassList("hub-market-stock");
                _stockList.Add(button);
            }
            if (stocks.Count == 0)
                _stockList.Add(new Label("Акций пока нет."));
        }

        private void RenderNews(List<NewsDto> news)
        {
            _newsList.Clear();
            int count = Math.Min(news.Count, 5);
            for (int i = 0; i < count; i++)
            {
                var item = news[i];
                var row = new VisualElement();
                row.AddToClassList("hub-market-news");
                var source = new Label("День " + item.day + " · " + item.sectorName);
                source.AddToClassList("hub-muted");
                var headline = new Label(item.headline);
                headline.AddToClassList("hub-quest-title");
                row.Add(source);
                row.Add(headline);
                _newsList.Add(row);
            }
            if (count == 0)
            {
                var empty = new Label("Новости появятся после первого игрового дня.");
                empty.AddToClassList("hub-muted");
                _newsList.Add(empty);
            }
        }

        private void SelectStock(StockDto stock)
        {
            _selected = stock;
            _detail.style.display = DisplayStyle.Flex;
            _root.Q<Label>("market-selected").text = stock.ticker + " · " + stock.name + " — " + stock.price.ToString("F2") + " ₸";
            var holding = _portfolio?.holdings?.Find(h => h.stockId == stock.id);
            _root.Q<Label>("market-position").text = holding == null
                ? "У тебя пока нет этой акции"
                : "Позиция: " + Math.Abs(holding.quantity).ToString("F0") + " шт. (" +
                  (holding.quantity > 0 ? "покупка" : "шорт") + ")";
        }

        private async Task Trade(string type)
        {
            if (_selected == null) return;
            if (_quantity.value <= 0)
            {
                _status.text = "Количество должно быть больше нуля.";
                return;
            }

            foreach (var button in _tradeButtons) button.SetEnabled(false);
            _status.text = "Выполняем сделку...";
            try
            {
                var result = await IvrApi.Market.PlaceOrder(_selected.id, type, _quantity.value);
                if (!result.Success)
                {
                    _status.text = "Сделка не выполнена: " + result.Error;
                    return;
                }
                _portfolio = result.Data;
                RenderBalance();
                SelectStock(_selected);
                _status.text = "Сделка выполнена.";
            }
            finally
            {
                foreach (var button in _tradeButtons) button.SetEnabled(true);
            }
        }
    }
}
