#region Copyright (C) 2005-2006 Team MediaPortal

/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.TV.Database;
using MediaPortal.TV.Recording;
using MediaPortal.TV.Scanning;

namespace MediaPortal.Configuration.Sections
{
  public class Wizard_DVBSTV : MediaPortal.Configuration.SectionSettings, AutoTuneCallback
  {
    private System.ComponentModel.IContainer components = null;
    class Transponder
    {
      public string SatName;
      public string FileName;
      public override string ToString()
      {
        return SatName;
      }
    }

    string _description = "";
    int m_diseqcLoops = 1;
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox3;
    private System.Windows.Forms.ProgressBar progressBar3;
    private MediaPortal.UserInterface.Controls.MPButton button3;
    private System.Windows.Forms.Panel panel1;
    private MediaPortal.UserInterface.Controls.MPComboBox cbTransponder;
    private MediaPortal.UserInterface.Controls.MPLabel lblStatus;
    private MediaPortal.UserInterface.Controls.MPComboBox cbTransponder2;
    private MediaPortal.UserInterface.Controls.MPComboBox cbTransponder3;
    private MediaPortal.UserInterface.Controls.MPComboBox cbTransponder4;
    int m_currentDiseqc = 1;
    TVCaptureDevice _card;
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox2;
    private MediaPortal.UserInterface.Controls.MPLabel label31;
    private MediaPortal.UserInterface.Controls.MPCheckBox useLNB4;
    private MediaPortal.UserInterface.Controls.MPCheckBox useLNB3;
    private MediaPortal.UserInterface.Controls.MPCheckBox useLNB2;
    private MediaPortal.UserInterface.Controls.MPCheckBox useLNB1;
    private MediaPortal.UserInterface.Controls.MPComboBox lnbkind4;
    private MediaPortal.UserInterface.Controls.MPComboBox lnbkind3;
    private MediaPortal.UserInterface.Controls.MPComboBox lnbkind2;
    private MediaPortal.UserInterface.Controls.MPLabel label30;
    private MediaPortal.UserInterface.Controls.MPLabel label32;
    private MediaPortal.UserInterface.Controls.MPComboBox diseqcd;
    private MediaPortal.UserInterface.Controls.MPComboBox diseqcc;
    private MediaPortal.UserInterface.Controls.MPComboBox diseqcb;
    private MediaPortal.UserInterface.Controls.MPComboBox diseqca;
    private MediaPortal.UserInterface.Controls.MPComboBox lnbkind1;
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox4;
    private MediaPortal.UserInterface.Controls.MPTextBox circularMHZ;
    private MediaPortal.UserInterface.Controls.MPLabel label20;
    private MediaPortal.UserInterface.Controls.MPTextBox cbandMHZ;
    private MediaPortal.UserInterface.Controls.MPLabel label21;
    private MediaPortal.UserInterface.Controls.MPGroupBox mpGroupBox1;
    private MediaPortal.UserInterface.Controls.MPTextBox lnb1MHZ;
    private MediaPortal.UserInterface.Controls.MPLabel lnb1;
    private MediaPortal.UserInterface.Controls.MPTextBox lnbswMHZ;
    private MediaPortal.UserInterface.Controls.MPLabel switchMHZ;
    private MediaPortal.UserInterface.Controls.MPTextBox lnb0MHZ;
    private MediaPortal.UserInterface.Controls.MPLabel label22;
    string[] _tplFiles;
    private ProgressBar progressBarQuality;
    private MediaPortal.UserInterface.Controls.MPLabel mpLabel2;
    private MediaPortal.UserInterface.Controls.MPLabel mpLabel1;
    private ProgressBar progressBarStrength;
    private MediaPortal.UserInterface.Controls.MPLabel mpLabel3;
    private MediaPortal.UserInterface.Controls.MPLabel labelDescription;
    TVCaptureDevice captureCard ;
    public delegate void ScanFinishedHandler(object sender, EventArgs args);
    public event ScanFinishedHandler OnScanFinished;

    public Wizard_DVBSTV()
      : this("DVB-S TV")
    {
      _card = null;
    }

