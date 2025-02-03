using PasswordManager.Core.Models;
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

namespace PasswordManager.App.ViewModels
{
        public class PasswordGroupManagementViewModel : ViewModelBase
        {
                private readonly IPasswordGroupRepository _groupRepository;
                private readonly IStoredPasswordRepository _passwordRepository;
                private readonly IDialogService _dialogService;
                private readonly IAuditLogRepository _auditLogRepository;

                public ObservableCollection<PasswordGroupModel> Groups { get; private set; }
                public ObservableCollection<StoredPasswordModel> GroupPasswords { get; private set; }

                private PasswordGroupModel _selectedGroup;
                public PasswordGroupModel SelectedGroup
                {
                        get => _selectedGroup;
                        set
                        {
                                if (SetProperty(ref _selectedGroup, value))
                                {
                                        LoadGroupPasswords();
                                        ((RelayCommand)EditGroupCommand).RaiseCanExecuteChanged();
                                        ((RelayCommand)DeleteGroupCommand).RaiseCanExecuteChanged();
                                }
                        }
                }

                public ICommand AddGroupCommand { get; private set; }
                public ICommand EditGroupCommand { get; private set; }
                public ICommand DeleteGroupCommand { get; private set; }
                public ICommand RefreshCommand { get; private set; }

                public PasswordGroupManagementViewModel(
                    IPasswordGroupRepository groupRepository,
                    IStoredPasswordRepository passwordRepository,
                    IDialogService dialogService,
                    IAuditLogRepository auditLogRepository)
                {
                        _groupRepository = groupRepository;
                        _passwordRepository = passwordRepository;
                        _dialogService = dialogService;
                        _auditLogRepository = auditLogRepository;

                        Groups = new ObservableCollection<PasswordGroupModel>();
                        GroupPasswords = new ObservableCollection<StoredPasswordModel>();

                        AddGroupCommand = new RelayCommand(_ => ExecuteAddGroup());
                        EditGroupCommand = new RelayCommand(_ => ExecuteEditGroup(), _ => CanExecuteGroupAction());
                        DeleteGroupCommand = new RelayCommand(_ => ExecuteDeleteGroup(), _ => CanExecuteGroupAction());
                        RefreshCommand = new RelayCommand(_ => LoadGroups());

                        LoadGroups();
                }

                private void LoadGroups()
                {
                        var groups = _groupRepository.GetByUserId(SessionManager.CurrentUser.UserId);
                        Groups.Clear();
                        foreach (var group in groups)
                        {
                                Groups.Add(group);
                        }
                }

                private void LoadGroupPasswords()
                {
                        GroupPasswords.Clear();
                        if (SelectedGroup != null)
                        {
                                var passwords = _passwordRepository.GetByGroup(SelectedGroup.GroupId);
                                foreach (var password in passwords)
                                {
                                        GroupPasswords.Add(password);
                                }
                        }
                }

                private bool CanExecuteGroupAction()
                {
                        return SelectedGroup != null;
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

                        var group = new PasswordGroupModel
                        {
                                UserId = SessionManager.CurrentUser.UserId,
                                GroupName = groupName,
                                Description = description
                        };

                        try
                        {
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

                private void ExecuteEditGroup()
                {
                        string groupName = _dialogService.ShowPrompt("Enter new group name:", "Edit Group");
                        if (string.IsNullOrWhiteSpace(groupName)) return;

                        if (_groupRepository.IsGroupNameTaken(SessionManager.CurrentUser.UserId, groupName) &&
                            groupName.ToLower() != SelectedGroup.GroupName.ToLower())
                        {
                                _dialogService.ShowError("A group with this name already exists.");
                                return;
                        }

                        string description = _dialogService.ShowPrompt("Enter new description (optional):", "Edit Group");

                        try
                        {
                                SelectedGroup.GroupName = groupName;
                                SelectedGroup.Description = description;
                                _groupRepository.Update(SelectedGroup);

                                _auditLogRepository.LogAction(
                                    SessionManager.CurrentUser.UserId,
                                    "Group_Updated",
                                    $"Updated password group: {groupName}",
                                    "localhost");

                                LoadGroups();
                                _dialogService.ShowMessage("Group updated successfully.");
                        }
                        catch (Exception ex)
                        {
                                _dialogService.ShowError("Failed to update group: " + ex.Message);
                        }
                }

                private void ExecuteDeleteGroup()
                {
                        int passwordCount = _groupRepository.GetPasswordCount(SelectedGroup.GroupId);
                        string message = passwordCount > 0
                            ? $"This group contains {passwordCount} passwords. The passwords will be ungrouped but not deleted. Continue?"
                            : "Are you sure you want to delete this group?";

                        if (_dialogService.ShowConfirmation(message))
                        {
                                try
                                {
                                        _groupRepository.Delete(SelectedGroup.GroupId);
                                        _auditLogRepository.LogAction(
                                            SessionManager.CurrentUser.UserId,
                                            "Group_Deleted",
                                            $"Deleted password group: {SelectedGroup.GroupName}",
                                            "localhost");

                                        LoadGroups();
                                        _dialogService.ShowMessage("Group deleted successfully.");
                                }
                                catch (Exception ex)
                                {
                                        _dialogService.ShowError("Failed to delete group: " + ex.Message);
                                }
                        }
                }
        }
}
