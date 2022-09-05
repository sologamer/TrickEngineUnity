using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// A login service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class TrickLoginService<T> : TrickLoginService where T : ITrickLoginUserData
    {
        /// <summary>
        /// True if the user is authenticated
        /// </summary>
        public bool Authenticated { get; set; }
        /// <summary>
        /// The current user data
        /// </summary>
        public T CurrentUser { get; private set; }
        
        /// <summary>
        /// The current login platform id the user is logged in to. Can be null
        /// </summary>
        public string CurrentLoginPlatformId { get; private set; }

        /// <summary>
        /// Event to call when the user logged logs in
        /// </summary>
        public event Action<(LoginStatus LoginStatus, T UserData)> OnUserLogin;
        
        /// <summary>
        /// Event when the user logs out
        /// </summary>
        public event Action<bool> OnUserLogout; 

        private Task<Task> _logoutTask;
        private Task<Task> _loginTask;

        private Dictionary<string, ITrickLoginPlatform<T>> LoginPlatforms { get; } = new Dictionary<string, ITrickLoginPlatform<T>>();

        /// <summary>
        /// Authenticate to a certain platform by it's id. Must be a registered platform.
        /// </summary>
        /// <param name="platformId">The unique platform id to login to. For example Guest, Facebook, Apple</param>
        /// <param name="userInput">User input which contains token/guid or anything to authenticate the user</param>
        /// <param name="userLoginCallback">Callback of the login</param>
        public void AuthenticateByPlatform(string platformId, string[] userInput, Action<(LoginStatus LoginStatus, T UserData)> userLoginCallback = null)
        {
            if (platformId != null && LoginPlatforms.TryGetValue(platformId, out var platform))
            {
                if (_loginTask == null || _loginTask.IsCompleted)
                {
                    UnityTrickTask.StartNewTask(async () =>
                    {
                        var tuple = await platform.Authenticate(userInput);
                        if (tuple.LoginStatus == LoginStatus.Success)
                        {
                            Authenticated = true;
                            CurrentLoginPlatformId = platformId;
                            CurrentUser = tuple.UserData;
                        }
                        else
                        {
                            Authenticated = false;
                            CurrentLoginPlatformId = default;
                            CurrentUser = default;
                        }

                        OnUserLogin?.Invoke(tuple);
                        userLoginCallback?.Invoke(tuple);
                    });
                    return;
                }
            }
            else
            {
                Debug.LogWarning($"[TrickLoginService] Login platform '{platformId}' not registered!");
            }
            
            OnUserLogin?.Invoke((LoginStatus.Failed, default));
            userLoginCallback?.Invoke((LoginStatus.Failed, default));
        }


        public void Logout(string[] userInput, Action<bool> userLogoutCallback = null)
        {
            if (Authenticated)
            {
                if (CurrentLoginPlatformId != null && LoginPlatforms.TryGetValue(CurrentLoginPlatformId, out var platform))
                {
                    if (_logoutTask == null || _logoutTask.IsCompleted)
                    {
                        _logoutTask = UnityTrickTask.StartNewTask(async () =>
                        {
                            var loggedOut = await platform.Logout(userInput);
                            if (loggedOut)
                            {
                                Authenticated = false;
                                CurrentLoginPlatformId = default;
                                CurrentUser = default;
                            }

                            OnUserLogout?.Invoke(loggedOut);
                            userLogoutCallback?.Invoke(loggedOut);
                        });
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"[TrickLoginService] Login platform '{CurrentLoginPlatformId}' not registered!");
                }
            }
            
            OnUserLogout?.Invoke(false);
            userLogoutCallback?.Invoke(false);
        }

        public void RegisterLoginPlatform(string platformId, ITrickLoginPlatform<T> platform)
        {
            if (platformId != null) LoginPlatforms[platformId] = platform;
            else Debug.LogWarning($"[TrickLoginService] Login platform '{platformId}' is not allowed!");
        }
    }

    public class TrickLoginService
    {
    
    }
}