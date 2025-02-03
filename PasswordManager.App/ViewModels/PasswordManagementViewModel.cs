using PasswordManager.App.Views;
using PasswordManager.Core.Models;
using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Core.Services;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using PasswordManager.Data.Repositories;

namespace PasswordManager.App.ViewModels
{
        public class PasswordManagementViewModel : ViewModelBase
        {
                private readonly IStoredPasswordRepository _passwordRepository;
                private readonly IPasswordGroupRepository _groupRepository;
                private readonly ISecurityService _securityService;
                private readonly IEncryptionService _encryptionService;
                private readonly IDialogService _dialogService;
                private readonly IPasswordStrengthService _passwordStrengthService;
                private readonly IAuditLogRepository _auditLogRepository;

                public ObservableCollection<StoredPasswordModel> Passwords { get; private set; }
                public ObservableCollection<PasswordGroupModel> Groups { get; private set; }

                private StoredPasswordModel _selectedPassword;
                private PasswordGroupModel _selectedGroup;
                private string _searchText;
                private bool _showExpiredOnly;

                public StoredPasswordModel SelectedPassword
                {
                        get => _selectedPassword;
                        set
                        {
                                SetProperty(ref _selectedPassword, value);
                                ((RelayCommand)EditPasswordCommand).RaiseCanExecuteChanged();
                                ((RelayCommand)DeletePasswordCommand).RaiseCanExecuteChanged();
                        }
                }

                public PasswordGroupModel SelectedGroup
                {
                        get => _selectedGroup;
                        set
                        {
                                SetProperty(ref _selectedGroup, value);
                                FilterPasswords();
                        }
                }

                public string SearchText
                {
                        get { return _searchText; }
                        set
                        {
                                SetProperty(ref _searchText, value);
                                FilterPasswords();
                        }
                }

                public bool ShowExpiredOnly
                {
                        get { return _showExpiredOnly; }
                        set
                        {
                                SetProperty(ref _showExpiredOnly, value);
                                FilterPasswords();
                        }
                }

                public ICommand AddPasswordCommand { get; }
                public ICommand EditPasswordCommand { get; }
                public ICommand DeletePasswordCommand { get; }
                public ICommand RefreshCommand { get; }
                public ICommand CopyUsernameCommand { get; }
                public ICommand CopyPasswordCommand { get; }
                public ICommand AddGroupCommand { get; }
                public ICommand MoveToGroupCommand { get; }

                public PasswordManagementViewModel(
                        IStoredPasswordRepository passwordRepository,
                        IPasswordGroupRepository groupRepository,
                        ISecurityService securityService,
                        IEncryptionService encryptionService,
                        IDialogService dialogService,
                        IPasswordStrengthService passwordStrengthService,
                        IAuditLogRepository auditLogRepository)
                {
                        _passwordRepository = passwordRepository;
                        _groupRepository = groupRepository;
                        _securityService = securityService;
                        _encryptionService = encryptionService;
                        _dialogService = dialogService;
                        _passwordStrengthService = passwordStrengthService;
                        _auditLogRepository = auditLogRepository;

                        Passwords = new ObservableCollection<StoredPasswordModel>();
                        Groups = new ObservableCollection<PasswordGroupModel>();

                        AddPasswordCommand = new RelayCommand(_ => ExecuteAddPassword());
                        EditPasswordCommand = new RelayCommand(_ => ExecuteEditPassword(), _ => CanExecutePasswordAction());
                        DeletePasswordCommand = new RelayCommand(_ => ExecuteDeletePassword(), _ => CanExecutePasswordAction());
                        RefreshCommand = new RelayCommand(_ => LoadData());
                        CopyUsernameCommand = new RelayCommand(ExecuteCopyUsername);
                        CopyPasswordCommand = new RelayCommand(ExecuteCopyPassword);
                        AddGroupCommand = new RelayCommand(_ => ExecuteAddGroup());
                        MoveToGroupCommand = new RelayCommand(ExecuteMoveToGroup, _ => CanExecutePasswordAction());

                        LoadData();
                }

