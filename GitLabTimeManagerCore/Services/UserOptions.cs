using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitLabTimeManager.Types;

namespace GitLabTimeManager.Services
{
    public class ProfileService : IProfileService
    {
        private const string Location = "profile.json";

        public IUserProfile Deserialize()
        {
            var profile = new UserProfile();
            try
            {
                using var stream = new StreamReader(Location);
                var json = stream.ReadToEnd();
                profile = JsonSerializer.Deserialize<UserProfile>(json);
            }
            catch (FileNotFoundException)
            {
                Serialize(profile);
            }
            catch
            {
                throw new IncorrectProfileException();
            }

            return profile;
        }

        public void Serialize(IUserProfile profile)
        {
            using var stream = new StreamWriter(Location);
            var json = JsonSerializer.Serialize(profile);
            stream.Write(json);

            Serialized?.Invoke(this, profile);
        }

        public event EventHandler<IUserProfile> Serialized;
    }

    public interface IProfileService
    {
        IUserProfile Deserialize();

        void Serialize(IUserProfile profile);

        event EventHandler<IUserProfile> Serialized;
    }

    public interface IUserProfile
    {
        string Token { get; set; }

        string Url { get; set; }

        LabelSettings LabelSettings { get; set; }

        Dictionary<string, IList<string>> UserGroups { get; set; }
    }

    public class LabelSettings
    {
        public BoardStateLabels BoardStateLabels { get; set; }
        
        public IReadOnlyList<string> OtherBoardLabels { get; set; }

        [JsonIgnore]
        public IReadOnlyList<string> AllBoardLabels { get; set; }

        public IReadOnlyList<string> ExcludeLabels { get; set; }

        public IReadOnlyList<string> PassedLabels { get; set; }
    }

    public class BoardStateLabels
    {
        public string ToDoLabel { get; set; }

        public string DoingLabel { get; set; }

        public string DoneLabel { get; set; }
    }

    public class UserProfile : IUserProfile
    {
        public string Token { get; set; }

        public string Url { get; set; }

        public Dictionary<string, IList<string>> UserGroups { get; set; }
        
        public LabelSettings LabelSettings { get; set; } = new()
        {
            BoardStateLabels = new BoardStateLabels(),
            OtherBoardLabels = Array.Empty<string>(),
            ExcludeLabels = Array.Empty<string>(),
            PassedLabels = Array.Empty<string>(),
        };

        /// <summary> Constructor for Json Deserializer</summary>
        public UserProfile()
        {
        }

        public UserProfile(IProfileService profileService)
        {
            var userProfile = profileService.Deserialize();

            Url = userProfile.Url;
            Token = userProfile.Token;
            LabelSettings = userProfile.LabelSettings;
            LabelSettings.PassedLabels ??= Array.Empty<string>();

            LabelSettings.AllBoardLabels = new List<string>(LabelSettings.OtherBoardLabels)
            {
                LabelSettings.BoardStateLabels.ToDoLabel, 
                LabelSettings.BoardStateLabels.DoingLabel, 
                LabelSettings.BoardStateLabels.DoneLabel, 
            };

            UserGroups = userProfile.UserGroups ?? new Dictionary<string, IList<string>>();
        }
    }
}