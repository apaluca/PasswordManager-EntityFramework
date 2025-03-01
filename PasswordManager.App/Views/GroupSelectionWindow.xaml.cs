﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PasswordManager.App.Views
{
        public partial class GroupSelectionWindow : Window
        {
                public int SelectedIndex => GroupComboBox.SelectedIndex;

                public GroupSelectionWindow(List<string> groups, int currentGroupIndex = -1)
                {
                        InitializeComponent();
                        GroupComboBox.ItemsSource = groups;
                        GroupComboBox.SelectedIndex = currentGroupIndex;
                }

                private void OKButton_Click(object sender, RoutedEventArgs e)
                {
                        DialogResult = true;
                        Close();
                }

                private void CancelButton_Click(object sender, RoutedEventArgs e)
                {
                        DialogResult = false;
                        Close();
                }
        }
}
