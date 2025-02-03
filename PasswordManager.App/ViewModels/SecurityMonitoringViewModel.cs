using PasswordManager.Core.Models;
using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordManager.Core.Services;
using PasswordManager.Data.Repositories;
using PasswordManager.Data.Context;

namespace PasswordManager.App.ViewModels
{
        public class SecurityMonitoringViewModel : ViewModelBase
        {
                private readonly ILoginAttemptRepository _loginAttemptRepository;
                private readonly IDialogService _dialogService;
                private readonly IAuditLogRepository _auditLogRepository;

                public ObservableCollection<LoginAttemptModel> LoginAttempts { get; private set; }
                public ObservableCollection<IPAnalysisModel> IPAnalysis { get; private set; }
                public ObservableCollection<SecurityMetric> SecurityMetrics { get; private set; }

                private string _selectedTimeRange;
                public string SelectedTimeRange
                {
                        get => _selectedTimeRange;
                        set
                        {
                                if (SetProperty(ref _selectedTimeRange, value))
                                {
                                        LoadData();
                                }
                        }
                }

                private string _filterIPAddress;
                public string FilterIPAddress
                {
                        get => _filterIPAddress;
                        set
                        {
                                if (SetProperty(ref _filterIPAddress, value))
                                {
                                        LoadData();
                                }
                        }
                }

                private int _totalFailedAttempts;
                public int TotalFailedAttempts
                {
                        get => _totalFailedAttempts;
                        private set => SetProperty(ref _totalFailedAttempts, value);
                }

                private int _uniqueIPAddresses;
                public int UniqueIPAddresses
                {
                        get => _uniqueIPAddresses;
                        private set => SetProperty(ref _uniqueIPAddresses, value);
                }

                private double _averageAttemptsPerIP;
                public double AverageAttemptsPerIP
                {
                        get => _averageAttemptsPerIP;
                        private set => SetProperty(ref _averageAttemptsPerIP, value);
                }

                public List<string> TimeRanges { get; } = new List<string>
                {
                        "Last Hour",
                        "Last 24 Hours",
                        "Last 7 Days",
                        "Last 30 Days"
                };

                public ICommand RefreshCommand { get; }
                public ICommand ExportReportCommand { get; }

                public SecurityMonitoringViewModel(
                    ILoginAttemptRepository loginAttemptRepository,
                    IDialogService dialogService,
                    IAuditLogRepository auditLogRepository)
                {
                        _loginAttemptRepository = loginAttemptRepository;
                        _dialogService = dialogService;
                        _auditLogRepository = auditLogRepository;

                        LoginAttempts = new ObservableCollection<LoginAttemptModel>();
                        IPAnalysis = new ObservableCollection<IPAnalysisModel>();
                        SecurityMetrics = new ObservableCollection<SecurityMetric>();
                        SelectedTimeRange = "Last 24 Hours";

                        RefreshCommand = new RelayCommand(_ => LoadData());
                        ExportReportCommand = new RelayCommand(ExecuteExportReport);

                        LoadData();
                }

                private void LoadData()
                {
                        try
                        {
                                DateTime startDate = GetStartDateFromRange();
                                var attempts = _loginAttemptRepository.GetAttemptsByDateRange(startDate, DateTime.Now)
                                    .Where(a => string.IsNullOrWhiteSpace(FilterIPAddress) ||
                                               a.IPAddress.Contains(FilterIPAddress));

                                // Calculate statistics first
                                TotalFailedAttempts = attempts.Count(a => !a.IsSuccessful);

                                var ipGroups = attempts.GroupBy(a => a.IPAddress);
                                UniqueIPAddresses = ipGroups.Count();
                                AverageAttemptsPerIP = attempts.Count() / (double)Math.Max(1, UniqueIPAddresses);

                                // Update login attempts
                                LoginAttempts.Clear();
                                foreach (var attempt in attempts.OrderByDescending(a => a.AttemptDate))
                                {
                                        LoginAttempts.Add(new LoginAttemptModel
                                        {
                                                AttemptId = attempt.AttemptId,
                                                Username = attempt.Username,
                                                AttemptDate = attempt.AttemptDate,
                                                IsSuccessful = attempt.IsSuccessful,
                                                IPAddress = attempt.IPAddress,
                                                UserAgent = attempt.UserAgent
                                        });
                                }

                                // Calculate IP analysis
                                var ipAnalysis = ipGroups.Select(g => new IPAnalysisModel
                                {
                                        IPAddress = g.Key,
                                        TotalAttempts = g.Count(),
                                        FailedAttempts = g.Count(a => !a.IsSuccessful),
                                        LastAttempt = g.Max(a => a.AttemptDate),
                                        SuccessRate = g.Count(a => a.IsSuccessful) / (double)g.Count(),
                                        RiskLevel = CalculateRiskLevel(g.Count(), g.Count(a => !a.IsSuccessful))
                                });

                                IPAnalysis.Clear();
                                foreach (var analysis in ipAnalysis.OrderByDescending(a => a.FailedAttempts))
                                {
                                        IPAnalysis.Add(analysis);
                                }

                                // Update security metrics
                                UpdateSecurityMetrics(attempts, ipAnalysis);
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load security data: " + ex.Message);
                        }
                }