    public Wizard_DVBSTV(string name)
      : base(name)
    {
      _card = null;
      // This call is required by the Windows Form Designer.
      InitializeComponent();

      // TODO: Add any initialization after the InitializeComponent call
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (components != null)
        {
          components.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    #region Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.groupBox3 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.labelDescription = new MediaPortal.UserInterface.Controls.MPLabel();
      this.mpLabel3 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.progressBarQuality = new System.Windows.Forms.ProgressBar();
      this.mpLabel2 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.mpLabel1 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.progressBarStrength = new System.Windows.Forms.ProgressBar();
      this.groupBox2 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.label31 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.useLNB4 = new MediaPortal.UserInterface.Controls.MPCheckBox();
      this.useLNB3 = new MediaPortal.UserInterface.Controls.MPCheckBox();
      this.cbTransponder4 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.useLNB2 = new MediaPortal.UserInterface.Controls.MPCheckBox();
      this.cbTransponder3 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.useLNB1 = new MediaPortal.UserInterface.Controls.MPCheckBox();
      this.cbTransponder2 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.lnbkind4 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.cbTransponder = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.lnbkind3 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.lnbkind2 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.label30 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.label32 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.diseqcd = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.diseqcc = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.diseqcb = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.diseqca = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.lnbkind1 = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.groupBox4 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.circularMHZ = new MediaPortal.UserInterface.Controls.MPTextBox();
      this.label20 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.cbandMHZ = new MediaPortal.UserInterface.Controls.MPTextBox();
      this.label21 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.mpGroupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.lnb1MHZ = new MediaPortal.UserInterface.Controls.MPTextBox();
      this.lnb1 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.lnbswMHZ = new MediaPortal.UserInterface.Controls.MPTextBox();
      this.switchMHZ = new MediaPortal.UserInterface.Controls.MPLabel();
      this.lnb0MHZ = new MediaPortal.UserInterface.Controls.MPTextBox();
      this.label22 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.panel1 = new System.Windows.Forms.Panel();
      this.lblStatus = new MediaPortal.UserInterface.Controls.MPLabel();
      this.progressBar3 = new System.Windows.Forms.ProgressBar();
      this.button3 = new MediaPortal.UserInterface.Controls.MPButton();
      this.groupBox3.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.groupBox4.SuspendLayout();
      this.mpGroupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // groupBox3
      // 
      this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox3.Controls.Add(this.labelDescription);
      this.groupBox3.Controls.Add(this.mpLabel3);
      this.groupBox3.Controls.Add(this.progressBarQuality);
      this.groupBox3.Controls.Add(this.mpLabel2);
      this.groupBox3.Controls.Add(this.mpLabel1);
      this.groupBox3.Controls.Add(this.progressBarStrength);
      this.groupBox3.Controls.Add(this.groupBox2);
      this.groupBox3.Controls.Add(this.groupBox4);
      this.groupBox3.Controls.Add(this.mpGroupBox1);
      this.groupBox3.Controls.Add(this.panel1);
      this.groupBox3.Controls.Add(this.lblStatus);
      this.groupBox3.Controls.Add(this.progressBar3);
      this.groupBox3.Controls.Add(this.button3);
      this.groupBox3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.groupBox3.Location = new System.Drawing.Point(0, 0);
      this.groupBox3.Name = "groupBox3";
      this.groupBox3.Size = new System.Drawing.Size(532, 408);
      this.groupBox3.TabIndex = 0;
      this.groupBox3.TabStop = false;
      this.groupBox3.Text = "Setup Digital TV (DVB-S Satellite)";
      // 
      // labelDescription
      // 
      this.labelDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.labelDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelDescription.Location = new System.Drawing.Point(17, 360);
      this.labelDescription.Name = "labelDescription";
      this.labelDescription.Size = new System.Drawing.Size(500, 22);
      this.labelDescription.TabIndex = 46;
      // 
      // mpLabel3
      // 
      this.mpLabel3.Location = new System.Drawing.Point(17, 269);
      this.mpLabel3.Name = "mpLabel3";
      this.mpLabel3.Size = new System.Drawing.Size(67, 16);
      this.mpLabel3.TabIndex = 45;
      this.mpLabel3.Text = "Progress:";
      // 
      // progressBarQuality
      // 
      this.progressBarQuality.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBarQuality.Location = new System.Drawing.Point(103, 306);
      this.progressBarQuality.Name = "progressBarQuality";
      this.progressBarQuality.Size = new System.Drawing.Size(340, 16);
      this.progressBarQuality.Step = 1;
      this.progressBarQuality.TabIndex = 44;
      // 
      // mpLabel2
      // 
      this.mpLabel2.Location = new System.Drawing.Point(18, 305);
      this.mpLabel2.Name = "mpLabel2";
      this.mpLabel2.Size = new System.Drawing.Size(81, 16);
      this.mpLabel2.TabIndex = 43;
      this.mpLabel2.Text = "Signal quality:";
      // 
      // mpLabel1
      // 
      this.mpLabel1.Location = new System.Drawing.Point(17, 285);
      this.mpLabel1.Name = "mpLabel1";
      this.mpLabel1.Size = new System.Drawing.Size(80, 16);
      this.mpLabel1.TabIndex = 42;
      this.mpLabel1.Text = "Signal strength:";
      // 
      // progressBarStrength
      // 
      this.progressBarStrength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBarStrength.Location = new System.Drawing.Point(103, 285);
      this.progressBarStrength.Name = "progressBarStrength";
      this.progressBarStrength.Size = new System.Drawing.Size(340, 16);
      this.progressBarStrength.Step = 1;
      this.progressBarStrength.TabIndex = 41;
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.label31);
      this.groupBox2.Controls.Add(this.useLNB4);
      this.groupBox2.Controls.Add(this.useLNB3);
      this.groupBox2.Controls.Add(this.cbTransponder4);
      this.groupBox2.Controls.Add(this.useLNB2);
      this.groupBox2.Controls.Add(this.cbTransponder3);
      this.groupBox2.Controls.Add(this.useLNB1);
      this.groupBox2.Controls.Add(this.cbTransponder2);
      this.groupBox2.Controls.Add(this.lnbkind4);
      this.groupBox2.Controls.Add(this.cbTransponder);
      this.groupBox2.Controls.Add(this.lnbkind3);
      this.groupBox2.Controls.Add(this.lnbkind2);
      this.groupBox2.Controls.Add(this.label30);
      this.groupBox2.Controls.Add(this.label32);
      this.groupBox2.Controls.Add(this.diseqcd);
      this.groupBox2.Controls.Add(this.diseqcc);
      this.groupBox2.Controls.Add(this.diseqcb);
      this.groupBox2.Controls.Add(this.diseqca);
      this.groupBox2.Controls.Add(this.lnbkind1);
      this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.groupBox2.Location = new System.Drawing.Point(20, 120);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(431, 144);
      this.groupBox2.TabIndex = 40;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "LNB setup";
      // 
      // label31
      // 
      this.label31.Location = new System.Drawing.Point(302, 22);
      this.label31.Name = "label31";
      this.label31.Size = new System.Drawing.Size(64, 16);
      this.label31.TabIndex = 21;
      this.label31.Text = "Sattelite:";
      // 
      // useLNB4
      // 
      this.useLNB4.AutoSize = true;
      this.useLNB4.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.useLNB4.Location = new System.Drawing.Point(6, 110);
      this.useLNB4.Name = "useLNB4";
      this.useLNB4.Size = new System.Drawing.Size(58, 17);
      this.useLNB4.TabIndex = 31;
      this.useLNB4.Text = "LNB#4";
      this.useLNB4.UseVisualStyleBackColor = true;
      this.useLNB4.CheckedChanged += new System.EventHandler(this.useLNB4_CheckedChanged);
      // 
      // useLNB3
      // 
      this.useLNB3.AutoSize = true;
      this.useLNB3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.useLNB3.Location = new System.Drawing.Point(6, 86);
      this.useLNB3.Name = "useLNB3";
      this.useLNB3.Size = new System.Drawing.Size(58, 17);
      this.useLNB3.TabIndex = 30;
      this.useLNB3.Text = "LNB#3";
      this.useLNB3.UseVisualStyleBackColor = true;
      this.useLNB3.CheckedChanged += new System.EventHandler(this.useLNB3_CheckedChanged);
      // 
      // cbTransponder4
      // 
      this.cbTransponder4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.cbTransponder4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbTransponder4.Location = new System.Drawing.Point(265, 112);
      this.cbTransponder4.Name = "cbTransponder4";
      this.cbTransponder4.Size = new System.Drawing.Size(148, 21);
      this.cbTransponder4.TabIndex = 8;
      // 
      // useLNB2
      // 
      this.useLNB2.AutoSize = true;
      this.useLNB2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.useLNB2.Location = new System.Drawing.Point(6, 62);
      this.useLNB2.Name = "useLNB2";
      this.useLNB2.Size = new System.Drawing.Size(58, 17);
      this.useLNB2.TabIndex = 29;
      this.useLNB2.Text = "LNB#2";
      this.useLNB2.UseVisualStyleBackColor = true;
      this.useLNB2.CheckedChanged += new System.EventHandler(this.useLNB2_CheckedChanged);
      // 
      // cbTransponder3
      // 
      this.cbTransponder3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.cbTransponder3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbTransponder3.Location = new System.Drawing.Point(265, 88);
      this.cbTransponder3.Name = "cbTransponder3";
      this.cbTransponder3.Size = new System.Drawing.Size(148, 21);
      this.cbTransponder3.TabIndex = 6;
      // 
      // useLNB1
      // 
      this.useLNB1.AutoSize = true;
      this.useLNB1.Checked = true;
      this.useLNB1.CheckState = System.Windows.Forms.CheckState.Checked;
      this.useLNB1.Enabled = false;
      this.useLNB1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.useLNB1.Location = new System.Drawing.Point(6, 38);
      this.useLNB1.Name = "useLNB1";
      this.useLNB1.Size = new System.Drawing.Size(58, 17);
      this.useLNB1.TabIndex = 28;
      this.useLNB1.Text = "LNB#1";
      this.useLNB1.UseVisualStyleBackColor = true;
      this.useLNB1.CheckedChanged += new System.EventHandler(this.useLNB1_CheckedChanged);
      // 
      // cbTransponder2
      // 
      this.cbTransponder2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.cbTransponder2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbTransponder2.Location = new System.Drawing.Point(265, 64);
      this.cbTransponder2.Name = "cbTransponder2";
      this.cbTransponder2.Size = new System.Drawing.Size(148, 21);
      this.cbTransponder2.TabIndex = 4;
      // 
      // lnbkind4
      // 
      this.lnbkind4.Items.AddRange(new object[] {
            "Ku-Band",
            "C-Band",
            "Circular"});
      this.lnbkind4.Location = new System.Drawing.Point(187, 112);
      this.lnbkind4.Name = "lnbkind4";
      this.lnbkind4.Size = new System.Drawing.Size(72, 21);
      this.lnbkind4.TabIndex = 26;
      // 
      // cbTransponder
      // 
      this.cbTransponder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.cbTransponder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbTransponder.Location = new System.Drawing.Point(265, 40);
      this.cbTransponder.Name = "cbTransponder";
      this.cbTransponder.Size = new System.Drawing.Size(148, 21);
      this.cbTransponder.TabIndex = 2;
      // 
      // lnbkind3
      // 
      this.lnbkind3.Items.AddRange(new object[] {
            "Ku-Band",
            "C-Band",
            "Circular"});
      this.lnbkind3.Location = new System.Drawing.Point(187, 88);
      this.lnbkind3.Name = "lnbkind3";
      this.lnbkind3.Size = new System.Drawing.Size(72, 21);
      this.lnbkind3.TabIndex = 25;
      // 
      // lnbkind2
      // 
      this.lnbkind2.Items.AddRange(new object[] {
            "Ku-Band",
            "C-Band",
            "Circular"});
      this.lnbkind2.Location = new System.Drawing.Point(187, 64);
      this.lnbkind2.Name = "lnbkind2";
      this.lnbkind2.Size = new System.Drawing.Size(72, 21);
      this.lnbkind2.TabIndex = 24;
      // 
      // label30
      // 
      this.label30.Location = new System.Drawing.Point(203, 23);
      this.label30.Name = "label30";
      this.label30.Size = new System.Drawing.Size(56, 16);
      this.label30.TabIndex = 22;
      this.label30.Text = "LNB:";
      // 
      // label32
      // 
      this.label32.Location = new System.Drawing.Point(80, 23);
      this.label32.Name = "label32";
      this.label32.Size = new System.Drawing.Size(80, 16);
      this.label32.TabIndex = 20;
      this.label32.Text = "DiSEqC:";
      // 
      // diseqcd
      // 
      this.diseqcd.Items.AddRange(new object[] {
            "None",
            "Simple A",
            "Simple B",
            "Level 1 A/A",
            "Level 1 B/A",
            "Level 1 A/B",
            "Level 1 B/B"});
      this.diseqcd.Location = new System.Drawing.Point(71, 112);
      this.diseqcd.Name = "diseqcd";
      this.diseqcd.Size = new System.Drawing.Size(104, 21);
      this.diseqcd.TabIndex = 14;
      this.diseqcd.Text = "None";
      // 
      // diseqcc
      // 
      this.diseqcc.Items.AddRange(new object[] {
            "None",
            "Simple A",
            "Simple B",
            "Level 1 A/A",
            "Level 1 B/A",
            "Level 1 A/B",
            "Level 1 B/B"});
      this.diseqcc.Location = new System.Drawing.Point(71, 88);
      this.diseqcc.Name = "diseqcc";
      this.diseqcc.Size = new System.Drawing.Size(104, 21);
      this.diseqcc.TabIndex = 13;
      this.diseqcc.Text = "None";
      // 
      // diseqcb
      // 
      this.diseqcb.Items.AddRange(new object[] {
            "None",
            "Simple A",
            "Simple B",
            "Level 1 A/A",
            "Level 1 B/A",
            "Level 1 A/B",
            "Level 1 B/B"});
      this.diseqcb.Location = new System.Drawing.Point(71, 64);
      this.diseqcb.Name = "diseqcb";
      this.diseqcb.Size = new System.Drawing.Size(104, 21);
      this.diseqcb.TabIndex = 12;
      this.diseqcb.Text = "None";
      // 
      // diseqca
      // 
      this.diseqca.Items.AddRange(new object[] {
            "None",
            "Simple A",
            "Simple B",
            "Level 1 A/A",
            "Level 1 B/A",
            "Level 1 A/B",
            "Level 1 B/B"});
      this.diseqca.Location = new System.Drawing.Point(71, 40);
      this.diseqca.Name = "diseqca";
      this.diseqca.Size = new System.Drawing.Size(104, 21);
      this.diseqca.TabIndex = 1;
      this.diseqca.Text = "None";
      // 
      // lnbkind1
      // 
      this.lnbkind1.Items.AddRange(new object[] {
            "Ku-Band",
            "C-Band",
            "Circular"});
      this.lnbkind1.Location = new System.Drawing.Point(187, 40);
      this.lnbkind1.Name = "lnbkind1";
      this.lnbkind1.Size = new System.Drawing.Size(72, 21);
      this.lnbkind1.TabIndex = 27;
      // 
      // groupBox4
      // 
      this.groupBox4.Controls.Add(this.circularMHZ);
      this.groupBox4.Controls.Add(this.label20);
      this.groupBox4.Controls.Add(this.cbandMHZ);
      this.groupBox4.Controls.Add(this.label21);
      this.groupBox4.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.groupBox4.Location = new System.Drawing.Point(252, 19);
      this.groupBox4.Name = "groupBox4";
      this.groupBox4.Size = new System.Drawing.Size(176, 96);
      this.groupBox4.TabIndex = 39;
      this.groupBox4.TabStop = false;
      this.groupBox4.Text = "C-Band / Circular Config:";
      // 
      // circularMHZ
      // 
      this.circularMHZ.Location = new System.Drawing.Point(96, 45);
      this.circularMHZ.Name = "circularMHZ";
      this.circularMHZ.Size = new System.Drawing.Size(64, 20);
      this.circularMHZ.TabIndex = 3;
      this.circularMHZ.Text = "10750";
      // 
      // label20
      // 
      this.label20.Location = new System.Drawing.Point(16, 48);
      this.label20.Name = "label20";
      this.label20.Size = new System.Drawing.Size(80, 16);
      this.label20.TabIndex = 2;
      this.label20.Text = "Circular (MHz)";
      // 
      // cbandMHZ
      // 
      this.cbandMHZ.Location = new System.Drawing.Point(96, 21);
      this.cbandMHZ.Name = "cbandMHZ";
      this.cbandMHZ.Size = new System.Drawing.Size(64, 20);
      this.cbandMHZ.TabIndex = 1;
      this.cbandMHZ.Text = "5150";
      // 
      // label21
      // 
      this.label21.Location = new System.Drawing.Point(16, 24);
      this.label21.Name = "label21";
      this.label21.Size = new System.Drawing.Size(80, 16);
      this.label21.TabIndex = 0;
      this.label21.Text = "C-Band (MHz)";
      // 
      // mpGroupBox1
      // 
      this.mpGroupBox1.Controls.Add(this.lnb1MHZ);
      this.mpGroupBox1.Controls.Add(this.lnb1);
      this.mpGroupBox1.Controls.Add(this.lnbswMHZ);
      this.mpGroupBox1.Controls.Add(this.switchMHZ);
      this.mpGroupBox1.Controls.Add(this.lnb0MHZ);
      this.mpGroupBox1.Controls.Add(this.label22);
      this.mpGroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.mpGroupBox1.Location = new System.Drawing.Point(20, 19);
      this.mpGroupBox1.Name = "mpGroupBox1";
      this.mpGroupBox1.Size = new System.Drawing.Size(176, 96);
      this.mpGroupBox1.TabIndex = 38;
      this.mpGroupBox1.TabStop = false;
      this.mpGroupBox1.Text = "Ku-Band Config:";
      // 
      // lnb1MHZ
      // 
      this.lnb1MHZ.Location = new System.Drawing.Point(104, 69);
      this.lnb1MHZ.Name = "lnb1MHZ";
      this.lnb1MHZ.Size = new System.Drawing.Size(56, 20);
      this.lnb1MHZ.TabIndex = 7;
      this.lnb1MHZ.Text = "10600";
      // 
      // lnb1
      // 
      this.lnb1.Location = new System.Drawing.Point(16, 72);
      this.lnb1.Name = "lnb1";
      this.lnb1.Size = new System.Drawing.Size(72, 16);
      this.lnb1.TabIndex = 6;
      this.lnb1.Text = "LNB1 (Mhz)";
      // 
      // lnbswMHZ
      // 
      this.lnbswMHZ.Location = new System.Drawing.Point(104, 45);
      this.lnbswMHZ.Name = "lnbswMHZ";
      this.lnbswMHZ.Size = new System.Drawing.Size(56, 20);
      this.lnbswMHZ.TabIndex = 3;
      this.lnbswMHZ.Text = "11700";
      // 
      // switchMHZ
      // 
      this.switchMHZ.Location = new System.Drawing.Point(16, 48);
      this.switchMHZ.Name = "switchMHZ";
      this.switchMHZ.Size = new System.Drawing.Size(80, 16);
      this.switchMHZ.TabIndex = 2;
      this.switchMHZ.Text = "Switch (MHz)";
      // 
      // lnb0MHZ
      // 
      this.lnb0MHZ.Location = new System.Drawing.Point(104, 21);
      this.lnb0MHZ.Name = "lnb0MHZ";
      this.lnb0MHZ.Size = new System.Drawing.Size(56, 20);
      this.lnb0MHZ.TabIndex = 1;
      this.lnb0MHZ.Text = "9750";
      // 
      // label22
      // 
      this.label22.Location = new System.Drawing.Point(16, 24);
      this.label22.Name = "label22";
      this.label22.Size = new System.Drawing.Size(72, 16);
      this.label22.TabIndex = 0;
      this.label22.Text = "LNB0 (Mhz)";
      // 
      // panel1
      // 
      this.panel1.Location = new System.Drawing.Point(432, 360);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(1, 1);
      this.panel1.TabIndex = 12;
      // 
      // lblStatus
      // 
      this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblStatus.Location = new System.Drawing.Point(17, 324);
      this.lblStatus.Name = "lblStatus";
      this.lblStatus.Size = new System.Drawing.Size(500, 20);
      this.lblStatus.TabIndex = 10;
      // 
      // progressBar3
      // 
      this.progressBar3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBar3.Location = new System.Drawing.Point(103, 265);
      this.progressBar3.Name = "progressBar3";
      this.progressBar3.Size = new System.Drawing.Size(340, 16);
      this.progressBar3.Step = 1;
      this.progressBar3.TabIndex = 9;
      // 
      // button3
      // 
      this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.button3.Location = new System.Drawing.Point(454, 279);
      this.button3.Name = "button3";
      this.button3.Size = new System.Drawing.Size(72, 22);
      this.button3.TabIndex = 11;
      this.button3.Text = "Scan";
      this.button3.UseVisualStyleBackColor = true;
      this.button3.Click += new System.EventHandler(this.button1_Click);
      // 
      // Wizard_DVBSTV
      // 
      this.Controls.Add(this.groupBox3);
      this.Name = "Wizard_DVBSTV";
      this.Size = new System.Drawing.Size(545, 408);
      this.groupBox3.ResumeLayout(false);
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.groupBox4.ResumeLayout(false);
      this.groupBox4.PerformLayout();
      this.mpGroupBox1.ResumeLayout(false);
      this.mpGroupBox1.PerformLayout();
      this.ResumeLayout(false);

    }
    #endregion

