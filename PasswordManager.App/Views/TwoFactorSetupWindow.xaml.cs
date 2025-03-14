﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public partial class TwoFactorSetupWindow : Window
        {
                public TwoFactorSetupWindow()
                {
                        InitializeComponent();
                }

                private void VerificationCodeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
                {
                        e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
                }
        }
}
