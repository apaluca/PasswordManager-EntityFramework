using PasswordManager.Core.MVVM;
using PasswordManager.Core.Models;
using PasswordManager.Core.Services;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Windows.Threading;
using PasswordManager.Core.MVVM.Interfaces;

namespace PasswordManager.App.ViewModels
{
        public class SecurityManagementViewModel : ViewModelBase, IDisposable
        {
                private readonly ISecurityManagementRepository _securityRepository;
                private readonly IDialogService _dialogService;
                private readonly DispatcherTimer _refreshTimer;
                private readonly DispatcherTimer _securityCheckTimer;

                // Observable Collections
                public ObservableCollection<SecurityAlertModel> ActiveAlerts { get; private set; }
                public ObservableCollection<BlockedIPModel> BlockedIPs { get; private set; }
                public ObservableCollection<string> AlertSeverityFilters { get; private set; }

                // Current Policy
                private PasswordPolicyModel _currentPolicy;
                public PasswordPolicyModel CurrentPolicy
                {
                        get => _currentPolicy;
                        set => SetProperty(ref _currentPolicy, value);
                }

                // Security Metrics
                private SecurityMetricsModel _securityMetrics;
                public SecurityMetricsModel SecurityMetrics
                {
                        get => _securityMetrics;
                        set => SetProperty(ref _securityMetrics, value);
                }

                // Filter Properties
                private string _selectedAlertSeverityFilter;
                public string SelectedAlertSeverityFilter
                {
                        get => _selectedAlertSeverityFilter;
                        set
                        {
                                if (SetProperty(ref _selectedAlertSeverityFilter, value))
                                {
                                        LoadAlerts();
                                }
                        }
                }

                // Commands
                public ICommand RefreshCommand { get; }
                public ICommand UpdatePolicyCommand { get; }
                public ICommand BlockIPCommand { get; }
                public ICommand UnblockIPCommand { get; }
                public ICommand ResolveAlertCommand { get; }
                public ICommand RefreshAlertsCommand { get; }
                public ICommand RefreshBlocksCommand { get; }

                public SecurityManagementViewModel(
                    ISecurityManagementRepository securityRepository,
                    IDialogService dialogService)
                {
                        _securityRepository = securityRepository;
                        _dialogService = dialogService;

                        // Initialize collections
                        ActiveAlerts = new ObservableCollection<SecurityAlertModel>();
                        BlockedIPs = new ObservableCollection<BlockedIPModel>();
                        AlertSeverityFilters = new ObservableCollection<string>
                        {
                                "All",
                                "Critical",
                                "High",
                                "Medium",
                                "Low"
                        };
                        SelectedAlertSeverityFilter = "All";

                        // Initialize commands
                        RefreshCommand = new RelayCommand(_ => LoadData());
                        UpdatePolicyCommand = new RelayCommand(_ => ExecuteUpdatePolicy());
                        BlockIPCommand = new RelayCommand(_ => ExecuteBlockIP());
                        UnblockIPCommand = new RelayCommand(ExecuteUnblockIP);
                        ResolveAlertCommand = new RelayCommand(ExecuteResolveAlert);
                        RefreshAlertsCommand = new RelayCommand(_ => LoadAlerts());
                        RefreshBlocksCommand = new RelayCommand(_ => LoadBlockedIPs());

                        // Set up refresh timer (every 5 minutes)
                        _refreshTimer = new DispatcherTimer
                        {
                                Interval = TimeSpan.FromMinutes(5)
                        };
                        _refreshTimer.Tick += (s, e) => LoadData();
                        _refreshTimer.Start();

                        // Set up security check timer (every 15 minutes)
                        _securityCheckTimer = new DispatcherTimer
                        {
                                Interval = TimeSpan.FromMinutes(15)
                        };
                        _securityCheckTimer.Tick += (s, e) =>
                        {
                                _securityRepository.CheckForSuspiciousActivity();
                                _securityRepository.CleanupExpiredBlocks();
                                LoadData();
                        };
                        _securityCheckTimer.Start();

                        // Load initial data
                        LoadData();
                }

                public void Dispose()
                {
                        _refreshTimer?.Stop();
                        _securityCheckTimer?.Stop();
                }

                private void LoadData()
                {
                        LoadPolicy();
                        LoadAlerts();
                        LoadBlockedIPs();
                        LoadMetrics();
                }

