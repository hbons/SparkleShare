//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public enum PageType {
        None,
        User,
        Privacy,
        Host,
        Address,
        Invite,
        Progress,
        Storage,
        CryptoSetup,
        CryptoPassword
    }


    public partial class PageController {

        public bool WindowIsOpen { get; private set; }

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };


        PageType current_page;
        const int page_delay = 1000;

        public event ChangePageEventHandler ChangePageEvent = delegate { };
        public delegate void ChangePageEventHandler (PageType page);

        public event PageCanContinueEventHandler PageCanContinueEvent = delegate { };
        public delegate void PageCanContinueEventHandler (PageType page_type, bool can_continue);


        public readonly List<Preset> Presets = new List<Preset> ();
        public Preset SelectedPreset;


        public Uri FetchAddress { get; private set; }
        public double ProgressBarPercentage  { get; private set; }


        public SparkleInvite PendingInvite { get; private set; }


        public PageController ()
        {
            LoadPresets ();

            ChangePageEvent += delegate (PageType page_type) {
                this.current_page = page_type;
            };

            SparkleShare.Controller.InviteReceived += delegate (SparkleInvite invite) {
                PendingInvite = invite;

                ChangePageEvent (PageType.Invite);
                ShowWindowEvent ();
            };

            SparkleShare.Controller.ShowSetupWindowEvent += ShowSetupWindowEventHandler;
        }


        void ShowSetupWindowEventHandler (PageType page_type)
        {
            if (page_type == PageType.Storage ||
                page_type == PageType.CryptoSetup ||
                page_type == PageType.CryptoPassword) {

                ChangePageEvent (page_type);
                return;
            }

            if (PendingInvite != null) {
                WindowIsOpen = true;
                ShowWindowEvent ();
                return;
            }

            if (this.current_page == PageType.Progress ||
                this.current_page == PageType.CryptoSetup ||
                this.current_page == PageType.CryptoPassword) {

                ShowWindowEvent ();
                return;
            }

            if (page_type == PageType.Host) {
                if (WindowIsOpen) {
                    if (this.current_page == PageType.None)
                        ChangePageEvent (PageType.Host);

                } else if (!SparkleShare.Controller.FirstRun) {
                    WindowIsOpen = true;
                    ChangePageEvent (PageType.Host);
                }

                ShowWindowEvent ();
                return;
            }

            WindowIsOpen = true;
            ChangePageEvent (page_type);
            ShowWindowEvent ();
        }


        public void QuitClicked ()
        {
            SparkleShare.Controller.Quit ();
        }


        public void CancelClicked (PageType? canceled_page)
        {
            if (canceled_page == PageType.Progress) {
                SparkleShare.Controller.StopFetcher ();

                if (PendingInvite != null)
                    ChangePageEvent (PageType.Invite);
                else
                    ChangePageEvent (PageType.Host);

                return;
            }

            if (canceled_page == PageType.CryptoSetup ||
                canceled_page == PageType.CryptoPassword) {

                CancelClicked (PageType.Progress);
                return;
            }

            Reset ();

            WindowIsOpen = false;
            HideWindowEvent ();
        }


        PageType [] page_order = { PageType.User, PageType.Privacy,
            PageType.Host, PageType.Address, PageType.Progress };

        public void BackClicked (PageType? page_type)
        {
            PageType page = (PageType) page_type;

            int current_index = Array.IndexOf (page_order, page);
            int back_index = current_index - 1;

            if (back_index > -1)
                ChangePageEvent (page_order [back_index]);
        }


        public void Reset ()
        {
            PendingInvite = null;
            SelectedPreset = Presets [0];
            FetchAddress = null;
        }


        public void ProgressPageCompleted ()
        {
            this.current_page = PageType.None;
            Reset ();

            WindowIsOpen = false;
            HideWindowEvent ();
        }
    }
}