                private void LoadData()
                {
                        LoadGroups();
                        LoadPasswords();
                }

                private void LoadGroups()
                {
                        try
                        {
                                var groups = _groupRepository.GetByUserId(SessionManager.CurrentUser.UserId);
                                Groups.Clear();
                                Groups.Add(new PasswordGroupModel { GroupId = -1, GroupName = "All Passwords" });
                                foreach (var group in groups)
                                {
                                        Groups.Add(group);
                                }

                                if (SelectedGroup == null)
                                {
                                        SelectedGroup = Groups.First(); // "All Passwords"
                                }
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load groups: " + ex.Message);
                        }
                }

                private void LoadPasswords()
                {
                        try
                        {
                                var passwords = _passwordRepository.GetByUserId(SessionManager.CurrentUser.UserId);
                                Passwords.Clear();
                                foreach (var password in passwords)
                                {
                                        Passwords.Add(password);
                                }

                                FilterPasswords();
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to load passwords: " + ex.Message);
                        }
                }

                private void FilterPasswords()
                {
                        var filtered = _passwordRepository.GetByUserId(SessionManager.CurrentUser.UserId);

                        // Filter by group
                        if (SelectedGroup != null)
                        {
                                if (SelectedGroup.GroupId == -1) // "All Passwords"
                                {
                                        // No filtering
                                }
                                else if (SelectedGroup.GroupId == 0) // "Ungrouped"
                                {
                                        filtered = filtered.Where(p => !p.GroupId.HasValue);
                                }
                                else
                                {
                                        filtered = filtered.Where(p => p.GroupId == SelectedGroup.GroupId);
                                }
                        }

                        // Filter by search text
                        if (!string.IsNullOrWhiteSpace(SearchText))
                        {
                                string search = SearchText.ToLower();
                                filtered = filtered.Where(p =>
                                    p.SiteName.ToLower().Contains(search) ||
                                    p.Username.ToLower().Contains(search) ||
                                    p.SiteUrl?.ToLower().Contains(search) == true);
                        }

                        // Filter expired passwords
                        if (ShowExpiredOnly)
                        {
                                filtered = filtered.Where(p => p.IsExpired);
                        }

                        Passwords.Clear();
                        foreach (var password in filtered)
                        {
                                Passwords.Add(password);
                        }
                }

                private void ExecuteAddGroup()
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