    public TVCaptureDevice Card
    {
      set
      {
        _card = value;
      }
    }
    Transponder LoadTransponderName(string fileName)
    {
      Transponder ts = new Transponder();
      ts.FileName = fileName;
      ts.SatName = fileName;

      string line;
      System.IO.TextReader tin = System.IO.File.OpenText(@"Tuningparameters\"+fileName);
      while (true)
      {
        line = tin.ReadLine();
        if (line == null) break;
        string search = line.ToLower();
        int pos = search.IndexOf("satname");
        if (pos >= 0)
        {
          pos = search.IndexOf("=");
          if (pos > 0)
          {
            ts.SatName = line.Substring(pos + 1);
            ts.SatName = ts.SatName.Trim();
            break;
          }
        }
      }
      tin.Close();

      return ts;
    }

    public override void OnSectionActivated()
    {
      captureCard=_card;
      lblStatus.Text = "";
      cbTransponder.Items.Clear();
      cbTransponder2.Items.Clear();
      cbTransponder3.Items.Clear();
      cbTransponder4.Items.Clear();
      string[] files = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + @"\Tuningparameters");
      foreach (string file in files)
      {
        string fileName = System.IO.Path.GetFileName(file);
        if (fileName.ToLower().IndexOf(".tpl") >= 0)
        {
          Transponder ts = LoadTransponderName(fileName);
          if (ts != null)
          {
            cbTransponder.Items.Add(ts);
            cbTransponder2.Items.Add(ts);
            cbTransponder3.Items.Add(ts);
            cbTransponder4.Items.Add(ts);
          }
        }
      }
      if (cbTransponder.Items.Count > 0)
        cbTransponder.SelectedIndex = 0;
      if (cbTransponder2.Items.Count > 0)
        cbTransponder2.SelectedIndex = 0;
      if (cbTransponder3.Items.Count > 0)
        cbTransponder3.SelectedIndex = 0;
      if (cbTransponder4.Items.Count > 0)
        cbTransponder4.SelectedIndex = 0;

      LoadDVBSSettings();

      m_diseqcLoops = 1;
      cbTransponder.Enabled = true;
      cbTransponder2.Enabled = false;
      cbTransponder3.Enabled = false;
      cbTransponder4.Enabled = false;


      captureCard=_card;
      if (_card == null)
      {
        TVCaptureCards cards = new TVCaptureCards();
        cards.LoadCaptureCards();
        foreach (TVCaptureDevice dev in cards.captureCards)
        {
          if (dev.Network == NetworkType.DVBS)
          {
            captureCard = dev;
            break;
          }
        }
      }

      if (captureCard == null) return;
      string filename = String.Format(@"database\card_{0}.xml", captureCard.FriendlyName);
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(filename))
      {
        if (xmlreader.GetValueAsBool("dvbs", "useLNB2", false) == true)
        {
          m_diseqcLoops++;
          cbTransponder2.Enabled = true;
        }
        if (xmlreader.GetValueAsBool("dvbs", "useLNB3", false) == true)
        {
          m_diseqcLoops++;
          cbTransponder3.Enabled = true;
        }
        if (xmlreader.GetValueAsBool("dvbs", "useLNB4", false) == true)
        {
          m_diseqcLoops++;
          cbTransponder4.Enabled = true;
        }
      }
    }

