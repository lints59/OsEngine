﻿/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Market.Servers;

namespace OsEngine.Market
{
    /// <summary>
    /// Логика взаимодействия для ServerPrimeUi.xaml
    /// </summary>
    public partial class ServerMasterUi
    {
        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="isTester">вызывается ли метод из тестера</param>
        public ServerMasterUi(bool isTester)
        {
            InitializeComponent();

            List<IServer> servers = ServerMaster.GetServers();

            if (isTester)
            {
                servers = ServerMaster.GetServers();

                if (servers == null ||
                    servers.Find(s => s.ServerType == ServerType.Tester) == null)
                {
                    ServerMaster.CreateServer(ServerType.Tester, false);
                }

                Close();
                
                servers = ServerMaster.GetServers();
                servers.Find(s => s.ServerType == ServerType.Tester).ShowDialog();
            }

            CreateSourceGrid();
            RePaintSourceGrid();

            CheckBoxServerAutoOpen.IsChecked = ServerMaster.NeadToConnectAuto;
            CheckBoxServerAutoOpen.Click += CheckBoxServerAutoOpen_Click;

            ServerMaster.Log.StartPaint(HostLog);

            for (int i = 0; servers != null && i < servers.Count; i++)
            {
                servers[i].ConnectStatusChangeEvent += ServerStatusChangeEvent;
            }

            Title = OsLocalization.Market.TitleServerMasterUi;
            TabItem1.Header = OsLocalization.Market.TabItem1;
            TabItem2.Header = OsLocalization.Market.TabItem2;
            CheckBoxServerAutoOpen.Content = OsLocalization.Market.Label20;

            ServerMaster.ServerCreateEvent += ServerMasterOnServerCreateEvent;
        }

        private void ServerMasterOnServerCreateEvent(IServer newServer)
        {
            List<IServer> servers = ServerMaster.GetServers();

            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].ServerType == ServerType.Optimizer)
                {
                    continue;
                }
                servers[i].ConnectStatusChangeEvent -= ServerStatusChangeEvent;
                servers[i].ConnectStatusChangeEvent += ServerStatusChangeEvent;
            }
            RePaintSourceGrid();
        }

        void CheckBoxServerAutoOpen_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxServerAutoOpen.IsChecked.HasValue)
            {
                ServerMaster.NeadToConnectAuto = CheckBoxServerAutoOpen.IsChecked.Value;
            }
            ServerMaster.Save();
        }

        /// <summary>
        /// таблица источников
        /// </summary>
        private DataGridView _gridSources;

        /// <summary>
        /// сохдать таблицу источников
        /// </summary>
        private void CreateSourceGrid()
        {
            DataGridView newGrid = DataGridFactory.GetDataGridServers();

            _gridSources = newGrid;
            _gridSources.DoubleClick += _gridSources_DoubleClick;
            HostSource.Child = _gridSources;
            HostSource.VerticalAlignment = VerticalAlignment.Top;
        }

        /// <summary>
        /// перерисовать таблицу источников
        /// </summary>
        private void RePaintSourceGrid()
        {
            if (_gridSources.InvokeRequired)
            {
                _gridSources.Invoke(new Action(RePaintSourceGrid));
                return;
            }

            _gridSources.Rows.Clear();

            List<IServer> servers = ServerMaster.GetServers();

            List<ServerType> serverTypes = ServerMaster.ServersTypes;

            for (int i = 0; servers != null && i < servers.Count; i++)
            {
                DataGridViewRow row1 = new DataGridViewRow();
                row1.Cells.Add(new DataGridViewTextBoxCell());
                row1.Cells[0].Value = servers[i].ServerType;
                row1.Cells.Add(new DataGridViewTextBoxCell());
                row1.Cells[1].Value = servers[i].ServerStatus;
                _gridSources.Rows.Add(row1);

                serverTypes.Remove(serverTypes.Find(s => s == servers[i].ServerType));

                if (servers[i].ServerStatus == ServerConnectStatus.Connect)
                {
                    DataGridViewCellStyle style = new DataGridViewCellStyle();
                    style.BackColor = Color.MediumSeaGreen;
                    style.SelectionBackColor = Color.Green;
                    style.ForeColor = Color.Black;
                    style.SelectionForeColor = Color.Black;
                    row1.Cells[1].Style = style;
                    row1.Cells[0].Style = style;
                }
                else
                {
                    DataGridViewCellStyle style = new DataGridViewCellStyle();
                    style.BackColor = Color.Coral;
                    style.SelectionBackColor = Color.Chocolate;
                    style.ForeColor = Color.Black;
                    style.SelectionForeColor = Color.Black;
                    row1.Cells[1].Style = style;
                    row1.Cells[0].Style = style;
                }
            }

            for (int i = 0; i < serverTypes.Count; i++)
            {
                DataGridViewRow row1 = new DataGridViewRow();
                row1.Cells.Add(new DataGridViewTextBoxCell());
                row1.Cells[0].Value = serverTypes[i].ToString();
                row1.Cells.Add(new DataGridViewTextBoxCell());
                row1.Cells[1].Value = "Disabled";
                _gridSources.Rows.Add(row1);
            }
        }

        /// <summary>
        /// событие двойного клика на таблицу источников
        /// </summary>
        void _gridSources_DoubleClick(object sender, EventArgs e)
        {
            if (_gridSources.CurrentCell.RowIndex <= -1)
            {
                return;
            }

            ServerType type;
            Enum.TryParse(_gridSources.SelectedRows[0].Cells[0].Value.ToString(),out type);

            List<IServer> servers = ServerMaster.GetServers();

            if (servers == null ||
                servers.Find(serv => serv.ServerType == type) == null)
            {
                // нужно впервые создать сервер
                ServerMaster.CreateServer(type, true);

                servers = ServerMaster.GetServers();

                if (servers == null)
                { // чтото пошло не так
                    return;
                }
                else
                { // подписываемся на событие изменения статуса
                    IServer myServ = servers.Find(serv => serv.ServerType == type);

                    if (myServ != null)
                    {
                        myServ.ConnectStatusChangeEvent += ServerStatusChangeEvent;
                    } 
                }
            }

            IServer myServer = servers.Find(serv => serv.ServerType == type);

            if (myServer == null)
            {
                return;
            }

             myServer.ShowDialog();
        }

        /// <summary>
        /// событие измениня статуса сервера
        /// </summary>
        /// <param name="newState"></param>
        void ServerStatusChangeEvent(string newState)
        {
            RePaintSourceGrid();
        }
    }
}
