﻿using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ScnEdit {
    
    class SearchResultsPanel : DockContent {

        private static Font _defaultFont = new Font("MS Reference Sans Serif", 8);
        private SearchResultsView RV;

        internal static Main Main;
        internal SearchResults Items { get { return RV.DataSource.Items; } }

        internal SearchResultsPanel() {
            Font = _defaultFont;
            DockPanel = Main.DockPanel;
            Text = Messages.SearchResults;
            RV = new SearchResultsView();
            Controls.Add(RV);
            Show(DockPanel, DockState.DockBottomAutoHide);
        }

        internal void ReloadScheme() { RV.ReloadScheme(); }

        internal static void Reset() {
            if (Main.SearchResultsPanel != null && !Main.SearchResultsPanel.IsDisposed) Main.SearchResultsPanel.Items.Clear();
        }

        internal new static void Show() {
            if (Main.SearchResultsPanel == null || Main.SearchResultsPanel.IsDisposed)
                Main.SearchResultsPanel = new SearchResultsPanel();
            
            
            Main.SearchResultsPanel.VisibleState = DockState.DockBottom;
            Main.DockPanel.DockBottomPortion = Main.SearchResultsPanel.RV.Height;
        }

        internal static void Add(SearchResult result) {
            Show();
            Main.SearchResultsPanel.Items.Add(result);
        }

        internal static void CloseIfEmpty() {
            if (Main.SearchResultsPanel != null && !Main.SearchResultsPanel.IsDisposed && Main.SearchResultsPanel.Items.Count < 1)
                Main.SearchResultsPanel.Close();
        }

    }

    class SearchResultsView : DataGridView {

        internal new SearchResultsData DataSource {
            get { return base.DataSource as SearchResultsData; }
            set { base.DataSource = value; }
        }

        internal int MaxRowsVisible { get; set; }

        internal SearchResultsView() {
            DataSource = new SearchResultsData();
            ReloadScheme();
            AllowDrop = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToOrderColumns = false;
            AllowUserToResizeRows = false;
            ColumnHeadersVisible = true;
            RowHeadersVisible = true;
            EnableHeadersVisualStyles = false;
            ReadOnly = true;
            Dock = DockStyle.Top;
            BorderStyle = BorderStyle.None;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            MaxRowsVisible = 10;
        }

        internal void ReloadScheme() {
            GridColor = EditorSyntax.Styles.ColorScheme.ServiceLine;
            BackgroundColor = EditorSyntax.Styles.ColorScheme.Background;
            ForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            ColumnHeadersDefaultCellStyle.BackColor = EditorSyntax.Styles.ColorScheme.Background;
            ColumnHeadersDefaultCellStyle.ForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = EditorSyntax.Styles.ColorScheme.ProjectSelection;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            RowHeadersDefaultCellStyle.BackColor = EditorSyntax.Styles.ColorScheme.Background;
            RowHeadersDefaultCellStyle.ForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            RowHeadersDefaultCellStyle.SelectionBackColor = EditorSyntax.Styles.ColorScheme.ProjectSelection;
            RowHeadersDefaultCellStyle.SelectionForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            DefaultCellStyle.BackColor = EditorSyntax.Styles.ColorScheme.Background;
            DefaultCellStyle.ForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
            DefaultCellStyle.SelectionBackColor = EditorSyntax.Styles.ColorScheme.ProjectSelection;
            DefaultCellStyle.SelectionForeColor = EditorSyntax.Styles.ColorScheme.ProjectText;
        }

        protected override void OnParentChanged(EventArgs e) {
            Font = Parent.Font;
            ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            MinimumSize = new Size(MinimumSize.Width, ColumnHeadersHeight + 50);
            Height = MinimumSize.Height;
        }

        protected override void OnRowsAdded(DataGridViewRowsAddedEventArgs e) {
            base.OnRowsAdded(e);
            if (Rows.Count > 0) {
                int height = 0;
                for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
                    if (RowCount < MaxRowsVisible) height += Rows[i].Height;
                if (height > 0) Height += height;
            }
        }

        protected override void OnRowsRemoved(DataGridViewRowsRemovedEventArgs e) {
            base.OnRowsRemoved(e);
            if (Rows.Count < 1) Height = MinimumSize.Height;
        }

        protected override void OnCellDoubleClick(DataGridViewCellEventArgs e) {
            if (e.RowIndex >= 0) {
                var item = DataSource.Items[e.RowIndex];
                var file = ProjectFile.All.FirstOrDefault(i => i.Path == item.Path);
                if (file != null) {
                    var editor = file.Editor;
                    var start = new Place(item.Column - 1, item.Line - 1);
                    var startPosition = editor.PlaceToPosition(start);
                    var endPositon = startPosition + item.Fragment.Length;
                    var end = editor.PositionToPlace(endPositon);
                    editor.Selection = new Range(editor, start, end);
                    editor.DoSelectionVisible();
                    editor.File.Container.Activate();
                }
            }
        }
    }

    class SearchResultsData : DataTable {

        internal SearchResults Items { get; set; }
        
        internal SearchResultsData() {
            Columns.Add(Messages.PathHeader, typeof(String));
            Columns.Add(Messages.FragmentHeader, typeof(String));
            Columns.Add(Messages.FileHeader, typeof(String));
            Columns.Add(Messages.LineHeader, typeof(Int32));
            Columns.Add(Messages.ColumnHeader, typeof(Int32));
            Items = new SearchResults(this);
        }
        
    }

    class SearchResults : List<SearchResult> {

        internal readonly SearchResultsData Data;

        internal SearchResults(SearchResultsData dt) {
            Data = dt;
        }

        internal new void Add(SearchResult r) {
            base.Add(r);
            Data.Rows.Add(new object[] { r.Path, r.Fragment, r.File, r.Line, r.Column });
        }

        internal new void Clear() {
            base.Clear();
            Data.Rows.Clear();
        }

    }

    class SearchResult {
        public string Path;
        public string Fragment;
        public string File;
        public int Line;
        public int Column;
    }

}