                private DateTime GetStartDateFromRange()
                {
                        switch (SelectedTimeRange)
                        {
                                case "Last Hour":
                                        return DateTime.Now.AddHours(-1);
                                case "Last 7 Days":
                                        return DateTime.Now.AddDays(-7);
                                case "Last 30 Days":
                                        return DateTime.Now.AddDays(-30);
                                default:
                                        return DateTime.Now.AddDays(-1); // Default to "Last 24 Hours"
                        }
                }

                private void UpdateSecurityMetrics(IEnumerable<LoginAttempt> attempts, IEnumerable<IPAnalysisModel> ipAnalysis)
                {
                        SecurityMetrics.Clear();

                        // Failed login attempts
                        int failedAttempts = attempts.Count(a => !a.IsSuccessful);
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Failed Login Attempts",
                                Value = failedAttempts.ToString(),
                                Status = failedAttempts > 50 ? "Critical" :
                                     failedAttempts > 20 ? "Warning" : "Good"
                        });

                        // Unique IP addresses
                        int uniqueIPs = ipAnalysis.Count();
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Unique IP Addresses",
                                Value = uniqueIPs.ToString(),
                                Status = "Info"
                        });

                        // High-risk IPs
                        int highRiskIPs = ipAnalysis.Count(ip => ip.RiskLevel == "High");
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "High Risk IPs",
                                Value = highRiskIPs.ToString(),
                                Status = highRiskIPs > 0 ? "Warning" : "Good"
                        });

                        // Success rate
                        double successRate = attempts.Any()
                            ? (double)attempts.Count(a => a.IsSuccessful) / attempts.Count() * 100
                            : 100;
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Login Success Rate",
                                Value = $"{successRate:F1}%",
                                Status = successRate < 70 ? "Critical" :
                                     successRate < 85 ? "Warning" : "Good"
                        });

                        // Recent activity
                        int recentAttempts = attempts
                            .Count(a => a.AttemptDate >= DateTime.Now.AddHours(-1));
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Recent Activity (1h)",
                                Value = recentAttempts.ToString(),
                                Status = recentAttempts > 100 ? "Warning" : "Info"
                        });
                }

                private string CalculateRiskLevel(int totalAttempts, int failedAttempts)
                {
                        double failureRate = failedAttempts / (double)totalAttempts;

                        if (failedAttempts >= 10 && failureRate > 0.8) return "High";
                        if (failedAttempts >= 5 && failureRate > 0.6) return "Medium";
                        return "Low";
                }

                DateTime GetSelectedTimeRange(string selectedTimeRange)
                {
                        switch (selectedTimeRange)
                        {
                                case "Last Hour":
                                        return DateTime.Now.AddHours(-1);
                                case "Last 7 Days":
                                        return DateTime.Now.AddDays(-7);
                                case "Last 30 Days":
                                        return DateTime.Now.AddDays(-30);
                                default:
                                        return DateTime.Now.AddDays(-1); // Default to "Last 24 Hours"
                        }
                }


                private void ExecuteExportReport(object parameter)
                {
                        var report = new StringBuilder();
                        report.AppendLine("Security Analysis Report");
                        report.AppendLine($"Generated: {DateTime.Now}");
                        report.AppendLine($"Time Range: {SelectedTimeRange}");
                        report.AppendLine();

                        report.AppendLine("Security Metrics:");
                        foreach (var metric in SecurityMetrics)
                        {
                                report.AppendLine($"{metric.Name}: {metric.Value} ({metric.Status})");
                        }

                        report.AppendLine("\nHigh Risk IPs:");
                        foreach (var ip in IPAnalysis.Where(ip => ip.RiskLevel == "High"))
                        {
                                report.AppendLine($"IP: {ip.IPAddress}");
                                report.AppendLine($"  Failed Attempts: {ip.FailedAttempts}");
                                report.AppendLine($"  Success Rate: {ip.SuccessRate:P}");
                                report.AppendLine($"  Last Attempt: {ip.LastAttempt}");
                                report.AppendLine();
                        }

                        _dialogService.ShowMessage(report.ToString(), "Security Analysis Report");
                }
        }
}
