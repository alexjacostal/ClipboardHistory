﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipboardHistoryApp.Classes;

namespace ClipboardHistoryApp.Controls
{
    public partial class MyControl : IDisposable
    {
        #region Fields
        private IntPtr _visualStudioHandle = IntPtr.Zero;
        private ClipboardUpdateNotifier _clipboardUpdateNotifier;
        private HistoryCollection _historyCollection;
        #endregion


        #region Properties
        public HistoryCollection HistoryCollection
        {
            get { return this._historyCollection; }
        }
        #endregion


        #region Constructor
        public MyControl()
        {
            InitializeComponent();
            InitializeClipboardUpdateNotifier();
            InitializeHistoryCollection();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = this;
            }
            AddClipboardDataToHistoryCollection();
        }
        #endregion


        #region Private Methods
        private void InitializeClipboardUpdateNotifier()
        {
            _clipboardUpdateNotifier = new ClipboardUpdateNotifier(ClipboardUpdateNotifier_ClipboardUpdate);
            _clipboardUpdateNotifier.EnableNotifications();
        }

        private void InitializeHistoryCollection()
        {
            _historyCollection = new HistoryCollection();
        }

        private void AddClipboardDataToHistoryCollection()
        {
            AddStringToHistoryCollection(GetClipboardTextOrEmpty());
        }

        private void AddStringToHistoryCollection(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _historyCollection.AddItem(new ClipboardDataItem(text));
        }

        private void AddErrorMessageToHistoryCollection(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return;
            _historyCollection.AddItem(new ClipboardDataItem(errorMessage, true));
        }

        private void CopyHistoryCollectionLineToClipboard(int lineNumber)
        {
            if ((lineNumber < 0) || (lineNumber >= _historyCollection.Count)) return;
            SetClipboardTextOrError(_historyCollection[lineNumber].CopyDataFull);
        }

        private string GetClipboardTextOrEmpty()
        {
            string text = string.Empty;
            if (!Clipboard.ContainsText()) return text;
            try
            {
                text = Clipboard.GetText();
            }
            catch (Exception ex)
            {
                text = string.Empty;
                AddErrorMessageToHistoryCollection(
                    string.Format("Exception: {0}\nMost likely, another application is hooking the clipboard.", ex.Message));
            }
            return text;
        }

        private void SetClipboardTextOrError(string text)
        {
            _clipboardUpdateNotifier.DisableNotifications();
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                AddErrorMessageToHistoryCollection(
                    string.Format("Exception: {0}\nMost likely, another application is hooking the clipboard.", ex.Message));
            }
            _clipboardUpdateNotifier.EnableNotifications();
        }

        private static bool IsControlKeyDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void SaveConfigurationWithRefresh(object sender)
        {
            if (Configuration.SavePropertyOrRevert(sender))
            {
                this.HistoryCollection.Refresh();
            }
        }
        
        private void LoadValidationRules()
        {
            // TODO: KG - I hate this solution, but keeping it for now.
            this.TextBoxHistoryCollectionCapacity.Tag = "HistoryCollectionCapacity";
            this.TextBoxCopyDataShortNumLines.Tag = "CopyDataShortNumLines";
            this.TextBoxToolTipHoverDelay.Tag = "ToolTipHoverDelay";
            this.CheckBoxVisualStudioClipboardOnly.Tag = "VisualStudioClipboardOnly";
            this.CheckBoxPreventDuplicateItems.Tag = "PreventDuplicateItems";
        }

        private void LoadConfigurationValues()
        {
            this.TextBoxHistoryCollectionCapacity.Text = Configuration.HistoryCollectionCapacity.ToString(CultureInfo.InvariantCulture);
            this.TextBoxCopyDataShortNumLines.Text = Configuration.CopyDataShortNumLines.ToString(CultureInfo.InvariantCulture);
            this.TextBoxToolTipHoverDelay.Text = Configuration.ToolTipHoverDelay.ToString(CultureInfo.InvariantCulture);
            ToolTipService.SetInitialShowDelay(ListBoxHistory, Configuration.ToolTipHoverDelay);
            this.CheckBoxVisualStudioClipboardOnly.IsChecked = Configuration.VisualStudioClipboardOnly;
            this.CheckBoxPreventDuplicateItems.IsChecked = Configuration.PreventDuplicateItems;
        }
        #endregion


        #region Event Handlers
        private void MyToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _visualStudioHandle = Process.GetCurrentProcess().MainWindowHandle;
            LoadValidationRules();
            LoadConfigurationValues();
        }

        private void ClipboardUpdateNotifier_ClipboardUpdate(object sender, EventArgs e)
        {
            var clipboardEvent = e as ClipboardEventArgs;
            if (Configuration.VisualStudioClipboardOnly)
            {
                if (clipboardEvent != null && clipboardEvent.Hwnd != _visualStudioHandle) return;
            }
            AddClipboardDataToHistoryCollection();
        }

        private void ListBoxHistory_KeyDown(object sender, KeyEventArgs e)
        {
            var listbox = (ListBox)sender;
            if ((Key.C == e.Key) && IsControlKeyDown())
            {
                CopyHistoryCollectionLineToClipboard(listbox.SelectedIndex);
                e.Handled = true;
            }
        }

        private void ListBoxHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listbox = (ListBox)sender;
            var lineNumber = listbox.SelectedIndex;
            if ((lineNumber >= 0) && (lineNumber < _historyCollection.Count))
            {
                SetClipboardTextOrError(_historyCollection[lineNumber].CopyDataFull);
            }
        }

        private void UpdateToolTipHoverDelay(object sender, RoutedEventArgs e)
        {
            e.Handled = false; // Needed for Control to Update correctly.
            Configuration.SavePropertyOrRevert(sender);
        }

        private void UpdateConfiguration(object sender, RoutedEventArgs e)
        {
            e.Handled = false; // Needed for Control to Update correctly.
            SaveConfigurationWithRefresh(sender);
        }
        #endregion


        #region IDisposable Interface / Resources Cleanup
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MyControl()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _clipboardUpdateNotifier.Dispose();
                _clipboardUpdateNotifier = null;
                _historyCollection = null;
            }
        }
        #endregion
    }
}
