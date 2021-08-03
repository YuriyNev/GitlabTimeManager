using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitLabTimeManager.Types;
using JetBrains.Annotations;

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

            //if (!Verify(profile))
            //    throw new IncorrectProfileException();

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

        private static bool Verify(IUserProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Token))
                return false;
            
            if (string.IsNullOrEmpty(profile.Url))
                return false;
            
            return true;
        }
    }

    public interface IProfileService
    {
        [NotNull] IUserProfile Deserialize();

        [NotNull] void Serialize(IUserProfile profile);

        event EventHandler<IUserProfile> Serialized;
    }

    public interface IUserProfile
    {
        string Token { get; set; }

        string Url { get; set; }

        int RequestMonths { get; set; }

        LabelSettings LabelSettings { get; set; }
    }

    public class LabelSettings
    {
        public BoardStateLabels BoardStateLabels { get; set; }
        
        public IReadOnlyList<string> OtherBoardLabels { get; set; }

        [JsonIgnore]
        public IReadOnlyList<string> AllBoardLabels { get; set; }

        public IReadOnlyList<string> ExcludeLabels { get; set; }
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

#if DEBUG
        public int RequestMonths { get; set; } = 1;
#else
        public int RequestMonths { get; set; } = 4;
#endif

        public LabelSettings LabelSettings { get; set; } = new LabelSettings
        {
            BoardStateLabels = new BoardStateLabels(),
            OtherBoardLabels = Array.Empty<string>(),
            ExcludeLabels = Array.Empty<string>(),
        };

        /// <summary> Constructor for Json Deserializer</summary>
        public UserProfile()
        {
        }

        public UserProfile([NotNull] IProfileService profileService)
        {
            var userProfile = profileService.Deserialize();

            Url = userProfile.Url;
            Token = userProfile.Token;
            RequestMonths = userProfile.RequestMonths;
            LabelSettings = userProfile.LabelSettings;

            LabelSettings.AllBoardLabels = new List<string>(LabelSettings.OtherBoardLabels)
            {
                LabelSettings.BoardStateLabels.ToDoLabel, 
                LabelSettings.BoardStateLabels.DoingLabel, 
                LabelSettings.BoardStateLabels.DoneLabel, 
            };
        }
    }
}