    private void button1_Click(object sender, System.EventArgs e)
    {
      SaveDVBSSettings();
      LoadDVBSSettings();
      m_diseqcLoops = 1;
      string filename = String.Format(@"database\card_{0}.xml", captureCard.FriendlyName);
      if (useLNB2.Checked) m_diseqcLoops ++;
      if (useLNB3.Checked) m_diseqcLoops ++;
      if (useLNB4.Checked) m_diseqcLoops ++;
      _tplFiles = new string[m_diseqcLoops];
      Transponder ts = (Transponder)cbTransponder.SelectedItem;
      _tplFiles[0] = @"Tuningparameters\" + ts.FileName;

      if (useLNB2.Checked)
      {
        ts = (Transponder)cbTransponder2.SelectedItem;
        _tplFiles[1] = @"Tuningparameters\" + ts.FileName;
      }
      if (useLNB3.Checked)
      {
        ts = (Transponder)cbTransponder3.SelectedItem;
        _tplFiles[2] = @"Tuningparameters\" + ts.FileName;
      }
      if (useLNB4.Checked)
      {
        ts = (Transponder)cbTransponder4.SelectedItem;
        _tplFiles[3] = @"Tuningparameters\" + ts.FileName;
      }

      cbTransponder.Enabled = false;
      cbTransponder2.Enabled = false;
      cbTransponder3.Enabled = false;
      cbTransponder4.Enabled = false;
      progressBar3.Visible = true;
      button3.Enabled = false;
      GUIGraphicsContext.form = this.FindForm();
      GUIGraphicsContext.VideoWindow = new Rectangle(panel1.Location, panel1.Size);
      SaveDVBSSettings();
      Thread thread = new Thread(new ThreadStart(DoScan));
      thread.IsBackground = true;
      thread.Start();
    }

