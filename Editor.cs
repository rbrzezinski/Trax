﻿using FastColoredTextBoxNS;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ScnEdit {
    
    class Editor : FastColoredTextBox {

        #region Fields

        internal EditorFile File;
        internal Encoding Encoding;
        internal EditorSyntax SyntaxModule;

        #endregion

        #region Events

        public event EventHandler FileIODone;

        #endregion

        #region Methods

        public Editor(EditorFile file) {
            DoubleBuffered = true;
            File = file;
            Dock = DockStyle.Fill;
            WordWrapMode = WordWrapMode.WordWrapControlWidth;
            HighlightingRangeType = HighlightingRangeType.VisibleRange;
            switch (File.Type) {
                case ScnEdit.EditorFile.Types.HTML: Language = Language.HTML; break;
                default: Language = Language.Custom; break;
            }
            Encoding = Encoding.GetEncoding(Properties.Settings.Default.EncodingDefault);
            WordWrap = Properties.Settings.Default.WordWrap;
            if (File.Type == EditorFile.Types.Timetable) {
                IsReplaceMode = true;
                KeyDown += Editor_KeyDown;
            }
            SyntaxModule = new EditorSyntax(this);
            if (File.Type == EditorFile.Types.SceneryMain || File.Type == EditorFile.Types.SceneryPart) ToolTipNeeded += SyntaxModule.HintParser;
            SelectionChanged += Editor_SelectionChanged;
            SelectionChangedDelayed += SyntaxModule.SameWordHighlight;
            IsReplaceModeChanged += Editor_IsReplaceModeChanged;
        }

        public void ReloadScheme() {
            SyntaxModule = null;
            SyntaxModule = new EditorSyntax(this);
            SyntaxModule.GetStyles(true);
            SyntaxModule.HighlightSyntax(VisibleRange);
            File.Container.DocumentMap.BackColor = EditorSyntax.Styles.Background;
        }
        public void LoadFromFile() {
            Application.OpenForms[0].BeginInvoke(new Action(() => {
                Encoding = File.EncodingDefault;
                if (!File.AutoDecoding || File.HasHtmlEncoding) OpenBindingFile(File.Path, Encoding);
                else Text = (File as ProjectFile).Text;
                if (File.Container != null) File.Container.UpdateText();
                EndUpdate();
                IsChanged = File.IsConverted;
                ClearUndo();
                if (File.Role == EditorFile.Roles.Log) {
                    ReadOnly = true;
                    TextChangedDelayed += ShowLogTail;
                }
                if (File.Container != null) File.Container.UpdateText();
                if (FileIODone != null) FileIODone.Invoke(this, EventArgs.Empty);
            }));
        }

        public void SaveToFile() {
            base.SaveToFile(File.Path, Encoding);
            IsChanged = false;
            File.Container.UpdateText(true);
            if (FileIODone != null) FileIODone.Invoke(this, EventArgs.Empty);
        }

        public void CommentSelectedLines() {
            Selection = GetLinesFromSelection();
            SelectedText = new ScnSyntax.NonCommentLine().Replace(SelectedText, "$1//$2");
        }

        public void UncommentSelectedLines() {
            Selection = GetLinesFromSelection();
            SelectedText = new ScnSyntax.CommentLine().Replace(SelectedText, "$1$2");
        }

        public void ToggleComment() {
            if (Language == FastColoredTextBoxNS.Language.Custom) {
                Selection = GetLinesFromSelection();
                if (SelectedText.Trim().StartsWith("//")) UncommentSelectedLines(); else CommentSelectedLines();
            } else CommentSelected();
            OnSyntaxHighlight(new TextChangedEventArgs(Selection));
        }

        #endregion

        #region Helpers

        private static bool IsSpecialKey(Keys k) {
            switch (k) {
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.ShiftKey:
                case Keys.Insert:
                case Keys.Delete:
                    return true;
                default:
                    return false;
            }
        }

        private void ShowLogTail(object sender, EventArgs e) {
            GoEnd();
            TextChangedDelayed -= ShowLogTail;
        }

        private Range GetLinesFromSelection() {
            var range = new Range(this, Selection.Start, Selection.End);
            range.Normalize();
            var startOffset = PlaceToPosition(range.Start);
            var endOffset = PlaceToPosition(range.End);
            for (int i = startOffset; i >= 0; i--) if (i == 0 || Text[i - 1] == '\r' || Text[i - 1] == '\n') { startOffset = i; break; }
            if (endOffset > startOffset) for (int i = endOffset - 1; i < Text.Length; i++) if (Text[i] == '\r' || Text[i] == '\n') { endOffset = i; break; }
            return new Range(this, PositionToPlace(endOffset), PositionToPlace(startOffset));
        }

        #endregion

        #region Event handlers

        private void Editor_KeyDown(object sender, KeyEventArgs e) {
            if (IsReplaceMode) { // usable replace mode
                if (e.Modifiers == Keys.None || e.Modifiers == Keys.Shift) {
                    var s = Selection;
                    var p = s.Start;
                    var q = s.End;
                    switch (e.KeyCode) {
                        case Keys.Delete:
                            BeginAutoUndo();
                            s.End = new Place(p.iChar + 1, p.iLine);
                            ClearSelected();
                            InsertText(" ");
                            e.Handled = true;
                            EndAutoUndo();
                            break;
                        case Keys.Back:
                            BeginAutoUndo();
                            s.End = new Place(p.iChar - 1, p.iLine);
                            ClearSelected();
                            InsertText(" ");
                            s.Start = new Place(p.iChar - 1, p.iLine);
                            e.Handled = true;
                            EndAutoUndo();
                            break;
                        default:
                            if (!IsSpecialKey(e.KeyCode) && p != q) {
                                BeginAutoUndo();
                                var n = Selection.Clone(); n.Normalize();
                                IsReplaceMode = true;
                                InsertText("".PadLeft(n.End.iChar - n.Start.iChar, ' '));
                                s.Start = n.Start; s.End = n.Start;
                                EndAutoUndo();
                            };
                            break;
                    }
                }
            }
        }

        private void Editor_IsReplaceModeChanged(object sender, EventArgs e) {
            Status.Replace = IsReplaceMode;
        }

        private void Editor_SelectionChanged(object sender, EventArgs e) {
            Status.Line = Selection.Start.iLine + 1;
            Status.Column = Selection.Start.iChar + 1;
            Status.Selection = Selection.Text.Length;
        }

        public override void OnVisibleRangeChanged() {
            base.OnVisibleRangeChanged();
            Status.FileSize = Text.Length;
            Status.FileLines = Range.End.iLine + 1;
        }

        public override void OnTextChanged(Range r) {
            base.OnTextChanged(r);
            if (File.Role == EditorFile.Roles.Log) {
                Selection.Start = new Place(0, Range.End.iLine);
                DoCaretVisible();
            }
        }

        protected override void OnPaint(PaintEventArgs e) { try { base.OnPaint(e); } catch (ArgumentException) { Zoom = Zoom; } }
        public override void Paste() {
            base.Paste();
            OnSyntaxHighlight(new TextChangedEventArgs(Selection));
        }

        #endregion

    }

}