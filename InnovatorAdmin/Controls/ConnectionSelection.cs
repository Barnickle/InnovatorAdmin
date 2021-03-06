﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Aras.Tools.InnovatorAdmin.Connections;

namespace Aras.Tools.InnovatorAdmin.Controls
{
  public partial class ConnectionSelection : UserControl, IWizardStep
  {
    private IWizard _wizard;

    public bool MultiSelect 
    {
      get { return connEditor.MultiSelect; }
      set { connEditor.MultiSelect = value; }
    }
    public Action GoNextAction { get; set; }

    public ConnectionSelection()
    {
      InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      connEditor.LoadConnectionLibrary(ConnectionManager.Current.Library);
    }

    public void Configure(IWizard wizard)
    {
      _wizard = wizard;
      _wizard.Message = "Select a connection to use";
      _wizard.NextEnabled = connEditor.SelectedConnections.Any();
    }

    public void GoNext()
    {
      ConnectionManager.Current.Save();
      
      if (!connEditor.SelectedConnections.Any())
      {
        MessageBox.Show(resources.Messages.NoConnectionSelected);
      }
      else
      {
        string msg;
        _wizard.ConnectionInfo = connEditor.SelectedConnections;
        var conn = ConnectionEditor.Login(_wizard.ConnectionInfo.First(), out msg);
        if (conn == null)
        {
          MessageBox.Show(msg);
        }
        else
        {
          _wizard.Connection = conn;
          this.GoNextAction();
        }
      }
    }

    private void connEditor_SelectionChanged(object sender, EventArgs e)
    {
      _wizard.NextEnabled = connEditor.SelectedConnections.Any();
    }
  }
}
