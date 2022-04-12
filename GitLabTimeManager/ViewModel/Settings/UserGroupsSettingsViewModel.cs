using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Catel.Collections;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.ViewModel.Settings;

using MahApps.Metro.Controls.Dialogs;

namespace GitLabTimeManager.ViewModel;

public class UserGroupsSettingsViewModel : SettingsViewModelBase
{
    public static readonly PropertyData AllUsersProperty = RegisterProperty<UserGroupsSettingsViewModel, IReadOnlyList<string>>(x => x.AllUsers);
    public static readonly PropertyData GroupsProperty = RegisterProperty<UserGroupsSettingsViewModel, FastObservableDictionary<string, IList<string>>>(x => x.Groups);
    public static readonly PropertyData GroupsListProperty = RegisterProperty<UserGroupsSettingsViewModel, IReadOnlyList<string>>(x => x.GroupsList);
    public static readonly PropertyData SelectedGroupProperty = RegisterProperty<UserGroupsSettingsViewModel, string>(x => x.SelectedGroup);
    public static readonly PropertyData UsersProperty = RegisterProperty<UserGroupsSettingsViewModel, IReadOnlyList<string>>(x => x.Users);
    public static readonly PropertyData SelectedUserFromAvailableProperty = RegisterProperty<UserGroupsSettingsViewModel, string>(x => x.SelectedUserFromAvailable);
    public static readonly PropertyData SelectedUserProperty = RegisterProperty<UserGroupsSettingsViewModel, string>(x => x.SelectedUser);

    public string SelectedUser
    {
        get => GetValue<string>(SelectedUserProperty);
        set => SetValue(SelectedUserProperty, value);
    }

    public string SelectedUserFromAvailable
    {
        get => GetValue<string>(SelectedUserFromAvailableProperty);
        set => SetValue(SelectedUserFromAvailableProperty, value);
    }

    public IReadOnlyList<string> Users
    {
        get => GetValue<IReadOnlyList<string>>(UsersProperty);
        set => SetValue(UsersProperty, value);
    }

    public string SelectedGroup
    {
        get => GetValue<string>(SelectedGroupProperty);
        set => SetValue(SelectedGroupProperty, value);
    }

    public IReadOnlyList<string> GroupsList
    {
        get => GetValue<IReadOnlyList<string>>(GroupsListProperty);
        set => SetValue(GroupsListProperty, value);
    }

    public FastObservableDictionary<string, IList<string>> Groups
    {
        get => GetValue<FastObservableDictionary<string, IList<string>>>(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    public IReadOnlyList<string> AllUsers
    {
        get => GetValue<IReadOnlyList<string>>(AllUsersProperty);
        set => SetValue(AllUsersProperty, value);
    }

    public ICommand AddGroupCommand { get; }
    public ICommand RemoveGroupCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand RemoveUserCommand { get; }

    private static IDialogCoordinator Coordinator => DialogCoordinator.Instance;

    public UserGroupsSettingsViewModel(
        UsersArgument argument,
        IProfileService profileService,
        IUserProfile userProfile,
        INotificationMessageService messageService) : base(profileService, userProfile, messageService)
    {
        AllUsers = argument.Users;

        AddGroupCommand = new Command(AddGroup);
        RemoveGroupCommand = new Command(RemoveGroup);
        AddUserCommand = new Command(AddUser);
        RemoveUserCommand = new Command(RemoveUser);

        SelectedUserFromAvailable = AllUsers.FirstOrDefault();
        ApplyOptions(userProfile);
    }

    private void RemoveGroup()
    {
        Groups.Remove(SelectedGroup);
        UpdateGroupsList();
    }

    private void RemoveUser()
    {
        if (Groups.TryGetValue(SelectedGroup, out var users))
        {
            if (users.Contains(SelectedUser))
            {
                users.Remove(SelectedUser);
                Users = new List<string>(users);
            }
        }
    }

    private void AddUser()
    {
        if (Groups.TryGetValue(SelectedGroup, out var users))
        {
            if (!users.Contains(SelectedUserFromAvailable))
            {
                users.Add(SelectedUserFromAvailable);
                Users = new List<string>(users);
            }
        }
    }

    private async void AddGroup()
    {
        var newGroup = await ShowInputDialogAsync(this, "Добавление группы", "Добавить новую группу?", string.Empty);
        Groups.Add(newGroup, new List<string>());
        UpdateGroupsList();
    }

    public override Action<IUserProfile> SaveAction =>
        profile =>
        {
            profile.UserGroups = Groups.ToDictionary(x => x.Key, x => x.Value);
        };

    protected sealed override void ApplyOptions(IUserProfile userProfile)
    {
        Groups = new FastObservableDictionary<string, IList<string>>(userProfile.UserGroups);
        UpdateGroupsList();
        SelectedGroup = Groups.Select(x => x.Key).FirstOrDefault();
    }

    private void UpdateGroupsList()
    {
        GroupsList = new List<string>(Groups.Keys);
    }

    protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedGroup))
        {
            if (Groups.TryGetValue(SelectedGroup, out var users))
            {
                Users = new List<string>(users);
            }
        }
    }

    public Task<string> ShowInputDialogAsync(object viewModel, string title, string message, string oldValue)
    {
        return Coordinator.ShowInputAsync(viewModel, title, message,
            new MetroDialogSettings
            {
                ColorScheme = MetroDialogColorScheme.Theme,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
                AffirmativeButtonText = "Применить",
                NegativeButtonText = "Отмена",
                AnimateShow = true,
                DefaultText = oldValue,
            });
    }
}

public class UsersArgument
{
    public IReadOnlyList<string> Users { get; }

    public UsersArgument(IReadOnlyList<string> users)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
    }
}