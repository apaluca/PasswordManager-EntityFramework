using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordManager.Core.Services;
using PasswordManager.Data.Context;
using PasswordManager.Data.Repositories;
using System.Net;
using PasswordManager.Core.Utilities;
using PasswordManager.Core.Models;

namespace PasswordManager.App.ViewModels
{
        public class LoginViewModel : ViewModelBase
        {
                private readonly IUserRepository _userRepository;
                private readonly ISecurityService _securityService;
                private readonly ILoginAttemptRepository _loginAttemptRepository;
                private readonly IDialogService _dialogService;
                private readonly INavigationService _navigationService;
                private readonly IAuditLogRepository _auditLogRepository;
                private readonly ISecurityManagementRepository _securityRepository;

                private string _username;
                private string _password;
                private string _twoFactorCode;
                private bool _isTwoFactorRequired;
                private User _currentUser;

                public string Username
                {
                        get => _username;
                        set
                        {
                                SetProperty(ref _username, value);
                                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                        }
                }

                public string Password
                {
                        get => _password;
                        set
                        {
                                SetProperty(ref _password, value);
                                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                        }
                }

                public string TwoFactorCode
                {
                        get => _twoFactorCode;
                        set
                        {
                                SetProperty(ref _twoFactorCode, value);
                                ((RelayCommand)VerifyTwoFactorCommand).RaiseCanExecuteChanged();
                        }
                }

                public bool IsTwoFactorRequired
                {
                        get => _isTwoFactorRequired;
                        private set => SetProperty(ref _isTwoFactorRequired, value);
                }

                public ICommand LoginCommand { get; }
                public ICommand VerifyTwoFactorCommand { get; }
                public ICommand BackToLoginCommand { get; }

                public LoginViewModel(
                    IUserRepository userRepository,
                    ISecurityService securityService,
                    ILoginAttemptRepository loginAttemptRepository,
                    IDialogService dialogService,
                    INavigationService navigationService,
                    IAuditLogRepository auditLogRepository,
                    ISecurityManagementRepository securityRepository)
                {
                        _userRepository = userRepository;
                        _securityService = securityService;
                        _loginAttemptRepository = loginAttemptRepository;
                        _dialogService = dialogService;
                        _navigationService = navigationService;
                        _auditLogRepository = auditLogRepository;
                        _securityRepository = securityRepository;

                        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
                        VerifyTwoFactorCommand = new RelayCommand(ExecuteVerifyTwoFactor, CanExecuteVerifyTwoFactor);
                        BackToLoginCommand = new RelayCommand(BackToLogin);
                }

                private bool CanExecuteLogin(object parameter)
                {
                        return !string.IsNullOrWhiteSpace(Username) &&
                               !string.IsNullOrWhiteSpace(Password) &&
                               !IsBusy;
                }