                                LoadGroups();
                                _dialogService.ShowMessage("Group created successfully.");
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to create group: " + ex.Message);
                        }
                }

                private void ExecuteMoveToGroup(object parameter)
                {
                        if (SelectedPassword == null) return;

                        // Create items for combobox
                        var items = Groups.Where(g => g.GroupId > 0)  // Exclude "All" and "Ungrouped"
                                         .Select(g => g.GroupName)
                                         .ToList();
                        items.Insert(0, "Ungrouped");

                        int currentGroupIndex = SelectedPassword.GroupId.HasValue ?
                            items.FindIndex(g => g == SelectedPassword.GroupName) :
                            0;  // "Ungrouped"

                        var dialog = new GroupSelectionWindow(items, currentGroupIndex);
                        if (dialog.ShowDialog() == true)
                        {
                                try
                                {
                                        int? newGroupId;
                                        if (dialog.SelectedIndex == 0)
                                        {
                                                newGroupId = null;
                                        }
                                        else
                                        {
                                                newGroupId = Groups.First(g => g.GroupName == items[dialog.SelectedIndex]).GroupId;
                                        }

                                        _passwordRepository.UpdateGroup(SelectedPassword.Id, newGroupId);
                                        _auditLogRepository.LogAction(
                                            SessionManager.CurrentUser.UserId,
                                            "Password_GroupChanged",
                                            $"Moved password {SelectedPassword.SiteName} to group: {(newGroupId.HasValue ? items[dialog.SelectedIndex] : "Ungrouped")}",
                                            "localhost");

                                        LoadPasswords();
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to move password: " + ex.Message);
                                }
                        }
                }

                private bool CanExecutePasswordAction()
                {
                        return SelectedPassword != null;
                }

                private void ExecuteAddPassword()
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
                                Owner = Application.Current.MainWindow,
                                DataContext = viewModel
                        };

                        viewModel.RequestClose += (s, e) =>
                        {
                                window.DialogResult = true;
                                window.Close();
                                LoadPasswords();
                        };

                        window.ShowDialog();
                }

                private void ExecuteEditPassword()
                {
                        try
                        {
                                // Debug information
                                System.Diagnostics.Debug.WriteLine($"SelectedPassword: {SelectedPassword}");
                                System.Diagnostics.Debug.WriteLine($"SelectedPassword Id: {SelectedPassword?.Id}");
                                System.Diagnostics.Debug.WriteLine($"SelectedPassword SiteName: {SelectedPassword?.SiteName}");

                                var viewModel = new PasswordEntryViewModel(
                                    _passwordRepository,
                                    _groupRepository,
                                    _securityService,
                                    _encryptionService,
                                    _dialogService,
                                    _passwordStrengthService,
                                    _auditLogRepository,
                                    SelectedPassword
                                );

                                var window = new PasswordEntryWindow
                                {
                                        Owner = Application.Current.MainWindow,
                                        DataContext = viewModel
                                };

                                viewModel.RequestClose += (s, e) =>
                                {
                                        window.DialogResult = true;
                                        window.Close();
                                        LoadPasswords();
                                };

                                window.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to edit password: " + ex.Message);
                                // Additional logging
                                System.Diagnostics.Debug.WriteLine($"Full Exception: {ex}");
                        }
                }

                private void ExecuteDeletePassword()
                {
                        if (_dialogService.ShowConfirmation(
                            $"Are you sure you want to delete the password for {SelectedPassword.SiteName}?"))
                        {
                                try
                                {
                                        _passwordRepository.Delete(SelectedPassword.Id);
                                        _auditLogRepository.LogAction(
                                            SessionManager.CurrentUser.UserId,
                                            "User_PasswordDeleted",
                                            $"Deleted stored password for site: {SelectedPassword.SiteName}",
                                            "localhost");
                                        LoadPasswords();
                                        _dialogService.ShowMessage("Password deleted successfully.");
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to delete password: " + ex.Message);
                                }
                        }
                }

                private void ExecuteCopyUsername(object parameter)
                {
                        var password = parameter as StoredPasswordModel;
                        if (password != null)
                        {
                                try
                                {
                                        Clipboard.SetText(password.Username);
                                        _dialogService.ShowMessage("Username copied to clipboard!");

                                        // Clear clipboard after 30 seconds
                                        Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t =>
                                        {
                                                Application.Current.Dispatcher.Invoke(() =>
                                                {
                                                        if (Clipboard.GetText() == password.Username)
                                                        {
                                                                Clipboard.Clear();
                                                        }
                                                });
                                        });
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to copy username: " + ex.Message);
                                }
                        }
                }

                private void ExecuteCopyPassword(object parameter)
                {
                        var password = parameter as StoredPasswordModel;
                        if (password != null)
                        {
                                try
                                {
                                        string decryptedPassword = _encryptionService.Decrypt(password.EncryptedPassword);
                                        Clipboard.SetText(decryptedPassword);
                                        _auditLogRepository.LogAction(
                                            SessionManager.CurrentUser.UserId,
                                            "User_PasswordCopied",
                                            $"Password copied for site: {password.SiteName}",
                                            "localhost");
                                        _dialogService.ShowMessage("Password copied to clipboard!");

                                        // Clear clipboard after 30 seconds
                                        Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t =>
                                        {
                                                Application.Current.Dispatcher.Invoke(() =>
                                                {
                                                        if (Clipboard.GetText() == decryptedPassword)
                                                        {
                                                                Clipboard.Clear();
                                                        }
                                                });
                                        });
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to copy password: " + ex.Message);
                                }
                        }
                }
        }
}