    private void DoScan()
    {
      captureCard.CreateGraph();
      ITuning tuning = new DVBSTuning();
      tuning.AutoTuneTV(captureCard, this, _tplFiles);
      tuning.Start();
      while (!tuning.IsFinished())
      {
        tuning.Next();
      }
      captureCard.DeleteGraph();

      captureCard = null;
      if (OnScanFinished != null)
        OnScanFinished(this, null);
    }

    #region AutoTuneCallback
    public void OnNewChannel()
    {
    }

    public void OnSignal(int quality, int strength)
    {
      if (quality < 0) quality = 0;
      if (quality > 100) quality = 100;
      if (strength < 0) strength = 0;
      if (strength > 100) strength = 100;
      progressBarQuality.Value = quality;
      progressBarStrength.Value = strength;
    }

    public void OnStatus(string description)
    {
      lblStatus.Text = description;
    }
    public void OnStatus2(string description)
    {
      _description = description;
      labelDescription.Text = description;
    }

    public void OnProgress(int percentDone)
    {
      progressBar3.Value = percentDone;
    }

    public void OnEnded()
    {
    }
    public void UpdateList()
    {
    }


    #endregion



    void LoadDVBSSettings()
    {
      if (captureCard == null)
      {
        Log.Write("load DVBS:no card");
        return;
      }
      Log.Write("load DVBS:{0}", captureCard.FriendlyName);
      string filename = String.Format(@"database\card_{0}.xml", captureCard.FriendlyName);


      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(filename))
      {
        lnb0MHZ.Text = xmlreader.GetValueAsInt("dvbs", "LNB0", 9750).ToString();
        lnb1MHZ.Text = xmlreader.GetValueAsInt("dvbs", "LNB1", 10600).ToString();
        lnbswMHZ.Text = xmlreader.GetValueAsInt("dvbs", "Switch", 11700).ToString();
        cbandMHZ.Text = xmlreader.GetValueAsInt("dvbs", "CBand", 5150).ToString();
        circularMHZ.Text = xmlreader.GetValueAsInt("dvbs", "Circular", 10750).ToString();
        useLNB1.Checked = true;
        useLNB1.Enabled = false;
        useLNB2.Checked = xmlreader.GetValueAsBool("dvbs", "useLNB2", false);
        useLNB3.Checked = xmlreader.GetValueAsBool("dvbs", "useLNB3", false);
        useLNB4.Checked = xmlreader.GetValueAsBool("dvbs", "useLNB4", false);

        useLNB1_CheckedChanged(null, null);
        useLNB2_CheckedChanged(null, null);
        useLNB3_CheckedChanged(null, null);
        useLNB4_CheckedChanged(null, null);

        int lnbKind = xmlreader.GetValueAsInt("dvbs", "lnbKind", 0);
        switch (lnbKind)
        {
          case 0: lnbkind1.SelectedItem = "Ku-Band"; break;
          case 1: lnbkind1.SelectedItem = "C-Band"; break;
          case 2: lnbkind1.SelectedItem = "Circular"; break;
        }
        lnbKind = xmlreader.GetValueAsInt("dvbs", "lnbKind2", 0);
        switch (lnbKind)
        {
          case 0: lnbkind2.SelectedItem = "Ku-Band"; break;
          case 1: lnbkind2.SelectedItem = "C-Band"; break;
          case 2: lnbkind2.SelectedItem = "Circular"; break;
        }
        lnbKind = xmlreader.GetValueAsInt("dvbs", "lnbKind3", 0);
        switch (lnbKind)
        {
          case 0: lnbkind3.SelectedItem = "Ku-Band"; break;
          case 1: lnbkind3.SelectedItem = "C-Band"; break;
          case 2: lnbkind3.SelectedItem = "Circular"; break;
        }
        lnbKind = xmlreader.GetValueAsInt("dvbs", "lnbKind4", 0);
        switch (lnbKind)
        {
          case 0: lnbkind4.SelectedItem = "Ku-Band"; break;
          case 1: lnbkind4.SelectedItem = "C-Band"; break;
          case 2: lnbkind4.SelectedItem = "Circular"; break;
        }
        int diseqc = xmlreader.GetValueAsInt("dvbs", "diseqc", 1);
        if (diseqc == 0) diseqc = 1;
        switch (diseqc)
        {
          case 0: diseqca.SelectedItem = "None"; break;
          case 1: diseqca.SelectedItem = "Simple A"; break;
          case 2: diseqca.SelectedItem = "Simple B"; break;
          case 3: diseqca.SelectedItem = "Level 1 A/A"; break;
          case 4: diseqca.SelectedItem = "Level 1 B/A"; break;
          case 5: diseqca.SelectedItem = "Level 1 A/B"; break;
          case 6: diseqca.SelectedItem = "Level 1 B/B"; break;

        }
        diseqc = xmlreader.GetValueAsInt("dvbs", "diseqc2", 1);
        if (diseqc == 0) diseqc = 1;
        switch (diseqc)
        {
          case 0: diseqcb.SelectedItem = "None"; break;
          case 1: diseqcb.SelectedItem = "Simple A"; break;
          case 2: diseqcb.SelectedItem = "Simple B"; break;
          case 3: diseqcb.SelectedItem = "Level 1 A/A"; break;
          case 4: diseqcb.SelectedItem = "Level 1 B/A"; break;
          case 5: diseqcb.SelectedItem = "Level 1 A/B"; break;
          case 6: diseqcb.SelectedItem = "Level 1 B/B"; break;

        }
        diseqc = xmlreader.GetValueAsInt("dvbs", "diseqc3", 1);
        if (diseqc == 0) diseqc = 1;
        switch (diseqc)
        {
          case 0: diseqcc.SelectedItem = "None"; break;
          case 1: diseqcc.SelectedItem = "Simple A"; break;
          case 2: diseqcc.SelectedItem = "Simple B"; break;
          case 3: diseqcc.SelectedItem = "Level 1 A/A"; break;
          case 4: diseqcc.SelectedItem = "Level 1 B/A"; break;
          case 5: diseqcc.SelectedItem = "Level 1 A/B"; break;
          case 6: diseqcc.SelectedItem = "Level 1 B/B"; break;

        }
        diseqc = xmlreader.GetValueAsInt("dvbs", "diseqc4", 1);
        if (diseqc == 0) diseqc = 1;
        switch (diseqc)
        {
          case 0: diseqcd.SelectedItem = "None"; break;
          case 1: diseqcd.SelectedItem = "Simple A"; break;
          case 2: diseqcd.SelectedItem = "Simple B"; break;
          case 3: diseqcd.SelectedItem = "Level 1 A/A"; break;
          case 4: diseqcd.SelectedItem = "Level 1 B/A"; break;
          case 5: diseqcd.SelectedItem = "Level 1 A/B"; break;
          case 6: diseqcd.SelectedItem = "Level 1 B/B"; break;

        }
        string transponder = xmlreader.GetValueAsString("dvbs", "transponder1", "");
        Log.Write("1:{0}", transponder);
        if (transponder != "")
        {
          for (int i = 0; i < cbTransponder.Items.Count; ++i)
          {
            Transponder tp = (Transponder)cbTransponder.Items[i];
            if (tp.SatName == transponder)
            {
              cbTransponder.SelectedIndex = i;
              break;
            }
          }
        }
        transponder = xmlreader.GetValueAsString("dvbs", "transponder2", "");

        for (int i = 0; i < cbTransponder2.Items.Count; ++i)
        {
          Transponder tp = (Transponder)cbTransponder2.Items[i];
          if (tp.SatName == transponder)
          {
            cbTransponder2.SelectedIndex = i;
            break;
          }
        }
        transponder = xmlreader.GetValueAsString("dvbs", "transponder3", "");

        for (int i = 0; i < cbTransponder3.Items.Count; ++i)
        {
          Transponder tp = (Transponder)cbTransponder3.Items[i];
          if (tp.SatName == transponder)
          {
            cbTransponder3.SelectedIndex = i;
            break;
          }
        }
        transponder = xmlreader.GetValueAsString("dvbs", "transponder4", "");

        for (int i = 0; i < cbTransponder4.Items.Count; ++i)
        {
          Transponder tp = (Transponder)cbTransponder4.Items[i];
          if (tp.SatName == transponder)
          {
            cbTransponder4.SelectedIndex = i;
            break;
          }
        }


      }
    }

    void SaveDVBSSettings()
    {
      Log.Write("Save DVBS:{0}", captureCard.FriendlyName);
      string filename = String.Format(@"database\card_{0}.xml", captureCard.FriendlyName);
      // save settings

      using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(filename))
      {
        xmlwriter.SetValue("dvbs", "LNB0", lnb0MHZ.Text);
        xmlwriter.SetValue("dvbs", "LNB1", lnb1MHZ.Text);
        xmlwriter.SetValue("dvbs", "Switch", lnbswMHZ.Text);
        xmlwriter.SetValue("dvbs", "CBand", cbandMHZ.Text);
        xmlwriter.SetValue("dvbs", "Circular", circularMHZ.Text);
        xmlwriter.SetValueAsBool("dvbs", "useLNB1", true);
        xmlwriter.SetValueAsBool("dvbs", "useLNB2", useLNB2.Checked);
        xmlwriter.SetValueAsBool("dvbs", "useLNB3", useLNB3.Checked);
        xmlwriter.SetValueAsBool("dvbs", "useLNB4", useLNB4.Checked);

        if (diseqca.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "diseqc", diseqca.SelectedIndex);
        }
        if (diseqcb.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "diseqc2", diseqcb.SelectedIndex);
        }
        if (diseqcc.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "diseqc3", diseqcc.SelectedIndex);
        }
        if (diseqcd.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "diseqc4", diseqcd.SelectedIndex);
        }
        if (lnbkind1.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "lnbKind", lnbkind1.SelectedIndex);
          xmlwriter.SetValue("dvbs", "transponder1", ((Transponder)cbTransponder.SelectedItem).SatName);
        }
        if (lnbkind2.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "lnbKind2", lnbkind2.SelectedIndex);
          xmlwriter.SetValue("dvbs", "transponder2", ((Transponder)cbTransponder2.SelectedItem).SatName);
        }
        if (lnbkind3.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "lnbKind3", lnbkind3.SelectedIndex);
          xmlwriter.SetValue("dvbs", "transponder3", ((Transponder)cbTransponder3.SelectedItem).SatName);
        }
        if (lnbkind4.SelectedIndex >= 0)
        {
          xmlwriter.SetValue("dvbs", "lnbKind4", lnbkind4.SelectedIndex);
          xmlwriter.SetValue("dvbs", "transponder4", ((Transponder)cbTransponder4.SelectedItem).SatName);
        }
      }
      MediaPortal.Profile.Settings.SaveCache();
    }

    private void useLNB1_CheckedChanged(object sender, EventArgs e)
    {
      cbTransponder2.Enabled = true;
      diseqca.Enabled = true;
      lnbkind1.Enabled = true;
    }

    private void useLNB2_CheckedChanged(object sender, EventArgs e)
    {
      cbTransponder2.Enabled = useLNB2.Checked;
      diseqcb.Enabled = useLNB2.Checked;
      lnbkind2.Enabled = useLNB2.Checked;
    }

    private void useLNB3_CheckedChanged(object sender, EventArgs e)
    {
      cbTransponder3.Enabled = useLNB3.Checked;
      diseqcc.Enabled = useLNB3.Checked;
      lnbkind3.Enabled = useLNB3.Checked;
    }

    private void useLNB4_CheckedChanged(object sender, EventArgs e)
    {
      cbTransponder4.Enabled = useLNB4.Checked;
      diseqcd.Enabled = useLNB4.Checked;
      lnbkind4.Enabled = useLNB4.Checked;
    }

  }
}

