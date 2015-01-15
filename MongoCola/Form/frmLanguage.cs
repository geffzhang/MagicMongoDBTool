﻿using System;
using System.IO;
using System.Windows.Forms;
using SystemUtility;

namespace MongoCola
{
    public partial class frmLanguage : Form
    {
        public frmLanguage()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmLanguage_Load(object sender, EventArgs e)
        {
            cmbLanguage.Text = "English";
            cmbLanguage.Items.Add("English");
            if (!Directory.Exists("Language")) return;
            foreach (var FileName in Directory.GetFiles("Language"))
            {
                cmbLanguage.Items.Add(new FileInfo(FileName).Name.Substring(0,
                    new FileInfo(FileName).Name.Length - 4));
            }
        }

        /// <summary>
        ///     OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdOK_Click(object sender, EventArgs e)
        {
            SystemConfig.config.LanguageFileName = cmbLanguage.Text + ".xml";
            ConfigHelper.SaveToConfigFile();
            Close();
        }
    }
}