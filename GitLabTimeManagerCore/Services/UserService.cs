using GitLabApiClient.Models.Projects.Responses;
using GitLabApiClient.Models.Users.Responses;
using GitLabTimeManager.Services;

namespace GitLabTimeManagerCore.Services
{
    public class UserService : IUserService
    {
        private IUserProfile UserProfile { get; }
        private ISourceControl SourceControl { get; }

        public UserService(IUserProfile userProfile, ISourceControl sourceControl)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
        }

        public List<User> GetRealUsers(IReadOnlyList<User> allUsers, User user)
        {
            return user.IsGroup()
                ? ExtractUsersFromGroup(allUsers, UserProfile.UserGroups, user.Name)
                : new List<User> { user };
        }

        private List<User> ExtractUsersFromGroup(IReadOnlyList<User> allUsers, Dictionary<string, IList<string>> userProfileUserGroups, string group)
        {
            if (userProfileUserGroups == null) throw new ArgumentNullException(nameof(userProfileUserGroups));
            if (group == null) throw new ArgumentNullException(nameof(group));

            List<User> users;
            if (userProfileUserGroups.TryGetValue(group, out var u))
            {
                users = allUsers
                    .Where(x => u.Contains(x.Name))
                    .ToList();
            }
            else
            {
                users = Array.Empty<User>().ToList();
            }

            return users;
        }

        public async Task<IReadOnlyList<Label>> FetchGroupLabelsAsync(CancellationToken cancellationToken)
        {
            var labels = await SourceControl.FetchGroupLabelsAsync();
            return labels
                .Select(groupLabel => groupLabel.ConvertToLabel())
                .ToList();
        }

        public async Task<IReadOnlyList<User>> FetchUsersAsync(CancellationToken cancellationToken)
        {
            var users = await SourceControl.FetchAllUsersAsync().ConfigureAwait(true);
            users = users
                .OrderBy(x => x.Name)
                .ToList();

            int position = 0;
            foreach (var group in UserProfile.UserGroups)
            {
                users.Insert(position, UserGroupEx.CreateUserAsGroup(@group.Key));
                position++;
            }

            return users.ToList();
        }
    }

    public interface IUserService
    {
        List<User> GetRealUsers(IReadOnlyList<User> allUsers, User user);
        Task<IReadOnlyList<Label>> FetchGroupLabelsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<User>> FetchUsersAsync(CancellationToken cancellationToken);
    }

    public static class UserGroupEx
    {
        private static string GroupMarker => "group";

        public static bool IsGroup(this User user) => user.Organization == GroupMarker;

        public static User CreateUserAsGroup(string name) => new() { Name = name, Organization = GroupMarker };
    }
}