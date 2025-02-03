using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Core.Services;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordManager.App.Views;
using PasswordManager.Core.Services.Interfaces;
using System.Windows;
using PasswordManager.Core.Models;

namespace PasswordManager.App.ViewModels
{
        public class DashboardViewModel : ViewModelBase
        {
                private readonly IStoredPasswordRepository _passwordRepository;
                private readonly IPasswordGroupRepository _groupRepository;
                private readonly ILoginAttemptRepository _loginAttemptRepository;
                private readonly IAuditLogRepository _auditLogRepository;
                private readonly IDialogService _dialogService;
                private readonly ISecurityService _securityService;
                private readonly IEncryptionService _encryptionService;
                private readonly IPasswordStrengthService _passwordStrengthService;

                public ObservableCollection<StoredPasswordModel> ExpiringPasswords { get; private set; }
                public ObservableCollection<StoredPasswordModel> RecentPasswords { get; private set; }
                public ObservableCollection<SecurityMetric> SecurityMetrics { get; private set; }
                public ObservableCollection<LoginAttemptModel> RecentLoginAttempts { get; private set; }
                public ObservableCollection<AuditLogModel> RecentAuditLogs { get; private set; }
                public ObservableCollection<PasswordGroupModel> Groups { get; private set; }

                private int _totalPasswords;
                public int TotalPasswords
                {
                        get => _totalPasswords;
                        set => SetProperty(ref _totalPasswords, value);
                }

                private int _totalGroups;
                public int TotalGroups
                {
                        get => _totalGroups;
                        set => SetProperty(ref _totalGroups, value);
                }

                private int _expiringPasswordCount;
                public int ExpiringPasswordCount
                {
                        get => _expiringPasswordCount;
                        set => SetProperty(ref _expiringPasswordCount, value);
                }

                private int _weakPasswordCount;
                public int WeakPasswordCount
                {
                        get => _weakPasswordCount;
                        set => SetProperty(ref _weakPasswordCount, value);
                }

                private int _failedLoginAttempts;
                public int FailedLoginAttempts
                {
                        get => _failedLoginAttempts;
                        set => SetProperty(ref _failedLoginAttempts, value);
                }

                public bool IsAdministrator => SessionManager.CurrentUser.Role == "Administrator";
                public bool IsITSpecialist => SessionManager.CurrentUser.Role == "IT_Specialist";

                public ICommand RefreshCommand { get; }
                public ICommand AddPasswordCommand { get; }
                public ICommand AddGroupCommand { get; }

                public DashboardViewModel(
                    IStoredPasswordRepository passwordRepository,
                    IPasswordGroupRepository groupRepository,
                    ILoginAttemptRepository loginAttemptRepository,
                    IAuditLogRepository auditLogRepository,
                    IDialogService dialogService,
                    ISecurityService securityService,
                    IEncryptionService encryptionService,
                    IPasswordStrengthService passwordStrengthService)
                {
                        _passwordRepository = passwordRepository;
                        _groupRepository = groupRepository;
                        _loginAttemptRepository = loginAttemptRepository;
                        _auditLogRepository = auditLogRepository;
                        _dialogService = dialogService;
                        _securityService = securityService;
                        _encryptionService = encryptionService;
                        _passwordStrengthService = passwordStrengthService;

                        // Initialize collections
                        RecentPasswords = new ObservableCollection<StoredPasswordModel>();
                        RecentLoginAttempts = new ObservableCollection<LoginAttemptModel>();
                        RecentAuditLogs = new ObservableCollection<AuditLogModel>();
                        ExpiringPasswords = new ObservableCollection<StoredPasswordModel>();
                        SecurityMetrics = new ObservableCollection<SecurityMetric>();
                        Groups = new ObservableCollection<PasswordGroupModel>();

                        // Initialize commands
                        RefreshCommand = new RelayCommand(_ => LoadDashboardData());
                        AddPasswordCommand = new RelayCommand(ExecuteAddPassword);
                        AddGroupCommand = new RelayCommand(ExecuteAddGroup);

                        LoadDashboardData();
                }

