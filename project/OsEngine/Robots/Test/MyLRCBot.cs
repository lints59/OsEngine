using System.Collections.Generic;
using System.Windows;
using OsEngine.Entity;
using System.Drawing;
using OsEngine.Indicators;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;

/// <summary>
/// Trend strategy based on the Macd indicator and trail stop
/// “рендова€ стратеги€ на основе индикатора Macd и трейлстопа
/// </summary>
public class MyLRCBot : BotPanel
{
    public MyLRCBot(string name, StartProgram startProgram)
        : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];
        _tab.CandleFinishedEvent += Strateg_CandleFinishedEvent;
        //_tab.CandleUpdateEvent += Strateg_CandleUpdateEvent;

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
        Volume = CreateParameter("Volume", 1, 1.0m, 50, 4);
        Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
        TrailStop = CreateParameter("Trail Stop Percent", 0.7m, 0.3m, 3, 0.1m);

        paramTrixHiPeriod = CreateParameter("Hi period", 9, 3, 50, 1);
        paramTrixLowPeriod = CreateParameter("Low period", 81, 3, 50, 1);
        paramTrixHi = CreateParameter("Trix Hi", 0.1m, 0.01m, 100, 0.01m);
        paramTrixLow = CreateParameter("Trix Low", 0.1m, 0.01m, 100, 0.01m);

        _TrixHi = new LinearRegressionCurve(name + "LRC1", false);
        _TrixHi = (LinearRegressionCurve)_tab.CreateCandleIndicator(_TrixHi, "Prime");
        //_TrixHi = IndicatorsFactory.CreateIndicatorByName("LinearRegressionCurve", name + "Trix", false);
        //_TrixHi = (Aindicator)_tab.CreateCandleIndicator(_TrixHi, "TrixArea");
        _TrixHi.Lenght = paramTrixHiPeriod.ValueInt;
        _TrixHi.ColorBase = Color.Red;
        _TrixHi.Save();

        _TrixLow = new LinearRegressionCurve(name + "LRC2", false);
        _TrixLow = (LinearRegressionCurve)_tab.CreateCandleIndicator(_TrixLow, "Prime");
        //_TrixLow = IndicatorsFactory.CreateIndicatorByName("LinearRegressionCurve", name + "TrixLow", false);
        //_TrixLow = (Aindicator)_tab.CreateCandleIndicator(_TrixLow, "TrixArea");
        _TrixLow.Lenght = paramTrixLowPeriod.ValueInt;
        _TrixLow.ColorBase = Color.LightBlue;
        _TrixLow.Save();

        //_frac = new Fractal(name + "Fractal", false);
        //_frac = (Fractal)_tab.CreateCandleIndicator(_frac, "Prime");
        //_frac.Save();



        //_rsi = new StochRsi(name + "StochRsi", false);
        //_rsi = (StochRsi)_tab.CreateCandleIndicator(_rsi, "RsiArea");
        //_rsi.K = paramRSIK.ValueInt;
        //_rsi.D = paramRSID.ValueInt;
        //_rsi.StochasticLength = paramRSIstoch.ValueInt;
        //_rsi.RsiLenght = paramRSIper.ValueInt;
        //_rsi.Save();

        //_sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Bollinger", false);
        //_sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
        //_sma.Save();

        //_stoc = new StochasticOscillator(name + "ST", false);
        //_stoc = (StochasticOscillator)_tab.CreateCandleIndicator(_stoc, "StocArea");
        //_stoc.Save();

        ParametrsChangeByUser += BotTest_ParametrsChangeByUser; 
        
    }

    void BotTest_ParametrsChangeByUser()
    {

        if (_TrixHi.Lenght != paramTrixHiPeriod.ValueInt)
        {
            _TrixHi.Lenght = paramTrixHiPeriod.ValueInt;
            _TrixHi.Reload();
        }

        if (_TrixLow.Lenght != paramTrixLowPeriod.ValueInt)
        {
            _TrixLow.Lenght = paramTrixLowPeriod.ValueInt;
            _TrixLow.Reload();
        }

        //Upline.Value = UpLineValue.ValueDecimal;
        //Upline.Refresh();

        //Downline.Value = DownLineValue.ValueDecimal;
        //Downline.Refresh();
    }

    /// <summary>
    /// uniq strategy name
    /// вз€ть уникальное им€
    /// </summary>
    public override string GetNameStrategyType()
    {
        return "MyLRCBot";
    }
    /// <summary>
    /// settings GUI
    /// показать окно настроек
    /// </summary>
    public override void ShowIndividualSettingsDialog()
    {
        MessageBox.Show("” мен€ нет настроек");
    }
    /// <summary>
    /// tab to trade
    /// вкладка дл€ торговли
    /// </summary>
    private BotTabSimple _tab;

    /// <summary>
    /// Fractal 
    /// </summary>
    //private Fractal  _frac;

    /// StochRsi 
    /// </summary>
    //private StochRsi _rsi;

    /// <summary>
    /// Macd 
    /// </summary>
    private LinearRegressionCurve _TrixHi;
    private LinearRegressionCurve _TrixLow;
    //private Aindicator _sma;

    //settings настройки публичные

    public StrategyParameterDecimal TrailStop;

    public StrategyParameterInt Slippage;

    public StrategyParameterDecimal Volume;

    public StrategyParameterString Regime;

    public StrategyParameterInt paramTrixHiPeriod;
    public StrategyParameterInt paramTrixLowPeriod;
    public StrategyParameterDecimal paramTrixHi;
    public StrategyParameterDecimal paramTrixLow;

    private decimal _lastClose;
    private decimal _lastTrixHi;
    private decimal _prevTrixHi;
    private decimal _lastTrixLow;
    private decimal _prevTrixLow;
    static int _TrixHi_min_pos;
    static int _TrixHi_max_pos;
    static int _TrixLow_min_pos;
    static int _TrixLow_max_pos;
    static bool _TrixHi_Up = false;
    static bool _TrixLow_Up = false;
    static int _TrixHi_max_period;
    static int _TrixHi_min_period;
    static int _TrixLow_max_period;
    static int _TrixLow_min_period;
    static bool _Trix_seek_Hi = false;
    static Candle _lastCandle;

    // logic логика

    /// <summary>
    /// candle finished event
    /// событие завершени€ свечи
    /// </summary>
    //private void Strateg_CandleFinishedEvent(List<Candle> candles)
    //{

    //    Strateg_CandleUpdateEvent(candles);
    //    _lastCandle = candles[candles.Count - 1];
    //    _prevRsiK = _lastRsiK;
    //    _prevRsiD = _lastRsiD;

    //}

    // logic логика

    /// <summary>
    /// candle finished event
    /// событие завершени€ свечи
    /// </summary>
