﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace escript
{
    //public class ConsoleBox : RichTextBox
    //{
    //    protected override void WndProc(ref Message m)
    //    {
    //        switch (m.Msg)
    //        {
    //            case 0x0302: //WM_PASTE
    //            case 0x0300: //WM_CUT
    //            case 0x000C: //WM_SETTEXT
    //                if (!IsCaretAtWritablePosition())
    //                    MoveCaretToEndOfText();
    //                break;
    //            case 0x0303: //WM_CLEAR
    //                return;
    //        }
    //        base.WndProc(ref m);
    //    }
    //}
    public partial class CustomConsoleWindow : Form
    {
        private Color fontColor = Color.White;
        private Color fontBackColor = Color.Black;
        bool waiting = false;
        bool isOneKey = false;
        bool scrollToEnd = false;
        string readLineResult = "";
        string beforeText = "";
        System.Timers.Timer timer;
        public Keys oneKeyKey = Keys.None;
        List<string> commandList = new List<string>();
        int commandListIdx = -1;

        public Font DefaultFontX;
        
        public CustomConsoleWindow()
        {
            InitializeComponent();
            fontBackColor = this.BackColor;
            EConsole.cWndHandle = this.Handle;
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //textBoxCommand.Focus();
            Variables.Set("forceGC", "0");
            int sWidth = SystemInformation.VirtualScreen.Width;
            int sHeight = SystemInformation.VirtualScreen.Height;
            if (this.Height >= sHeight) this.Height = sHeight - 100;
            if (this.Width >= sWidth) this.Width = sWidth - 100;
            DefaultFontX = new Font(textBoxOutput.Font.FontFamily, textBoxOutput.Font.Size);

            UpdateFSize();
            textBoxOutput.BringToFront();

            mnColorSolid.BackColor = textBoxOutput.BackColor;
            mnColorSolid.ForeColor = textBoxOutput.ForeColor;
            mnColorSolid.Text = " ";

            everythingVer.Text = Cmd.Process("OSVersion");
        }

        public void UpdateFSize()
        {
            cbFontFamily.Items.Add("Default");
            cbFontSize.Items.Add("Default");

            for (int i = 0; i < 100; i++)
            {
                cbFontSize.Items.Add(i.ToString());
            }
            cbFontSize.Text = DefaultFontX.Size.ToString();
            

            foreach (FontFamily font in System.Drawing.FontFamily.Families)
            {
                cbFontFamily.Items.Add(font.Name);
            }
            cbFontFamily.Text = DefaultFontX.FontFamily.Name;
        }
        

        public void SetFont(string font)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetFont(font); })); return; }
            textBoxOutput.Font = new Font(font, textBoxOutput.Font.Size);
        }
        public void SetFontSize(float size)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetFontSize(size); })); return; }
            textBoxOutput.Font = new Font(textBoxOutput.Font.FontFamily, size);
        }

        public void Clear()
        {
            if (InvokeRequired) { Invoke(new Action(delegate { Clear();  })); return; }
            textBoxOutput.ReadOnly = false;
            textBoxOutput.Clear();
            if (!waiting) textBoxOutput.ReadOnly = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (scrollToEnd)
            {
                if (textBoxOutput.InvokeRequired)
                {
                    textBoxOutput.Invoke(new Action(delegate {
                        textBoxOutput.Select(textBoxOutput.Text.Length, 0);
                        textBoxOutput.ScrollToCaret();
                    }));
                }
                else
                {
                    textBoxOutput.Select(textBoxOutput.Text.Length, 0);
                    textBoxOutput.ScrollToCaret();
                }
                //textBoxCommand.Focus();
                scrollToEnd = false;
            }
            timer.Dispose();
        }
        
        public void ScrollToEnd()
        {
#if !IsCore
            GlobalVars.SendMessage(textBoxOutput.Handle, GlobalVars.WM_VSCROLL, (IntPtr)GlobalVars.SB_PAGEBOTTOM, IntPtr.Zero);
            textBoxOutput.SelectionStart = textBoxOutput.Text.Length;
#else
            scrollToEnd = true;
            try
            {
                timer.Dispose();
            }
            catch { }
            timer = new System.Timers.Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
#endif
        }
        
        public void Write(object text)
        {

            Variables.Set("forceGC", "0");

            textBoxOutput.ReadOnly = false;
            int start = textBoxOutput.TextLength;
            textBoxOutput.AppendText(text.ToString());
            int end = textBoxOutput.TextLength;

            // Textbox may transform chars, so (end-start) != text.Length
            textBoxOutput.Select(start, end - start);

            textBoxOutput.SelectionColor = fontColor;

            textBoxOutput.SelectionBackColor = fontBackColor;
            // could set box.SelectionBackColor, box.SelectionFont too.

            textBoxOutput.SelectionLength = 0; // clear


            ScrollToEnd();
            if(!waiting) textBoxOutput.ReadOnly = true;
        }

        public void WriteLine(object text)
        {
            WriteLine(text, Color.Black);
        }

        public void WriteLine(object text, Color color)
        {
            Variables.Set("forceGC", "0");
            textBoxOutput.ReadOnly = false;

            int start = textBoxOutput.TextLength;
            textBoxOutput.AppendText(text.ToString() + Environment.NewLine);
            int end = textBoxOutput.TextLength;

            // Textbox may transform chars, so (end-start) != text.Length
            textBoxOutput.Select(start, end - start);

            if (color != Color.Black) textBoxOutput.SelectionColor = color;
            else textBoxOutput.SelectionColor = fontColor;
            textBoxOutput.SelectionBackColor = fontBackColor;
            // could set box.SelectionBackColor, box.SelectionFont too.

            textBoxOutput.SelectionLength = 0; // clear

            ScrollToEnd();
            if (!waiting) textBoxOutput.ReadOnly = true;
        }

        public Color ForegroundColor
        {
            set
            {
                Variables.Set("forceGC", "0");
                fontColor = value;



                if (textBoxOutput.InvokeRequired)
                {
                    textBoxOutput.Invoke(new Action(delegate
                    {
                        textBoxOutput.Select(textBoxOutput.Text.Length, textBoxOutput.Text.Length);
                        textBoxOutput.SelectionColor = fontColor;
                    }));
                }
                else
                {
                    textBoxOutput.Select(textBoxOutput.Text.Length, textBoxOutput.Text.Length);
                    textBoxOutput.SelectionColor = fontColor;
                }

            }
        }

        public Color BackgroundColor
        {
            set
            {
                if (value == Color.Black) value = BackColor;

                Variables.Set("forceGC", "0");
                fontBackColor = value;



                if (textBoxOutput.InvokeRequired)
                {
                    textBoxOutput.Invoke(new Action(delegate
                    {
                        textBoxOutput.Select(textBoxOutput.Text.Length, textBoxOutput.Text.Length);
                        textBoxOutput.SelectionBackColor = fontBackColor;
                    }));
                }
                else
                {
                    textBoxOutput.Select(textBoxOutput.Text.Length, textBoxOutput.Text.Length);
                    textBoxOutput.SelectionBackColor = fontBackColor;
                }

            }
        }



        private void CustomConsoleWindow_Load(object sender, EventArgs e)
        {

        }

        public string ReadLine()
        {
            Variables.Set("forceGC", "0");
            readLineResult = "";
            waiting = true; isOneKey = false;
            this.Invoke(new Action(delegate {
                beforeText = textBoxOutput.Text;
                //textBoxCommand.Clear();
                textBoxOutput.ReadOnly = false;
                textBoxOutput.Focus();
                //textBoxCommand.Focus();
            }));
            while (waiting == true)
            {
                Thread.Sleep(100);
            }
            return readLineResult;
        }

        public string ReadKey()
        {
            Variables.Set("forceGC", "0");
            readLineResult = "";
            waiting = true;
            this.Invoke(new Action(delegate {
                textBoxOutput.ReadOnly = false;
                beforeText = textBoxOutput.Text;
                textBoxOutput.Focus();
                //textBoxCommand.Clear();
                // textBoxCommand.Focus();
            }));
            isOneKey = true;
            while (waiting == true)
            {
                Thread.Sleep(100);
            }
            return readLineResult;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Variables.Set("forceGC", "0");
            //readLineResult = textBoxCommand.Text;
            try
            {
                readLineResult = textBoxOutput.Text.Remove(0, beforeText.Length);
                if (!isOneKey) textBoxOutput.AppendText(Environment.NewLine);
            }
            catch { }
                //textBoxCommand.Clear();
                //if(!isOneKey) WriteLine(readLineResult);
                waiting = false;
            isOneKey = false;
            commandList.Add(readLineResult);
        }


        private void CustomConsoleWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!EConsole.FakeKill)
            {
                Cmd.Process("exit");
            }
            else EConsole.FakeKill = false;
        }

        private void textBoxOutput_TextChanged(object sender, EventArgs e)
        {
            if(waiting)
            {
                try
                {
                    textBoxOutput.Text.Remove(0, beforeText.Length);
                }
                catch { textBoxOutput.Text = beforeText; ScrollToEnd(); }

                if (isOneKey)
                {
                    button1_Click(null, null);
                }
            }
            //textBoxOutput.SelectionStart = textBoxOutput.Text.Length;
            //textBoxOutput.ScrollToCaret();
            //textBoxCommand.Focus();
        }

        private void textBoxCommand_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBoxOutput_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Cmd.Process("start " + e.LinkText);
        }

        private void CustomConsoleWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void textBoxOutput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F4)
            {
                EConsole.Console_CancelKeyPress(null, null);
            }

            if (isOneKey)
            {
                button1_Click(null, null);
                oneKeyKey = e.KeyCode;
            }
            //if(commandListIdx == -1)
            //{
            //    commandListIdx = commandList.Count - 1;
            //}
            //textBoxOutput.AppendText(commandList[commandListIdx]);
        }

        public void CheckWritablePosition()
        {
            int writablePosition = beforeText.Length;

            
            if (textBoxOutput.SelectionStart < writablePosition)
            {
                ScrollToEnd();

            }
        }

        private void textBoxOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control && !e.Shift && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
            {
                int writablePosition = beforeText.Length;


                if (e.KeyCode == Keys.Home)
                {
                    textBoxOutput.SelectionLength = 0;
                    textBoxOutput.SelectionStart = writablePosition;;
                }

                else if (textBoxOutput.SelectionStart < writablePosition)
                {
                    ScrollToEnd();

                }


                if (e.KeyCode == Keys.Back && textBoxOutput.SelectionStart <= writablePosition)
                {
                    e.SuppressKeyPress = true;
                    //Program.Debug("someone is removing escript");
                }
            }
        }

        private void CustomConsoleWindow_KeyUp(object sender, KeyEventArgs e)
        {
           
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CheckWritablePosition();
            textBoxOutput.Cut();
        }

        private void gayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxOutput.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckWritablePosition();
            textBoxOutput.Paste();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch
            {
            }
        }

        private void defaultToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            cbFontSize.Text = DefaultFontX.Size.ToString();
            cbFontSize_TextChanged(null, null);
        }

        private void cbFontSize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if(cbFontSize.Text == "Default")
                {
                    defaultToolStripMenuItem1_Click(null, null);
                    return;
                }
                textBoxOutput.Font = new Font(textBoxOutput.Font.FontFamily, float.Parse(cbFontSize.Text.ToString()));
            }
            catch
            {
                defaultToolStripMenuItem1_Click(null, null);
            }
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cbFontFamily.Text = DefaultFontX.FontFamily.Name;
            cbFontFamily_TextChanged(null, null);
            //textBoxOutput.Font = new Font(DefaultFont.FontFamily, textBoxOutput.Font.Size);
        }

        private void cbFontFamily_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbFontFamily.Text == "Default")
                {
                    defaultToolStripMenuItem_Click(null, null);
                    return;
                }
                textBoxOutput.Font = new Font(cbFontFamily.Text, textBoxOutput.Font.Size);
            }
            catch
            {
                defaultToolStripMenuItem_Click(null, null);
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckWritablePosition();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxOutput.Visible = false;

            RichTextBox aboutBox = new RichTextBox();

            aboutBox.Size = textBoxOutput.Size;
            aboutBox.Margin = textBoxOutput.Margin;
            aboutBox.Dock = textBoxOutput.Dock;
            aboutBox.KeyUp += AboutBox_KeyUp;
            aboutBox.BackColor = Color.FromArgb(40, 40, 40);
            aboutBox.ForeColor = textBoxOutput.ForeColor;
            aboutBox.BorderStyle = BorderStyle.None;
            aboutBox.LinkClicked += textBoxOutput_LinkClicked;

            string resourceName = "escript.about.rtf";
            using (var resource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                aboutBox.LoadFile(resource, RichTextBoxStreamType.RichText);
            }

            aboutBox.ReadOnly = true;

            Controls.Add(aboutBox);
            aboutBox.BringToFront();
            aboutBox.Focus();
            aboutBox.Select(0, 0);
            //WindowState = FormWindowState.Maximized;
        }

        private void AboutBox_KeyUp(object sender, KeyEventArgs e)
        {
            ((RichTextBox)sender).Dispose();
            textBoxOutput.Visible = true;
            textBoxOutput.BringToFront();
            Cmd.Process("PrintIntro");
        }

        private void tbBackgroundColor_TextChanged(object sender, EventArgs e)
        {

        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog c = new ColorDialog();
            c.Color = textBoxOutput.BackColor;
            c.FullOpen = true;
            if (c.ShowDialog() == DialogResult.OK)
            {
                mnColorSolid.BackColor = c.Color;
                textBoxOutput.BackColor = c.Color;
            }
        }
    }
}