                private void LoadDashboardData()
                {
                        var userId = SessionManager.CurrentUser.UserId;
                        var passwords = _passwordRepository.GetByUserId(userId).ToList();
                        var groups = _groupRepository.GetByUserId(userId).ToList();

                        // Update basic metrics
                        TotalPasswords = passwords.Count;
                        TotalGroups = groups.Count;

                        // Update Groups collection
                        Groups.Clear();
                        foreach (var group in groups)
                        {
                                Groups.Add(group);
                        }

                        // Get recent passwords
                        var recentPasswords = passwords
                            .OrderByDescending(p => p.ModifiedDate)
                            .Take(5);

                        RecentPasswords.Clear();
                        foreach (var password in recentPasswords)
                        {
                                RecentPasswords.Add(password);
                        }

                        // Get expiring passwords (within 30 days)
                        var thirtyDaysFromNow = DateTime.Now.AddDays(30);
                        var expiring = passwords
                            .Where(p => p.ExpirationDate <= thirtyDaysFromNow)
                            .OrderBy(p => p.ExpirationDate);

                        ExpiringPasswords.Clear();
                        foreach (var password in expiring)
                        {
                                ExpiringPasswords.Add(password);
                        }
                        ExpiringPasswordCount = expiring.Count();

                        // Calculate security metrics
                        SecurityMetrics.Clear();
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Total Passwords",
                                Value = TotalPasswords.ToString(),
                                Status = "Info"
                        });
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Password Groups",
                                Value = TotalGroups.ToString(),
                                Status = "Info"
                        });
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "Expiring Soon",
                                Value = ExpiringPasswordCount.ToString(),
                                Status = ExpiringPasswordCount > 0 ? "Warning" : "Good"
                        });
                        SecurityMetrics.Add(new SecurityMetric
                        {
                                Name = "2FA Status",
                                Value = SessionManager.CurrentUser.TwoFactorEnabled ? "Enabled" : "Disabled",
                                Status = SessionManager.CurrentUser.TwoFactorEnabled ? "Good" : "Warning"
                        });
                }

                private void ExecuteAddPassword(object parameter)
                {
                        var viewModel = new PasswordEntryViewModel(
                            _passwordRepository,
                            _groupRepository,
                            _securityService,
                            _encryptionService,
                            _dialogService,
                            _passwordStrengthService,
                            _auditLogRepository);

                        var window = new PasswordEntryWindow
                        {
                                DataContext = viewModel,
                                Owner = Application.Current.MainWindow
                        };

                        viewModel.RequestClose += (s, e) =>
                        {
                                window.DialogResult = true;
                                window.Close();
                                LoadDashboardData();
                        };

                        window.ShowDialog();
                }

                private void ExecuteAddGroup(object parameter)
                {
                        string groupName = _dialogService.ShowPrompt("Enter group name:");
                        if (string.IsNullOrWhiteSpace(groupName)) return;

                        if (_groupRepository.IsGroupNameTaken(SessionManager.CurrentUser.UserId, groupName))
                        {
                                _dialogService.ShowError("A group with this name already exists.");
                                return;
                        }

                        string description = _dialogService.ShowPrompt("Enter group description (optional):");

                        try
                        {
                                var group = new PasswordGroupModel
                                {
                                        UserId = SessionManager.CurrentUser.UserId,
                                        GroupName = groupName,
                                        Description = description
                                };

                                _groupRepository.Create(group);
                                _auditLogRepository.LogAction(
                                    SessionManager.CurrentUser.UserId,
                                    "Group_Created",
                                    $"Created password group: {groupName}",
                                    "localhost");

                                LoadDashboardData();
                                _dialogService.ShowMessage("Group created successfully.");
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to create group: " + ex.Message);
                        }
                }
        }
}
