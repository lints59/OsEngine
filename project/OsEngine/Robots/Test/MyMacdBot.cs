using System.Collections.Generic;
using System.Windows;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;

/// <summary>
/// Trend strategy based on the Macd indicator and trail stop
/// Трендовая стратегия на основе индикатора Macd и трейлстопа
/// </summary>
public class MyMacdTrail : BotPanel
{
    public MyMacdTrail(string name, StartProgram startProgram)
        : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];
        _tab.CandleFinishedEvent += Strateg_CandleFinishedEvent;
        _tab.CandleUpdateEvent += Strateg_CandleUpdateEvent;

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
        Volume = CreateParameter("Volume", 1, 1.0m, 50, 4);
        Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
        TrailStop = CreateParameter("Trail Stop Percent", 0.7m, 0.3m, 3, 0.1m);

        paramRSIper = CreateParameter("RSI period", 14, 14, 100, 1);
        paramRSIstoch = CreateParameter("RSI stochastic period", 14, 14, 100, 1);
        paramRSIK = CreateParameter("RSI K", 5, 1, 100, 1);
        paramRSID = CreateParameter("RSI D", 3, 1, 100, 1);

        _macd = IndicatorsFactory.CreateIndicatorByName("MacdLine",name + "MACD", false);
        _macd = (Aindicator)_tab.CreateCandleIndicator(_macd, "MacdArea");
        _macd.Save();

        _rsi = new StochRsi(name + "StochRsi", false);
        _rsi = (StochRsi)_tab.CreateCandleIndicator(_rsi, "RsiArea");
        _rsi.K = paramRSIK.ValueInt;
        _rsi.D = paramRSID.ValueInt;
        _rsi.StochasticLength = paramRSIstoch.ValueInt;
        _rsi.RsiLenght = paramRSIper.ValueInt;
        _rsi.Save();

        _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Bollinger", false);
        _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
        _sma.Save();

        //_stoc = new StochasticOscillator(name + "ST", false);
        //_stoc = (StochasticOscillator)_tab.CreateCandleIndicator(_stoc, "StocArea");
        //_stoc.Save();

        ParametrsChangeByUser += RviTrade_ParametrsChangeByUser; 
        
    }

    void RviTrade_ParametrsChangeByUser()
    {
        _rsi.K = paramRSIK.ValueInt;
        _rsi.D = paramRSID.ValueInt;
        _rsi.StochasticLength = paramRSIstoch.ValueInt;
        _rsi.RsiLenght = paramRSIper.ValueInt;
        _rsi.Save();

        //Upline.Value = UpLineValue.ValueDecimal;
        //Upline.Refresh();

        //Downline.Value = DownLineValue.ValueDecimal;
        //Downline.Refresh();
    }

    /// <summary>
    /// uniq strategy name
    /// взять уникальное имя
    /// </summary>
    public override string GetNameStrategyType()
    {
        return "MyMacdTrail";
    }
    /// <summary>
    /// settings GUI
    /// показать окно настроек
    /// </summary>
    public override void ShowIndividualSettingsDialog()
    {
        MessageBox.Show("У меня нет настроек");
    }
    /// <summary>
    /// tab to trade
    /// вкладка для торговли
    /// </summary>
    private BotTabSimple _tab;

    /// <summary>
    /// StochRsi 
    /// </summary>
    private StochRsi _rsi;

    /// <summary>
    /// Macd 
    /// </summary>
    private Aindicator _macd;
    private Aindicator _sma;

    //settings настройки публичные

    public StrategyParameterDecimal TrailStop;

    public StrategyParameterInt Slippage;

    public StrategyParameterDecimal Volume;

    public StrategyParameterString Regime;

    public StrategyParameterInt paramRSIper;
    public StrategyParameterInt paramRSIstoch;
    public StrategyParameterInt paramRSIK;
    public StrategyParameterInt paramRSID;

    private decimal _lastClose;
    private decimal _lastMacdDown;
    private decimal _lastMacdUp;
    static decimal _lastRsiK;
    static decimal _lastRsiD;
    static decimal _prevRsiK;
    static decimal _prevRsiD;
    static Candle _lastCandle;

    // logic логика

    /// <summary>
    /// candle finished event
    /// событие завершения свечи
    /// </summary>
    private void Strateg_CandleFinishedEvent(List<Candle> candles)
    {

        Strateg_CandleUpdateEvent(candles);
        _lastCandle = candles[candles.Count - 1];
        _prevRsiK = _lastRsiK;
        _prevRsiD = _lastRsiD;

    }

    // logic логика

    /// <summary>
    /// candle finished event
    /// событие завершения свечи
    /// </summary>
    private void Strateg_CandleUpdateEvent(List<Candle> candles)
    {
        if (Regime.ValueString == "Off")
        {
            return;
        }

        if (_macd.DataSeries[0].Values == null )
        {
            return;
        }

        if ( _lastCandle == null)
        {
            _lastCandle = candles[candles.Count - 1];
        }

        //    candles[candles.Count - 1].TimeStart.Month != 12)

        _lastClose = candles[candles.Count - 1].Close;
        _lastRsiK = _rsi.ValuesK[_rsi.ValuesK.Count - 1];
        _lastRsiD = _rsi.ValuesD[_rsi.ValuesD.Count - 1];

        if ((_prevRsiK == 0) && (_prevRsiD == 0))
        {
            return;
        }



        _lastMacdUp = _macd.DataSeries[0].Values[_macd.DataSeries[0].Values.Count - 1];
        _lastMacdDown = _macd.DataSeries[1].Values[_macd.DataSeries[1].Values.Count - 1];

        List<Position> openPositions = _tab.PositionsOpenAll;

        if (openPositions != null && openPositions.Count != 0)
        {
            for (int i = 0; i < openPositions.Count; i++)
            {
                LogicClosePosition(candles, openPositions[i]);

            }
        }

        if (Regime.ValueString == "OnlyClosePosition")
        {
            return;
        }
        if (openPositions == null || openPositions.Count == 0)
        {
            if (candles[candles.Count - 1].TimeStart != _lastCandle.TimeStart)
            {
                LogicOpenPosition(candles, openPositions);
            }
        }
    }

    /// <summary>
    /// logic open position
    /// логика открытия первой позиции
    /// </summary>
    private void LogicOpenPosition(List<Candle> candles, List<Position> position)
    {
        //if (_lastMacdDown < 0 && _lastMacdUp > _lastMacdDown
        //                      && Regime.ValueString != "OnlyShort")
        //{
        //    _tab.BuyAtLimit(Volume.ValueDecimal, _lastClose + Slippage.ValueInt * _tab.Securiti.PriceStep);
        //}

        //   _tab.BuyAtAceberg()

        //if (_sma.DataSeries[0].Values == null ||
        //    _sma.ParametersDigit[0].Value + 3 > _sma.DataSeries[0].Values.Count)

        if (_lastMacdDown > 0 && _lastMacdUp < _lastMacdDown
                              && Regime.ValueString != "OnlyLong")
        {
        //    _tab.SellAtLimit(Volume.ValueDecimal, _lastClose - Slippage.ValueInt * _tab.Securiti.PriceStep);
        }

        //        if (_lastRsiK < 40 && _lastMacdUp < 0 && _lastMacdDown < 0 && _lastRsiK >= _lastRsiD && _prevRsiD > _prevRsiK
  //      if (_lastMacdUp < 0 && _lastMacdDown < 0 && Regime.ValueString != "OnlyShort")
            if (_lastRsiK >= _lastRsiD && _prevRsiD > _prevRsiK)
            {
                _tab.BuyAtLimit(Volume.ValueDecimal, _lastClose + Slippage.ValueInt * _tab.Securiti.PriceStep);
                //_tab.BuyAtMarket(Volume.ValueDecimal);
                _lastCandle = candles[candles.Count - 1];
        }

        //if (_lastRsiD > 60 && _lastRsiK <= _lastRsiD && _prevRsiD < _prevRsiK
        //                      && Regime.ValueString != "OnlyLong")
        //{
        //    //_tab.SellAtLimit(Volume.ValueDecimal, _lastClose - Slippage.ValueInt * _tab.Securiti.PriceStep);
        //    _tab.SellAtMarket(Volume.ValueDecimal);
        //}
    }

    /// <summary>
    /// logic close position
    /// логика зыкрытия позиции
    /// </summary>
    private void LogicClosePosition(List<Candle> candles, Position position)
    {
        //if (position.Direction == Side.Buy)
        //{
        //    _tab.CloseAtTrailingStop(position,
        //        _lastClose - _lastClose * TrailStop.ValueDecimal / 100,
        //        _lastClose - _lastClose * TrailStop.ValueDecimal / 100);
        //}

        //if (position.Direction == Side.Sell)
        //{
        //    _tab.CloseAtTrailingStop(position,
        //        _lastClose + _lastClose * TrailStop.ValueDecimal / 100,
        //        _lastClose + _lastClose * TrailStop.ValueDecimal / 100);
        //}


        //decimal lastClose = candles[candles.Count - 1].Close;
        if (position.Direction == Side.Buy)
        {
            //if (_lastRsiD > 60 && _lastMacdUp > 0 && _lastRsiK <= _lastRsiD && _prevRsiD < _prevRsiK
//            if (_lastMacdUp > 0 && Regime.ValueString != "OnlyLong")
                if (_lastRsiK <= _lastRsiD && _prevRsiD < _prevRsiK)
                {
                    _tab.SellAtLimit(Volume.ValueDecimal, _lastClose - Slippage.ValueInt * _tab.Securiti.PriceStep);
                    //_tab.CloseAtMarket(position, Volume.ValueDecimal);
                }


        //    if (lastClose < _lastMa || _lastRsi > Upline.Value)
        //    {
        //        _tab.CloseAtLimit(position, _lastPrice - Slipage, position.OpenVolume);
        //    }
        }

        //if (position.Direction == Side.Sell)
        //{
        //    if (lastClose > _lastMa || _lastRsi < Downline.Value)
        //    {
        //        _tab.CloseAtLimit(position, _lastPrice + Slipage, position.OpenVolume);

        //    }
        //}
    }
}