                private void LoadPolicy()
                {
                        try
                        {
                                CurrentPolicy = _securityRepository.GetActivePolicy();
                                if (CurrentPolicy == null)
                                {
                                        CurrentPolicy = new PasswordPolicyModel
                                        {
                                                MinLength = 8,
                                                RequireUppercase = true,
                                                RequireLowercase = true,
                                                RequireNumbers = true,
                                                RequireSpecialChars = true,
                                                MaxLoginAttempts = 5,
                                                LockoutDurationMinutes = 30
                                        };
                                }
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load password policy: " + ex.Message);
                        }
                }

                private void LoadAlerts()
                {
                        try
                        {
                                var alerts = _securityRepository.GetActiveAlerts();
                                if (SelectedAlertSeverityFilter != "All")
                                {
                                        alerts = alerts.Where(a => a.Severity.ToString() == SelectedAlertSeverityFilter);
                                }

                                ActiveAlerts.Clear();
                                foreach (var alert in alerts)
                                {
                                        ActiveAlerts.Add(alert);
                                }
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load security alerts: " + ex.Message);
                        }
                }

                private void LoadBlockedIPs()
                {
                        try
                        {
                                var blocks = _securityRepository.GetActiveBlocks();
                                BlockedIPs.Clear();
                                foreach (var block in blocks)
                                {
                                        BlockedIPs.Add(block);
                                }
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load blocked IPs: " + ex.Message);
                        }
                }

                private void LoadMetrics()
                {
                        try
                        {
                                SecurityMetrics = _securityRepository.GetSecurityMetrics();
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load security metrics: " + ex.Message);
                        }
                }

                private void ExecuteUpdatePolicy()
                {
                        try
                        {
                                if (!ValidatePolicy())
                                        return;

                                CurrentPolicy.ModifiedByUserId = SessionManager.CurrentUser.UserId;
                                _securityRepository.CreatePolicy(CurrentPolicy);

                                LoadPolicy(); // Reload to get updated policy
                                _dialogService.ShowMessage("Password policy updated successfully.");
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to update policy: " + ex.Message);
                        }
                }

                private bool ValidatePolicy()
                {
                        if (CurrentPolicy.MinLength < 8)
                        {
                                _dialogService.ShowError("Minimum password length must be at least 8 characters.");
                                return false;
                        }

                        if (CurrentPolicy.MaxLoginAttempts < 1)
                        {
                                _dialogService.ShowError("Maximum login attempts must be at least 1.");
                                return false;
                        }

                        if (CurrentPolicy.LockoutDurationMinutes < 1)
                        {
                                _dialogService.ShowError("Lockout duration must be at least 1 minute.");
                                return false;
                        }

                        return true;
                }

                private void ExecuteBlockIP()
                {
                        string ipAddress = _dialogService.ShowPrompt("Enter IP address to block:");
                        if (string.IsNullOrWhiteSpace(ipAddress)) return;

                        if (_securityRepository.IsIPBlocked(ipAddress))
                        {
                                _dialogService.ShowError("This IP address is already blocked.");
                                return;
                        }

                        string reason = _dialogService.ShowPrompt("Enter reason for blocking:");
                        if (string.IsNullOrWhiteSpace(reason)) return;

                        try
                        {
                                var block = new BlockedIPModel
                                {
                                        IPAddress = ipAddress,
                                        Reason = reason,
                                        BlockedByUserId = SessionManager.CurrentUser.UserId,
                                        IsActive = true
                                };

                                _securityRepository.BlockIP(block);
                                LoadBlockedIPs();
                                _dialogService.ShowMessage("IP address blocked successfully.");
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to block IP: " + ex.Message);
                        }
                }

                private void ExecuteUnblockIP(object parameter)
                {
                        if (parameter is BlockedIPModel block)
                        {
                                if (_dialogService.ShowConfirmation($"Are you sure you want to unblock {block.IPAddress}?"))
                                {
                                        try
                                        {
                                                _securityRepository.UnblockIP(block.BlockId);
                                                LoadBlockedIPs();
                                                _dialogService.ShowMessage("IP address unblocked successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                                _dialogService.ShowError("Failed to unblock IP: " + ex.Message);
                                        }
                                }
                        }
                }

                private void ExecuteResolveAlert(object parameter)
                {
                        if (parameter is SecurityAlertModel alert)
                        {
                                string notes = _dialogService.ShowPrompt("Enter resolution notes:");
                                if (string.IsNullOrWhiteSpace(notes)) return;

                                try
                                {
                                        _securityRepository.ResolveAlert(alert.AlertId, SessionManager.CurrentUser.UserId, notes);
                                        LoadAlerts();
                                        _dialogService.ShowMessage("Alert resolved successfully.");
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to resolve alert: " + ex.Message);
                                }
                        }
                }
        }
}