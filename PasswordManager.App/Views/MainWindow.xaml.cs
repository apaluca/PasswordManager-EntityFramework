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
        public partial class MainWindow : Window
        {
                public MainWindow()
                {
                        InitializeComponent();
                        this.Loaded += MainWindow_Loaded;
                }

                private void MainWindow_Loaded(object sender, RoutedEventArgs e)
                {
                        if (DataContext == null)
                        {
                                MessageBox.Show("DataContext is null!");
                        }
                }
        }
}
