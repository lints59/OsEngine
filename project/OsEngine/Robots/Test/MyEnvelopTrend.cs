/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.IO;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using System.Drawing;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.Trend
{
    /// <summary>
    /// Trend strategy based on indicator Envelop
    /// Трендовая стратегия на основе индикатора конверт(Envelop)
    /// </summary>
    public class MyTest : BotPanel
    {
        public MyTest(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage %", 0, 0, 100, 0.1m);
            Volume = CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);
            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 0.2m, 10, 0.1m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 40, 10, 200, 5);
            TrailStop = CreateParameter("Trail Stop %", 1, 0.1m, 100, 0.1m);

            _sma = new LinearRegressionCurve(name + "LRC1", false);
            _sma = (LinearRegressionCurve)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.Lenght = 8;
            _sma.Save();

            _envelop1 = new Envelops(name + "Envelop1", false);
            _envelop1 = (Envelops)_tab.CreateCandleIndicator(_envelop1, "Prime");
            _envelop1.Save();

            _envelop2 = new Envelops(name + "Envelop2", false);
            _envelop2 = (Envelops)_tab.CreateCandleIndicator(_envelop2, "Prime");
            _envelop2.Save();

            _envelop3 = new Envelops(name + "Envelop3", false);
            _envelop3 = (Envelops)_tab.CreateCandleIndicator(_envelop3, "Prime");
            _envelop3.Save();

            _envelop1.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop1.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            //_envelop1.ColorUp = Color.Red;

            _envelop2.Deviation = EnvelopDeviation.ValueDecimal * 1.5m;
            _envelop2.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            _envelop3.Deviation = EnvelopDeviation.ValueDecimal * 2.5m;
            _envelop3.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            ParametrsChangeByUser += MyTest_ParametrsChangeByUser;
        }

        private void MyTest_ParametrsChangeByUser()
        {
            _envelop1.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop1.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            _envelop2.Deviation = EnvelopDeviation.ValueDecimal * 1.5m;
            _envelop2.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            _envelop3.Deviation = EnvelopDeviation.ValueDecimal * 2.5m;
            _envelop3.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            _envelop1.Reload();
            _envelop2.Reload();
            _envelop3.Reload();
        }

        // public settings / настройки публичные

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterDecimal Slippage;

        /// <summary>
        /// volume for entry
        /// объём для входа
        /// </summary>
        public StrategyParameterDecimal Volume;

        /// <summary>
        /// Envelop deviation from center moving average 
        /// Envelop отклонение от скользящей средней
        /// </summary>
        public StrategyParameterDecimal EnvelopDeviation;

        /// <summary>
        /// moving average length in Envelop 
        /// длинна скользящей средней в конверте
        /// </summary>
        public StrategyParameterInt EnvelopMovingLength;

        /// <summary>
        /// Trail stop length in percent
        /// длинна трейлинг стопа в процентах
        /// </summary>
        public StrategyParameterDecimal TrailStop;

        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;


        // indicators / индикаторы

        private Envelops _envelop1;
        private Envelops _envelop2;
        private Envelops _envelop3;

        private LinearRegressionCurve _sma;

        private decimal _last_envelop1_Up;
        private decimal _last_envelop1_Down;
        private decimal _last_envelop2_Up;
        private decimal _last_envelop2_Down;
        private decimal _last_envelop3_Up;
        private decimal _last_envelop3_Down;
        private decimal _prev_envelop1_Up;
        private decimal _prev_envelop1_Down;
        private decimal _prev_envelop2_Up;
        private decimal _prev_envelop2_Down;
        private decimal _prev_envelop3_Up;
        private decimal _prev_envelop3_Down;
        private decimal _last_sma;
        static int _open_pos_mode;

        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            if (position.Direction == Side.Buy)
            {
                decimal activationPrice = _envelop3.ValuesUp[_envelop3.ValuesUp.Count - 1] -
                    _envelop3.ValuesUp[_envelop3.ValuesUp.Count - 1] * (TrailStop.ValueDecimal / 100);

                decimal orderPrice = activationPrice - _tab.Securiti.PriceStep * Slippage.ValueDecimal;

                _tab.CloseAtTrailingStop(position,
                    activationPrice, orderPrice, "_tab_PositionOpeningSuccesEvent");

                //_tab.CloseAtStop(position, position.EntryPrice - Stop.ValueInt * _tab.Securiti.PriceStep, position.EntryPrice - Stop.ValueInt * _tab.Securiti.PriceStep); _tab.CloseAtStop(position, position.EntryPrice - Stop.ValueInt * _tab.Securiti.PriceStep, position.EntryPrice - Stop.ValueInt * _tab.Securiti.PriceStep);
                //_tab.CloseAtProfit(position, position.EntryPrice + Profit.ValueInt * _tab.Securiti.PriceStep, position.EntryPrice + Profit.ValueInt * _tab.Securiti.PriceStep);
            }
            if (position.Direction == Side.Sell)
            {
                decimal activationPrice = _envelop3.ValuesDown[_envelop3.ValuesDown.Count - 1] +
                    _envelop3.ValuesDown[_envelop3.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);

                decimal orderPrice = activationPrice + _tab.Securiti.PriceStep * Slippage.ValueDecimal;

                _tab.CloseAtTrailingStop(position,
                    activationPrice, orderPrice);
            }


        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString != "On")
            {
                return;
            }

            if(candles.Count +5 < _envelop1.MovingAverage.Lenght)
            {
                return;
            }

            _prev_envelop3_Up = _envelop3.ValuesUp[_envelop3.ValuesUp.Count - 2];
            _prev_envelop3_Down = _envelop3.ValuesDown[_envelop3.ValuesUp.Count - 2];
            _last_envelop3_Up = _envelop3.ValuesUp[_envelop3.ValuesUp.Count - 1];
            _last_envelop3_Down = _envelop3.ValuesDown[_envelop3.ValuesUp.Count - 1];
            _last_sma = _sma.Values[_sma.Values.Count - 1];


            List<Position> positions = _tab.PositionsOpenAll;

            if(positions.Count == 0)
            { // open logic
                if (_prev_envelop3_Down > _last_sma && _last_envelop3_Down <= _last_sma
                                      && Regime.ValueString != "OnlyLong")
                {
                    _tab.BuyAtMarket(Volume.ValueDecimal);
                    //_tab.BuyAtLimit(Volume.ValueDecimal, _lastClose + Slippage.ValueInt * _tab.Securiti.PriceStep);

                    _open_pos_mode = 3;
                }
                //_tab.BuyAtStop(Volume.ValueDecimal,
                //    _envelop1.ValuesUp[_envelop1.ValuesUp.Count - 1] + 
                //    Slippage.ValueInt * _tab.Securiti.PriceStep,
                //    _envelop1.ValuesUp[_envelop1.ValuesUp.Count - 1],
                //    StopActivateType.HigherOrEqual,1);

                //_tab.SellAtStop(Volume.ValueDecimal,
                //     _envelop1.ValuesDown[_envelop1.ValuesDown.Count - 1] -
                //     Slippage.ValueInt * _tab.Securiti.PriceStep,
                //    _envelop1.ValuesDown[_envelop1.ValuesDown.Count - 1],
                //    StopActivateType.LowerOrEqyal, 1);
            }
            else
            { // trail stop logic

                if(positions[0].State != PositionStateType.Open)
                {
                    return;
                }

                if(positions[0].Direction == Side.Buy)
                {
                    decimal activationPrice = _last_envelop3_Up - _last_envelop3_Up * (TrailStop.ValueDecimal / 100);

                    if (_open_pos_mode == 3)
                    {
                        activationPrice = _last_envelop3_Up - _last_envelop3_Up * (TrailStop.ValueDecimal / 100);
                    }
                    else
                    if (_open_pos_mode == 2)
                    {
                        activationPrice = _last_envelop2_Up - _last_envelop2_Up * (TrailStop.ValueDecimal / 100);
                    }
                    else
                    {
                        activationPrice = _last_envelop1_Up -+ _last_envelop1_Up * (TrailStop.ValueDecimal / 100);
                    }



                        decimal orderPrice = activationPrice - _tab.Securiti.PriceStep * Slippage.ValueDecimal;

                    _tab.CloseAtTrailingStop(positions[0], activationPrice, orderPrice);
                }
                if (positions[0].Direction == Side.Sell)
                {
                    decimal activationPrice = _envelop1.ValuesDown[_envelop1.ValuesDown.Count - 1] +
                        _envelop1.ValuesDown[_envelop1.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);

                    decimal orderPrice = activationPrice + _tab.Securiti.PriceStep * Slippage.ValueDecimal;

                    _tab.CloseAtTrailingStop(positions[0],
                        activationPrice, orderPrice);
                }
            }
        }

        public override string GetNameStrategyType()
        {
            return "MyTest";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }

        /// <summary>
        /// tab to trade
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab;
    }
}
