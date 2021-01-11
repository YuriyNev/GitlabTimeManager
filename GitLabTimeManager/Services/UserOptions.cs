using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GitLabTimeManager.Types;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public class ProfileService : IProfileService
    {
        private const string Location = "profile.json";

        public IUserProfile Deserialize()
        {
            using var stream = new StreamReader(Location);
            var json = stream.ReadToEnd();
            var profile = JsonSerializer.Deserialize<UserProfile>(json);

            if (!Verify(profile))
                throw new IncorrectProfileException();
            
            return profile!;
        }

        public void Serialize(IUserProfile profile)
        {
            using var stream = new StreamWriter(Location);
            var json = JsonSerializer.Serialize<IUserProfile>(profile);
            stream.Write(json);
        }
        
        private static bool Verify(IUserProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Token))
                return false;
            
            if (string.IsNullOrEmpty(profile.Url))
                return false;
            
            if (profile.Projects?.Count == 0)
                return false;

            return true;
        }
    }

    public interface IProfileService
    {
        [NotNull] IUserProfile Deserialize();

        [NotNull] void Serialize(IUserProfile profile);
    }

    public interface IUserProfile
    {
        public string Token { get; set; }

        public string Url { get; set; }

        public IReadOnlyList<int> Projects { get; set; }
    }

    public class UserProfile : IUserProfile
    {
        public string Token { get; set; }

        public string Url { get; set; }

        public IReadOnlyList<int> Projects { get; set; }

        public UserProfile()
        {
            
        }
        
        public UserProfile([NotNull] IProfileService profileService)
        {
            var userProfile = profileService.Deserialize();

            Url = userProfile.Url;
            Token = userProfile.Token;
            Projects = userProfile.Projects;
        }
    }
}