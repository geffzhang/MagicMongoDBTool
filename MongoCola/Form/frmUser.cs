﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SystemUtility;
using Common;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoUtility.Basic;
using MongoUtility.Core;
using MongoUtility.Security;
using ResourceLib;
using ResourceLib.Properties;
using Utility = Common.Utility;

namespace MongoCola
{
    public partial class frmUser : Form
    {
        /// <summary>
        ///     是否作为Admin
        /// </summary>
        private readonly Boolean _IsAdmin;

        private readonly String _ModifyName = String.Empty;
        private readonly Dictionary<String, BsonElement> OtherDBRolesDict = new Dictionary<string, BsonElement>();

        /// <summary>
        ///     frmUser
        /// </summary>
        /// <param name="IsAdmin"></param>
        public frmUser(Boolean IsAdmin)
        {
            InitializeComponent();
            _IsAdmin = IsAdmin;
            foreach (var item in RuntimeMongoDBContext.GetCurrentServer().GetDatabaseNames())
            {
                cmbDB.Items.Add(item);
            }
            if (!IsAdmin)
            {
                //Admin以外的不能有otherDBRoles
                Width = Width/2;
            }
            userRoles.IsAdmin = IsAdmin;
        }

        /// <summary>
        ///     frmUser
        /// </summary>
        /// <param name="IsAdmin"></param>
        /// <param name="UserName"></param>
        public frmUser(Boolean IsAdmin, String UserName)
        {
            InitializeComponent();
            _IsAdmin = IsAdmin;
            _ModifyName = UserName;
            cmbDB.Items.Clear();
            foreach (var item in RuntimeMongoDBContext.GetCurrentServer().GetDatabaseNames())
            {
                cmbDB.Items.Add(item);
            }
            if (!IsAdmin)
            {
                //Admin以外的不能有otherDBRoles
                Width = Width/2;
            }
        }

        /// <summary>
        ///     确定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (txtConfirmPsw.Text != txtPassword.Text)
            {
                MyMessageBox.ShowMessage("Error", "Password and Confirm Password not match!");
                return;
            }
            //MongoUser不能同时具备Password和userSource字段！
            var user = new User
            {
                Username = txtUserName.Text,
                Password = txtUserName.Text,
                roles = userRoles.getRoles()
            };
            var otherDBRoles = new BsonDocument();
            foreach (var item in OtherDBRolesDict.Values)
            {
                otherDBRoles.Add(item);
            }
            user.otherDBRoles = otherDBRoles;
            user.userSource = txtuserSource.Text;
            if (txtUserName.Text == String.Empty)
            {
                MyMessageBox.ShowMessage("Error", "Please fill username!");
                return;
            }
            //2013/08/13 用户结构发生大的变化
            //取消了ReadOnly字段，添加了Roles等字段
            //简化逻辑，不论新建还是修改，AddUser都可以
            try
            {
                User.AddUserToSystem(user, _IsAdmin);
            }
            catch (Exception ex)
            {
                Utility.ExceptionDeal(ex);
            }
            Close();
        }

        /// <summary>
        ///     关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmUser_Load(object sender, EventArgs e)
        {
            if (_ModifyName != String.Empty)
            {
                Text = "Change User Config";
                txtUserName.Enabled = false;
                txtUserName.Text = _ModifyName;
                var userInfo = RuntimeMongoDBContext.GetCurrentDataBase().GetCollection(ConstMgr.COLLECTION_NAME_USER)
                    .FindOneAs<BsonDocument>(Query.EQ("user", _ModifyName));
                userRoles.setRoles(userInfo["roles"].AsBsonArray);
                OtherDBRolesDict.Clear();
                foreach (var item in userInfo["otherDBRoles"].AsBsonDocument)
                {
                    OtherDBRolesDict.Add(item.Name, item);
                }
                RefreshOtherDBRoles();
            }
            if (!SystemConfig.IsUseDefaultLanguage)
            {
                if (_ModifyName == String.Empty)
                {
                    Text =
                        SystemConfig.guiConfig.MStringResource.GetText(_IsAdmin
                            ? StringResource.TextType.Main_Menu_Operation_Server_AddUserToAdmin
                            : StringResource.TextType.Main_Menu_Operation_Database_AddUser);
                    Icon = GetSystemIcon.ConvertImgToIcon(Resources.AddUserToDB);
                }
                else
                {
                    Icon = GetSystemIcon.ConvertImgToIcon(Resources.DBkey);
                    Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_ChangePassword);
                }
                lblUserName.Text =
                    SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Username);
                lblPassword.Text =
                    SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Password);
                lblConfirmPsw.Text =
                    SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_ConfirmPassword);
                //chkReadOnly.Text = SystemConfig.guiConfig.MStringResource.GetText(MagicMongoDBTool.Module.StringResource.TextType.Common_ReadOnly);
                colRoles.Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Roles);
                colDataBase.Text =
                    SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_DataBase);
                cmdOK.Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_OK);
                cmdCancel.Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Cancel);
            }
        }

        /// <summary>
        ///     刷新角色
        /// </summary>
        private void RefreshOtherDBRoles()
        {
            lstOtherRoles.Items.Clear();
            foreach (var item in OtherDBRolesDict.Keys)
            {
                lstOtherRoles.Items.Add(new ListViewItem(new[] {item, OtherDBRolesDict[item].Value.ToString()}));
            }
        }

        /// <summary>
        ///     增加角色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdAddRole_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(cmbDB.Text))
            {
                MyMessageBox.ShowMessage("Error", "Please Select A Database");
                return;
            }
            var mUserRole = new frmUserRole(new BsonArray());
            mUserRole.ShowDialog();
            var otherRole = new BsonElement(cmbDB.Text, mUserRole.Result);
            if (OtherDBRolesDict.ContainsKey(cmbDB.Text))
            {
                OtherDBRolesDict[cmbDB.Text] = otherRole;
            }
            else
            {
                OtherDBRolesDict.Add(cmbDB.Text, otherRole);
            }
            RefreshOtherDBRoles();
        }

        /// <summary>
        ///     删除角色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdDelRole_Click(object sender, EventArgs e)
        {
            if (lstOtherRoles.SelectedItems.Count == 0)
            {
                MyMessageBox.ShowMessage("Error", "Please Select A Database");
            }
            else
            {
                OtherDBRolesDict.Remove(lstOtherRoles.SelectedItems[0].Text);
                RefreshOtherDBRoles();
            }
        }

        /// <summary>
        ///     修改角色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdModifyRole_Click(object sender, EventArgs e)
        {
            if (lstOtherRoles.SelectedItems.Count == 0)
            {
                MyMessageBox.ShowMessage("Error", "Please Select A Database");
            }
            else
            {
                var DBName = lstOtherRoles.SelectedItems[0].Text;
                var mUserRole = new frmUserRole(OtherDBRolesDict[DBName].Value.AsBsonArray);
                mUserRole.ShowDialog();
                var otherRole = new BsonElement(cmbDB.Text, mUserRole.Result);
                OtherDBRolesDict[DBName] = otherRole;
                RefreshOtherDBRoles();
            }
        }
    }
}