//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSScriptTableView.cs $
//
// Version:         $Revision: 8933 $,
//                  $Date: 2016-11-23 09:20:07 +0200 (ke, 23 marras 2016) $
//                  $Author: gurux01 $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux DLMS/COSEM Director: http://www.gurux.org/GXDLMSDirector
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Gurux.DLMS.Objects;
using Gurux.DLMS;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// http://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSScriptTable
    /// </summary>
    [GXDLMSViewAttribute(typeof(Gurux.DLMS.Objects.GXDLMSScriptTable))]
    partial class GXDLMSScriptTableView : Form, IGXDLMSView
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSScriptTableView()
        {
            InitializeComponent();
        }

        #region IGXDLMSView Members

        public GXDLMSObject Target
        {
            get;
            set;
        }

        public void OnValueChanged(int index, object value, bool user, bool connected)
        {
            if (index == 2)
            {
                ScriptsTree.Nodes.Clear();
                foreach (GXDLMSScript it in ((GXDLMSScriptTable)Target).Scripts)
                {
                    TreeNode s = ScriptsTree.Nodes.Add(it.Id.ToString());
                    s.Tag = it;
                    foreach (GXDLMSScriptAction a in it.Actions)
                    {
                        if (a.Target != null)
                        {
                            s.Nodes.Add(a.Target.ToString()).Tag = a;
                        }
                    }
                }
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public void OnAccessRightsChange(int index, AccessMode access, bool connected)
        {
            bool enabled = connected && (access & AccessMode.Write) != 0;
            if (index == 2)
            {
                addToolStripMenuItem.Enabled = editToolStripMenuItem.Enabled = removeToolStripMenuItem.Enabled =
                    AddBtn.Enabled = EditBtn.Enabled = RemoveBtn.Enabled = enabled;
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }

        }

        public void OnAccessRightsChange(int index, MethodAccessMode mode, bool connected)
        {
        }

        delegate void ExecuteScriptEventHandler(GXActionArgs arg);

        void OnExecuteScript(GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ExecuteScriptEventHandler(OnExecuteScript), arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                if (ScriptsTree.SelectedNode != null)
                {
                    DialogResult ret = GXHelpers.ShowMessageBox(this, Properties.Resources.ScriptTableExecute, "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Yes)
                    {
                        arg.Value = (Target as GXDLMSScriptTable).Execute(arg.Client, (GXDLMSScript)ScriptsTree.SelectedNode.Tag);
                    }
                    arg.Handled = ret != DialogResult.Yes;
                }
                else
                {
                    //Do nothing if script is not select.
                    arg.Handled = true;
                }
            }
        }

        public void PreAction(GXActionArgs arg)
        {
            if (arg.Index == 1)
            {
                //Execute selected script.
                OnExecuteScript(arg);
            }
        }

        public void PostAction(GXActionArgs arg)
        {
            arg.Action = ActionType.None;
        }

        public System.Windows.Forms.ErrorProvider ErrorProvider
        {
            get
            {
                return errorProvider1;
            }
        }

        public string Description
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public void OnDirtyChange(int index, bool Dirty)
        {

        }

        #endregion

        /// <summary>
        /// Add new script.
        /// </summary>
        private void AddBtn_Click(object sender, EventArgs e)
        {
            try
            {
                GXDLMSScript s = new GXDLMSScript();
                GXDLMSScriptDlg dlg = new GXDLMSScriptDlg(s, Target.Parent, !addToolStripMenuItem.Enabled);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    GXDLMSScriptTable target = Target as GXDLMSScriptTable;
                    TreeNode node = ScriptsTree.Nodes.Add(s.Id.ToString());
                    node.Tag = s;
                    foreach (GXDLMSScriptAction a in s.Actions)
                    {
                        if (a.Target != null)
                        {
                            node.Nodes.Add(a.Target.ToString()).Tag = a;
                        }
                    }
                    target.Scripts.Add(s);
                    errorProvider1.SetError(ScriptsTree, Properties.Resources.ValueChangedTxt);
                    Target.UpdateDirty(2, target.Scripts);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Edit script.
        /// </summary>
        private void EditBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (ScriptsTree.SelectedNode != null)
                {
                    GXDLMSScript s = null;
                    object target = ScriptsTree.SelectedNode.Tag;
                    if (target is GXDLMSScript)
                    {
                        s = target as GXDLMSScript;
                    }
                    else if (target is GXDLMSScriptAction)
                    {
                        s = ScriptsTree.SelectedNode.Parent.Tag as GXDLMSScript;
                    }
                    GXDLMSScriptDlg dlg = new GXDLMSScriptDlg(s, Target.Parent, !addToolStripMenuItem.Enabled);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        GXDLMSScriptTable st = Target as GXDLMSScriptTable;
                        errorProvider1.SetError(ScriptsTree, Properties.Resources.ValueChangedTxt);
                        Target.UpdateDirty(2, st.Scripts);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Remove selected scripts.
        /// </summary>
        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (ScriptsTree.SelectedNode != null)
                {
                    GXDLMSScriptTable st = Target as GXDLMSScriptTable;
                    object target = ScriptsTree.SelectedNode.Tag;
                    if (target is GXDLMSScript)
                    {
                        ((GXDLMSScriptTable)Target).Scripts.Remove(target as GXDLMSScript);
                    }
                    else if (target is GXDLMSScriptAction)
                    {
                        GXDLMSScript s = ScriptsTree.SelectedNode.Parent.Tag as GXDLMSScript;
                        s.Actions.Remove(target as GXDLMSScriptAction);
                    }
                    ScriptsTree.SelectedNode.Remove();
                    errorProvider1.SetError(ScriptsTree, Properties.Resources.ValueChangedTxt);
                    Target.UpdateDirty(2, st.Scripts);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ScriptsTree_DoubleClick(object sender, EventArgs e)
        {
            EditBtn_Click(null, null);
        }
    }
}
