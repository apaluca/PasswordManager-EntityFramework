using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Core.Services;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordManager.Data.Repositories;

namespace PasswordManager.App.ViewModels
{
        public class ChangePasswordViewModel : ViewModelBase
        {
                private readonly IUserRepository _userRepository;
                private readonly ISecurityService _securityService;
                private readonly IPasswordStrengthService _passwordStrengthService;
                private readonly IDialogService _dialogService;
                private readonly IAuditLogRepository _auditLogRepository;
                private readonly ISecurityManagementRepository _securityRepository;

                private string _currentPassword;
                private string _newPassword;
                private string _confirmPassword;
                private PasswordStrength _passwordStrength;

                public string CurrentPassword
                {
                        get => _currentPassword;
                        set => SetProperty(ref _currentPassword, value);
                }

                public string NewPassword
                {
                        get => _newPassword;
                        set
                        {
                                if (SetProperty(ref _newPassword, value))
                                {
                                        _passwordStrength = _passwordStrengthService.CheckStrength(value);
                                        OnPropertyChanged(nameof(PasswordStrength));
                                        OnPropertyChanged(nameof(StrengthDescription));
                                        OnPropertyChanged(nameof(StrengthColor));
                                        ((RelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
                                }
                        }
                }

                public string ConfirmPassword
                {
                        get => _confirmPassword;
                        set
                        {
                                if (SetProperty(ref _confirmPassword, value))
                                {
                                        ((RelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
                                }
                        }
                }

                public PasswordStrength PasswordStrength => _passwordStrength;
                public string StrengthDescription => _passwordStrengthService.GetStrengthDescription(_passwordStrength);
                public string StrengthColor => _passwordStrengthService.GetStrengthColor(_passwordStrength);

                public ICommand ChangePasswordCommand { get; }
                public ICommand CancelCommand { get; }
                public ICommand GeneratePasswordCommand { get; }

                public event EventHandler RequestClose;

                public ChangePasswordViewModel(
                    IUserRepository userRepository,
                    ISecurityService securityService,
                    IPasswordStrengthService passwordStrengthService,
                    IDialogService dialogService,
                    IAuditLogRepository auditLogRepository,
                    ISecurityManagementRepository securityRepository)
                {
                        _userRepository = userRepository;
                        _securityService = securityService;
                        _passwordStrengthService = passwordStrengthService;
                        _dialogService = dialogService;
                        _auditLogRepository = auditLogRepository;
                        _securityRepository = securityRepository;

                        ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
                        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
                        GeneratePasswordCommand = new RelayCommand(_ => ExecuteGeneratePassword());
                }

                private bool ValidatePasswordAgainstPolicy(string password)
                {
                        var policy = _securityRepository.GetActivePolicy();
                        if (policy == null) return true; // If no policy exists, allow any password

                        if (password.Length < policy.MinLength)
                        {
                                SetError($"Password must be at least {policy.MinLength} characters long.");
                                return false;
                        }

                        if (policy.RequireUppercase && !password.Any(char.IsUpper))
                        {
                                SetError("Password must contain at least one uppercase letter.");
                                return false;
                        }

                        if (policy.RequireLowercase && !password.Any(char.IsLower))
                        {
                                SetError("Password must contain at least one lowercase letter.");
                                return false;
                        }

                        if (policy.RequireNumbers && !password.Any(char.IsDigit))
                        {
                                SetError("Password must contain at least one number.");
                                return false;
                        }

                        if (policy.RequireSpecialChars && !password.Any(c => !char.IsLetterOrDigit(c)))
                        {
                                SetError("Password must contain at least one special character.");
                                return false;
                        }

                        return true;
                }

                private bool CanExecuteChangePassword(object parameter)
                {
                        return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                               !string.IsNullOrWhiteSpace(NewPassword) &&
                               !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                               NewPassword == ConfirmPassword &&
                               _passwordStrength >= PasswordStrength.Medium;
                }

                private void ExecuteChangePassword(object parameter)
                {
                        try
                        {
                                // Verify current password
                                if (!_userRepository.ValidateUser(
                                    SessionManager.CurrentUser.Username,
                                    CurrentPassword))
                                {
                                        _auditLogRepository.LogAction(
                                            SessionManager.CurrentUser.UserId,
                                            "Security_FailedPasswordChange",
                                            "Failed password change attempt - incorrect current password",
                                            "localhost");
                                        SetError("Current password is incorrect");
                                        return;
                                }

                                // Validate against password policy
                                if (!ValidatePasswordAgainstPolicy(NewPassword))
                                {
                                        return; // Error message already set by ValidatePasswordAgainstPolicy
                                }

                                // Change password
                                _userRepository.ChangePassword(
                                    SessionManager.CurrentUser.UserId,
                                    NewPassword);

                                _auditLogRepository.LogAction(
                                    SessionManager.CurrentUser.UserId,
                                    "User_PasswordChanged",
                                    "User successfully changed their password",
                                    "localhost");

                                _dialogService.ShowMessage("Password changed successfully!");
                                RequestClose?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                                SetError("Failed to change password: " + ex.Message);
                        }
                }

                private void ExecuteGeneratePassword()
                {
                        NewPassword = _securityService.GenerateStrongPassword();
                        ConfirmPassword = NewPassword;
                }
        }
}
