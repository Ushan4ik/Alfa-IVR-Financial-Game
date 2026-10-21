using System.Collections.Generic;
using System.Threading.Tasks;
using IVR;
using IVR.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace IVR.Game
{
    public class StockMarketUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private ScrollView _stockList;
        private ScrollView _newsList;

        private Label _dayText, _cashText, _totalText;
        private VisualElement _detailEmpty, _detailContent, _detailHoldingRow;
        private Label _detailTitle, _detailPrice, _detailHolding, _tradeStatus;
        private IntegerField _qtyField;
        private Button _buyBtn, _sellBtn, _shortBtn, _coverBtn;

        private List<StockDto> _stocks = new();
        private PortfolioDto _portfolio;
        private MarketStateDto _state;
        private StockDto _selectedStock;
        private bool _tradeInProgress;

        private void Start()
        {
            if (uiDocument == null) { Debug.LogError("UIDocument не назначен!"); return; }

            _root = uiDocument.rootVisualElement;

            _stockList = _root.Q<ScrollView>("stock-list");
            _newsList  = _root.Q<ScrollView>("news-list");

            _dayText   = _root.Q<Label>("day-text");
            _cashText  = _root.Q<Label>("cash-text");
            _totalText = _root.Q<Label>("total-text");

            _detailEmpty      = _root.Q<VisualElement>("detail-empty");
            _detailContent    = _root.Q<VisualElement>("detail-content");
            _detailHoldingRow = _root.Q<VisualElement>("detail-holding-row");
            _detailTitle      = _root.Q<Label>("detail-title");
            _detailPrice      = _root.Q<Label>("detail-price");
            _detailHolding    = _root.Q<Label>("detail-holding");
            _tradeStatus      = _root.Q<Label>("trade-status");
            _qtyField         = _root.Q<IntegerField>("qty-field");

            _buyBtn   = _root.Q<Button>("buy-btn");
            _sellBtn  = _root.Q<Button>("sell-btn");
            _shortBtn = _root.Q<Button>("short-btn");
            _coverBtn = _root.Q<Button>("cover-btn");

            _buyBtn.clicked   += () => _ = Trade("BUY");
            _sellBtn.clicked  += () => _ = Trade("SELL");
            _shortBtn.clicked += () => _ = Trade("SHORT");
            _coverBtn.clicked += () => _ = Trade("COVER");

            _root.Q<Button>("leaderboard-btn").clicked += () => GameManager.Instance.LoadGame("Leaderboard");
            _root.Q<Button>("exit-btn").clicked += () => GameManager.Instance.BackToMain();

            _ = LoadAll();
        }

        private async Task LoadAll()
        {
            var stateResult = await IvrApi.Market.GetState();
            if (stateResult.Success) _state = stateResult.Data;

            var stocksResult = await IvrApi.Market.GetStocks();
            if (stocksResult.Success) _stocks = stocksResult.Data;

            var portfolioResult = await IvrApi.Market.GetPortfolio();
            if (portfolioResult.Success) _portfolio = portfolioResult.Data;

            var newsResult = await IvrApi.Market.GetNews();

            BuildStockRows();
            if (newsResult.Success) BuildNewsRows(newsResult.Data);
            RefreshHeader();
        }

        private void RefreshHeader()
        {
            if (_state != null) _dayText.text = $"День {_state.currentDay}";
            if (_portfolio != null)
            {
                _cashText.text  = $"Наличные: {_portfolio.cash:F0} ₸";
                _totalText.text = $"Капитал: {_portfolio.totalValue:F0} ₸";
            }
        }

        private void BuildStockRows()
        {
            _stockList.Clear();
            foreach (var stock in _stocks)
            {
                var row = new VisualElement();
                row.AddToClassList("profile-row");
                row.style.cursor = new StyleCursor(StyleKeyword.Auto);

                var left = new VisualElement();
                var name = new Label($"{stock.ticker} — {stock.name}");
                name.AddToClassList("profile-value");
                var sector = new Label(stock.sectorName);
                sector.AddToClassList("profile-label");
                left.Add(name);
                left.Add(sector);

                var right = new VisualElement();
                right.style.alignItems = Align.FlexEnd;
                var price = new Label($"{stock.price:F2} ₸");
                price.AddToClassList("profile-value");
                var change = new Label(FormatChange(stock.changePercent));
                change.style.color = ChangeColor(stock.changePercent);
                right.Add(price);
                right.Add(change);

                row.Add(left);
                row.Add(right);

                var captured = stock;
                row.RegisterCallback<ClickEvent>(_ => SelectStock(captured));

                _stockList.Add(row);
            }
        }

        private void BuildNewsRows(List<NewsDto> news)
        {
            _newsList.Clear();
            foreach (var item in news)
            {
                var row = new VisualElement();
                row.AddToClassList("profile-row");

                var header = new Label($"День {item.day} · {item.sectorName}");
                header.AddToClassList("profile-label");

                var headline = new Label(item.headline);
                headline.AddToClassList("profile-value");
                headline.style.whiteSpace = WhiteSpace.Normal;
                headline.style.color = ChangeColor(item.sentiment);

                row.Add(header);
                row.Add(headline);
                _newsList.Add(row);
            }
        }

        private void SelectStock(StockDto stock)
        {
            _selectedStock = stock;
            _tradeStatus.text = "";

            _detailEmpty.style.display = DisplayStyle.None;
            _detailContent.style.display = DisplayStyle.Flex;

            _detailTitle.text = $"{stock.ticker} — {stock.name} ({stock.sectorName})";
            _detailPrice.text = $"{stock.price:F2} ₸  ({FormatChange(stock.changePercent)})";
            _detailPrice.style.color = ChangeColor(stock.changePercent);

            var holding = _portfolio?.holdings.Find(h => h.stockId == stock.id);
            if (holding != null)
            {
                _detailHoldingRow.style.display = DisplayStyle.Flex;
                var side = holding.quantity > 0 ? "лонг" : "шорт";
                _detailHolding.text = $"{System.Math.Abs(holding.quantity):F0} шт. ({side}), P/L {holding.unrealizedPnl:F0} ₸";
                _detailHolding.style.color = ChangeColor(holding.unrealizedPnl);
            }
            else
            {
                _detailHoldingRow.style.display = DisplayStyle.None;
            }
        }

        private async Task Trade(string type)
        {
            if (_selectedStock == null || _tradeInProgress) return;
            int qty = _qtyField.value;
            if (qty <= 0)
            {
                _tradeStatus.text = "Укажи количество больше нуля";
                _tradeStatus.style.color = ChangeColor(-1);
                return;
            }

            _tradeInProgress = true;
            SetTradeButtonsEnabled(false);
            _tradeStatus.text = "Выполняем сделку...";
            try
            {
                var result = await IvrApi.Market.PlaceOrder(_selectedStock.id, type, qty);
                if (!result.Success)
                {
                    _tradeStatus.text = "Сделка не выполнена: " + result.Error;
                    _tradeStatus.style.color = ChangeColor(-1);
                    return;
                }

                _portfolio = result.Data;
                _tradeStatus.text = "Сделка выполнена";
                _tradeStatus.style.color = ChangeColor(1);

                RefreshHeader();
                SelectStock(_selectedStock);
            }
            finally
            {
                _tradeInProgress = false;
                SetTradeButtonsEnabled(true);
            }
        }

        private void SetTradeButtonsEnabled(bool enabled)
        {
            _buyBtn.SetEnabled(enabled);
            _sellBtn.SetEnabled(enabled);
            _shortBtn.SetEnabled(enabled);
            _coverBtn.SetEnabled(enabled);
        }

        private static string FormatChange(double percent) => (percent >= 0 ? "+" : "") + $"{percent:F2}%";

        private static StyleColor ChangeColor(double value) => new StyleColor(
            value >= 0 ? new Color(0.2f, 0.8f, 0.4f) : new Color(1f, 0.3f, 0.3f)
        );
    }
}