                private async void ExecuteLogin(object parameter)
                {
                        try
                        {
                                IsBusy = true;
                                ClearError();

                                string ipAddress = await IPUtility.GetPublicIPAsync();

                                // Get current policy
                                var policy = _securityRepository.GetActivePolicy();

                                // Check if IP is blocked
                                if (_securityRepository.IsIPBlocked(ipAddress))
                                {
                                        await _loginAttemptRepository.RecordAttemptAsync(
                                            Username,
                                            false,
                                            ipAddress,
                                            "WPF Client - Blocked IP");

                                        _auditLogRepository.LogAction(
                                            null,
                                            "Security_BlockedIPAttempt",
                                            $"Login attempt from blocked IP: {ipAddress}",
                                            ipAddress);

                                        SetError("This IP address has been blocked. Please contact your administrator.");
                                        IsBusy = false;
                                        return;
                                }

                                if (policy != null)
                                {
                                        bool isLocked = _loginAttemptRepository.IsUserLockedOut(
                                            Username,
                                            policy.LockoutDurationMinutes,
                                            policy.MaxLoginAttempts);

                                        if (isLocked)
                                        {
                                                var remainingTime = _loginAttemptRepository.GetRemainingLockoutTime(
                                                    Username,
                                                    policy.LockoutDurationMinutes,
                                                    policy.MaxLoginAttempts);

                                                if (remainingTime.HasValue)
                                                {
                                                        await _loginAttemptRepository.RecordAttemptAsync(
                                                            Username,
                                                            false,
                                                            ipAddress,
                                                            "WPF Client - Account Locked");

                                                        _auditLogRepository.LogAction(
                                                            null,
                                                            "Security_LockedAccountAttempt",
                                                            $"Login attempt on locked account: {Username}",
                                                            ipAddress);

                                                        SetError($"Account is locked. Please try again in {Math.Ceiling(remainingTime.Value.TotalMinutes)} minutes.");
                                                        IsBusy = false;
                                                        return;
                                                }
                                        }
                                }

                                _currentUser = _userRepository.GetByUsername(Username);
                                bool isValid = _currentUser != null &&
                                              _securityService.VerifyPassword(Password, _currentUser.PasswordHash);

                                if (!isValid)
                                {
                                        await _loginAttemptRepository.RecordAttemptAsync(
                                            Username,
                                            false,
                                            ipAddress,
                                            "WPF Client");

                                        _auditLogRepository.LogAction(
                                            null,
                                            "Security_FailedLogin",
                                            $"Failed login attempt for username: {Username}",
                                            ipAddress);

                                        if (policy != null)
                                        {
                                                var failureCount = _loginAttemptRepository.GetFailedAttempts(
                                                    Username,
                                                    TimeSpan.FromMinutes(policy.LockoutDurationMinutes));

                                                var remainingAttempts = policy.MaxLoginAttempts - failureCount;

                                                if (remainingAttempts > 0)
                                                        SetError($"Invalid username or password. {remainingAttempts} attempts remaining.");
                                                else
                                                {
                                                        _securityRepository.CreateAlert(new SecurityAlertModel
                                                        {
                                                                AlertType = AlertType.BruteForceAttempt,
                                                                Severity = AlertSeverity.High,
                                                                Description = $"Account locked after {policy.MaxLoginAttempts} failed attempts",
                                                                Source = "Login System",
                                                                IPAddress = ipAddress,
                                                                Username = Username
                                                        });

                                                        SetError($"Account locked. Please try again in {policy.LockoutDurationMinutes} minutes.");
                                                }
                                        }
                                        else
                                        {
                                                SetError("Invalid username or password.");
                                        }

                                        IsBusy = false;
                                        return;
                                }

                                // Clear failed attempts on successful login
                                //_loginAttemptRepository.ClearFailedAttempts(Username);

                                // Handle 2FA if enabled
                                if (_currentUser.TwoFactorEnabled ?? false)
                                {
                                        IsTwoFactorRequired = true;
                                        _auditLogRepository.LogAction(
                                            _currentUser.UserId,
                                            "Security_2FARequired",
                                            $"2FA verification required for user {Username}",
                                            ipAddress);
                                        IsBusy = false;
                                        return;
                                }

                                CompleteLogin(ipAddress);
                        }
                        catch (Exception ex)
                        {
                                SetError("An error occurred during login: " + ex.Message);
                        }
                        finally
                        {
                                IsBusy = false;
                        }
                }

                private void CompleteLogin(string ipAddress)
                {
                        try
                        {
                                _loginAttemptRepository.RecordAttempt(
                                    Username,
                                    true,
                                    ipAddress,
                                    "WPF Client");

                                _auditLogRepository.LogAction(
                                    _currentUser.UserId,
                                    "Security_SuccessfulLogin",
                                    $"Successful login for user {Username}",
                                    ipAddress);

                                _userRepository.SetLastLoginDate(_currentUser.UserId);

                                SessionManager.CurrentUser = new Core.Models.UserModel
                                {
                                        UserId = _currentUser.UserId,
                                        Username = _currentUser.Username,
                                        Email = _currentUser.Email,
                                        Role = _currentUser.Role.RoleName,
                                        IsActive = _currentUser.IsActive ?? false,
                                        LastLoginDate = _currentUser.LastLoginDate,
                                        TwoFactorEnabled = _currentUser.TwoFactorEnabled ?? false
                                };

                                _navigationService.NavigateToMain();
                        }
                        finally
                        {
                                IsBusy = false;
                        }
                }

                private bool CanExecuteVerifyTwoFactor(object parameter)
                {
                        return IsTwoFactorRequired &&
                               !string.IsNullOrWhiteSpace(TwoFactorCode) &&
                               TwoFactorCode.Length == 6 &&
                               !IsBusy;
                }

                private async void ExecuteVerifyTwoFactor(object parameter)
                {
                        try
                        {
                                IsBusy = true;
                                ClearError();

                                string ipAddress = await IPUtility.GetPublicIPAsync();

                                if (_securityService.ValidateTwoFactorCode(_currentUser.TwoFactorSecret, TwoFactorCode))
                                {
                                        CompleteLogin(ipAddress);
                                }
                                else
                                {
                                        _loginAttemptRepository.RecordAttempt(
                                            Username,
                                            false,
                                            ipAddress,
                                            "WPF Client - 2FA Failed");
                                        SetError("Invalid verification code");
                                }
                        }
                        catch (Exception ex)
                        {
                                SetError("An error occurred during verification: " + ex.Message);
                        }
                        finally
                        {
                                IsBusy = false;
                        }
                }

                private void BackToLogin(object parameter)
                {
                        IsTwoFactorRequired = false;
                        TwoFactorCode = string.Empty;
                }
        }
}