//    private void Strateg_CandleUpdateEvent(List<Candle> candles)
    private void Strateg_CandleFinishedEvent(List<Candle> candles)
    {
        if (Regime.ValueString == "Off")
        {
            return;
        }

        //if ((_frac.ValuesUp == null) || (_frac.ValuesDown.Count <= 3))
        //{
        //    return;
        //}

        if (_TrixHi.Values == null)
        {
            return;
        }

        //задержка на вход по периоду
        if (_TrixHi.Values.Count < _TrixHi.Lenght  * 8)
        {
            return;

        }

        //задержка на вход по периоду
        if (_TrixLow.Values.Count < _TrixLow.Lenght * 8)
        {
            return;

        }

        if ( _lastCandle == null)
        {
            _lastCandle = candles[candles.Count - 1];
        }

        //    candles[candles.Count - 1].TimeStart.Month != 12)

        _lastClose = candles[candles.Count - 1].Close;
        _lastTrixHi = _TrixHi.Values[_TrixHi.Values.Count - 1];
        _prevTrixHi = _TrixHi.Values[_TrixHi.Values.Count - 2];
        _lastTrixLow = _TrixLow.Values[_TrixLow.Values.Count - 1];
        _prevTrixLow = _TrixLow.Values[_TrixLow.Values.Count - 2];

        if (_TrixHi_Up == true)
        { // раньше ехали вверх
            if (_lastTrixHi < _prevTrixHi)
            {   // максимум
                _TrixHi_Up = false;
                _TrixHi_max_period = candles.Count - _TrixHi_max_pos;
                _TrixHi_max_pos = candles.Count;
            }
        }
        else
        { // раньше ехали вниз
            if (_lastTrixHi >= _prevTrixHi)
            {   // минимум
                _TrixHi_Up = true;
                _TrixHi_min_period = candles.Count - _TrixHi_min_pos;
                _TrixHi_min_pos = candles.Count;
            }
        }

        if (_TrixLow_Up == true)
        { // раньше ехали вверх
            if (_lastTrixLow < _prevTrixLow)
            {   // максимум
                _TrixLow_Up = false;
                _TrixLow_max_period = candles.Count - _TrixLow_max_pos;
                _TrixLow_max_pos = candles.Count;
            }
        }
        else
        { // раньше ехали вниз
            if (_lastTrixLow >= _prevTrixLow)
            {   // минимум
                _TrixLow_Up = true;
                _TrixLow_min_period = candles.Count - _TrixLow_min_pos;
                _TrixLow_min_pos = candles.Count;
            }
        }

        //_lastMacdUp = _macd.DataSeries[0].Values[_macd.DataSeries[0].Values.Count - 1];77
        //_lastMacdDown = _macd.DataSeries[1].Values[_macd.DataSeries[1].Values.Count - 1];

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
        if (openPositions == null || openPositions.Count < 1)
        {
           // if (candles[candles.Count - 1].TimeStart != _lastCandle.TimeStart)
            {
                LogicOpenPosition(candles, openPositions);
            }
        }
    }

    /// <summary>
    /// logic open position
    /// логика открыти€ первой позиции
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

        //if (_TrixLow_min_pos == candles.Count && _TrixLow_min_period > _TrixLow.Lenght  * 3 && _TrixLow_max_period > _TrixLow.Lenght * 3
        //                      && Regime.ValueString != "OnlyLong")
        //{
        //    _Trix_seek_Hi = true;
        //    //_tab.BuyAtLimit(Volume.ValueDecimal, _lastClose + Slippage.ValueInt * _tab.Securiti.PriceStep);
        //}
        //else
        if ( _TrixHi_min_pos == candles.Count
                              && Regime.ValueString != "OnlyLong")
        {
            _Trix_seek_Hi = false;
            //_tab.BuyAtLimit(Volume.ValueDecimal, _lastClose + Slippage.ValueInt * _tab.Securiti.PriceStep);
            _tab.BuyAtMarket(Volume.ValueDecimal);
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
    /// логика зыкрыти€ позиции
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
//            if (((_lastTrixHi < _prevTrixHi && _lastTrixHi > paramTrixHi.ValueDecimal) || (_lastTrixHi > paramTrixHi.ValueDecimal * 1.5m))
            if (_TrixHi_max_pos == candles.Count
                            && Regime.ValueString != "OnlyLong")
                {
                    //_tab.CloseAtLimit(position, _lastClose - Slippage.ValueInt * _tab.Securiti.PriceStep, Volume.ValueDecimal);
                    _tab.CloseAtMarket(position, Volume.ValueDecimal);
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