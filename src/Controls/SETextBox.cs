﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Controls
{
    /// <summary>
    /// TextBox where double click selects current word
    /// </summary>
    public class SETextBox : TextBox
    {
        private string breakChars = "\".!?,)([]<>:;♪{}-/#*| ¿¡" + Environment.NewLine + "\t";
        string _dragText = string.Empty;
        int _dragStartFrom = 0;
        long _dragStartTicks = 0;
        bool _dragRemoveOld = false;
        bool _dragFromThis = false;

        public SETextBox()
        {
            AllowDrop = true;
            DragEnter += new DragEventHandler(SETextBox_DragEnter);
         //   DragOver += new DragEventHandler(SETextBox_DragOver); could draw some gfx where drop position is...
            DragDrop += new DragEventHandler(SETextBox_DragDrop);
            MouseDown += new MouseEventHandler(SETextBox_MouseDown);
            MouseUp += new MouseEventHandler(SETextBox_MouseUp);
            KeyDown += SETextBox_KeyDown;
        }

        void SETextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Back)
            {
                int index = SelectionStart;
                if (SelectionLength == 0)
                {
                    string s = Text;
                    int deleteFrom = index-1;

                    if (deleteFrom > 0 && deleteFrom < s.Length)
                    {
                        if (s[deleteFrom] == ' ')
                            deleteFrom--;
                        while (deleteFrom > 0 && !(breakChars).Contains(s.Substring(deleteFrom, 1)))
                        {
                            deleteFrom--;
                        }
                        if (deleteFrom == index - 1)
                        {
                            while (deleteFrom > 0 && (breakChars.Replace(" ", string.Empty)).Contains(s.Substring(deleteFrom - 1, 1)))
                            {
                                deleteFrom--;
                            }
                        }
                        if (s[deleteFrom] == ' ')
                            deleteFrom++;
                        Text = s.Remove(deleteFrom, index - deleteFrom);
                        SelectionStart = deleteFrom;
                    }
                }
                e.SuppressKeyPress = true;
            }
        }

        void SETextBox_MouseUp(object sender, MouseEventArgs e)
        {
            _dragRemoveOld = false;
            _dragFromThis = false;
        }

        void SETextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (MouseButtons == System.Windows.Forms.MouseButtons.Left && !string.IsNullOrEmpty(_dragText))
            {
                Point pt = new Point(e.X, e.Y);
                int index = GetCharIndexFromPosition(pt);
                if (index >= _dragStartFrom && index <= _dragStartFrom + _dragText.Length)
                {
                    // re-make selection
                    SelectionStart = _dragStartFrom;
                    SelectionLength = _dragText.Length;

                    DataObject dataObject = new DataObject();
                    dataObject.SetText(_dragText, TextDataFormat.UnicodeText);
                    dataObject.SetText(_dragText, TextDataFormat.Text);

                    _dragFromThis = true;
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        _dragRemoveOld = false;
                        DoDragDrop(dataObject, DragDropEffects.Copy);
                    }
                    else if (Control.ModifierKeys == Keys.None)
                    {
                        _dragRemoveOld = true;
                        DoDragDrop(dataObject, DragDropEffects.Move);
                    }
                }
            }
        }

        void SETextBox_DragDrop(object sender, DragEventArgs e)
        {
            Point pt = new Point(e.X, e.Y);
            pt = PointToClient(pt);
            int index = GetCharIndexFromPosition(pt);

            string newText = string.Empty;
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
                newText = (string)e.Data.GetData(DataFormats.UnicodeText);
            else
                newText = (string)e.Data.GetData(DataFormats.Text);

            if (Text.Trim().Length == 0)
            {
                Text = newText;
            }
            else
            {
                bool justAppend = index == Text.Length - 1 && index > 0;
                if (_dragFromThis)
                {
                    _dragFromThis = false;
                    long milliseconds = (DateTime.Now.Ticks - _dragStartTicks) / 10000;
                    if (milliseconds < 400)
                    {
                        SelectionLength = 0;
                        if (index == Text.Length - 1 && index > 0)
                            index++;
                        SelectionStart = index;
                        return; // too fast - nobody can drag'n'drop this fast
                    }

                    if (index >= _dragStartFrom && index <= _dragStartFrom + _dragText.Length)
                        return; // don't drop same text at same position

                    if (_dragRemoveOld)
                    {
                        _dragRemoveOld = false;
                        Text = Text.Remove(_dragStartFrom, _dragText.Length);

                        // fix spaces
                        if (_dragStartFrom == 0 && Text.Length > 0 && Text[0] == ' ')
                        {
                            Text = Text.Remove(0, 1);
                            index--;
                        }
                        else if (_dragStartFrom > 1 && Text.Length > _dragStartFrom + 1 && Text[_dragStartFrom] == ' ' && Text[_dragStartFrom - 1] == ' ')
                        {
                            Text = Text.Remove(_dragStartFrom, 1);
                            if (_dragStartFrom < index)
                                index--;
                        }
                        else if (_dragStartFrom > 0 && Text.Length > _dragStartFrom + 1 && Text[_dragStartFrom] == ' ' && ";:]<.!?".Contains(Text[_dragStartFrom + 1].ToString()))
                        {
                            Text = Text.Remove(_dragStartFrom, 1);
                            if (_dragStartFrom < index)
                                index--;
                        }

                        // fix index
                        if (index > _dragStartFrom)
                            index -= _dragText.Length;
                        if (index < 0)
                            index = 0;
                    }
                }
                if (justAppend)
                {
                    index = Text.Length;
                    Text += newText;
                }
                else
                {
                    Text = Text.Insert(index, newText);
                }

                // fix start spaces
                int endIndex = index + newText.Length;
                if (index > 0 && !newText.StartsWith(" ") && Text[index - 1] != ' ')
                {
                    Text = Text.Insert(index, " ");
                    endIndex++;
                }
                else if (index > 0 && newText.StartsWith(" ") && Text[index - 1] == ' ')
                {
                    Text = Text.Remove(index, 1);
                    endIndex--;
                }

                // fix end spaces
                if (endIndex < Text.Length && !newText.EndsWith(" ") && Text[endIndex] != ' ')
                {
                    bool lastWord = ";:]<.!?".Contains(Text[endIndex].ToString());
                    if (!lastWord)
                        Text = Text.Insert(endIndex, " ");
                }
                else if (endIndex < Text.Length && newText.EndsWith(" ") && Text[endIndex] == ' ')
                {
                    Text = Text.Remove(endIndex, 1);
                }

                SelectionStart = index+1;
                SelectCurrentWord(this);
            }

            _dragRemoveOld = false;
            _dragFromThis = false;
        }

        void SETextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                if (Control.ModifierKeys == Keys.Control)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_DBLCLICK = 0xA3;
            const int WM_LBUTTONDBLCLK = 0x203;
            const int WM_LBUTTONDOWN = 0x0201;

            if (m.Msg == WM_DBLCLICK || m.Msg == WM_LBUTTONDBLCLK)
            {
                SelectCurrentWord(this);
                return;
            }
            if (m.Msg == WM_LBUTTONDOWN)
            {
                _dragText = SelectedText;
                _dragStartFrom = SelectionStart;
                _dragStartTicks = DateTime.Now.Ticks;
            }
            base.WndProc(ref m);
        }

        private void SelectCurrentWord(TextBox tb)
        {
            int selectionLength = 0;
            int i = tb.SelectionStart;
            while (i > 0 && breakChars.Contains(tb.Text.Substring(i - 1, 1)) == false)
                i--;
            tb.SelectionStart = i;
            for (; i < tb.Text.Length; i++)
            {
                if (breakChars.Contains(tb.Text.Substring(i, 1)))
                    break;
                selectionLength++;
            }
            tb.SelectionLength = selectionLength;
            if (selectionLength > 0)
                this.OnMouseMove(null);
        }

    }
}
